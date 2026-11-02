@echo off
rem =====================================================================
rem  PUBLICAR LA APP DEL CELULAR (BlazorKiosco_v1) EN VERCEL
rem  1) Compila en modo Release
rem  2) Deja todo en _publicar\celular\wwwroot (con vercel.json para que anden las direcciones)
rem  3) Lo sube a Vercel (produccion)
rem
rem  Necesita (una sola vez): Node.js y la herramienta de Vercel ->  npm install -g vercel
rem  La primera vez Vercel pregunta: entrar con tu cuenta y "Link to existing project?" -> SI -> elegir tu proyecto.
rem  OJO: la app sale apuntando a la base que este activa en BlazorKiosco_v1\wwwroot\appsettings.json
rem =====================================================================
setlocal
cd /d "%~dp0"

echo.
echo === Base a la que va a apuntar la app publicada (appsettings.json) ===
findstr /i "SupabaseUrl //" "BlazorKiosco_v1\wwwroot\appsettings.json"
echo.
echo Si la linea SIN // no es la de PRODUCCION, cerra esta ventana y corregi el appsettings.json.
pause

echo.
echo === 1) Compilando (Release)... ===
dotnet publish "BlazorKiosco_v1\BlazorKioscoMobile.csproj" -c Release -o "_publicar\celular"
if errorlevel 1 (
    echo.
    echo *** NO SE PUDO COMPILAR. Mira los errores de arriba.
    pause
    exit /b 1
)

echo.
echo === 2) Preparando la carpeta para Vercel... ===
copy /y "BlazorKiosco_v1\vercel.json" "_publicar\celular\wwwroot\vercel.json" >nul

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
cd /d "%~dp0_publicar\celular\wwwroot"
call vercel --prod
if errorlevel 1 (
    echo.
    echo *** Vercel dio error. Mira el mensaje de arriba.
    pause
    exit /b 1
)

echo.
echo Listo: la app nueva ya esta publicada. En los celulares, cerrar y abrir la pagina (o recargar) para ver la version nueva.
pause
