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
echo Building and starting -- the first time can take a minute or two
echo (it downloads packages and compiles). Waiting for the site to answer...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0wait-isa.ps1"
set WAITCODE=%errorlevel%
echo.
if "%WAITCODE%"=="0" (
    echo IsaChatApp is up:  http://localhost:5067
) else (
    echo IsaChatApp did NOT come up. The last lines of the logs are shown above.
    echo Send those lines to Willis. Full logs: isa-run.log / isa-run.err.log
)
echo PID file: isa.pid  (used by stop-isa.bat)
echo.
pause
