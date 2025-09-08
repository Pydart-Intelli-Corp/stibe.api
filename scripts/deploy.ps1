# GitHub Webhook Deployment Script
# This script can be triggered by GitHub webhooks
param(
    [string]$BranchName = "master",
    [string]$RepoPath = "D:\MY PROJECTS\Stibe\stibe.api",
    [string]$IISPath = "C:\inetpub\wwwroot\StibeAPI",
    [string]$AppPoolName = "StibeAPI"
)

# Log function
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor Green
    Add-Content -Path "$RepoPath\deployment.log" -Value "[$timestamp] $Message"
}

try {
    Write-Log "Starting deployment process..."
    
    # Navigate to repository
    Set-Location $RepoPath
    
    # Pull latest changes
    Write-Log "Pulling latest changes from GitHub..."
    git fetch origin
    git reset --hard origin/$BranchName
    
    # Build and publish
    Write-Log "Building application..."
    dotnet restore
    dotnet build --configuration Release
    dotnet publish --configuration Release --output ".\deploy-temp"
    
    # Stop IIS App Pool
    Write-Log "Stopping IIS Application Pool..."
    Import-Module WebAdministration
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 5
    
    # Backup current deployment
    $backupPath = "$RepoPath\backups\backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    if (Test-Path $IISPath) {
        Write-Log "Creating backup..."
        New-Item -ItemType Directory -Force -Path "$RepoPath\backups" | Out-Null
        Copy-Item -Path $IISPath -Destination $backupPath -Recurse -Force
    }
    
    # Deploy new version
    Write-Log "Deploying to IIS..."
    if (Test-Path $IISPath) {
        Remove-Item -Path "$IISPath\*" -Recurse -Force
    } else {
        New-Item -ItemType Directory -Force -Path $IISPath | Out-Null
    }
    
    Copy-Item -Path ".\deploy-temp\*" -Destination $IISPath -Recurse -Force
    
    # Clean up temporary files
    Remove-Item -Path ".\deploy-temp" -Recurse -Force
    
    # Start IIS App Pool
    Write-Log "Starting IIS Application Pool..."
    Start-WebAppPool -Name $AppPoolName
    
    Write-Log "Deployment completed successfully!"
    
} catch {
    Write-Log "Deployment failed: $($_.Exception.Message)"
    
    # Rollback if backup exists
    if ($backupPath -and (Test-Path $backupPath)) {
        Write-Log "Rolling back to previous version..."
        Remove-Item -Path "$IISPath\*" -Recurse -Force
        Copy-Item -Path "$backupPath\*" -Destination $IISPath -Recurse -Force
        Start-WebAppPool -Name $AppPoolName
    }
    
    throw
}
