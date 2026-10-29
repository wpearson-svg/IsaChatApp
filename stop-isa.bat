@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ============================================
echo  Stopping IsaChatApp
echo ============================================

echo Unloading the Ollama model so it does not keep holding memory...
powershell -NoProfile -Command "try { $c = Get-Content appsettings.json -Raw | ConvertFrom-Json; $m = $c.Ollama.Model; $e = $c.Ollama.Endpoint; if (-not $e) { $e = 'http://localhost:11434' }; Invoke-RestMethod -Method Post -Uri ($e.TrimEnd('/') + '/api/generate') -ContentType 'application/json' -Body (@{ model = $m; keep_alive = 0 } | ConvertTo-Json) -TimeoutSec 5 | Out-Null; Write-Host ('Unloaded ' + $m) } catch { Write-Host 'Could not reach Ollama (skipped).' }"

if exist isa.pid (
    set /p ISA_PID=<isa.pid
    echo Found isa.pid -- stopping PID !ISA_PID! and its child processes...
    taskkill /PID !ISA_PID! /T /F
    del isa.pid
) else (
    echo No isa.pid file found.
)

echo.
echo Checking port 5067 for anything still listening (Tailscale forwarding is left alone)...
set FOUND=0
for /f "tokens=5" %%P in ('netstat -ano ^| findstr ":5067" ^| findstr "LISTENING"') do (
    tasklist /FI "PID eq %%P" /NH | findstr /I "tailscaled" >nul
    if errorlevel 1 (
    set FOUND=1
    echo Port 5067 is held by PID %%P:
    tasklist /FI "PID eq %%P" /NH
    echo Killing PID %%P...
    taskkill /PID %%P /T /F
    if errorlevel 1 (
        echo taskkill failed; trying PowerShell Stop-Process...
        powershell -NoProfile -Command "Stop-Process -Id %%P -Force -ErrorAction Continue"
    )
    )
)
if "!FOUND!"=="0" echo Nothing was listening on port 5067.

echo.
echo Waiting 3 seconds, then checking again...
timeout /t 3 /nobreak >nul
set STILL=0
for /f "tokens=5" %%P in ('netstat -ano ^| findstr ":5067" ^| findstr "LISTENING"') do (
    tasklist /FI "PID eq %%P" /NH | findstr /I "tailscaled" >nul
    if errorlevel 1 (
    set STILL=1
    echo STILL RUNNING: PID %%P is listening on port 5067 again.
    echo If the PID is a new number each time, something is restarting it
    echo  (dotnet watch, VS Code debugger, or another terminal running the app^).
    echo If the same PID survives, right-click this file and Run as administrator.
    )
)
if "!STILL!"=="0" echo Port 5067 is free.

echo.
echo Done.
pause
