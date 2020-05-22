@echo off

REM Vars
set "SLNDIR=%~dp0src"

REM Restore + Build
dotnet build "%SLNDIR%\msi" --nologo || exit /b

REM Run
dotnet run -p "%SLNDIR%\msi" --no-build
