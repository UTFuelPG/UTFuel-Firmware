@echo off

echo.
echo ===============================
echo     UTFuel HOST Build
echo ===============================
echo.

cmake -S . -B build -G Ninja

if errorlevel 1 (
    echo.
    echo [ERROR] CMake configuration failed.
    exit /b 1
)

cmake --build build

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed.
    exit /b 1
)

echo.
echo [OK] UTFuel build completed.
echo.