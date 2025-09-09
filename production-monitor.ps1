# Production Monitoring Dashboard for Stibe API
# This script provides real-time monitoring and alerting for the production environment

param(
    [int]$RefreshInterval = 30,
    [switch]$ContinuousMode,
    [switch]$AlertsOnly,
    [string]$LogPath = ".\logs\monitoring.log"
)

# Configuration
$ApiBaseUrl = "http://202.164.153.160:85/api"
$HealthEndpoint = "$ApiBaseUrl/health"
$MetricsEndpoint = "$ApiBaseUrl/health/metrics"
$DetailedHealthEndpoint = "$ApiBaseUrl/health/detailed"

# Thresholds for alerts
$ResponseTimeThreshold = 5000  # 5 seconds
$DiskSpaceThreshold = 10       # 10% free space
$MemoryThreshold = 1000        # 1GB memory usage
$ErrorRateThreshold = 5        # 5% error rate

# Colors for console output
$ColorGood = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"
$ColorInfo = "Cyan"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] [$Level] $Message"
    
    if (!(Test-Path (Split-Path $LogPath))) {
        New-Item -ItemType Directory -Path (Split-Path $LogPath) -Force | Out-Null
    }
    
    Add-Content -Path $LogPath -Value $LogMessage
    
    # Console output with colors
    $Color = switch ($Level) {
        "ERROR" { $ColorError }
        "WARN" { $ColorWarning }
        "SUCCESS" { $ColorGood }
        default { $ColorInfo }
    }
    
    Write-Host $LogMessage -ForegroundColor $Color
}

function Get-ApiHealth {
    try {
        $response = Invoke-RestMethod -Uri $HealthEndpoint -TimeoutSec 10 -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
            ResponseTime = (Measure-Command { Invoke-RestMethod -Uri $HealthEndpoint -TimeoutSec 10 }).TotalMilliseconds
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
            ResponseTime = -1
        }
    }
}

