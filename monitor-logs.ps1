# Stibe API Log Monitor Script
# Run this script on your IIS server to monitor logs in real-time

param(
    [string]$LogPath = ".\logs",
    [string]$IISLogPath = "C:\inetpub\logs\LogFiles\W3SVC1",
    [int]$TailLines = 50,
    [switch]$Follow,
    [switch]$ShowErrors,
    [switch]$ShowWarnings,
    [switch]$ShowUploadLogs
)

Write-Host "=== STIBE API LOG MONITOR ===" -ForegroundColor Green
Write-Host "Server: http://202.164.153.160:85" -ForegroundColor Yellow
Write-Host "Monitoring started at: $(Get-Date)" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Green

function Show-RecentLogs {
    param([string]$Path, [string]$Filter = "*", [int]$Lines = $TailLines)
    
    if (Test-Path $Path) {
        $files = Get-ChildItem -Path $Path -Filter $Filter | Sort-Object LastWriteTime -Descending
        if ($files) {
            $latestFile = $files[0]
            Write-Host "`n📄 Latest log file: $($latestFile.Name)" -ForegroundColor Cyan
            Write-Host "📅 Last modified: $($latestFile.LastWriteTime)" -ForegroundColor Cyan
            Write-Host "📊 Size: $([math]::Round($latestFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan
            Write-Host "----------------------------------------" -ForegroundColor Gray
            
            if ($Follow) {
                Get-Content $latestFile.FullName -Tail $Lines -Wait
            } else {
                Get-Content $latestFile.FullName -Tail $Lines
            }
        } else {
            Write-Warning "No log files found in $Path"
        }
    } else {
        Write-Warning "Log path does not exist: $Path"
    }
}

function Show-FilteredLogs {
    param([string]$Pattern, [string]$Description)
    
    Write-Host "`n🔍 $Description" -ForegroundColor Magenta
    Write-Host "----------------------------------------" -ForegroundColor Gray
    
    if (Test-Path $LogPath) {
        Get-ChildItem -Path $LogPath -Filter "*.log" | ForEach-Object {
            Get-Content $_.FullName | Where-Object { $_ -match $Pattern } | Select-Object -Last 20
        }
    }
}

# Main execution
Write-Host "`n1. Application Logs (Serilog)" -ForegroundColor Yellow
Show-RecentLogs -Path $LogPath -Filter "*.log"

Write-Host "`n2. IIS Stdout Logs" -ForegroundColor Yellow
Show-RecentLogs -Path ".\logs" -Filter "stdout*"

Write-Host "`n3. IIS Access Logs" -ForegroundColor Yellow
Show-RecentLogs -Path $IISLogPath -Filter "*.log"

if ($ShowErrors) {
    Show-FilteredLogs -Pattern "ERROR|FATAL|Exception" -Description "Recent Errors"
}

if ($ShowWarnings) {
    Show-FilteredLogs -Pattern "WARN|WARNING" -Description "Recent Warnings"
}

if ($ShowUploadLogs) {
    Show-FilteredLogs -Pattern "PROFILE.*IMAGE.*UPLOAD|profile.*image" -Description "Profile Image Upload Logs"
}

Write-Host "`n=== LOG MONITOR COMPLETED ===" -ForegroundColor Green
Write-Host "To monitor in real-time, run with -Follow parameter" -ForegroundColor Yellow

# Usage examples
Write-Host "`n📋 Usage Examples:" -ForegroundColor Cyan
Write-Host "  .\monitor-logs.ps1                    # Show recent logs"
Write-Host "  .\monitor-logs.ps1 -Follow            # Monitor in real-time"
Write-Host "  .\monitor-logs.ps1 -ShowErrors        # Show recent errors"
Write-Host "  .\monitor-logs.ps1 -ShowUploadLogs    # Show profile upload logs"
Write-Host "  .\monitor-logs.ps1 -TailLines 100     # Show last 100 lines"
