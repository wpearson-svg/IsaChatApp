@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ============================================
echo  Stopping IsaChatApp (debug - shows errors)
echo ============================================

if exist isa.pid (
    set /p ISA_PID=<isa.pid
    echo Found isa.pid -- stopping PID !ISA_PID! ...
    taskkill /PID !ISA_PID! /T /F
    echo taskkill exit code: !errorlevel!
    del isa.pid
) else (
    echo No isa.pid file found.
)

echo.
echo Checking what is on port 5067 right now:
netstat -ano | findstr ":5067"

echo.
echo Checking all dotnet.exe processes right now:
tasklist | findstr /I "dotnet"

echo.
echo Done.
pause
