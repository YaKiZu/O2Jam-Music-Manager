@echo off
setlocal
cd /d "%~dp0"

echo =========================================================================
echo   O2JAM MUSIC MANAGER - BUILD NATIVE .NET 8 WPF (AUDIO M30/OMC PREVIEW)
echo =========================================================================
echo.
echo Choisissez le mode de compilation :
echo   [1] Executable Ultra-Leger (~885 Ko, depend du runtime .NET 8) [RECOMMANDE]
echo   [2] Executable 100%% Autonome / Standalone (~72 Mo, tout inclus, zero prerequis)
echo   [3] Executer les Tests Unitaires C# (M30, Encryption, Notes, Reindex)
echo.
set /p MODE="Votre choix (1, 2 ou 3, defaut 1) : "
if "%MODE%"=="" set MODE=1

if "%MODE%"=="1" (
    echo.
    echo [*] Publication de l'executable Ultra-Leger avec audio M30/NVorbis integre...
    dotnet publish O2JamManager\O2JamManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo [OK] Succes ! Executable genere dans : publish\O2JamMusicManager.exe
    ) else (
        echo [ERREUR] La compilation a echoue.
    )
) else if "%MODE%"=="2" (
    echo.
    echo [*] Publication de l'executable Standalone / Autonome...
    dotnet publish O2JamManager\O2JamManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish_standalone
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo [OK] Succes ! Executable autonome genere dans : publish_standalone\O2JamMusicManager.exe
    ) else (
        echo [ERREUR] La compilation a echoue.
    )
) else if "%MODE%"=="3" (
    echo.
    echo [*] Execution des tests unitaires...
    dotnet test O2JamManager.Tests\O2JamManager.Tests.csproj
) else (
    echo Choix non valide.
)

echo.
pause
