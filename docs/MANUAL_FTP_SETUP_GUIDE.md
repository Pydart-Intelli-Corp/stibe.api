# Manual FTP Setup Guide for Remote IIS Server
# Follow these steps on your remote server (202.164.153.160)

## Prerequisites
- Windows Server with IIS installed
- Administrator access to the remote server
- Access to Server Manager or Control Panel

## Step 1: Install FTP Server Role

### Option A: Using Server Manager (Recommended)
1. Open **Server Manager**
2. Click **Add roles and features**
3. Click **Next** until you reach **Server Roles**
4. Expand **Web Server (IIS)**
5. Expand **FTP Server**
6. Check the following:
   - ✅ **FTP Service**
   - ✅ **FTP Extensibility**
7. Click **Next** and **Install**

### Option B: Using Control Panel
1. Open **Control Panel** → **Programs** → **Turn Windows features on or off**
2. Expand **Internet Information Services**
3. Expand **FTP Server**
4. Check:
   - ✅ **FTP Service**
   - ✅ **FTP Extensibility**
5. Click **OK** and restart if prompted

## Step 2: Create FTP Site

1. Open **IIS Manager**
   - Press `Win + R`, type `inetmgr`, press Enter
2. In the left panel, expand your server name
3. Right-click **Sites** → **Add FTP Site**
4. Configure the FTP Site:
   - **Site name**: `StibeAPI-FTP`
   - **Physical path**: `C:\inetpub\wwwroot\test`
   - Click **Next**

## Step 3: Configure Binding and SSL

1. **Binding Information**:
   - **IP Address**: Select **All Unassigned**
   - **Port**: `21`
   - **Virtual Host**: Leave empty
2. **SSL Options**:
   - Select **No SSL** (for simplicity, can be changed later)
   - Click **Next**

## Step 4: Configure Authentication and Authorization

1. **Authentication**:
   - ✅ Check **Basic**
   - ❌ Uncheck **Anonymous**
2. **Authorization**:
   - **Allow access to**: Select **Specified users**
   - **Users**: Enter the username you'll create (e.g., `stibe-deploy`)
   - **Permissions**: Check both **Read** and **Write**
3. Click **Finish**

## Step 5: Create FTP User Account

1. Open **Computer Management**:
   - Right-click **This PC** → **Manage**
   - Or press `Win + R`, type `compmgmt.msc`, press Enter
2. Expand **Local Users and Groups** → **Users**
3. Right-click in the users area → **New User**
4. Configure the user:
   - **User name**: `stibe-deploy`
   - **Password**: `StibeAPI2025!` (or your chosen password)
   - ✅ Check **Password never expires**
   - ❌ Uncheck **User must change password at next logon**
   - Click **Create** → **Close**

## Step 6: Set Folder Permissions

1. Open File Explorer and navigate to `C:\inetpub\wwwroot\test`
2. Right-click the **test** folder → **Properties**
3. Go to **Security** tab → **Edit**
4. Click **Add** → **Advanced** → **Find Now**
5. Select **stibe-deploy** user → **OK** → **OK**
6. Select **stibe-deploy** and check **Full control**
7. Click **OK** → **OK**

## Step 7: Configure Windows Firewall

1. Open **Windows Firewall with Advanced Security**
   - Press `Win + R`, type `wf.msc`, press Enter
2. Click **Inbound Rules** → **New Rule**
3. Select **Port** → **Next**
4. Select **TCP** and **Specific local ports**: `21`
5. Select **Allow the connection** → **Next**
6. Check all profiles (Domain, Private, Public) → **Next**
7. Name: `FTP Server` → **Finish**

**Note**: You may also need to allow FTP data ports (typically 20 or passive range)

## Step 8: Test FTP Connection

### From the server itself:
1. Open Command Prompt
2. Type: `ftp localhost`
3. Login with:
   - Username: `stibe-deploy`
   - Password: `StibeAPI2025!`
4. Type `dir` to list files
5. Type `quit` to exit

### From your local machine:
1. Open Command Prompt or PowerShell
2. Type: `ftp 202.164.153.160`
3. Login with the same credentials
4. Test file operations

## Step 9: Configure FTP for Passive Mode (Important for GitHub Actions)

1. In **IIS Manager**, select your FTP site
2. Double-click **FTP Firewall Support**
3. Set **External IP Address of Firewall**: `202.164.153.160`
4. Set **Data Channel Port Range**: `5000-5100` (example range)
5. Click **Apply**

## Step 10: Add Firewall Rules for Passive Ports

1. In Windows Firewall, create another **Inbound Rule**
2. Select **Port** → **TCP** → **Specific local ports**: `5000-5100`
3. Allow the connection and apply to all profiles
4. Name: `FTP Passive Ports`

## Verification Checklist

✅ **FTP Server role installed**
✅ **FTP site created and configured**
✅ **User account created with proper permissions**
✅ **Folder permissions set**
✅ **Firewall rules configured**
✅ **FTP connection tested**

## Troubleshooting

### Common Issues:

1. **Connection Timeout**:
   - Check Windows Firewall settings
   - Verify FTP service is running
   - Check network connectivity

2. **Authentication Failed**:
   - Verify username and password
   - Check user account is enabled
   - Ensure Basic authentication is enabled

3. **Permission Denied**:
   - Check folder permissions for the FTP user
   - Verify user has write access to the target directory

4. **Passive Mode Issues**:
   - Configure external IP in FTP Firewall Support
   - Open passive port range in firewall
   - Check router/network configuration

## Security Recommendations

1. **Use Strong Passwords**: Change default password to something more secure
2. **Enable SSL**: Consider configuring FTP over SSL/TLS
3. **Limit IP Access**: Restrict FTP access to specific IP ranges if possible
4. **Monitor Logs**: Regularly check FTP logs for suspicious activity
5. **Regular Updates**: Keep Windows Server updated

## Next Steps

After completing this setup:
1. Test FTP connection from your local machine
2. Add FTP credentials to GitHub repository secrets
3. Push code to trigger automatic deployment
4. Monitor deployment in GitHub Actions

## Support Commands

```cmd
# Check FTP service status
sc query ftpsvc

# Restart FTP service
net stop ftpsvc
net start ftpsvc

# Check FTP site status in PowerShell
Import-Module WebAdministration
Get-WebSite -Name "StibeAPI-FTP"
```
