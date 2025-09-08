# Quick Start Script for GitHub Actions IIS Deployment
# Run this script as Administrator to set up everything

param(
    [switch]$SkipIISSetup,
    [switch]$SkipValidation,
    [string]$GitHubToken = ""
)

Write-Host "🚀 Stibe API - GitHub Actions IIS Deployment Setup" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Error "❌ This script must be run as Administrator"
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath

Write-Host "📁 Project Root: $projectRoot" -ForegroundColor Gray

try {
    # Step 1: IIS Setup
    if (-not $SkipIISSetup) {
        Write-Host ""
        Write-Host "Step 1: Setting up IIS..." -ForegroundColor Yellow
        if (Test-Path "$projectRoot\scripts\setup-iis.ps1") {
            & "$projectRoot\scripts\setup-iis.ps1"
        } else {
            Write-Warning "IIS setup script not found. Run it manually if needed."
        }
    }

    # Step 2: Validate Setup
    if (-not $SkipValidation) {
        Write-Host ""
        Write-Host "Step 2: Validating setup..." -ForegroundColor Yellow
        if (Test-Path "$projectRoot\scripts\validate-setup.ps1") {
            & "$projectRoot\scripts\validate-setup.ps1"
        } else {
            Write-Warning "Validation script not found."
        }
    }

    # Step 3: GitHub Runner Setup Instructions
    Write-Host ""
    Write-Host "Step 3: GitHub Runner Setup" -ForegroundColor Yellow
    Write-Host "=============================" -ForegroundColor Yellow
    
    $runnerExists = Test-Path "C:\actions-runner"
    if (-not $runnerExists) {
        Write-Host "GitHub Actions Runner not found. Follow these steps:" -ForegroundColor White
        Write-Host ""
        Write-Host "1. Create runner directory:" -ForegroundColor Cyan
        Write-Host "   mkdir C:\actions-runner" -ForegroundColor Gray
        Write-Host "   cd C:\actions-runner" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. Go to your GitHub repository:" -ForegroundColor Cyan
        Write-Host "   https://github.com/Pydart-Intelli-Corp/stibe.api/settings/actions/runners" -ForegroundColor Blue
        Write-Host ""
        Write-Host "3. Click 'New self-hosted runner'" -ForegroundColor Cyan
        Write-Host "4. Follow the download and configuration instructions" -ForegroundColor Cyan
        Write-Host "5. Install as Windows Service:" -ForegroundColor Cyan
        Write-Host "   .\svc.sh install" -ForegroundColor Gray
        Write-Host "   .\svc.sh start" -ForegroundColor Gray
    } else {
        Write-Host "✅ GitHub Actions Runner directory found" -ForegroundColor Green
        
        # Check if service is running
        $runnerService = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
        if ($runnerService) {
            Write-Host "✅ GitHub Actions Runner service is installed" -ForegroundColor Green
            foreach ($service in $runnerService) {
                $status = $service.Status
                $color = if ($status -eq 'Running') { 'Green' } else { 'Yellow' }
                Write-Host "   $($service.Name): $status" -ForegroundColor $color
            }
        } else {
            Write-Host "⚠️  GitHub Actions Runner service not found" -ForegroundColor Yellow
            Write-Host "   Navigate to C:\actions-runner and run:" -ForegroundColor White
            Write-Host "   .\svc.sh install" -ForegroundColor Gray
            Write-Host "   .\svc.sh start" -ForegroundColor Gray
        }
    }

    # Step 4: Final Instructions
    Write-Host ""
    Write-Host "Step 4: Ready to Deploy!" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Your GitHub Actions workflow is configured and ready!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "1. Ensure your GitHub self-hosted runner is set up and running" -ForegroundColor Cyan
    Write-Host "2. Commit and push your changes to the 'master' branch" -ForegroundColor Cyan
    Write-Host "3. Monitor the deployment in GitHub Actions:" -ForegroundColor Cyan
    Write-Host "   https://github.com/Pydart-Intelli-Corp/stibe.api/actions" -ForegroundColor Blue
    Write-Host "4. Test your deployed API at:" -ForegroundColor Cyan
    Write-Host "   http://localhost/StibeAPI/api/test/health" -ForegroundColor Blue
    Write-Host ""
    Write-Host "Files created/updated:" -ForegroundColor Gray
    Write-Host "- .github/workflows/deploy-to-iis.yml" -ForegroundColor Gray
    Write-Host "- scripts/setup-iis.ps1" -ForegroundColor Gray
    Write-Host "- scripts/validate-setup.ps1" -ForegroundColor Gray
    Write-Host "- scripts/deploy.ps1" -ForegroundColor Gray
    Write-Host "- docs/GITHUB_ACTIONS_SETUP.md" -ForegroundColor Gray

} catch {
    Write-Error "❌ Setup failed: $($_.Exception.Message)"
    exit 1
}

Write-Host ""
Write-Host "🎉 Setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Tip: Run 'scripts\validate-setup.ps1' anytime to check your setup" -ForegroundColor Cyan
