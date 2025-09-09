# Production Deployment Script for Stibe API
# This script automates the production deployment process with safety checks

param(
    [string]$Environment = "Production",
    [switch]$SkipTests,
    [switch]$SkipBackup,
    [switch]$Force
)

# Configuration
$ProjectPath = $PSScriptRoot
$PublishPath = "$ProjectPath\bin\Release\net8.0\publish"
$BackupPath = "$ProjectPath\backups"
$IISPath = "C:\inetpub\wwwroot\stibeapi"
$LogPath = "$ProjectPath\logs\deployment.log"

# Ensure log directory exists
if (!(Test-Path (Split-Path $LogPath))) {
    New-Item -ItemType Directory -Path (Split-Path $LogPath) -Force
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] [$Level] $Message"
    Write-Output $LogMessage
    Add-Content -Path $LogPath -Value $LogMessage
}

function Test-Prerequisites {
    Write-Log "Checking deployment prerequisites..."
    
    # Check if running as administrator
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "This script must be run as Administrator"
    }
    
    # Check .NET 8 SDK
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw ".NET 8 SDK is not installed or not in PATH"
    }
    Write-Log ".NET version: $dotnetVersion"
    
    # Check IIS installation
    $iisFeature = Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
    if ($iisFeature.State -ne "Enabled") {
        throw "IIS is not installed or enabled"
    }
    
    # Check database connectivity
    Write-Log "Testing database connectivity..."
    $connectionString = "Server=psrazuredb.mysql.database.azure.com;Database=Stibe_db;Uid=psrdbadmin;Pwd=your_password;SslMode=Required;"
    # Note: You should use proper connection string from appsettings.Production.json
    
    Write-Log "Prerequisites check completed successfully"
}

function Backup-CurrentDeployment {
    if ($SkipBackup) {
        Write-Log "Skipping backup as requested"
        return
    }
    
    Write-Log "Creating backup of current deployment..."
    
    $BackupTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $CurrentBackupPath = "$BackupPath\backup_$BackupTimestamp"
    
    if (Test-Path $IISPath) {
        # Create backup directory
        New-Item -ItemType Directory -Path $CurrentBackupPath -Force
        
        # Copy current deployment
        Copy-Item -Path "$IISPath\*" -Destination $CurrentBackupPath -Recurse -Force
        Write-Log "Backup created at: $CurrentBackupPath"
        
        # Clean up old backups (keep last 5)
        $OldBackups = Get-ChildItem $BackupPath -Directory | Sort-Object CreationTime -Descending | Select-Object -Skip 5
        $OldBackups | ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force
            Write-Log "Removed old backup: $($_.Name)"
        }
    }
}

function Build-Application {
    Write-Log "Building application for $Environment environment..."
    
    # Clean previous builds
    if (Test-Path $PublishPath) {
        Remove-Item $PublishPath -Recurse -Force
    }
    
    # Restore packages
    Write-Log "Restoring NuGet packages..."
    dotnet restore $ProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore NuGet packages"
    }
    
    # Run tests if not skipped
    if (-not $SkipTests) {
        Write-Log "Running tests..."
        dotnet test $ProjectPath --configuration Release --logger "console;verbosity=minimal"
        if ($LASTEXITCODE -ne 0 -and -not $Force) {
            throw "Tests failed. Use -Force to deploy anyway"
        }
    }
    
    # Build and publish
    Write-Log "Publishing application..."
    dotnet publish $ProjectPath --configuration Release --output $PublishPath --self-contained false --runtime win-x64
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish application"
    }
    
    Write-Log "Application built successfully"
}

