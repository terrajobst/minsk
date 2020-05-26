@echo off

REM Vars
set "SLNDIR=%~dp0src"

REM Restore + Build
dotnet build "%SLNDIR%\minsk.sln" --nologo || exit /b

REM Test
dotnet test "%SLNDIR%\Minsk.Tests" --nologo --no-build
