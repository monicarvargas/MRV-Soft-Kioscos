@echo off
rem =====================================================================
rem  PISAR LA BASE DE TEST CON LA DE PRODUCCION
rem
rem   1. Hace una copia de PRODUCCION (la misma de todas las noches: backup_produccion.bat).
rem   2. Hace una copia de TEST como esta ahora (por si hay que volver atras).
rem   3. Borra las tablas de TEST y las carga con la copia de PRODUCCION.
rem
rem  PRODUCCION solo se LEE: no se toca nada ahi.
rem  Datos de conexion: config_backup.bat (produccion) y config_test.bat (test).
rem  Las fotos de los productos (Storage) no se copian, solo la base.
rem =====================================================================
setlocal
title Pisar base de TEST con PRODUCCION

call "%~dp0config_backup.bat"
set "PROD_HOST=%PGHOST%"
set "PROD_USER=%PGUSER%"

if not exist "%~dp0config_test.bat" (
    echo.
    echo Falta el archivo config_test.bat con los datos de la base de TEST.
    echo Abrilo con el Bloc de notas, completalo y volve a ejecutar este.
    goto :fin_error
)
call "%~dp0config_test.bat"

if /i "%TEST_PASSWORD%"=="PONER_ACA_LA_CLAVE" goto :sin_config
if /i "%TEST_USER%"=="PONER_ACA_EL_USUARIO" goto :sin_config
if /i "%TEST_HOST%"=="PONER_ACA_EL_HOST" goto :sin_config

rem ---- seguridad: TEST no puede ser el mismo proyecto que PRODUCCION ----
if /i "%TEST_USER%"=="%PROD_USER%" (
    echo.
    echo *** ATENCION: en config_test.bat el usuario es el MISMO que el de PRODUCCION.
    echo *** Eso pisaria PRODUCCION. No se hace nada. Revisa config_test.bat.
    goto :fin_error
)

if not exist "%PGBIN%\pg_restore.exe" (
    echo No se encuentra pg_restore.exe en "%PGBIN%". Revisa PGBIN en config_backup.bat
    goto :fin_error
)

if not exist "%CARPETA%" mkdir "%CARPETA%"
set "LOG=%CARPETA%\pisar_test_log.txt"
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmm"') do set "FECHA=%%i"

echo.
echo  =====================================================================
echo    Se va a BORRAR la base de TEST y cargar con lo de PRODUCCION
echo  =====================================================================
echo    PRODUCCION (solo se copia):  %PROD_USER%
echo    TEST       (se pisa):        %TEST_USER%
echo.
echo    Todo lo que hayas cargado en TEST se pierde (antes se guarda una copia).
echo.
set "RESP="
set /p "RESP=   Para seguir escribi PISAR y Enter (cualquier otra cosa cancela): "
if /i not "%RESP%"=="PISAR" (
    echo.
    echo Cancelado. No se toco nada.
    goto :fin_ok
)

echo.>> "%LOG%"
echo [%date% %time%] ===== Empieza: pisar TEST (%TEST_USER%) con PRODUCCION (%PROD_USER%) >> "%LOG%"

rem ---------------- 1. copia de PRODUCCION ----------------
echo.
echo [1/4] Copia de PRODUCCION...
call "%~dp0backup_produccion.bat" auto
if errorlevel 1 (
    echo *** No se pudo hacer la copia de PRODUCCION. No se toco TEST.
    echo [%date% %time%] ERROR en la copia de produccion >> "%LOG%"
    goto :fin_error
)

set "DUMP="
for /f "delims=" %%F in ('dir /b /o-d "%CARPETA%\mrv_produccion_*.dump" 2^>nul') do (
    set "DUMP=%CARPETA%\%%F"
    goto :hay_dump
)
:hay_dump
if not defined DUMP (
    echo *** No se encontro la copia de PRODUCCION en "%CARPETA%". No se toco TEST.
    goto :fin_error
)
echo       OK: %DUMP%

