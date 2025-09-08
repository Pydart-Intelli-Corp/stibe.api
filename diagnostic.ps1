# Quick Diagnostic Script for Profile Image Upload Issue
# Run this script on your IIS server to diagnose the current issue

Write-Host "=== STIBE API DIAGNOSTIC REPORT ===" -ForegroundColor Green
Write-Host "Generated: $(Get-Date)" -ForegroundColor Yellow
Write-Host "Server: http://202.164.153.160:85" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Green

# 1. Check Application Status
Write-Host "`n1. 🔍 APPLICATION STATUS" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest "http://202.164.153.160:85/api/test/health" -TimeoutSec 10
    Write-Host "✅ API is responding: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ API not responding: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Check Directory Structure
Write-Host "`n2. 📁 DIRECTORY STRUCTURE" -ForegroundColor Cyan
$requiredDirs = @(
    "wwwroot",
    "wwwroot\uploads", 
    "wwwroot\uploads\profile-images",
    "logs"
)

foreach ($dir in $requiredDirs) {
    if (Test-Path $dir) {
        $size = (Get-ChildItem $dir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
        Write-Host "✅ $dir ($('{0:N2}' -f $size) MB)" -ForegroundColor Green
    } else {
        Write-Host "❌ $dir (missing)" -ForegroundColor Red
        Write-Host "   Creating directory..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# 3. Check Permissions
Write-Host "`n3. 🔐 PERMISSIONS CHECK" -ForegroundColor Cyan
try {
    $testFile = "wwwroot\uploads\test-permission.txt"
    "Test" | Out-File $testFile
    Remove-Item $testFile
    Write-Host "✅ Write permissions OK" -ForegroundColor Green
} catch {
    Write-Host "❌ Write permission issue: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Try: icacls 'wwwroot\uploads' /grant 'IIS_IUSRS:(OI)(CI)F' /T" -ForegroundColor Yellow
}

# 4. Check Recent Logs
Write-Host "`n4. 📊 RECENT LOGS" -ForegroundColor Cyan
if (Test-Path "logs") {
    $logFiles = Get-ChildItem "logs\*.log" | Sort-Object LastWriteTime -Descending
    if ($logFiles) {
        $latestLog = $logFiles[0]
        Write-Host "📄 Latest log: $($latestLog.Name)" -ForegroundColor Green
        Write-Host "📅 Modified: $($latestLog.LastWriteTime)" -ForegroundColor Green
        
        Write-Host "`n🔍 Last 10 log entries:" -ForegroundColor Yellow
        Get-Content $latestLog.FullName -Tail 10 | ForEach-Object {
            if ($_ -match "ERROR|FATAL") {
                Write-Host "❌ $_" -ForegroundColor Red
            } elseif ($_ -match "WARN") {
                Write-Host "⚠️  $_" -ForegroundColor Yellow
            } else {
                Write-Host "ℹ️  $_" -ForegroundColor White
            }
        }
    } else {
        Write-Host "❌ No log files found" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Logs directory not found" -ForegroundColor Red
}

# 5. Check Profile Upload Specific Issues
Write-Host "`n5. 🖼️ PROFILE UPLOAD DIAGNOSTICS" -ForegroundColor Cyan
if (Test-Path "logs") {
    $uploadErrors = Get-ChildItem "logs\*.log" | ForEach-Object {
        Get-Content $_.FullName | Where-Object { 
            $_ -match "PROFILE.*IMAGE.*UPLOAD.*ERROR|An error occurred while uploading profile image" 
        }
    } | Select-Object -Last 5
    
    if ($uploadErrors) {
        Write-Host "❌ Recent upload errors found:" -ForegroundColor Red
        $uploadErrors | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
    } else {
        Write-Host "✅ No recent upload errors found" -ForegroundColor Green
    }
}

# 6. Check File System Space
Write-Host "`n6. 💾 DISK SPACE" -ForegroundColor Cyan
$drive = Get-PSDrive -Name C
$freeGB = [math]::Round($drive.Free / 1GB, 2)
$totalGB = [math]::Round(($drive.Used + $drive.Free) / 1GB, 2)
$usedPercent = [math]::Round(($drive.Used / ($drive.Used + $drive.Free)) * 100, 1)

Write-Host "💾 Drive C: $freeGB GB free of $totalGB GB ($usedPercent% used)" -ForegroundColor Green

if ($freeGB -lt 1) {
    Write-Host "⚠️  Low disk space warning!" -ForegroundColor Yellow
}

# 7. Test API Endpoints
Write-Host "`n7. 🧪 API ENDPOINT TESTS" -ForegroundColor Cyan
$endpoints = @(
    "/api/test/health",
    "/swagger",
    "/api/auth/check-status"
)

foreach ($endpoint in $endpoints) {
    try {
        $url = "http://202.164.153.160:85$endpoint"
        $response = Invoke-WebRequest $url -TimeoutSec 5
        Write-Host "✅ $endpoint : $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "❌ $endpoint : $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 8. Recommendations
Write-Host "`n8. 💡 RECOMMENDATIONS" -ForegroundColor Cyan
Write-Host "To monitor logs in real-time:" -ForegroundColor Yellow
Write-Host "   .\monitor-logs.ps1 -Follow -ShowUploadLogs" -ForegroundColor White
Write-Host "To check specific upload issue:" -ForegroundColor Yellow
Write-Host "   Select-String -Path '.\logs\*.log' -Pattern 'PROFILE.*IMAGE.*UPLOAD' -Context 3" -ForegroundColor White
Write-Host "To test profile upload:" -ForegroundColor Yellow
Write-Host "   Trigger upload from Flutter app and immediately run .\monitor-logs.ps1 -ShowUploadLogs" -ForegroundColor White

Write-Host "`n=== DIAGNOSTIC COMPLETED ===" -ForegroundColor Green
