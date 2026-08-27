@echo off
cd /d "%~dp0"

echo =======================================================
echo          ClipOne - Build and Publish Script
echo =======================================================
echo.

echo [1/3] Closing running ClipOne instances...
taskkill /F /IM ClipOne.exe >nul 2>nul

echo [2/3] Publishing Native AOT (Release win-x64)...
dotnet publish ClipOne.csproj -c Release -r win-x64 -o "bin\Release\Publish"
if errorlevel 1 goto :FAILED

echo.
echo [3/3] Publish succeeded!
echo -------------------------------------------------------
echo Output directory: %~dp0bin\Release\Publish\
echo.

start "" "%~dp0bin\Release\Publish\"
goto :END

:FAILED
echo.
echo -------------------------------------------------------
echo [ERROR] Build failed! Please check the output above.
echo -------------------------------------------------------

:END
pause
