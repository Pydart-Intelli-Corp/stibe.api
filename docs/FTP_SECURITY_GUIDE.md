# Secure FTP Firewall Configuration Guide

## Required Ports for Public Access

### Port 21 (FTP Control Channel)
- **Purpose**: Initial FTP connection and commands
- **Protocol**: TCP
- **Access**: Public (required for GitHub Actions)
- **Security**: Use strong authentication

### Passive Ports (Data Channel)
- **Purpose**: File transfer data
- **Protocol**: TCP
- **Range**: 5000-5100 (example - you can choose different range)
- **Access**: Public (required for passive mode)

## Windows Firewall Rules Setup

### 1. FTP Control Port (Required)
```cmd
# Create inbound rule for FTP control
netsh advfirewall firewall add rule name="FTP-Control-Port" dir=in action=allow protocol=TCP localport=21
```

### 2. FTP Passive Ports (Required)
```cmd
# Create inbound rule for FTP passive data ports
netsh advfirewall firewall add rule name="FTP-Passive-Ports" dir=in action=allow protocol=TCP localport=5000-5100
```

### 3. Optional: Restrict to GitHub IP Ranges
```cmd
# Example: Restrict FTP to specific IP ranges (GitHub's IPs change, so this is optional)
netsh advfirewall firewall add rule name="FTP-GitHub-Only" dir=in action=allow protocol=TCP localport=21 remoteip=140.82.112.0/20,185.199.108.0/22
```

## Security Hardening Steps

### 1. Change Default Credentials
```
Username: stibe-deploy (keep)
Password: Change from StibeAPI2025! to something like:
         Stb#2025$Deploy!Secure#99
```

### 2. Configure Account Lockout
- Open Local Security Policy (secpol.msc)
- Account Policies → Account Lockout Policy
- Set lockout threshold: 5 invalid attempts
- Set lockout duration: 30 minutes

### 3. Enable FTP Logging
- IIS Manager → FTP Site → FTP Logging
- Enable logging to monitor access attempts

### 4. Regular Security Maintenance
- Monitor FTP logs for suspicious activity
- Change FTP password periodically
- Keep Windows Server updated
- Review firewall logs

## Alternative Secure Deployment Methods

If you're concerned about FTP security, consider these alternatives:

### 1. Web Deploy (More Secure)
- Uses HTTPS (port 443)
- Better authentication mechanisms
- Already configured in your GitHub workflows

### 2. Azure DevOps Pipelines
- Microsoft-hosted agents
- Better security controls
- Integration with Azure services

### 3. Self-hosted GitHub Runner
- Run GitHub Actions on your own server
- No need to open FTP ports publicly
- More control over the deployment process

## Current vs Recommended Configuration

### Current Setup (Basic Security):
```
✅ Port 21 open publicly
✅ Basic authentication
⚠️ Default password
⚠️ No IP restrictions
⚠️ No SSL/TLS
```

### Recommended Setup (Enhanced Security):
```
✅ Port 21 open publicly (required)
✅ Strong password
✅ Account lockout policy
✅ FTP logging enabled
✅ Regular monitoring
🔄 Consider FTPS upgrade
🔄 Consider IP restrictions
```

## Network Security Considerations

### Router/Network Level:
- Ensure your router allows port 21 and passive ports
- Consider setting up port forwarding rules
- Monitor network traffic for unusual patterns

### Server Level:
- Keep Windows Server updated
- Enable Windows Defender
- Regular security scans
- Backup your deployment directory

## Testing Security

After setup, test these scenarios:
1. ✅ GitHub Actions can deploy successfully
2. ✅ Invalid credentials are rejected
3. ✅ Account lockout works after multiple failed attempts
4. ✅ FTP logs capture all activities
5. ✅ Only authorized directories are accessible

## Monitoring Commands

```powershell
# Check FTP connections
netstat -an | findstr :21

# View FTP logs
Get-Content "C:\inetpub\logs\LogFiles\FTPSVC*\*.log" -Tail 20

# Check failed login attempts
Get-EventLog -LogName Security -InstanceId 4625 | Select-Object -First 10
```
