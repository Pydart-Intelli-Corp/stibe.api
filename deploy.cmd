@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off

:: ----------------------
:: KUDU Deployment Script
:: Version: 1.0.17
:: ----------------------

:: Prerequisites
:: -------------

:: Verify node.js installed
where node 2>nul >nul
IF %ERRORLEVEL% NEQ 0 (
  echo Missing node.js executable, please install node.js, if already installed make sure it can be reached from current environment.
  goto error
)

:: Setup
:: -----

setlocal enabledelayedexpansion

SET ARTIFACTS=%~dp0%..\artifacts

IF NOT DEFINED DEPLOYMENT_SOURCE (
  SET DEPLOYMENT_SOURCE=%~dp0%.
)

IF NOT DEFINED DEPLOYMENT_TARGET (
  SET DEPLOYMENT_TARGET=%ARTIFACTS%\wwwroot
)

IF NOT DEFINED NEXT_MANIFEST_PATH (
  SET NEXT_MANIFEST_PATH=%ARTIFACTS%\manifest

  IF NOT DEFINED PREVIOUS_MANIFEST_PATH (
    SET PREVIOUS_MANIFEST_PATH=%ARTIFACTS%\manifest
  )
)

IF NOT DEFINED KUDU_SYNC_CMD (
  :: Install kudu sync
  echo Installing Kudu Sync
  call npm install kudusync -g --silent
  IF !ERRORLEVEL! NEQ 0 goto error

  :: Locally just running "kuduSync" would also work
  SET KUDU_SYNC_CMD=%appdata%\npm\kuduSync.cmd
)

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Deployment
:: ----------

echo Handling .NET Web Application deployment.

:: 1. Stop the application
echo Stopping application...
%SYSTEMDRIVE%\Windows\System32\inetsrv\appcmd.exe stop site /site.name:"%WEBSITE_SITE_NAME%" >nul 2>&1

:: 2. Restore NuGet packages
IF EXIST "%DEPLOYMENT_SOURCE%\packages.config" (
  pushd "%DEPLOYMENT_SOURCE%"
  call :ExecuteCmd nuget restore
  IF !ERRORLEVEL! NEQ 0 goto error
  popd
)

IF EXIST "%DEPLOYMENT_SOURCE%\.nuget\packages.config" (
  pushd "%DEPLOYMENT_SOURCE%"
  call :ExecuteCmd nuget restore .nuget\packages.config -PackagesDirectory %DEPLOYMENT_SOURCE%\packages
  IF !ERRORLEVEL! NEQ 0 goto error
  popd
)

:: 3. Build to the temporary path
IF EXIST "%DEPLOYMENT_SOURCE%\*.sln" (
  pushd "%DEPLOYMENT_SOURCE%"
  call :ExecuteCmd nuget restore
  IF !ERRORLEVEL! NEQ 0 goto error
  popd
)

:: 4. KuduSync
IF /I "%IN_PLACE_DEPLOYMENT%" NEQ "1" (
  call :ExecuteCmd "%KUDU_SYNC_CMD%" -v 50 -f "%DEPLOYMENT_SOURCE%" -t "%DEPLOYMENT_TARGET%" -n "%NEXT_MANIFEST_PATH%" -p "%PREVIOUS_MANIFEST_PATH%" -i ".git;.hg;.deployment;deploy.cmd"
  IF !ERRORLEVEL! NEQ 0 goto error
)

:: 5. Start the application
echo Starting application...
%SYSTEMDRIVE%\Windows\System32\inetsrv\appcmd.exe start site /site.name:"%WEBSITE_SITE_NAME%" >nul 2>&1

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
goto end

:: Execute command routine that will echo out when error
:ExecuteCmd
setlocal
set _CMD_=%*
call %_CMD_%
if "%ERRORLEVEL%" NEQ "0" echo Failed exitCode=%ERRORLEVEL%, command=%_CMD_%
exit /b %ERRORLEVEL%

:error
endlocal
echo An error has occurred during web site deployment.
call :exitSetErrorLevel
call :exitFromFunction 2>nul

:exitSetErrorLevel
exit /b 1

:exitFromFunction
()

:end
endlocal
echo Finished successfully.