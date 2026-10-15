@echo off
setlocal
cd /d "%~dp0"
echo ============================================
echo  IsaChatApp -- live problem watcher
echo  Shows warnings, errors and crashes from the running app as they happen.
echo  Leave this window open next to the app. Ctrl+C to close.
echo  Add ALL after the command name (watch-isa.bat ALL) to see every log line.
echo ============================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0watch-isa.ps1" %1
pause
