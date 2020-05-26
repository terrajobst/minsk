#!/bin/bash

# Vars
slndir="$(dirname "${BASH_SOURCE[0]}")/src"

# Restore + Build
dotnet build "$slndir/msc" --nologo || exit

# Run
dotnet run -p "$slndir/msc" --no-build -- "$@"