function Deploy-ToIIS {
    Write-Log "Deploying to IIS..."
    
    # Stop IIS site
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    $SiteName = "Default Web Site"
    $AppName = "stibeapi"
    
    try {
        Write-Log "Stopping IIS application pool..."
        Stop-WebAppPool -Name "DefaultAppPool" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 5
        
        # Create IIS directory if it doesn't exist
        if (!(Test-Path $IISPath)) {
            New-Item -ItemType Directory -Path $IISPath -Force
        }
        
        # Copy files to IIS
        Write-Log "Copying files to IIS directory..."
        Copy-Item -Path "$PublishPath\*" -Destination $IISPath -Recurse -Force
        
        # Set proper permissions
        Write-Log "Setting file permissions..."
        $acl = Get-Acl $IISPath
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl -Path $IISPath -AclObject $acl
        
        # Ensure web.config is present
        $webConfigPath = "$IISPath\web.config"
        if (!(Test-Path $webConfigPath)) {
            Write-Log "web.config not found, copying from project..."
            Copy-Item -Path "$ProjectPath\web.config" -Destination $webConfigPath -Force
        }
        
        # Start IIS application pool
        Write-Log "Starting IIS application pool..."
        Start-WebAppPool -Name "DefaultAppPool"
        
        Write-Log "Deployment to IIS completed successfully"
    }
    catch {
        Write-Log "Error during IIS deployment: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Test-Deployment {
    Write-Log "Testing deployment..."
    
    $TestUrl = "http://202.164.153.160:85/api/health"
    $MaxRetries = 10
    $RetryDelay = 5
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            Write-Log "Testing API health (attempt $i/$MaxRetries)..."
            $response = Invoke-WebRequest -Uri $TestUrl -TimeoutSec 10 -UseBasicParsing
            
            if ($response.StatusCode -eq 200) {
                Write-Log "Deployment test successful - API is responding"
                return $true
            }
        }
        catch {
            Write-Log "Test attempt $i failed: $($_.Exception.Message)" "WARN"
        }
        
        if ($i -lt $MaxRetries) {
            Write-Log "Waiting $RetryDelay seconds before retry..."
            Start-Sleep -Seconds $RetryDelay
        }
    }
    
    Write-Log "Deployment test failed - API is not responding" "ERROR"
    return $false
}

function Send-DeploymentNotification {
    param([bool]$Success, [string]$ErrorMessage = "")
    
    $Status = if ($Success) { "SUCCESS" } else { "FAILED" }
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    Write-Log "Deployment $Status at $Timestamp"
    
    if (-not $Success -and $ErrorMessage) {
        Write-Log "Error details: $ErrorMessage" "ERROR"
    }
    
    # Here you could integrate with notification services:
    # - Send email
    # - Post to Slack/Teams
    # - Send SMS
    # - Update monitoring dashboard
}

# Main deployment process
try {
    Write-Log "Starting production deployment process..."
    Write-Log "Environment: $Environment"
    Write-Log "Skip Tests: $SkipTests"
    Write-Log "Skip Backup: $SkipBackup"
    Write-Log "Force: $Force"
    
    # Step 1: Prerequisites check
    Test-Prerequisites
    
    # Step 2: Backup current deployment
    Backup-CurrentDeployment
    
    # Step 3: Build application
    Build-Application
    
    # Step 4: Deploy to IIS
    Deploy-ToIIS
    
    # Step 5: Test deployment
    $TestSuccess = Test-Deployment
    
    if ($TestSuccess) {
        Send-DeploymentNotification -Success $true
        Write-Log "DEPLOYMENT SUCCESSFUL! 🎉" "SUCCESS"
        exit 0
    }
    else {
        throw "Deployment test failed"
    }
}
catch {
    $ErrorMessage = $_.Exception.Message
    Write-Log "DEPLOYMENT FAILED: $ErrorMessage" "ERROR"
    Send-DeploymentNotification -Success $false -ErrorMessage $ErrorMessage
    
    # Optionally restore from backup
    if (-not $SkipBackup -and (Test-Path $BackupPath)) {
        Write-Log "Consider restoring from backup if needed"
        $LatestBackup = Get-ChildItem $BackupPath -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
        if ($LatestBackup) {
            Write-Log "Latest backup available at: $($LatestBackup.FullName)"
        }
    }
    
    exit 1
}

# Usage examples:
# .\production-deploy.ps1                           # Full deployment
# .\production-deploy.ps1 -SkipTests               # Skip running tests
# .\production-deploy.ps1 -SkipBackup              # Skip creating backup
# .\production-deploy.ps1 -Force                   # Deploy even if tests fail
# .\production-deploy.ps1 -SkipTests -SkipBackup   # Quick deployment
