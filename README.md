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

## Download (for end users)

1. Open the [Releases](https://github.com/YOUR_ORG/print-service-windows/releases) page on GitHub.
2. Download `PrintService-Windows-win-x64.zip` from the latest release.
3. Unzip to any folder (for example `C:\PrintService`).
4. Double-click `PrintService.exe` — the app appears in the system tray.

The zip includes `settings.json` and optional `scripts\` batch files for auto-start on login.

**Note:** HTML printing uses WebView2 (Microsoft Edge). Windows 11 includes it; on Windows 10, install the [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) if prompted.

## Create a new release (maintainers)

Push a version tag — GitHub Actions builds and publishes the zip automatically:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

You can also run the **Release** workflow manually from the GitHub Actions tab.

## Build locally

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
