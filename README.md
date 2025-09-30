# Stibe API - Professional Architecture# 🏗️ Stibe.API - Professional Shop Management Backend



## 🏗️ Project Structure<div align="center">



This project follows a **Clean Architecture** pattern with clear separation of concerns and professional organization.![Stibe API](https://img.shields.io/badge/Stibe-API-blue.svg)

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple.svg)](https://dotnet.microsoft.com/)

```[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)](https://docs.microsoft.com/en-us/ef/)

stibe.api/[![MySQL](https://img.shields.io/badge/MySQL-8.0-orange.svg)](https://mysql.com/)

├── 📁 src/                              # Source code (core application)[![JWT](https://img.shields.io/badge/Auth-JWT-red.svg)](https://jwt.io/)

│   ├── 📁 Core/                         # Business logic & domain models

│   │   ├── 📁 Configuration/            # Application configurations**🌟 Enterprise-Grade RESTful API for Modern Shop Operations 🌟**

│   │   ├── 📁 DTOs/                     # Data Transfer Objects

│   │   ├── 📁 Entities/                 # Domain entities*Powering the Stibe One Flutter application with secure, scalable backend services*

│   │   ├── 📁 Enums/                    # Enumeration types

│   │   └── 📁 Interfaces/               # Service contracts**📅 Version:** 1.0.0 | **🔄 Last Updated:** August 15, 2025

│   ├── 📁 Infrastructure/               # External concerns & implementations

│   │   ├── 📁 Data/                     # Database context & migrations</div>

│   │   ├── 📁 External/                 # Third-party integrations

│   │   └── 📁 Services/                 # Service implementations---

│   ├── 📁 Application/                  # Application services & logic

│   │   ├── 📁 Services/                 # Business services## 🎯 **Project Overview**

│   │   └── 📁 Validators/               # Input validation logic

│   └── 📁 Web/                          # HTTP layer (controllers, middleware)Stibe.API is a comprehensive ASP.NET Core 8.0 RESTful API designed to power professional shop management operations. Built with enterprise-grade architecture, it provides secure, scalable backend services for the Stibe One Flutter application.

│       ├── 📁 Controllers/              # API controllers

│       ├── 📁 Extensions/               # Service registration extensions### ✨ **Core Features**

│       └── 📁 Middleware/               # Custom middleware- **🔐 JWT Authentication**: Secure user authentication with token refresh

├── 📁 config/                           # Configuration files- **👥 User Management**: Complete registration, profile management, and roles

│   ├── 📁 certificates/                 # SSL certificates & security files- **🏪 Shop Operations**: Multi-shop support with comprehensive business data

│   ├── 📁 environments/                 # Environment-specific configs- **👨‍💼 Staff Management**: Employee scheduling, profiles, and service assignments

│   ├── 📁 secrets/                      # Sensitive credentials (git-ignored)- **🛍️ Service Management**: Service catalog, pricing, and category organization

│   └── appsettings.json                 # Main application settings- **📧 Email Services**: Automated notifications and verification emails

├── 📁 tests/                            # Test projects- **🌐 Google OAuth**: Social authentication integration

│   ├── 📁 Unit/                         # Unit tests- **🔒 Security First**: Comprehensive validation, encryption, and access control

│   └── 📁 Integration/                  # Integration tests

├── 📁 scripts/                          # Utility scripts### 🏗️ **Architecture Highlights**

│   ├── 📁 database/                     # Database scripts & migrations- **Clean Architecture**: Domain-driven design with proper separation of concerns

│   └── 📁 deployment/                   # Deployment scripts- **Entity Framework Core**: MySQL database with comprehensive migrations

├── 📁 docker/                           # Docker configuration- **Dependency Injection**: Professional IoC container usage throughout

├── 📁 docs/                             # Documentation- **OpenAPI/Swagger**: Complete API documentation and testing interface

├── 📁 logs/                             # Application logs (git-ignored)- **Error Handling**: Comprehensive exception handling and logging

├── 📁 wwwroot/                          # Static web files- **Configuration**: Environment-based settings with secure credential management

└── 📄 Program.cs                        # Application entry point

```---



## 🎯 Architecture Benefits## 🚀 **Getting Started**



### ✅ Professional Organization### 📋 **Prerequisites**

- **Clear separation of concerns** between layers```bash

- **Clean Architecture** principles with dependency inversion.NET 8.0 SDK

- **Testable** structure with proper abstraction layersMySQL 8.0+

- **Maintainable** codebase with logical groupingsVisual Studio 2022 / VS Code

Git

### 🔒 Security & Configuration```

- **Sensitive files** isolated in `config/secrets/` (git-ignored)

- **Environment-specific** configurations in dedicated folders### ⚡ **Quick Setup**

- **Certificate management** in secure location```bash

- **Configuration hierarchy** for different environments# 1. Clone the repository

git clone https://github.com/Pydart-Intelli-Corp/stibe.api.git

### 🚀 Development Experiencecd stibe.api

- **Intuitive navigation** - developers can quickly find what they need

- **Consistent structure** across different concerns# 2. Restore dependencies

- **Scalable organization** - easy to add new featuresdotnet restore

- **Professional standards** following industry best practices

# 3. Configure database connection in appsettings.json

## 🔧 Layer Responsibilities# Update ConnectionStrings:DefaultConnection with your MySQL settings



### 🎯 Core Layer (`src/Core/`)# 4. Run database migrations

- **Domain entities** - Pure business objectsdotnet ef database update

- **DTOs** - Data contracts for API communication

- **Interfaces** - Service contracts and abstractions# 5. Run the application

- **Configuration** - Application settings modelsdotnet run

- **Enums** - Type-safe constants

# 6. Access API documentation

### 🏗️ Infrastructure Layer (`src/Infrastructure/`)https://localhost:7147/swagger

- **Data access** - Entity Framework context & migrations```

- **External services** - Third-party API integrations

- **Service implementations** - Concrete implementations of core interfaces### 🔧 **Configuration**

- **File storage** - Azure Blob, local file systemsUpdate `appsettings.json` with your settings:

```json

### 📋 Application Layer (`src/Application/`){

- **Business services** - Orchestrate domain logic  "ConnectionStrings": {

- **Validation logic** - Input validation and business rules    "DefaultConnection": "Server=localhost;Database=StibeDB;User=root;Password=your_password;"

- **Use cases** - Application-specific workflows  },

  "JwtSettings": {

### 🌐 Web Layer (`src/Web/`)    "Key": "your-super-secure-jwt-signing-key",

- **Controllers** - HTTP endpoints and request handling    "Issuer": "Stibe.API",

- **Middleware** - Cross-cutting concerns (logging, auth, etc.)    "Audience": "Stibe.Client",

- **Extensions** - Service registration and configuration    "ExpiryMinutes": 60

  },

## 🔄 Migration Guide  "EmailConfiguration": {

    "SmtpServer": "smtp.gmail.com",

The project has been reorganized from the previous flat structure. Key changes:    "SmtpPort": 587,

    "SenderEmail": "noreply@stibe.com",

1. **Models** → `src/Core/` (Entities, DTOs)    "Username": "your-email@gmail.com",

2. **Services** → `src/Infrastructure/Services/`    "Password": "your-app-password"

3. **Controllers** → `src/Web/Controllers/`  }

4. **Data** → `src/Infrastructure/Data/`}

5. **Configuration** → `src/Core/Configuration/````

6. **Sensitive files** → `config/secrets/`

---

## 🚀 Getting Started

## 📚 **Complete Documentation**

1. **Restore packages**: `dotnet restore`

2. **Update database**: `dotnet ef database update`### 📖 **Master Reference**

3. **Run application**: `dotnet run`**[COMPREHENSIVE_API_DOCUMENTATION.md](./COMPREHENSIVE_API_DOCUMENTATION.md)** - Complete technical documentation covering:

4. **Access Swagger**: `https://localhost:5001/swagger`- **Architecture & Project Structure** - Detailed system design and file organization

- **Authentication System** - JWT implementation, Google OAuth, and security features

## 📝 Environment Configuration- **Data Models & Entities** - Database schema, relationships, and DTOs

- **Controllers & Endpoints** - Complete API reference with examples

- **Development**: Uses `config/appsettings.json`- **Services & Business Logic** - Service layer architecture and implementations

- **Production**: Override via environment variables- **Configuration System** - Environment setup and feature flags

- **Secrets**: Store in `config/secrets/` (git-ignored)- **Database & Migrations** - Entity Framework setup and database management

- **Testing & Quality** - Unit testing, integration testing, and best practices

## 🔒 Security Notes

### 🚀 **Deployment & Setup**

- Never commit files in `config/secrets/`**[docs/DEPLOYMENT_GUIDE.md](./docs/DEPLOYMENT_GUIDE.md)** - Comprehensive deployment guide covering:

- Certificate files are now in `config/certificates/`- **Multiple Deployment Strategies** - FTP, GitHub Actions, Self-hosted runners, Web Deploy

- Sensitive credentials use environment variables in production- **IIS Configuration** - Complete server setup and configuration

- **Security Configuration** - Production security best practices

## 📊 Features- **Troubleshooting** - Common issues and diagnostic procedures

- **Local Development** - Development environment setup

- ✅ **Clean Architecture** implementation

- ✅ **JWT Authentication** with Google OAuth### 🔧 **Specialized Documentation**

- ✅ **Azure Blob Storage** integration**[OTP_SERVICE_DOCUMENTATION.md](./OTP_SERVICE_DOCUMENTATION.md)** - OTP service implementation and usage guide

- ✅ **Payment processing** with Razorpay

- ✅ **PDF generation** and receipts### 📊 **Documentation Quality**

- ✅ **Email notifications** (configurable mock/real)- **Complete Coverage**: Every controller, service, and entity documented

- ✅ **Location services** with Google Maps- **Current Information**: All content reflects actual implementation (v1.0.0)

- ✅ **Comprehensive logging** with Serilog- **Production Ready**: Enterprise-grade documentation standards

- ✅ **API documentation** with Swagger- **Practical Examples**: Real code samples and implementation patterns

- ✅ **Multi-environment** configuration support

---

## 🎯 Next Steps

## 🌐 **API Endpoints Overview**

1. Set up CI/CD pipelines using `scripts/deployment/`

2. Add comprehensive unit tests in `tests/Unit/`### 🔐 **Authentication**

3. Implement integration tests in `tests/Integration/````http

4. Configure Docker deployment using `docker/`POST /api/auth/login              # User login with JWT response

POST /api/auth/register           # New user registration

## 📚 Original DocumentationPOST /api/auth/forgot-password    # Password reset initiation

POST /api/auth/reset-password     # Password reset completion

For detailed API documentation, see the existing documentation files:POST /api/auth/refresh-token      # JWT token refresh

- `COMPREHENSIVE_API_DOCUMENTATION.md` - Complete API referencePOST /api/auth/google-auth        # Google OAuth authentication

- `docs/DEPLOYMENT_GUIDE.md` - Deployment instructionsGET  /api/auth/profile            # User profile retrieval

- `OTP_SERVICE_DOCUMENTATION.md` - OTP service documentationPUT  /api/auth/profile            # User profile updates
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