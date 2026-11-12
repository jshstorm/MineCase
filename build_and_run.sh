#!/bin/bash
cd "$(dirname "$0")"

if command -v mongosh > /dev/null 2>&1; then
    mongo_shell=mongosh
elif command -v mongo > /dev/null 2>&1; then
    mongo_shell=mongo
else
    mongo_shell=
fi

if [ -n "$mongo_shell" ]; then
    $mongo_shell --quiet --eval "db.runCommand({ ping: 1 })" > /dev/null
    if [ $? -ne 0 ]; then
        echo "mongodb not running"
        exit 1
    else
        echo "mongodb is online..."
    fi
else
    echo "mongosh is not available, skipping the mongodb check. Make sure mongodb is running on localhost:27017."
fi

echo building MineCase...
cd src
dotnet restore
dotnet build -c Debug || exit 1
cd -

echo start MineCase.Server...
cd src/MineCase.Server
dotnet run --no-build &
cd -

echo start MineCase.Gateway...
cd src/MineCase.Gateway
dotnet run --no-build
cd -
