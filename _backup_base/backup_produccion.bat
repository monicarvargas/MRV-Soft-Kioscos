@echo off
rem =====================================================================
rem  COPIA COMPLETA DE LA BASE DE PRODUCCION (todos los negocios)
rem  La corre sola la tarea programada todas las noches. Tambien se puede correr con doble clic.
rem  Guarda un archivo .dump por dia y borra los de mas de %DIAS% dias.
rem  Los datos de conexion estan en config_backup.bat
rem =====================================================================
setlocal
call "%~dp0config_backup.bat"

if not exist "%PGBIN%\pg_dump.exe" (
    echo No se encuentra pg_dump.exe en "%PGBIN%". Revisa PGBIN en config_backup.bat
    if /i not "%~1"=="auto" pause
    exit /b 1
)
if not exist "%CARPETA%" mkdir "%CARPETA%"

for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmm"') do set "FECHA=%%i"
set "ARCHIVO=%CARPETA%\mrv_produccion_%FECHA%.dump"
set "LOG=%CARPETA%\backup_log.txt"

echo.>> "%LOG%"
echo [%date% %time%] Empieza la copia >> "%LOG%"
echo Haciendo la copia de PRODUCCION... (puede tardar unos minutos)

"%PGBIN%\pg_dump.exe" --format=custom --no-owner --no-privileges --schema=public --file="%ARCHIVO%" 2>> "%LOG%"
if errorlevel 1 goto :error

for %%A in ("%ARCHIVO%") do set "TAM=%%~zA"
if "%TAM%"=="" goto :error
if %TAM% LSS 1000 goto :error

echo [%date% %time%] OK: %ARCHIVO% (%TAM% bytes) >> "%LOG%"
echo Copia OK: %ARCHIVO%

rem se borran las copias de mas de %DIAS% dias
forfiles /p "%CARPETA%" /m mrv_produccion_*.dump /d -%DIAS% /c "cmd /c del @path" >nul 2>&1

if /i not "%~1"=="auto" pause
exit /b 0

:error
echo [%date% %time%] ERROR: no se pudo hacer la copia (mira las lineas de arriba) >> "%LOG%"
if exist "%ARCHIVO%" del "%ARCHIVO%"
echo.
echo *** NO SE PUDO HACER LA COPIA. Mira el archivo: %LOG%
if /i not "%~1"=="auto" pause
exit /b 1
