# 🏗️ Stibe.API - Professional Shop Management Backend

<div align="center">

![Stibe API](https://img.shields.io/badge/Stibe-API-blue.svg)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)](https://docs.microsoft.com/en-us/ef/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-orange.svg)](https://mysql.com/)
[![JWT](https://img.shields.io/badge/Auth-JWT-red.svg)](https://jwt.io/)

**🌟 Enterprise-Grade RESTful API for Modern Shop Operations 🌟**

*Powering the Stibe One Flutter application with secure, scalable backend services*

**📅 Version:** 1.0.0 | **🔄 Last Updated:** August 15, 2025

</div>

---

## 🎯 **Project Overview**

Stibe.API is a comprehensive ASP.NET Core 8.0 RESTful API designed to power professional shop management operations. Built with enterprise-grade architecture, it provides secure, scalable backend services for the Stibe One Flutter application.

### ✨ **Core Features**
- **🔐 JWT Authentication**: Secure user authentication with token refresh
- **👥 User Management**: Complete registration, profile management, and roles
- **🏪 Shop Operations**: Multi-shop support with comprehensive business data
- **👨‍💼 Staff Management**: Employee scheduling, profiles, and service assignments
- **🛍️ Service Management**: Service catalog, pricing, and category organization
- **📧 Email Services**: Automated notifications and verification emails
- **🌐 Google OAuth**: Social authentication integration
- **🔒 Security First**: Comprehensive validation, encryption, and access control

### 🏗️ **Architecture Highlights**
- **Clean Architecture**: Domain-driven design with proper separation of concerns
- **Entity Framework Core**: MySQL database with comprehensive migrations
- **Dependency Injection**: Professional IoC container usage throughout
- **OpenAPI/Swagger**: Complete API documentation and testing interface
- **Error Handling**: Comprehensive exception handling and logging
- **Configuration**: Environment-based settings with secure credential management

---

## 🚀 **Getting Started**

### 📋 **Prerequisites**
```bash
.NET 8.0 SDK
MySQL 8.0+
Visual Studio 2022 / VS Code
Git
```

### ⚡ **Quick Setup**
```bash
# 1. Clone the repository
git clone https://github.com/Pydart-Intelli-Corp/stibe.api.git
cd stibe.api

# 2. Restore dependencies
dotnet restore

# 3. Configure database connection in appsettings.json
# Update ConnectionStrings:DefaultConnection with your MySQL settings

# 4. Run database migrations
dotnet ef database update

# 5. Run the application
dotnet run

# 6. Access API documentation
https://localhost:7147/swagger
```

### 🔧 **Configuration**
Update `appsettings.json` with your settings:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StibeDB;User=root;Password=your_password;"
  },
  "JwtSettings": {
    "Key": "your-super-secure-jwt-signing-key",
    "Issuer": "Stibe.API",
    "Audience": "Stibe.Client",
    "ExpiryMinutes": 60
  },
  "EmailConfiguration": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@stibe.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

---

## 📚 **Complete Documentation**

### 📖 **Master Reference**
**[COMPREHENSIVE_API_DOCUMENTATION.md](./COMPREHENSIVE_API_DOCUMENTATION.md)** - Complete technical documentation covering:
- **Architecture & Project Structure** - Detailed system design and file organization
- **Authentication System** - JWT implementation, Google OAuth, and security features
- **Data Models & Entities** - Database schema, relationships, and DTOs
- **Controllers & Endpoints** - Complete API reference with examples
- **Services & Business Logic** - Service layer architecture and implementations
- **Configuration System** - Environment setup and feature flags
- **Database & Migrations** - Entity Framework setup and database management
- **Testing & Quality** - Unit testing, integration testing, and best practices

### 🚀 **Deployment & Setup**
**[docs/DEPLOYMENT_GUIDE.md](./docs/DEPLOYMENT_GUIDE.md)** - Comprehensive deployment guide covering:
- **Multiple Deployment Strategies** - FTP, GitHub Actions, Self-hosted runners, Web Deploy
- **IIS Configuration** - Complete server setup and configuration
- **Security Configuration** - Production security best practices
- **Troubleshooting** - Common issues and diagnostic procedures
- **Local Development** - Development environment setup

### 🔧 **Specialized Documentation**
**[OTP_SERVICE_DOCUMENTATION.md](./OTP_SERVICE_DOCUMENTATION.md)** - OTP service implementation and usage guide

### 📊 **Documentation Quality**
- **Complete Coverage**: Every controller, service, and entity documented
- **Current Information**: All content reflects actual implementation (v1.0.0)
- **Production Ready**: Enterprise-grade documentation standards
- **Practical Examples**: Real code samples and implementation patterns

---

## 🌐 **API Endpoints Overview**

### 🔐 **Authentication**
```http
POST /api/auth/login              # User login with JWT response
POST /api/auth/register           # New user registration
POST /api/auth/forgot-password    # Password reset initiation
POST /api/auth/reset-password     # Password reset completion
POST /api/auth/refresh-token      # JWT token refresh
POST /api/auth/google-auth        # Google OAuth authentication
GET  /api/auth/profile            # User profile retrieval
PUT  /api/auth/profile            # User profile updates
```

### 🏪 **Shop Management**
```http
GET    /api/shop                 # List user's shops
POST   /api/shop                 # Create new shop
GET    /api/shop/{id}            # Get shop details
PUT    /api/shop/{id}            # Update shop information
DELETE /api/shop/{id}            # Delete shop (soft delete)
GET    /api/shop/{id}/stats      # Shop analytics and statistics
```

### 👨‍💼 **Staff Management**
```http
GET    /api/staff                 # List shop staff
POST   /api/staff                 # Add new staff member
GET    /api/staff/{id}            # Get staff details
PUT    /api/staff/{id}            # Update staff information
DELETE /api/staff/{id}            # Remove staff member
GET    /api/staff/{id}/schedule   # Staff scheduling
```

### 🛍️ **Service Management**
```http
GET    /api/service               # List shop services
POST   /api/service               # Create new service
GET    /api/service/{id}          # Get service details
PUT    /api/service/{id}          # Update service information
DELETE /api/service/{id}          # Delete service
POST   /api/service/{id}/upload   # Upload service images
```

---

## 🏗️ **Project Structure**

```
Stibe.API/
├── Program.cs                    # Application entry point
├── appsettings.json             # Configuration settings
├── Controllers/                 # API endpoints
│   ├── AuthController.cs        # Authentication endpoints
│   ├── ShopController.cs       # Shop management
│   ├── StaffController.cs       # Staff operations
│   ├── ServiceController.cs     # Service management
│   └── TestController.cs        # Health checks
├── Data/                        # Database context
│   ├── ApplicationDbContext.cs  # EF Core context
│   └── ApplicationDbContextFactory.cs
├── Models/                      # Data models
│   ├── DTOs/                   # Data transfer objects
│   └── Entities/               # Database entities
├── Services/                    # Business logic
│   ├── Implementations/        # Service implementations
│   └── Interfaces/             # Service contracts
├── Configuration/              # Settings classes
├── Migrations/                 # EF Core migrations
└── wwwroot/                   # Static files & uploads
```

---

## 🔧 **Technology Stack**

### **Backend Framework**
- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core** - ORM with MySQL support
- **MySQL 8.0** - Primary database

### **Authentication & Security**
- **JWT Bearer Tokens** - Stateless authentication
- **Google OAuth 2.0** - Social login integration
- **BCrypt** - Password hashing
- **Data Protection** - Secure credential storage

### **Communication & Documentation**
- **OpenAPI/Swagger** - API documentation
- **MailKit** - Email services
- **Humanizer** - Data formatting

### **Development Tools**
- **Microsoft.CodeAnalysis** - Code analysis
- **Entity Framework Tools** - Migrations and scaffolding

---

## 🐳 **Deployment**

### **Docker Support**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "stibe.api.dll"]
```

### **Production Environment**
- **Database**: MySQL 8.0+ with proper connection pooling
- **Security**: HTTPS enforcement, secure headers
- **Monitoring**: Structured logging with Serilog (recommended)
- **Caching**: Redis integration ready
- **Load Balancing**: Multiple instance support

---

## 📱 **Flutter Integration**

This API is specifically designed to work with the **Stibe One Flutter application**. Key integration features:

- **Consistent Response Format**: Standardized JSON responses
- **Error Handling**: Detailed error messages for client handling
- **Authentication Flow**: Complete JWT token management
- **File Uploads**: Image handling for shops, staff, and services
- **Real-time Ready**: WebSocket support preparation
- **Offline Sync**: Data synchronization patterns

---

## 🧪 **Testing**

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Test specific project
dotnet test Tests/Stibe.API.Tests.csproj
```

---

## 🤝 **Contributing**

We welcome contributions that maintain our professional standards:

1. **Follow Clean Architecture** principles
2. **Add comprehensive tests** for new features
3. **Update documentation** for API changes
4. **Follow C# coding conventions**
5. **Ensure security best practices**

---

## 📞 **Support & Resources**

- **📚 Complete Technical Documentation**: [COMPREHENSIVE_API_DOCUMENTATION.md](./COMPREHENSIVE_API_DOCUMENTATION.md)
- **� Deployment & Setup Guide**: [docs/DEPLOYMENT_GUIDE.md](./docs/DEPLOYMENT_GUIDE.md)
- **�🔧 OTP Service Guide**: [OTP_SERVICE_DOCUMENTATION.md](./OTP_SERVICE_DOCUMENTATION.md)
- **🔬 API Testing**: Swagger UI at `/swagger` endpoint
- **🐛 Issues**: GitHub Issues for bug reports
- **💼 Enterprise Support**: Available for production deployments

---

## 🛣️ **Roadmap**

### **Current Version (1.0.0)**
- ✅ Complete Authentication System
- ✅ Shop Management APIs
- ✅ Staff Management
- ✅ Service Management
- ✅ Email Services
- ✅ Google OAuth Integration

### **Upcoming Features**
- 🔄 Advanced Analytics APIs
- 🔄 Real-time Notifications
- 🔄 Payment Processing Integration
- 🔄 Advanced Reporting
- 🔄 Multi-tenant Support
- 🔄 WebSocket Integration

---

<div align="center">

**Built with ❤️ using ASP.NET Core 8.0**

*Enterprise-grade backend for modern shop management*

**Stibe.API - Powering Professional Shop Operations**

**📧 Contact**: [support@stibe.com](mailto:support@stibe.com)  
**🌐 Website**: [https://stibe.com](https://stibe.com)  
**📖 Docs**: [API Documentation](./COMPREHENSIVE_API_DOCUMENTATION.md)

</div>