function Get-ApiMetrics {
    try {
        $response = Invoke-RestMethod -Uri $MetricsEndpoint -TimeoutSec 10 -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Get-DetailedHealth {
    try {
        $response = Invoke-RestMethod -Uri $DetailedHealthEndpoint -TimeoutSec 15 -ErrorAction Stop
        return @{
            Success = $true
            Data = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Test-DatabaseConnectivity {
    # Test database connectivity through a simple API call
    try {
        $testUrl = "$ApiBaseUrl/auth/test-connection"  # You might need to create this endpoint
        $response = Invoke-RestMethod -Uri $testUrl -Method GET -TimeoutSec 5
        return $true
    }
    catch {
        return $false
    }
}

function Show-MonitoringDashboard {
    param($HealthData, $MetricsData, $DetailedData)
    
    Clear-Host
    
    # Header
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
    Write-Host "              STIBE API PRODUCTION MONITORING DASHBOARD        " -ForegroundColor $ColorInfo
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
    Write-Host "Last Updated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor $ColorInfo
    Write-Host ""
    
    # API Health Status
    Write-Host "🔥 API HEALTH STATUS" -ForegroundColor White
    Write-Host "─────────────────────" -ForegroundColor Gray
    
    if ($HealthData.Success) {
        $status = $HealthData.Data.Status
        $color = switch ($status) {
            "Healthy" { $ColorGood }
            "Degraded" { $ColorWarning }
            "Unhealthy" { $ColorError }
            default { $ColorWarning }
        }
        
        Write-Host "Status: $status" -ForegroundColor $color
        Write-Host "Environment: $($HealthData.Data.Environment)" -ForegroundColor $ColorInfo
        Write-Host "Version: $($HealthData.Data.Version)" -ForegroundColor $ColorInfo
        Write-Host "Uptime: $($HealthData.Data.Uptime)" -ForegroundColor $ColorInfo
        Write-Host "Response Time: $([math]::Round($HealthData.ResponseTime, 0)) ms" -ForegroundColor $ColorInfo
        
        if ($HealthData.Data.DatabaseStatus) {
            $dbColor = if ($HealthData.Data.DatabaseStatus -eq "Connected") { $ColorGood } else { $ColorError }
            Write-Host "Database: $($HealthData.Data.DatabaseStatus)" -ForegroundColor $dbColor
        }
        
        if ($HealthData.Data.Issues) {
            Write-Host "Issues:" -ForegroundColor $ColorError
            foreach ($issue in $HealthData.Data.Issues) {
                Write-Host "  • $issue" -ForegroundColor $ColorError
            }
        }
    }
    else {
        Write-Host "Status: API NOT RESPONDING" -ForegroundColor $ColorError
        Write-Host "Error: $($HealthData.Error)" -ForegroundColor $ColorError
    }
    
    Write-Host ""
    
    # System Resources
    Write-Host "💾 SYSTEM RESOURCES" -ForegroundColor White
    Write-Host "───────────────────" -ForegroundColor Gray
    
    if ($HealthData.Success -and $HealthData.Data.DiskSpace) {
        $disk = $HealthData.Data.DiskSpace
        $diskColor = if ($disk.FreeSpacePercent -gt 20) { $ColorGood } elseif ($disk.FreeSpacePercent -gt 10) { $ColorWarning } else { $ColorError }
        Write-Host "Disk Space: $([math]::Round($disk.FreeSpaceGB, 1)) GB free ($([math]::Round($disk.FreeSpacePercent, 1))%)" -ForegroundColor $diskColor
    }
    
    if ($HealthData.Success -and $HealthData.Data.Memory) {
        $memory = $HealthData.Data.Memory
        $memColor = if ($memory.WorkingSetMB -lt 500) { $ColorGood } elseif ($memory.WorkingSetMB -lt 1000) { $ColorWarning } else { $ColorError }
        Write-Host "Memory Usage: $([math]::Round($memory.WorkingSetMB, 1)) MB" -ForegroundColor $memColor
        Write-Host "GC Memory: $([math]::Round($memory.GcMemoryMB, 1)) MB" -ForegroundColor $ColorInfo
    }
    
    if ($MetricsData.Success) {
        $metrics = $MetricsData.Data
        Write-Host "Process ID: $($metrics.ProcessId)" -ForegroundColor $ColorInfo
        Write-Host "Thread Count: $($metrics.ThreadCount)" -ForegroundColor $ColorInfo
        Write-Host "Handle Count: $($metrics.HandleCount)" -ForegroundColor $ColorInfo
    }
    
    Write-Host ""
    
    # Detailed Health Checks
    if ($DetailedData.Success -and $DetailedData.Data.Checks) {
        Write-Host "🔍 DETAILED HEALTH CHECKS" -ForegroundColor White
        Write-Host "─────────────────────────" -ForegroundColor Gray
        
        foreach ($check in $DetailedData.Data.Checks.GetEnumerator()) {
            $checkName = $check.Key
            $checkData = $check.Value
            
            $checkColor = switch ($checkData.Status) {
                "Healthy" { $ColorGood }
                "Degraded" { $ColorWarning }
                "Unhealthy" { $ColorError }
                default { $ColorWarning }
            }
            
            Write-Host "$($checkName.ToUpper()): $($checkData.Status) - $($checkData.Description)" -ForegroundColor $checkColor
            if ($checkData.ResponseTime) {
                Write-Host "  Response Time: $($checkData.ResponseTime) ms" -ForegroundColor $ColorInfo
            }
        }
        
        Write-Host ""
    }
    
    # Performance Alerts
    $alerts = @()
    
    if ($HealthData.Success) {
        if ($HealthData.ResponseTime -gt $ResponseTimeThreshold) {
            $alerts += "High response time: $([math]::Round($HealthData.ResponseTime, 0)) ms"
        }
        
        if ($HealthData.Data.DiskSpace -and $HealthData.Data.DiskSpace.FreeSpacePercent -lt $DiskSpaceThreshold) {
            $alerts += "Low disk space: $([math]::Round($HealthData.Data.DiskSpace.FreeSpacePercent, 1))% free"
        }
        
        if ($HealthData.Data.Memory -and $HealthData.Data.Memory.WorkingSetMB -gt $MemoryThreshold) {
            $alerts += "High memory usage: $([math]::Round($HealthData.Data.Memory.WorkingSetMB, 1)) MB"
        }
        
        if ($HealthData.Data.Status -ne "Healthy") {
            $alerts += "API status is not healthy: $($HealthData.Data.Status)"
        }
    }
    else {
        $alerts += "API is not responding"
    }
    
    if ($alerts.Count -gt 0) {
        Write-Host "🚨 ALERTS" -ForegroundColor White
        Write-Host "─────────" -ForegroundColor Gray
        foreach ($alert in $alerts) {
            Write-Host "⚠️  $alert" -ForegroundColor $ColorError
        }
        Write-Host ""
    }
    elseif (-not $AlertsOnly) {
        Write-Host "✅ No alerts - System is performing well" -ForegroundColor $ColorGood
        Write-Host ""
    }
    
    # Instructions
    if ($ContinuousMode) {
        Write-Host "Press Ctrl+C to stop monitoring..." -ForegroundColor Gray
        Write-Host "Refresh interval: $RefreshInterval seconds" -ForegroundColor Gray
    }
    else {
        Write-Host "Run with -ContinuousMode for real-time monitoring" -ForegroundColor Gray
    }
}

function Send-Alert {
    param([string]$Message, [string]$Severity = "WARNING")
    
    Write-Log $Message $Severity
    
    # Here you can integrate with alerting systems:
    # - Send email notifications
    # - Post to Slack/Teams
    # - Send SMS alerts
    # - Trigger PagerDuty incidents
    # - Update monitoring dashboards
    
    # Example: Write to Windows Event Log
    try {
        if (-not [System.Diagnostics.EventLog]::SourceExists("StibeAPI")) {
            [System.Diagnostics.EventLog]::CreateEventSource("StibeAPI", "Application")
        }
        
        $eventType = switch ($Severity) {
            "ERROR" { "Error" }
            "WARN" { "Warning" }
            default { "Information" }
        }
        
        Write-EventLog -LogName Application -Source "StibeAPI" -EventId 1001 -EntryType $eventType -Message $Message
    }
    catch {
        # Ignore event log errors
    }
}

function Start-Monitoring {
    Write-Log "Starting production monitoring..." "INFO"
    
    do {
        try {
            # Collect monitoring data
            $healthData = Get-ApiHealth
            $metricsData = Get-ApiMetrics
            $detailedData = Get-DetailedHealth
            
            # Show dashboard (unless alerts only mode)
            if (-not $AlertsOnly) {
                Show-MonitoringDashboard -HealthData $healthData -MetricsData $metricsData -DetailedData $detailedData
            }
            
            # Check for alerts
            if (-not $healthData.Success) {
                Send-Alert "API is not responding: $($healthData.Error)" "ERROR"
            }
            elseif ($healthData.Data.Status -eq "Unhealthy") {
                Send-Alert "API status is Unhealthy" "ERROR"
            }
            elseif ($healthData.ResponseTime -gt $ResponseTimeThreshold) {
                Send-Alert "High API response time: $([math]::Round($healthData.ResponseTime, 0)) ms" "WARN"
            }
            
            # Wait for next refresh
            if ($ContinuousMode) {
                Start-Sleep -Seconds $RefreshInterval
            }
        }
        catch {
            Write-Log "Monitoring error: $($_.Exception.Message)" "ERROR"
            if ($ContinuousMode) {
                Start-Sleep -Seconds $RefreshInterval
            }
        }
    }
    while ($ContinuousMode)
}

# Main execution
try {
    Write-Host "Stibe API Production Monitoring" -ForegroundColor $ColorInfo
    Write-Host "===============================" -ForegroundColor $ColorInfo
    Write-Host ""
    
    if ($ContinuousMode) {
        Write-Host "Starting continuous monitoring (refresh every $RefreshInterval seconds)..." -ForegroundColor $ColorInfo
    }
    else {
        Write-Host "Performing single health check..." -ForegroundColor $ColorInfo
    }
    
    Write-Host ""
    
    Start-Monitoring
}
catch {
    Write-Log "Fatal monitoring error: $($_.Exception.Message)" "ERROR"
    exit 1
}

# Usage examples:
# .\production-monitor.ps1                              # Single health check
# .\production-monitor.ps1 -ContinuousMode             # Continuous monitoring
# .\production-monitor.ps1 -ContinuousMode -RefreshInterval 60  # Monitor every 60 seconds
# .\production-monitor.ps1 -AlertsOnly                 # Only show alerts
# .\production-monitor.ps1 -ContinuousMode -AlertsOnly # Background monitoring with alerts only
