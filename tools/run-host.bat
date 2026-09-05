@echo off

cmake -S . -B build -G Ninja

if errorlevel 1 (
    echo.
    echo CMake configuration failed.
    exit /b 1
)

cmake --build build

if errorlevel 1 (
    echo.
    echo UTFuel build failed.
    exit /b 1
)

echo.
echo ===============================
echo        UTFuel HOST
echo ===============================
echo.

build\utfuel_host.exe