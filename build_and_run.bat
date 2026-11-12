@echo off
pushd %~dp0

set MONGO_SHELL=
where mongosh >nul 2>nul && set MONGO_SHELL=mongosh
if not defined MONGO_SHELL (
    where mongo >nul 2>nul && set MONGO_SHELL=mongo
)

if defined MONGO_SHELL (
    %MONGO_SHELL% --quiet --eval "db.runCommand({ ping: 1 })" >nul
    IF ERRORLEVEL 1 (
        echo "mongodb not running"
        popd
        exit /b 1
    ) else (
        echo "mongodb is online..."
    )
) else (
    echo "mongosh is not available, skipping the mongodb check. Make sure mongodb is running on localhost:27017."
)

echo building MineCase...
pushd src
dotnet restore
dotnet build -c Debug
IF ERRORLEVEL 1 (
    popd
    popd
    exit /b 1
)
popd

echo start MineCase.Server...
pushd src\MineCase.Server
start "MineCase.Server" dotnet run --no-build
popd

echo start MineCase.Gateway...
pushd src\MineCase.Gateway
start "MineCase.Gateway" dotnet run --no-build
popd

popd
