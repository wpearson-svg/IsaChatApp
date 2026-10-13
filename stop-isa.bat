@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ============================================
echo  Stopping IsaChatApp
echo ============================================

if exist isa.pid (
    set /p ISA_PID=<isa.pid
    echo Found isa.pid -- stopping PID !ISA_PID! (and any child processes)...
    taskkill /PID !ISA_PID! /T /F >nul 2>&1
    del isa.pid
) else (
    echo No isa.pid file found.
)

echo Checking port 5067 for anything still listening...
set FOUND=0
for /f "tokens=5" %%P in ('netstat -ano ^| findstr ":5067" ^| findstr "LISTENING"') do (
    echo Killing PID %%P on port 5067...
    taskkill /PID %%P /T /F >nul 2>&1
    set FOUND=1
)
if "!FOUND!"=="0" (
    echo Nothing left listening on port 5067.
)

echo.
echo Done.
pause
