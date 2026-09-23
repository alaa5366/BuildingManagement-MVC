@echo off
echo ============================================
echo   Building Management - Deployment Script
echo ============================================
echo.

cd /d D:\projects\BuildingManagement-MVC

echo [1/3] Publishing...
dotnet publish -c Release
if errorlevel 1 (
    echo FAILED at publish stage!
    pause
    exit /b 1
)

echo.
echo [2/3] Deploying to Google Cloud...
gcloud app deploy --quiet
if errorlevel 1 (
    echo FAILED at deploy stage!
    pause
    exit /b 1
)

echo.
echo [3/3] Opening the site...
gcloud app browse

echo.
echo ============================================
echo   Deployment Complete! 🎉
echo ============================================
pause