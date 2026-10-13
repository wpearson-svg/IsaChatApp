@echo off
setlocal
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-prereqs.ps1"
if errorlevel 1 (
    echo.
    echo Startup stopped. Fix the items above, then run start-isa.bat again.
    pause
    exit /b 1
)

echo ============================================
echo  Starting IsaChatApp in the background...
echo  (this runs "dotnet run" under the hood, same
echo   as running it by hand -- just hidden, with
echo   no console window staying open)
echo ============================================

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0isa-run.ps1"
if errorlevel 1 (
    echo.
    echo Failed to start IsaChatApp. Check isa-run.err.log for details.
    pause
    exit /b 1
)

echo.
echo Building and starting -- this can take a little while the first time.
echo Once it's up:  http://localhost:5067
echo Logs:  isa-run.log / isa-run.err.log
echo PID file: isa.pid  (used by stop-isa.bat)
echo.
pause
