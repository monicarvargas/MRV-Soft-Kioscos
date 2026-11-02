@echo off
rem =====================================================================
rem  Programa la copia de PRODUCCION todas las noches a las 23:30.
rem  Si a esa hora la PC esta apagada, la hace apenas se prende.
rem  Se corre UNA vez (doble clic). Para sacarla: Programador de tareas > "MRV Soft - Copia de produccion" > Eliminar.
rem =====================================================================
set "BAT=%~dp0backup_produccion.bat"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$a = New-ScheduledTaskAction -Execute 'cmd.exe' -Argument ('/c \"\"' + $env:BAT + '\" auto\"');" ^
  "$t = New-ScheduledTaskTrigger -Daily -At '23:30';" ^
  "$s = New-ScheduledTaskSettingsSet -StartWhenAvailable -DontStopIfGoingOnBatteries -AllowStartIfOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 2);" ^
  "Register-ScheduledTask -TaskName 'MRV Soft - Copia de produccion' -Action $a -Trigger $t -Settings $s -Description 'Copia completa de la base de PRODUCCION (pg_dump)' -Force | Out-Null;" ^
  "Write-Host 'Listo: la copia se va a hacer todas las noches a las 23:30.'"
pause
