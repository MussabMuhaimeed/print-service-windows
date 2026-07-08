# Print Service Windows

Native Windows tray application for **silent printing** (C# / Windows Forms). No Node.js, no npm, no separate runtime when published as self-contained.

Your web app (Vue, React, etc.) keeps using the same HTTP API on `http://127.0.0.1:4510`.

## What you get

- **System tray icon** near the clock
- **Status window** — running/stopped, port, URL
- **Logs viewer** — live log tail + open log file
- **Settings** — default printer, paper, port, CORS origins
- **Start / Stop** from tray menu
- **Same REST API** as the Node.js version (`/health`, `/printers`, `/print`)

## Requirements (development only)

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (only to build; end users do not need it)

End users install a single `.exe` (or installer) — no Node.js.

## Build

```powershell
cd print-service-windows
dotnet restore
dotnet build -c Release
```

## Publish single-file installer-ready exe

This bundles .NET so users do not install anything else:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

Output: `publish\PrintService.exe` + `settings.json`

Copy both to the install folder. Double-click `PrintService.exe` — it appears in the system tray.

## Auto-start on Windows login

After publishing, copy `scripts\install-startup.bat` next to `PrintService.exe` and run it once.

Remove with `scripts\uninstall-startup.bat`.

## Tray menu

| Item | Action |
|------|--------|
| Status | Port, URL, running state |
| View Logs | In-app log viewer |
| Settings | Edit `settings.json` values |
| Start / Stop | Control HTTP service |
| Exit | Quit application |

## API (unchanged)

Same as the Node.js `print-service` project:

- `GET /health`
- `GET /printers`
- `POST /print` — `contentType`: `html`, `pdf`, `image`

Your Vue/React code does not need changes.

## How printing works

| Content | Pipeline |
|---------|----------|
| HTML | WebView2 (Edge) → PDF → silent print |
| PDF | PdfiumViewer → silent print |
| PNG/JPG | GDI+ → silent print |

## Log file location

```
%LOCALAPPDATA%\PrintService\print-service.log
```

## Optional: create MSI installer

Use [Inno Setup](https://jrsoftware.org/isinfo.php) or WiX to wrap `publish\PrintService.exe` into a setup wizard for your users.

## Comparison vs Node.js version

| | Node.js | C# WinForms |
|---|---------|-------------|
| User installs Node.js | Yes | **No** |
| System tray UI | No | **Yes** |
| Logs viewer | File only | **Built-in** |
| Status / port | Manual | **Built-in** |
| API compatible | — | **Yes** |
| HTML printing | Puppeteer | WebView2 (built into Windows) |
