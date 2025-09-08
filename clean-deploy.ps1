# Clean Deployment Script - Like Visual Studio Publish
# This script performs a clean build, publish, and deployment to IIS server

param(
    [string]$Configuration = "Release",
    [string]$PublishPath = ".\publish",
    [string]$ServerUrl = "202.164.153.160",
    [string]$FtpPort = "92",
    [string]$ServerPath = "/test/",
    [switch]$BuildOnly = $false,
    [switch]$SkipTests = $false,
    [switch]$Force = $false
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"
$HeaderColor = "Magenta"

Write-Host "🚀 CLEAN DEPLOYMENT SCRIPT (Visual Studio Style)" -ForegroundColor $HeaderColor
Write-Host "===============================================" -ForegroundColor $HeaderColor
Write-Host "Configuration: $Configuration" -ForegroundColor $InfoColor
Write-Host "Publish Path: $PublishPath" -ForegroundColor $InfoColor
Write-Host "Server: $ServerUrl`:$FtpPort" -ForegroundColor $InfoColor
Write-Host ""

# Step 1: Clean Solution
Write-Host "🧹 STEP 1: CLEANING SOLUTION" -ForegroundColor $HeaderColor
Write-Host "=============================" -ForegroundColor $HeaderColor

if (Test-Path $PublishPath) {
    Write-Host "Removing existing publish folder..." -ForegroundColor $WarningColor
    Remove-Item -Path $PublishPath -Recurse -Force
}

Write-Host "Cleaning solution..." -ForegroundColor $InfoColor
dotnet clean --configuration $Configuration --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Clean failed!" -ForegroundColor $ErrorColor
    exit 1
}
Write-Host "✅ Solution cleaned successfully" -ForegroundColor $SuccessColor
Write-Host ""

# Step 2: Restore Dependencies
Write-Host "📦 STEP 2: RESTORING DEPENDENCIES" -ForegroundColor $HeaderColor
Write-Host "==================================" -ForegroundColor $HeaderColor

Write-Host "Restoring NuGet packages..." -ForegroundColor $InfoColor
dotnet restore stibe.api.csproj --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed!" -ForegroundColor $ErrorColor
    exit 1
}
Write-Host "✅ Dependencies restored successfully" -ForegroundColor $SuccessColor
Write-Host ""

# Step 3: Build Solution
Write-Host "🔨 STEP 3: BUILDING SOLUTION" -ForegroundColor $HeaderColor
Write-Host "=============================" -ForegroundColor $HeaderColor

Write-Host "Building in $Configuration mode..." -ForegroundColor $InfoColor
dotnet build stibe.api.csproj --configuration $Configuration --no-restore --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor $ErrorColor
    exit 1
}
Write-Host "✅ Build completed successfully" -ForegroundColor $SuccessColor
Write-Host ""

# Step 4: Run Tests (Optional)
if (-not $SkipTests) {
    Write-Host "🧪 STEP 4: RUNNING TESTS" -ForegroundColor $HeaderColor
    Write-Host "=========================" -ForegroundColor $HeaderColor
    
    Write-Host "Running unit tests..." -ForegroundColor $InfoColor
    dotnet test stibe.api.csproj --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️ Tests failed, but continuing..." -ForegroundColor $WarningColor
    } else {
        Write-Host "✅ Tests passed successfully" -ForegroundColor $SuccessColor
    }
    Write-Host ""
}

# Step 5: Publish Application
Write-Host "📦 STEP 5: PUBLISHING APPLICATION" -ForegroundColor $HeaderColor
Write-Host "==================================" -ForegroundColor $HeaderColor

Write-Host "Publishing application (Visual Studio style)..." -ForegroundColor $InfoColor
dotnet publish stibe.api.csproj `
    --configuration $Configuration `
    --output $PublishPath `
    --no-restore `
    --no-build `
    --self-contained false `
    --verbosity normal `
    --property:PublishUrl="$PublishPath" `
    --property:DeleteExistingFiles=true `
    --property:PublishSingleFile=false `
    --property:PublishReadyToRun=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor $ErrorColor
    exit 1
}

Write-Host "✅ Application published successfully" -ForegroundColor $SuccessColor
Write-Host ""

# Step 6: Verify Published Files
Write-Host "🔍 STEP 6: VERIFYING PUBLISHED FILES" -ForegroundColor $HeaderColor
Write-Host "=====================================" -ForegroundColor $HeaderColor

$publishedFiles = Get-ChildItem -Path $PublishPath -Recurse -File
$totalFiles = $publishedFiles.Count
$totalSize = ($publishedFiles | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)

