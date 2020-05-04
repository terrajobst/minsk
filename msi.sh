#!/bin/bash

# Vars
RepositoryDir="$(dirname "${BASH_SOURCE[0]}")/"
SolutionDir="${RepositoryDir}src/"
MinskToolsPath="${RepositoryDir}artifacts/Minsk/"

# Restore + Build + Publish
dotnet publish "${SolutionDir}msi" --nologo || exit

# Run
dotnet "${MinskToolsPath}msi.dll"
