# FTP Error 530 Troubleshooting Guide
# "User cannot log in, home directory inaccessible"

## Error Analysis
The FTP error 530 typically means:
- ✅ FTP server is reachable (connection established)
- ✅ Port 92 is accessible
- ❌ User authentication failed OR home directory issues
- ❌ User permissions not configured correctly

## Common Causes & Solutions

### 1. Home Directory Configuration

#### Problem: FTP user has no proper home directory
#### Solution: Configure user's home directory in IIS

**Steps to fix:**
1. Open **IIS Manager**
2. Select your FTP site **"StibeAPI-FTP"**
3. Double-click **"FTP Authorization Rules"**
4. Check if user "test" has proper authorization

### 2. User Account Configuration

#### Problem: Windows user account issues
#### Solution: Verify user account settings

**Check these in Computer Management:**
```
Computer Management → Local Users and Groups → Users → test
- Account is enabled ✅
- Password never expires ✅
- User cannot change password ✅
- Account is not locked out ✅
```

### 3. FTP Authorization Rules

#### Problem: User not authorized for FTP access
#### Solution: Add proper authorization

**IIS Manager Steps:**
1. Select FTP Site → **FTP Authorization Rules**
2. Should have rule for user "test" with Read+Write permissions
3. If missing, click **Add Allow Rule**:
   - Allow access to: **Specified users**
   - User: `test`
   - Permissions: **Read** ✅ **Write** ✅

### 4. FTP Home Directory Settings

#### Problem: Virtual directory not configured
#### Solution: Check FTP home directory

**IIS Manager Steps:**
1. Select FTP Site → **Advanced Settings**
2. Check **Physical Path**: Should be `C:\inetpub\wwwroot\test`
3. Verify directory exists and has correct permissions

### 5. Directory Permissions

#### Problem: User lacks filesystem permissions
#### Solution: Set proper NTFS permissions

**Windows Explorer Steps:**
1. Navigate to `C:\inetpub\wwwroot\test`
2. Right-click → **Properties** → **Security**
3. Ensure user "test" has:
   - **Full Control** ✅ or at minimum:
   - **Read & Execute** ✅
   - **Write** ✅
   - **Modify** ✅

## Quick Diagnostic Commands

Run these on your remote server to diagnose:

### Check User Account
```cmd
net user test
```

### Check FTP Service Status
```cmd
sc query ftpsvc
```

### Test Local FTP Connection
```cmd
ftp localhost 92
# Try logging in with test/Access$404
```

### Check Directory Permissions
```powershell
Get-Acl "C:\inetpub\wwwroot\test" | Format-Table -Wrap
```

## Step-by-Step Fix Procedure

### Step 1: Verify User Account
1. Open **Computer Management** (compmgmt.msc)
2. Go to **Local Users and Groups** → **Users**
3. Find user **"test"**
4. Right-click → **Properties**
5. Ensure:
   - **Account is disabled**: ❌ (unchecked)
   - **Password never expires**: ✅ (checked)
   - **User cannot change password**: ✅ (checked)

### Step 2: Reset User Password
1. Right-click user **"test"** → **Set Password**
2. Set password to: `Access$404`
3. Click **OK**

### Step 3: Check FTP Site Configuration
1. Open **IIS Manager**
2. Select your FTP site
3. Double-click **FTP Authorization Rules**
4. Verify rule exists for user "test"
5. If not, click **Add Allow Rule**:
   - Specified users: `test`
   - Permissions: Read ✅ Write ✅

### Step 4: Verify Directory Permissions
1. Navigate to `C:\inetpub\wwwroot\test`
2. Right-click → **Properties** → **Security**
3. Click **Edit** → **Add**
4. Add user "test" with **Full Control**

### Step 5: Test FTP Connection
```cmd
ftp 202.164.153.160 92
# Username: test
# Password: Access$404
```

## Alternative Solutions

### Option 1: Use Different FTP User
Create a new FTP user with proper setup:

```cmd
# Create new user
net user ftpdeploy Access$404 /add
net localgroup "IIS_IUSRS" ftpdeploy /add

# Update GitHub secrets to use:
# FTP_USERNAME: ftpdeploy
# FTP_PASSWORD: Access$404
```

### Option 2: Use Built-in IIS User
Use IIS application pool identity:

**IIS Manager:**
1. FTP Authorization Rules
2. Add Allow Rule
3. Select: **All users** or **All anonymous users**

### Option 3: Switch to SFTP/Web Deploy
Consider using Web Deploy instead of FTP for better security and reliability.

## Testing Your Fix

After implementing the fix:

### Test 1: Local FTP Test
```cmd
ftp localhost 92
# Should connect successfully with test/Access$404
```

### Test 2: Remote FTP Test
```cmd
ftp 202.164.153.160 92
# Should connect from external machine
```

### Test 3: GitHub Actions Test
Push a small change to trigger deployment:
```bash
git add .
git commit -m "Test FTP fix for error 530"
git push origin master
```

## Most Likely Fix

Based on the error, try this first:

1. **Reset the user password** in Computer Management
2. **Add explicit FTP authorization** in IIS Manager
3. **Set Full Control permissions** on the target directory

This should resolve the "home directory inaccessible" error.
