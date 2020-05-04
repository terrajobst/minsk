#!/bin/bash

# Vars
RepositoryDir="$(dirname "${BASH_SOURCE[0]}")/"
SolutionDir="${RepositoryDir}src/"

# Restore + Build
dotnet build "${SolutionDir}minsk.sln" --nologo || exit

# Test
dotnet test "${SolutionDir}Minsk.Tests" --nologo --no-build || exit

# Publish
dotnet publish "${SolutionDir}minsk.sln" --nologo --no-build
