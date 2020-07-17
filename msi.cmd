@echo off

REM Vars
set "RepositoryDir=%~dp0"
set "SolutionDir=%RepositoryDir%src\"
set "MinskToolsPath=%RepositoryDir%artifacts\Minsk\"

REM Restore + Build + Publish
dotnet publish "%SolutionDir%msi" --nologo || exit /b

REM Run
dotnet "%MinskToolsPath%msi.dll"
