@echo off

REM Vars
set "RepositoryDir=%~dp0"
set "SolutionDir=%RepositoryDir%src\"

REM Restore + Build
dotnet build "%SolutionDir%minsk.sln" --nologo || exit /b

REM Test
dotnet test "%SolutionDir%Minsk.Tests" --nologo --no-build || exit /b

REM Publish
dotnet publish "%SolutionDir%minsk.sln" --nologo --no-build
