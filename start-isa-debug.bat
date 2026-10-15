@echo off
setlocal
cd /d "%~dp0"
echo ============================================
echo  IsaChatApp -- DEBUG start (visible window)
echo  Leave this window open while you use the app.
echo  Press Ctrl+C to stop it. Errors show right here.
echo ============================================
echo.
echo .NET SDKs installed:
dotnet --list-sdks
echo.
echo Anything already using port 5067:
netstat -ano | findstr ":5067" | findstr "LISTENING"
echo (if a line printed above, run stop-isa.bat first)
echo.
echo Building and starting...
dotnet run -c Release --urls http://localhost:5067
echo.
echo The app has stopped. Copy any red error text above and send it to Willis.
pause
