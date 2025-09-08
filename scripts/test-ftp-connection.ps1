# Test FTP Connection Script
# Run this script from your local machine to test FTP connectivity

param(
    [Parameter(Mandatory=$true)]
    [string]$FtpServer = "202.164.153.160",
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [string]$Password
)

Write-Host "Testing FTP connection to $FtpServer..." -ForegroundColor Green

try {
    # Create FTP request
    $ftpUri = "ftp://$FtpServer/"
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
    $request.UseBinary = $true
    $request.UsePassive = $true
    $request.KeepAlive = $false

    # Get response
    $response = $request.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $content = $reader.ReadToEnd()
    
    Write-Host "FTP Connection Successful!" -ForegroundColor Green
    Write-Host "Directory listing:" -ForegroundColor Yellow
    Write-Host $content
    
    $reader.Close()
    $response.Close()
    
    return $true
} catch {
    Write-Host "FTP Connection Failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    return $false
}

# Test file upload
Write-Host "`nTesting file upload..." -ForegroundColor Green

try {
    # Create a test file
    $testContent = "FTP Test - $(Get-Date)"
    $testFile = "ftp-test.txt"
    $testContent | Out-File -FilePath $testFile -Encoding UTF8
    
    # Upload test file
    $uploadUri = "ftp://$FtpServer/$testFile"
    $request = [System.Net.FtpWebRequest]::Create($uploadUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
    $request.UseBinary = $true
    $request.UsePassive = $true
    $request.KeepAlive = $false
    
    $fileBytes = [System.IO.File]::ReadAllBytes($testFile)
    $request.ContentLength = $fileBytes.Length
    
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($fileBytes, 0, $fileBytes.Length)
    $requestStream.Close()
    
    $response = $request.GetResponse()
    Write-Host "File upload successful!" -ForegroundColor Green
    $response.Close()
    
    # Clean up test file
    Remove-Item $testFile
    
} catch {
    Write-Host "File upload failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nFTP Test Complete!" -ForegroundColor Cyan
