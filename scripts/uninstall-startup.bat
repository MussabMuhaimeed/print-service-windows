@echo off
setlocal

set "APP_NAME=PrintService"
set "REG_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Run"

reg delete "%REG_KEY%" /v "%APP_NAME%" /f 2>nul
echo Removed startup entry for Print Service (if it existed).

endlocal