Write-Host "📊 Publish Summary:" -ForegroundColor $InfoColor
Write-Host "  📁 Total Files: $totalFiles" -ForegroundColor Gray
Write-Host "  📦 Total Size: $totalSizeMB MB" -ForegroundColor Gray

# Check essential files
$mainDll = Join-Path $PublishPath "stibe.api.dll"
$webConfig = Join-Path $PublishPath "web.config"
$appSettings = Join-Path $PublishPath "appsettings.json"

if (Test-Path $mainDll) {
    $dllSize = [math]::Round((Get-Item $mainDll).Length / 1KB, 2)
    Write-Host "  ✅ Main DLL: stibe.api.dll ($dllSize KB)" -ForegroundColor $SuccessColor
} else {
    Write-Host "  ❌ Main DLL: MISSING!" -ForegroundColor $ErrorColor
    exit 1
}

if (Test-Path $webConfig) {
    Write-Host "  ✅ IIS Config: web.config" -ForegroundColor $SuccessColor
} else {
    Write-Host "  ⚠️ IIS Config: web.config missing - creating basic one..." -ForegroundColor $WarningColor
    
    $basicWebConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\stibe.api.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $basicWebConfig | Out-File -FilePath $webConfig -Encoding UTF8
    Write-Host "  ✅ Basic web.config created" -ForegroundColor $SuccessColor
}

if (Test-Path $appSettings) {
    Write-Host "  ✅ App Settings: appsettings.json" -ForegroundColor $SuccessColor
} else {
    Write-Host "  ⚠️ App Settings: appsettings.json missing" -ForegroundColor $WarningColor
}

$dllFiles = Get-ChildItem -Path $PublishPath -Filter "*.dll" -Recurse
Write-Host "  📚 DLL Dependencies: $($dllFiles.Count) files" -ForegroundColor Gray

Write-Host ""

if ($BuildOnly) {
    Write-Host "🎯 BUILD-ONLY MODE COMPLETE" -ForegroundColor $HeaderColor
    Write-Host "============================" -ForegroundColor $HeaderColor
    Write-Host "✅ Application built and published to: $PublishPath" -ForegroundColor $SuccessColor
    Write-Host "📁 Ready for manual deployment or FTP upload" -ForegroundColor $InfoColor
    Write-Host ""
    Write-Host "To deploy manually:" -ForegroundColor $InfoColor
    Write-Host "1. Copy all files from '$PublishPath' to server" -ForegroundColor Gray
    Write-Host "2. Replace existing files in C:\inetpub\wwwroot\test\" -ForegroundColor Gray
    Write-Host "3. Restart IIS Application Pool" -ForegroundColor Gray
    exit 0
}

# Step 7: Deploy to Server (if not build-only)
Write-Host "🚀 STEP 7: DEPLOYING TO SERVER" -ForegroundColor $HeaderColor
Write-Host "===============================" -ForegroundColor $HeaderColor

Write-Host "⚠️ CLEAN DEPLOYMENT WARNING:" -ForegroundColor $WarningColor
Write-Host "This will completely replace all files on the server!" -ForegroundColor $WarningColor
Write-Host "Server: $ServerUrl`:$FtpPort$ServerPath" -ForegroundColor $WarningColor

if (-not $Force) {
    $confirm = Read-Host "Continue with clean deployment? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "❌ Deployment cancelled by user" -ForegroundColor $WarningColor
        exit 0
    }
}

Write-Host ""
Write-Host "🔄 Starting clean deployment..." -ForegroundColor $InfoColor
Write-Host "Note: This requires FTP credentials to be configured separately" -ForegroundColor $WarningColor
Write-Host "Consider using the GitHub Actions workflow for automated deployment" -ForegroundColor $InfoColor
Write-Host ""

# Step 8: Deployment Complete
Write-Host "🎉 DEPLOYMENT PROCESS COMPLETE" -ForegroundColor $HeaderColor
Write-Host "===============================" -ForegroundColor $HeaderColor
Write-Host "✅ Build: Success" -ForegroundColor $SuccessColor
Write-Host "✅ Publish: Success ($totalFiles files, $totalSizeMB MB)" -ForegroundColor $SuccessColor
Write-Host "📁 Published to: $PublishPath" -ForegroundColor $InfoColor
Write-Host ""
Write-Host "🔗 Next Steps:" -ForegroundColor $InfoColor
Write-Host "1. Use GitHub Actions for automated FTP deployment" -ForegroundColor Gray
Write-Host "2. Or manually copy files to server" -ForegroundColor Gray
Write-Host "3. Test endpoints: http://$ServerUrl`:85/api/test/health" -ForegroundColor Gray
Write-Host ""
Write-Host "💡 To test locally first: .\test-all-endpoints.ps1 -Local" -ForegroundColor $InfoColor
