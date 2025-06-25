# MySQL Database Setup Instructions

## 1. Make sure MySQL is running
- Start MySQL service from Windows Services or MySQL Workbench
- Default port should be 3306

## 2. Update database connection
The application is configured to use:
- Database: stibe_db
- User: root
- Password: 2232
- Port: 3306

## 3. Create database manually if needed:
```sql
CREATE DATABASE IF NOT EXISTS stibe_db;
```

## 4. The application will automatically create tables on first run in Development mode
