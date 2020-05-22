#!/bin/bash

# Vars
slndir="$(dirname "${BASH_SOURCE[0]}")/src"

# Restore + Build
dotnet build "$slndir/msi" --nologo || exit

# Run
dotnet run -p "$slndir/msi" --no-build
