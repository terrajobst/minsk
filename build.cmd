@echo off

dotnet build .\src\minsk.sln /nologo
dotnet test .\src\Minsk.Tests\Minsk.Tests.csproj