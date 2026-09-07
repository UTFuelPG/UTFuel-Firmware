@echo off

echo.
echo ==================================
echo       UTFuel HOST Test Suite
echo ==================================
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
echo Running tests...
echo.

ctest --test-dir build --output-on-failure

if errorlevel 1 (
    echo.
    echo [ERROR] UTFuel tests failed.
    exit /b 1
)

echo.
echo ==================================
echo   ALL UTFuel TESTS PASSED
echo ==================================
echo.