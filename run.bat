@echo off
echo Cleaning previous build...
dotnet clean --nologo
echo.
echo Building fresh...
dotnet build --no-incremental
if %errorlevel% neq 0 (
    echo Build failed.    
    exit /b %errorlevel%
)
echo.
echo Running application...
start "" dotnet run --no-build
