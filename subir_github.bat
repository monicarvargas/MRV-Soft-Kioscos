@echo off
rem =====================================================================
rem  SUBIR LOS CAMBIOS A GITHUB (copia del codigo en la nube)
rem  1) Muestra que archivos cambiaron
rem  2) Controla que NO se suba nada con claves (appsettings, config_backup, datos_supabase...)
rem  3) Te pide una descripcion corta y guarda los cambios (commit)
rem  4) Los sube a GitHub (push)
rem  5) VERIFICA que GitHub quedo igual a tu compu y te lo dice
rem
rem  Necesita Git. Si no esta instalado aparte, usa el que trae Visual Studio.
rem =====================================================================
setlocal EnableExtensions
cd /d "%~dp0"

rem ---- buscar git ----
rem Primero el Git que trae Visual Studio (el mismo que usa el boton de Git del Visual),
rem si no, el Git instalado aparte (C:\Program Files\Git).
set "GIT="
for %%V in (18 2022) do for %%E in (Enterprise Professional Community) do (
    if not defined GIT if exist "C:\Program Files\Microsoft Visual Studio\%%V\%%E\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe" set "GIT=C:\Program Files\Microsoft Visual Studio\%%V\%%E\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe"
)
if not defined GIT (
    where git >nul 2>&1
    if not errorlevel 1 set "GIT=git"
)
if not defined GIT (
    echo.
    echo *** No encontre Git. Instalalo desde https://git-scm.com y volve a correr este archivo.
    pause
    exit /b 1
)

echo Usando Git: %GIT%
for /f "delims=" %%B in ('""%GIT%" rev-parse --abbrev-ref HEAD"') do set "RAMA=%%B"
echo.
echo === Rama: %RAMA% ===
echo.
echo === 1) Archivos cambiados ===
"%GIT%" status --short
echo.

rem ---- agregar todo lo que cambio (lo que esta en .gitignore no se agrega) ----
"%GIT%" add -A

rem ---- 2) control de seguridad: nada con claves ----
"%GIT%" diff --cached --name-only | findstr /i /r "appsettings\.json appsettings\.produccion config_backup\.bat config_test\.bat datos_supabase" >nul
if not errorlevel 1 (
    echo.
    echo *** ATENCION: entre los cambios hay un archivo con CLAVES:
    "%GIT%" diff --cached --name-only | findstr /i /r "appsettings\.json appsettings\.produccion config_backup\.bat config_test\.bat datos_supabase"
    echo *** No se subio nada. Revisa el .gitignore o avisale a Claude.
    "%GIT%" reset -q
    pause
    exit /b 1
)

rem ---- 3) commit (si hay algo nuevo) ----
"%GIT%" diff --cached --quiet
if errorlevel 1 (
    set "MSG="
    set /p "MSG=Que cambiaste? (Enter = Cambios del dia): "
    call :commit
    if errorlevel 1 (
        echo.
        echo *** No se pudo guardar el commit. Mira el mensaje de arriba.
        pause
        exit /b 1
    )
) else (
    echo No hay archivos nuevos para guardar. Igual reviso si quedo algo sin subir...
)

rem ---- 4) subir ----
echo.
echo === Subiendo a GitHub... ===
"%GIT%" push origin %RAMA%
if errorlevel 1 (
    echo.
    echo GitHub tiene cambios que esta compu no tiene. Los traigo y vuelvo a probar...
    "%GIT%" pull --rebase origin %RAMA%
    if errorlevel 1 (
        "%GIT%" rebase --abort >nul 2>&1
        echo.
        echo *** No se pudo juntar con lo que hay en GitHub ^(cambios en los mismos archivos^).
        echo *** No se perdio nada en tu compu. Avisale a Claude con el mensaje de arriba.
        pause
        exit /b 1
    )
    "%GIT%" push origin %RAMA%
    if errorlevel 1 (
        echo.
        echo *** No se pudo subir. Mira el mensaje de arriba ^(internet, usuario de GitHub...^).
        echo *** Si aparecio una ventana de error de git-remote-https o libidn2: el Git de esta compu esta roto,
        echo *** hay que reinstalarlo desde https://git-scm.com ^(tus cambios ya quedaron guardados, se suben despues^).
        pause
        exit /b 1
    )
)

rem ---- 5) verificar que GitHub quedo igual ----
"%GIT%" fetch -q origin
for /f "delims=" %%L in ('""%GIT%" rev-parse HEAD"') do set "LOCAL=%%L"
for /f "delims=" %%R in ('""%GIT%" rev-parse origin/%RAMA%"') do set "REMOTO=%%R"
echo.
if "%LOCAL%"=="%REMOTO%" (
    echo ==========================================================
    echo   VERIFICADO: GitHub tiene exactamente lo mismo que tu compu.
    echo ==========================================================
    "%GIT%" log -1 --format="  Ultimo cambio subido: %%s  (%%cd)" --date=format:"%%d/%%m/%%Y %%H:%%M"
) else (
    echo *** OJO: GitHub NO quedo igual que tu compu. Volve a correr este archivo.
)
echo.
pause
exit /b 0

:commit
if "%MSG%"=="" set "MSG=Cambios del %date%"
"%GIT%" commit -q -m "%MSG%"
exit /b %errorlevel%
