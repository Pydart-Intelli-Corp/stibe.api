# Local Development Setup Script for Stibe API
# This script helps set up the local development environment with secrets

Write-Host "Stibe API - Local Development Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check if appsettings.Secrets.json exists
$secretsFile = "appsettings.Secrets.json"
if (Test-Path $secretsFile) {
    Write-Host "Found $secretsFile" -ForegroundColor Green
    
    # Validate JSON structure
    try {
        $secrets = Get-Content $secretsFile | ConvertFrom-Json
        Write-Host "Secrets file is valid JSON" -ForegroundColor Green
        
        # Check required sections
        $requiredSections = @("ConnectionStrings", "JwtSettings", "GoogleOAuth", "FileStorage", "SmtpSettings", "Razorpay")
        $missingSection = $false
        
        foreach ($section in $requiredSections) {
            if ($secrets.PSObject.Properties.Name -contains $section) {
                Write-Host "Section '$section' found" -ForegroundColor Green
            } else {
                Write-Host "Section '$section' missing" -ForegroundColor Red
                $missingSection = $true
            }
        }
        
        if (-not $missingSection) {
            Write-Host "`nAll required sections are present!" -ForegroundColor Green
        } else {
            Write-Host "`nSome sections are missing. Please check your secrets file." -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "Secrets file contains invalid JSON: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "$secretsFile not found!" -ForegroundColor Red
    Write-Host "`nTo create the secrets file:" -ForegroundColor Yellow
    Write-Host "1. Copy appsettings.Secrets.json.template" -ForegroundColor Yellow
    Write-Host "2. Or create it manually with all required secret values" -ForegroundColor Yellow
    Write-Host "3. Ensure it is in the same directory as this script" -ForegroundColor Yellow
}

# Check .NET SDK
Write-Host "`nChecking .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET SDK not found or not in PATH" -ForegroundColor Red
}

# Check if packages need restore
if (Test-Path "obj") {
    Write-Host "NuGet packages appear to be restored" -ForegroundColor Green
} else {
    Write-Host "NuGet packages may need to be restored" -ForegroundColor Yellow
    Write-Host "Run: dotnet restore" -ForegroundColor Yellow
}

Write-Host "`nTo start the API in development mode:" -ForegroundColor Cyan
Write-Host "dotnet run" -ForegroundColor White

Write-Host "`nAPI Documentation will be available at:" -ForegroundColor Cyan
Write-Host "https://localhost:7001/swagger" -ForegroundColor White
Write-Host "http://localhost:5001/swagger" -ForegroundColor White

Write-Host "`nSecurity Note:" -ForegroundColor Yellow
Write-Host "Never commit appsettings.Secrets.json to version control!" -ForegroundColor Yellow