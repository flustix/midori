#!/bin/sh

if [ -z "$1" ]; then
    echo "Error: You forgot to provide the first argument!"
    echo "Usage: $0 <version yyyy.mdd.b>"
    exit 1
fi

dotnet pack Midori -c Release /p:Version=$1

if [ -z "$$NUGET_KEY" ]; then
    echo "Error: Missing NUGET_KEY envvar!"
    exit 1
fi

dotnet nuget push Midori/bin/Release/flustix.Midori.$1.nupkg --source "nuget.org" --api-key $NUGET_KEY