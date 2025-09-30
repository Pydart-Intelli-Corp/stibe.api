# Scripts Directory

This directory contains utility scripts for database management and deployment automation.

## 📁 database/
Database-related scripts and utilities:
- `ConvertPaymentStatus.sql` - Payment status conversion script
- Migration scripts and database maintenance tools

## 📁 deployment/
Deployment automation scripts:
- Build scripts
- Deploy scripts
- Environment setup scripts
- CI/CD pipeline scripts

## Usage

### Database Scripts
```bash
# Run SQL scripts against your database
mysql -u username -p database_name < database/script_name.sql
```

### Deployment Scripts
```bash
# Make scripts executable (Linux/Mac)
chmod +x deployment/deploy.sh

# Run deployment script
./deployment/deploy.sh
```

## Best Practices

1. **Version your scripts** - Keep track of which scripts have been applied
2. **Test scripts** - Always test on development/staging first
3. **Backup first** - Always backup before running database scripts
4. **Document changes** - Include comments explaining what each script does
5. **Rollback plans** - Have rollback scripts for critical changes