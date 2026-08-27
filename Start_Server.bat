@echo off
title Yogurting Modern English Server (.NET 8)
cd /d "%~dp0"
dotnet run --project src/Yogurting.Server/Yogurting.Server.csproj
pause
