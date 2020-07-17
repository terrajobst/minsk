@echo off

REM Vars
set "RepositoryDir=%~dp0"
set "SolutionDir=%RepositoryDir%src\"
set "MinskToolsPath=%RepositoryDir%artifacts\Minsk\"

REM Restore + Build + Publish
dotnet publish "%SolutionDir%msc" --nologo || exit /b

REM Run
dotnet "%MinskToolsPath%msc.dll" %*
