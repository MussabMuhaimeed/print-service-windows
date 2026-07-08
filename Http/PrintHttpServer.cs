using System.Net;
using System.Text;
using System.Text.Json;
using PrintService.Windows.Config;
using PrintService.Windows.Logging;
using PrintService.Windows.Models;
using PrintService.Windows.Services;

namespace PrintService.Windows.Http;

public sealed class PrintHttpServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SettingsRepository _settings;
    private readonly PrinterService _printerService;
    private readonly FileLogger _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool IsRunning { get; private set; }

    public int Port => _settings.Get().Port;

    public event Action<bool>? RunningStateChanged;

    public PrintHttpServer(SettingsRepository settings, PrinterService printerService, FileLogger logger)
    {
        _settings = settings;
        _printerService = printerService;
        _logger = logger;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        var port = _settings.Get().Port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add($"http://localhost:{port}/");

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            _logger.Error($"Failed to start HTTP server on port {port}", ex);
            throw;
        }

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        IsRunning = true;
        RunningStateChanged?.Invoke(true);
        _logger.Info($"print-service listening on http://127.0.0.1:{port}");
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        IsRunning = false;
        RunningStateChanged?.Invoke(false);
        _logger.Info("print-service stopped");
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is { IsListening: true })
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error("HTTP listener error", ex);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            AddCorsHeaders(request, response);

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            if (!IsLocalhost(request))
            {
                await WriteJsonAsync(response, 403, new ApiResponse { Success = false, Message = "Forbidden: localhost only" });
                return;
            }

            var path = request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            switch (path)
            {
                case "/health":
                    await WriteJsonAsync(response, 200, new ApiResponse { Success = true, Message = "print-service is running" });
                    break;

                case "/printers":
                    if (request.HttpMethod != "GET")
                    {
                        await WriteJsonAsync(response, 405, new ApiResponse { Success = false, Message = "Method not allowed" });
                        break;
                    }

                    var printers = _printerService.GetPrinters();
                    await WriteRawJsonAsync(response, 200, JsonSerializer.Serialize(printers, JsonOptions));
                    break;

                case "/print":
                    if (request.HttpMethod != "POST")
                    {
                        await WriteJsonAsync(response, 405, new ApiResponse { Success = false, Message = "Method not allowed" });
                        break;
                    }

                    await HandlePrintAsync(request, response);
                    break;

                default:
                    await WriteJsonAsync(response, 404, new ApiResponse { Success = false, Message = "Endpoint not found" });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Request handling failed", ex);
            try
            {
                await WriteJsonAsync(response, 500, new ApiResponse { Success = false, Message = ex.Message });
            }
            catch
            {
                // Response may already be closed.
            }
        }
    }

    private async Task HandlePrintAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        PrintRequest? printRequest;
        try
        {
            printRequest = JsonSerializer.Deserialize<PrintRequest>(body, JsonOptions);
        }
        catch
        {
            await WriteJsonAsync(response, 400, new ApiResponse { Success = false, Message = "Invalid JSON body" });
            return;
        }

        var validation = ValidatePrintRequest(printRequest);
        if (!validation.IsValid)
        {
            await WriteJsonAsync(response, 400, new ApiResponse { Success = false, Message = validation.Error! });
            return;
        }

        var start = DateTime.UtcNow;
        try
        {
            await _printerService.ExecutePrintJobAsync(printRequest!);
            var durationMs = (long)(DateTime.UtcNow - start).TotalMilliseconds;

            _logger.LogPrintJob(
                printRequest!.Printer ?? "(default)",
                printRequest.Copies,
                printRequest.ContentType,
                durationMs,
                success: true);

            await WriteJsonAsync(response, 200, new ApiResponse
            {
                Success = true,
                Message = "Print job sent successfully",
                DurationMs = durationMs,
            });
        }
        catch (Exception ex)
        {
            var durationMs = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            var message = ex.Message;

            _logger.LogPrintJob(
                printRequest!.Printer ?? "(default)",
                printRequest.Copies,
                printRequest.ContentType,
                durationMs,
                success: false,
                error: message);

            var status = message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? 404 : 500;
            await WriteJsonAsync(response, status, new ApiResponse { Success = false, Message = message });
        }
    }

    private static (bool IsValid, string? Error) ValidatePrintRequest(PrintRequest? request)
    {
        if (request is null)
        {
            return (false, "Request body must be a JSON object");
        }

        var validTypes = new[] { "html", "pdf", "image" };
        if (!validTypes.Contains(request.ContentType?.ToLowerInvariant()))
        {
            return (false, $"contentType must be one of: {string.Join(", ", validTypes)}");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return (false, "content is required and must be a non-empty string");
        }

        if (request.Copies < 1)
        {
            return (false, "copies must be a positive number");
        }

        return (true, null);
    }

    private void AddCorsHeaders(HttpListenerRequest request, HttpListenerResponse response)
    {
        var origin = request.Headers["Origin"];
        var allowed = _settings.Get().AllowedOrigins;

        if (!string.IsNullOrEmpty(origin) && allowed.Any(o => origin.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
        {
            response.Headers["Access-Control-Allow-Origin"] = origin;
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }
    }

    private static bool IsLocalhost(HttpListenerRequest request)
    {
        var remote = request.RemoteEndPoint?.Address;
        if (remote is null)
        {
            return true;
        }

        return IPAddress.IsLoopback(remote);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, ApiResponse payload)
    {
        await WriteRawJsonAsync(response, statusCode, JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static async Task WriteRawJsonAsync(HttpListenerResponse response, int statusCode, string json)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
