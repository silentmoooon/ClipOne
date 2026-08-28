@echo off
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\run" /v ClipOne /f 2>nul
reg delete "HKCU\Software\ClipOne" /f 2>nul
if exist "%LOCALAPPDATA%\ClipOne" rmdir /s /q "%LOCALAPPDATA%\ClipOne" 2>nul
echo ClipOne registry and local cache cleared.