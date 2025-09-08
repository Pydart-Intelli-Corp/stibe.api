# Simple Validation Script
Write-Host "Validating GitHub Actions Setup..." -ForegroundColor Cyan

$errors = @()
$warnings = @()

# Check Admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    $errors += "Must run as Administrator"
} else {
    Write-Host "✅ Running as Administrator" -ForegroundColor Green
}

# Check .NET
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "8.*") {
        Write-Host "✅ .NET 8.0 SDK: $dotnetVersion" -ForegroundColor Green
    } else {
        $warnings += ".NET version is $dotnetVersion, expected 8.x"
    }
} catch {
    $errors += ".NET SDK not found"
}

# Check Git
try {
    $gitVersion = git --version
    Write-Host "✅ Git: $gitVersion" -ForegroundColor Green
} catch {
    $warnings += "Git not found in PATH"
}

# Test build
Write-Host "Testing project build..." -ForegroundColor Yellow
try {
    Push-Location "D:\MY PROJECTS\Stibe\stibe.api"
    
    dotnet restore | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ dotnet restore successful" -ForegroundColor Green
    } else {
        $errors += "dotnet restore failed"
    }
    
    dotnet build --configuration Release --no-restore | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ dotnet build successful" -ForegroundColor Green
    } else {
        $errors += "dotnet build failed"
    }
    
    Pop-Location
} catch {
    $errors += "Build test failed: $($_.Exception.Message)"
}

# Summary
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
if ($errors.Count -eq 0) {
    Write-Host "✅ Basic checks passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Issues found:" -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "   $err" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host "⚠️ Warnings:" -ForegroundColor Yellow
    foreach ($warn in $warnings) {
        Write-Host "   $warn" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Next: Set up IIS and GitHub runner" -ForegroundColor Cyan
