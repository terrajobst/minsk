@echo off

dotnet build .\src\minsk.sln
dotnet test .\src\Minsk.Tests\Minsk.Tests.csproj