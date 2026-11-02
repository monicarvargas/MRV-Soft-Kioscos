@echo off
rem =====================================================================
rem  INSTALAR MRV SOFT EN ESTA PC (va adentro del ZIP, al lado de BlazorKioscoDesktop.exe)
rem  - Copia el programa a  %LOCALAPPDATA%\MRVSoft   (no pide permisos de administrador)
rem  - Crea el acceso directo "MRV Soft" en el escritorio
rem  Para ACTUALIZAR: descomprimir el ZIP nuevo y volver a hacer doble clic aca
rem  (los datos para vender sin internet no se pierden: estan en otra carpeta).
rem =====================================================================
setlocal
set "DESTINO=%LOCALAPPDATA%\MRVSoft"

echo.
echo Cerrando MRV Soft si esta abierto...
taskkill /im BlazorKioscoDesktop.exe /f >nul 2>&1

echo Copiando el programa a %DESTINO% ...
if not exist "%DESTINO%" mkdir "%DESTINO%"
robocopy "%~dp0." "%DESTINO%" /e /njh /njs /ndl /nfl /nc /ns /np >nul
if %errorlevel% geq 8 (
    echo *** No se pudo copiar el programa.
    pause
    exit /b 1
)

echo Creando el acceso directo en el escritorio...
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Desktop')+'\MRV Soft.lnk'); $s.TargetPath='%DESTINO%\BlazorKioscoDesktop.exe'; $s.WorkingDirectory='%DESTINO%'; $s.IconLocation='%DESTINO%\BlazorKioscoDesktop.exe,0'; $s.Save()"

echo.
echo Listo: en el escritorio esta el acceso directo "MRV Soft".
echo Si al abrir dice que falta "WebView2", instalalo desde: https://go.microsoft.com/fwlink/p/?LinkId=2124703
pause
