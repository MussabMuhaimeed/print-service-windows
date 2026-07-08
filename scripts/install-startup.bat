@echo off
setlocal

set "APP_NAME=PrintService"
set "REG_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Run"

for %%I in ("%~dp0PrintService.exe") do set "EXE_PATH=%%~fI"

if not exist "%EXE_PATH%" (
  echo PrintService.exe not found next to this script.
  echo Build the app first, then copy install-startup.bat beside PrintService.exe
  exit /b 1
)

reg add "%REG_KEY%" /v "%APP_NAME%" /t REG_SZ /d "\"%EXE_PATH%\"" /f
echo Print Service Windows will start automatically when you log in.
echo Entry: %REG_KEY%\%APP_NAME%

endlocal
