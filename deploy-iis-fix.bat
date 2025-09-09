@echo off
echo ================================================
echo Deploying IIS Configuration Fix
echo ================================================

echo Stopping IIS...
iisreset /stop

echo.
echo Copying corrected files...
copy /Y "e:\Stibe\publish-local-iis-fixed\web.config" "C:\inetpub\wwwroot\stibeAPI\web.config"
copy /Y "e:\Stibe\publish-local-iis-fixed\appsettings.Production.json" "C:\inetpub\wwwroot\stibeAPI\appsettings.Production.json"

echo.
echo Creating logs directory...
mkdir "C:\inetpub\wwwroot\stibeAPI\logs" 2>nul

echo.
echo Setting permissions...
icacls "C:\inetpub\wwwroot\stibeAPI\wwwroot\uploads" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\wwwroot\stibeAPI\logs" /grant "IIS_IUSRS:(OI)(CI)F" /T

echo.
echo Starting IIS...
iisreset /start

echo.
echo ================================================
echo Deployment completed!
echo ================================================
echo.
echo The following changes were made:
echo - Updated web.config (fixed MIME mapping conflicts)
echo - Updated appsettings.Production.json (FileStorage now points to local server)
echo - Created logs directory for debugging
echo - Set proper file permissions
echo.
echo You can now test the profile upload again.
echo.
pause
