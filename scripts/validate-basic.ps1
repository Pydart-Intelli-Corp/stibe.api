# GitHub Actions Setup Validation Script
# Run this script to validate your setup before deploying

Write-Host "🔍 Validating GitHub Actions Setup for IIS Deployment" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$errors = @()
$warnings = @()

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    $errors += "❌ Script must be run as Administrator"
} else {
    Write-Host "✅ Running as Administrator" -ForegroundColor Green
}

# Check IIS Installation
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "✅ IIS Web Administration module available" -ForegroundColor Green
} catch {
    $errors += "❌ IIS Web Administration module not available. Install IIS first."
}

# Check .NET 8.0 SDK
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "8.*") {
        Write-Host "✅ .NET 8.0 SDK installed: $dotnetVersion" -ForegroundColor Green
    } else {
        $warnings += "⚠️  .NET version is $dotnetVersion, expected 8.x"
    }
} catch {
    $errors += "❌ .NET SDK not found. Install .NET 8.0 SDK."
}

# Check ASP.NET Core Hosting Bundle
try {
    $hostingBundle = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*ASP.NET Core*Hosting*" }
    if ($hostingBundle) {
        Write-Host "✅ ASP.NET Core Hosting Bundle installed: $($hostingBundle.Name)" -ForegroundColor Green
    } else {
        $warnings += "⚠️  ASP.NET Core Hosting Bundle not detected. This is required for IIS."
    }
} catch {
    $warnings += "⚠️  Could not check for ASP.NET Core Hosting Bundle"
}

# Check Git
try {
    $gitVersion = git --version
    Write-Host "✅ Git installed: $gitVersion" -ForegroundColor Green
} catch {
    $warnings += "⚠️  Git not found in PATH. Required for source control."
}

# Check PowerShell version
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 5) {
    Write-Host "✅ PowerShell version: $($psVersion.ToString())" -ForegroundColor Green
} else {
    $warnings += "⚠️  PowerShell version $($psVersion.ToString()) may not be sufficient"
}

# Test project compilation
Write-Host "🔨 Testing project compilation..." -ForegroundColor Cyan
try {
    Push-Location "D:\MY PROJECTS\Stibe\stibe.api"
    
    dotnet restore | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ dotnet restore successful" -ForegroundColor Green
    } else {
        $errors += "❌ dotnet restore failed"
    }
    
    dotnet build --configuration Release --no-restore | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ dotnet build successful" -ForegroundColor Green
    } else {
        $errors += "❌ dotnet build failed"
    }
    
    Pop-Location
} catch {
    $errors += "❌ Error testing project compilation: $($_.Exception.Message)"
}

# Summary
Write-Host "" 
Write-Host "📊 Validation Summary:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

if ($errors.Count -eq 0) {
    Write-Host "🎉 All critical checks passed! Your setup is ready for GitHub Actions deployment." -ForegroundColor Green
} else {
    Write-Host "❌ Critical issues found:" -ForegroundColor Red
    foreach ($errorItem in $errors) {
        Write-Host "   $errorItem" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Warnings:" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "   $warning" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Fix any critical issues listed above" -ForegroundColor White
Write-Host "2. Set up IIS using: .\scripts\setup-iis.ps1" -ForegroundColor White
Write-Host "3. Set up GitHub self-hosted runner" -ForegroundColor White
Write-Host "4. Commit and push your changes to trigger deployment" -ForegroundColor White
Write-Host "5. Test your deployed API at: http://localhost/StibeAPI/api/test/health" -ForegroundColor White
