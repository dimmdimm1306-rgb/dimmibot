@echo off
title HP Live Log - StokBarangMAUI

echo ============================================================
echo  WATCHING HP LOG (live) - Device: ecb39be77f84
echo ============================================================
echo  Tag yang akan muncul:
echo    mono-stdout       - Console.WriteLine dari C#
echo    DOTNET            - .NET runtime
echo    [Chat]            - user input + bot output
echo    [AiChatService]   - AI flow
echo    [GDriveCmd]       - alias matching, filter
echo    [GDriveReader]    - HTTP call
echo.
echo  Ctrl+C untuk stop.
echo ============================================================
echo.

adb -s ecb39be77f84 logcat -c
adb -s ecb39be77f84 logcat mono-stdout:V DOTNET:V *:S

pause
