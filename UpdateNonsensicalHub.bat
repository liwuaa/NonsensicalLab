@echo off
setlocal enabledelayedexpansion

set "PACKAGES_DIR=%~dp0Packages"
set /a PULLED=0
set /a FAILED=0

if not exist "%PACKAGES_DIR%\" (
    echo Packages folder not found:
    echo %PACKAGES_DIR%
    pause
    exit /b 1
)

echo Pulling git repositories under:
echo %PACKAGES_DIR%

for /d %%D in ("%PACKAGES_DIR%\*") do (
    if exist "%%D\.git" (
        echo.
        echo [%%~nxD]
        pushd "%%D" >nul

        git pull --ff-only origin main
        if errorlevel 1 (
            echo Failed: %%~nxD
            set /a FAILED+=1
        ) else (
            set /a PULLED+=1
        )

        popd >nul
    )
)

echo.
echo Done. Pulled: %PULLED%, Failed: %FAILED%
pause

exit /b %FAILED%
