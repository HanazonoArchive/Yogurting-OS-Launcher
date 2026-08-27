@echo off
title Yogurting Online - Modern Server Launcher (.NET 8)
color 0a
echo ========================================================
echo   Yogurting Online - Modern Server Launcher (.NET 8)
echo ========================================================
echo.

echo [*] Cleaning up existing server and sniffer processes...
taskkill /F /IM quartet.exe 2>nul
taskkill /F /IM Yogurting.Server.exe 2>nul
taskkill /F /FI "WINDOWTITLE eq Yogurting Packet Sniffer*" 2>nul
timeout /t 1 /nobreak >nul

echo [*] Launching Modern Server in Direct Standalone Mode (Ports 10000-10004)...
echo.
cd /d "%~dp0"
dotnet run --project src/Yogurting.Server/Yogurting.Server.csproj
pause
