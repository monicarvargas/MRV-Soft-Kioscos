@echo off
rem =====================================================================
rem  PUBLICAR LA APP DEL CELULAR (BlazorKiosco_v1) EN VERCEL
rem  1) Pone los datos de PRODUCCION (appsettings.produccion.json) solo para publicar
rem  2) Compila en modo Release
rem  3) Vuelve a dejar tu appsettings.json como estaba (TEST), aunque falle la compilacion
rem  4) Deja todo en _publicar\celular\wwwroot (con vercel.json) y lo sube a Vercel (produccion)
rem
rem  Necesita (una sola vez):
rem   - Node.js y la herramienta de Vercel ->  npm install -g vercel
rem   - BlazorKiosco_v1\wwwroot\appsettings.produccion.json con los datos de PRODUCCION
rem  Tu appsettings.json queda SIEMPRE en TEST para programar.
rem =====================================================================
setlocal
cd /d "%~dp0"

set "CFG=BlazorKiosco_v1\wwwroot\appsettings.json"
set "CFG_PROD=BlazorKiosco_v1\wwwroot\appsettings.produccion.json"
set "CFG_GUARDADO=_publicar\appsettings_test_guardado_celular.json"
set "SALIDA=_publicar\celular\wwwroot"
rem codigo del proyecto de PRODUCCION en Supabase (esta en la direccion: https://CODIGO.supabase.co)
set "REF_PROD=snhfotjgraipantkypda"

if not exist "_publicar" mkdir "_publicar"

rem si una publicacion anterior se corto a la mitad, primero se vuelve a poner tu appsettings.json de TEST
if exist "%CFG_GUARDADO%" (
    copy /y "%CFG_GUARDADO%" "%CFG%" >nul
    del "%CFG_GUARDADO%"
    echo Se recupero tu appsettings.json de TEST de una publicacion que se habia cortado.
)

if not exist "%CFG_PROD%" (
    echo.
    echo *** Falta el archivo %CFG_PROD%
    echo *** Copia tu appsettings.json con ese nombre y dejale activos los datos de PRODUCCION.
    echo *** No se publico nada.
    pause
    exit /b 1
)
findstr /c:"%REF_PROD%" "%CFG_PROD%" >nul
if errorlevel 1 (
    echo.
    echo *** appsettings.produccion.json NO tiene la direccion de PRODUCCION ^(%REF_PROD%^).
    echo *** Revisalo. No se publico nada.
    pause
    exit /b 1
)

echo.
echo === La app se publica apuntando a PRODUCCION (appsettings.produccion.json) ===
echo === Tu appsettings.json de TEST se guarda y se vuelve a poner al terminar ===
echo.

copy /y "%CFG%" "%CFG_GUARDADO%" >nul
copy /y "%CFG_PROD%" "%CFG%" >nul
powershell -NoProfile -Command "(Get-Item '%CFG%').LastWriteTime = Get-Date"

echo === 1) Compilando (Release)... ===
dotnet publish "BlazorKiosco_v1\BlazorKioscoMobile.csproj" -c Release -o "_publicar\celular"
set "ERR=%errorlevel%"

rem ---- siempre: vuelve tu appsettings.json de TEST ----
copy /y "%CFG_GUARDADO%" "%CFG%" >nul
del "%CFG_GUARDADO%"
powershell -NoProfile -Command "(Get-Item '%CFG%').LastWriteTime = Get-Date"
echo.
echo (tu appsettings.json volvio a quedar como estaba, en TEST)

if not "%ERR%"=="0" (
    echo.
    echo *** NO SE PUDO COMPILAR. Mira los errores de arriba. No se subio nada a Vercel.
    pause
    exit /b 1
)

rem el archivo con los datos de produccion no tiene que quedar publicado aparte
del /q "%SALIDA%\appsettings.produccion.json*" >nul 2>&1

rem control final: lo que se va a subir apunta a PRODUCCION
findstr /c:"%REF_PROD%" "%SALIDA%\appsettings.json" >nul
if errorlevel 1 (
    echo.
    echo *** La version armada NO apunta a PRODUCCION. No se subio nada a Vercel.
    pause
    exit /b 1
)

echo.
echo === 2) Preparando la carpeta para Vercel... ===
copy /y "BlazorKiosco_v1\vercel.json" "%SALIDA%\vercel.json" >nul

rem numero de version (fecha y hora): la app abierta lo compara y avisa "Hay una version nueva"
powershell -NoProfile -Command "[IO.File]::WriteAllText('%~dp0%SALIDA%\version.txt', (Get-Date -Format 'yyyyMMdd-HHmmss'))"

where vercel >nul 2>&1
if errorlevel 1 (
    echo.
    echo *** Falta la herramienta de Vercel. Instalala con:  npm install -g vercel
    echo     Despues volve a correr este archivo.
    pause
    exit /b 1
)

echo.
echo === 3) Subiendo a Vercel (produccion)... ===
cd /d "%~dp0%SALIDA%"
call vercel --prod
if errorlevel 1 (
    echo.
    echo *** Vercel dio error. Mira el mensaje de arriba.
    pause
    exit /b 1
)

echo.
echo Listo: la app nueva ya esta publicada, apuntando a PRODUCCION.
echo Los que tengan el sistema abierto van a ver el aviso "Hay una version nueva"
echo dentro de los proximos 5 minutos ^(o al volver a la pestana^).
echo RECORDA: si hubo scripts SQL nuevos, tienen que estar corridos en PRODUCCION.
pause
