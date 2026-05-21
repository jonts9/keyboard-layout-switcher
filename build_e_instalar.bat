@echo off
setlocal

echo ============================================
echo  KeyboardLayoutSwitcher - Build e Instalacao
echo ============================================
echo.

:: Verifica se .NET SDK esta instalado
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERRO] .NET SDK nao encontrado.
    echo.
    echo Instale em: https://dotnet.microsoft.com/download
    echo Recomendado: .NET 8 SDK  ^(winget install Microsoft.DotNet.SDK.8^)
    echo.
    pause
    exit /b 1
)

echo [1/3] Compilando...
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%~dp0dist"

if errorlevel 1 (
    echo.
    echo [ERRO] Falha na compilacao. Verifique os erros acima.
    pause
    exit /b 1
)

echo.
echo [2/3] Copiando executavel...
set DEST=%LOCALAPPDATA%\KeyboardLayoutSwitcher
if not exist "%DEST%" mkdir "%DEST%"
copy /Y "%~dp0dist\KeyboardLayoutSwitcher.exe" "%DEST%\KeyboardLayoutSwitcher.exe" >nul

echo.
echo [3/3] Pronto!
echo.
echo  Executavel instalado em:
echo  %DEST%\KeyboardLayoutSwitcher.exe
echo.
echo  Para iniciar agora:
echo  %DEST%\KeyboardLayoutSwitcher.exe
echo.
echo  Para iniciar com o Windows automaticamente:
echo  Abra o programa e clique com botao direito no icone da bandeja
echo  selecione "Iniciar com o Windows"
echo.

set /p LAUNCH="Deseja iniciar o programa agora? (S/N): "
if /i "%LAUNCH%"=="S" (
    start "" "%DEST%\KeyboardLayoutSwitcher.exe"
)

endlocal
pause