rem ---------------- desde aca todo apunta a TEST ----------------
set "PGHOST=%TEST_HOST%"
set "PGPORT=%TEST_PORT%"
set "PGDATABASE=%TEST_DATABASE%"
set "PGUSER=%TEST_USER%"
set "PGPASSWORD=%TEST_PASSWORD%"
set "PGSSLMODE=require"
set "PGOPTIONS=-c statement_timeout=0"

rem ---------------- 2. copia de TEST como esta ahora ----------------
echo.
echo [2/4] Copia de TEST como esta ahora (por si hay que volver atras)...
if not exist "%CARPETA%\test" mkdir "%CARPETA%\test"
set "DUMP_TEST=%CARPETA%\test\test_antes_de_pisar_%FECHA%.dump"
"%PGBIN%\pg_dump.exe" --format=custom --no-owner --no-privileges --schema=public --file="%DUMP_TEST%" 2>> "%LOG%"
if errorlevel 1 (
    echo *** No se pudo copiar TEST. Revisa los datos de config_test.bat. No se toco nada.
    echo [%date% %time%] ERROR copiando TEST >> "%LOG%"
    goto :fin_error
)
echo       OK: %DUMP_TEST%

rem ---------------- 3. pisar TEST ----------------
echo.
echo [3/4] Cargando PRODUCCION en TEST... (puede tardar unos minutos)
rem se saca el "schema public" de la lista: se borran y recrean las tablas, pero no el schema
rem (el schema tiene los permisos que usa Supabase)
set "LISTA=%TEMP%\mrv_lista_restore.txt"
set "LISTA2=%TEMP%\mrv_lista_restore2.txt"
"%PGBIN%\pg_restore.exe" -l "%DUMP%" > "%LISTA%" 2>> "%LOG%"
if errorlevel 1 (
    echo *** No se pudo leer la copia. No se toco TEST.
    goto :fin_error
)
findstr /v /c:"SCHEMA - public" /c:"COMMENT - SCHEMA public" "%LISTA%" > "%LISTA2%"

rem --single-transaction: si algo falla, TEST queda como estaba (no queda a medias)
"%PGBIN%\pg_restore.exe" --clean --if-exists --no-owner --no-privileges --single-transaction --exit-on-error -L "%LISTA2%" -d "%TEST_DATABASE%" "%DUMP%" 2>> "%LOG%"
if errorlevel 1 (
    echo.
    echo *** Fallo la carga. TEST quedo COMO ESTABA, no se cambio nada.
    echo *** Mira el final del archivo: %LOG%
    echo [%date% %time%] ERROR en pg_restore - TEST sin cambios >> "%LOG%"
    goto :fin_error
)
echo       OK

rem ---------------- 4. permisos de Supabase y recargar la API ----------------
echo.
echo [4/4] Permisos de Supabase...
"%PGBIN%\psql.exe" -v ON_ERROR_STOP=1 -q -c "grant usage on schema public to anon, authenticated, service_role; grant all on all tables in schema public to anon, authenticated, service_role; grant all on all sequences in schema public to anon, authenticated, service_role; grant all on all routines in schema public to anon, authenticated, service_role; notify pgrst, 'reload schema';" 2>> "%LOG%"
if errorlevel 1 (
    echo *** Los datos se copiaron, pero no se pudieron dar los permisos. Mira %LOG%
    echo [%date% %time%] AVISO: fallaron los permisos >> "%LOG%"
    goto :fin_error
)
echo       OK

del "%LISTA%" "%LISTA2%" >nul 2>&1
echo [%date% %time%] OK: TEST pisado con %DUMP% >> "%LOG%"
echo.
echo  =====================================================================
echo    LISTO: la base de TEST quedo igual a PRODUCCION.
echo    Copia de TEST anterior: %DUMP_TEST%
echo  =====================================================================
goto :fin_ok

:sin_config
echo.
echo Falta completar config_test.bat (host, usuario y clave de la base de TEST).
echo Abrilo con el Bloc de notas, completalo y volve a ejecutar este.
goto :fin_error

:fin_error
echo.
pause
exit /b 1

:fin_ok
echo.
pause
exit /b 0
