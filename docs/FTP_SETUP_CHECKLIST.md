# FTP Setup Quick Checklist for Remote Server (202.164.153.160)

## Pre-Setup Information
- **Server**: 202.164.153.160
- **FTP Port**: 21
- **Target Directory**: C:\inetpub\wwwroot\test
- **FTP Username**: stibe-deploy
- **FTP Password**: StibeAPI2025! (change to secure password)

## Setup Checklist

### [ ] 1. Install FTP Server
- Open Server Manager → Add Roles and Features
- Select Web Server (IIS) → FTP Server
- Install FTP Service and FTP Extensibility

### [ ] 2. Create FTP Site
- Open IIS Manager (inetmgr)
- Right-click Sites → Add FTP Site
- Name: StibeAPI-FTP
- Path: C:\inetpub\wwwroot\test
- Port: 21, No SSL

### [ ] 3. Configure Authentication
- Authentication: Basic (checked), Anonymous (unchecked)
- Authorization: Specified users = stibe-deploy
- Permissions: Read + Write

### [ ] 4. Create User Account
- Open Computer Management (compmgmt.msc)
- Users → New User
- Username: stibe-deploy
- Password: StibeAPI2025!
- Password never expires: checked

### [ ] 5. Set Folder Permissions
- Navigate to C:\inetpub\wwwroot\test
- Right-click → Properties → Security → Edit
- Add stibe-deploy user with Full Control

### [ ] 6. Configure Firewall
- Open Windows Firewall (wf.msc)
- New Inbound Rule for Port 21 (TCP)
- Allow connection for all profiles

### [ ] 7. Configure Passive Mode
- IIS Manager → FTP Site → FTP Firewall Support
- External IP: 202.164.153.160
- Data Channel Ports: 5000-5100

### [ ] 8. Add Passive Ports to Firewall
- Windows Firewall → New Inbound Rule
- TCP Ports: 5000-5100
- Allow connection

### [ ] 9. Test Connection
- Local test: ftp localhost
- Remote test: ftp 202.164.153.160
- Login: stibe-deploy / StibeAPI2025!

### [ ] 10. Verify Services
- FTP Service running
- IIS running
- Firewall rules active

## Quick Commands for Testing

```cmd
# Test FTP service status
sc query ftpsvc

# Test local FTP connection
ftp localhost

# Test remote FTP connection (from another machine)
ftp 202.164.153.160

# Restart FTP service if needed
net stop ftpsvc
net start ftpsvc
```

## Verification Steps

1. **Service Check**: FTP service shows as "Running"
2. **Local Test**: Can connect to ftp://localhost
3. **Remote Test**: Can connect to ftp://202.164.153.160
4. **Authentication**: Can login with stibe-deploy credentials
5. **File Operations**: Can upload/download files to test directory
6. **Permissions**: Can write to C:\inetpub\wwwroot\test

## Common Troubleshooting

| Problem | Solution |
|---------|----------|
| Connection timeout | Check firewall rules for port 21 |
| Authentication failed | Verify user exists and password correct |
| Permission denied | Check folder permissions for stibe-deploy |
| Passive mode issues | Configure external IP and passive ports |

## After Setup Complete

1. **Test from local machine**: Use the test script provided
2. **Add to GitHub**: Configure FTP_USERNAME and FTP_PASSWORD secrets
3. **Deploy**: Push code to master branch
4. **Verify**: Check http://202.164.153.160:85/test/api/test/health

---
**Note**: Replace the default password with a more secure one before production use!
