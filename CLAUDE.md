# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application (development)
dotnet run --project ApiIntegracao/ApiIntegracao.csproj

# Run with hot reload
dotnet watch --project ApiIntegracao/ApiIntegracao.csproj
```

### Database Operations
```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Add new migration
dotnet ef migrations add MigrationName --project ApiIntegracao/ApiIntegracao.csproj

# Update database with latest migrations
dotnet ef database update --project ApiIntegracao/ApiIntegracao.csproj

# Generate SQL script for migrations
dotnet ef migrations script --project ApiIntegracao/ApiIntegracao.csproj --output migration.sql --idempotent
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture Overview

This is an **ASP.NET Core 9.0 Web API** that serves as an integration middleware between CETTPRO API and FAT Portal, providing:

### Core Functionality
- **Data Synchronization**: Bidirectional sync with CETTPRO API (Courses, Classes, Students)
- **Schedule Generation**: Automated class schedule creation with business rules
- **Attendance Processing**: CSV/XLSX file processing for student attendance
- **Background Services**: Automated sync operations running twice daily

### Project Structure
```
ApiIntegracao/
├── Controllers/           # REST API endpoints (8 controllers)
├── Models/               # Entity classes (Curso, Turma, Aluno, etc.)
├── Data/                 # EF Core DbContext and configurations
├── Services/            
│   ├── Contracts/       # Service interfaces
│   └── Implementations/ # Service implementations
├── BackgroundServices/  # Automated sync service
├── DTOs/               # Data transfer objects
├── Configuration/      # Settings classes
├── Extensions/         # Extension methods and DI setup
├── HealthChecks/       # Custom health check implementations
├── Infrastructure/     # Cross-cutting concerns
└── Middleware/         # Custom middleware
```

### Key Technologies
- **ASP.NET Core 9.0** with minimal APIs
- **Entity Framework Core 9.0** with MySQL (Pomelo provider)
- **Serilog** for structured logging
- **Background Services** for automated operations
- **JWT Authentication** for API security
- **Swagger/OpenAPI** for documentation
- **Health Checks** for monitoring

### Database Design
- **Soft Delete Pattern**: All entities support soft deletion with audit trails
- **Entity Configurations**: Fluent API configurations in `Data/Configurations/`
- **Migration Strategy**: EF Core migrations with automatic deployment via CI/CD

### External Integration
- **CETTPRO API**: Primary data source for courses, classes, and students
- **Azure MySQL**: Production database with SSL required
- **Azure App Service**: Hosting platform with automatic deployment

## Development Patterns

### Service Layer Architecture
- All business logic implemented in services under `Services/Implementations/`
- Interface contracts defined in `Services/Contracts/`
- Dependency injection configured in `Extensions/ServiceCollectionExtensions.cs`

### Background Processing
- `SyncBackgroundService.cs` handles automated synchronization
- Configurable intervals and retry logic via `SyncSettings`
- Comprehensive error handling and logging

### API Design
- RESTful endpoints with consistent response patterns
- API versioning (v1) implemented
- Comprehensive input validation and error handling
- Swagger documentation with authentication support

### Configuration Management
- Environment-specific settings in `appsettings.{Environment}.json`
- Secure secrets via Azure Key Vault in production
- Feature flags for conditional functionality

### Logging Strategy
- Structured logging with Serilog
- File-based logs with rotation (10MB limit, 30 days retention)
- Application Insights integration for production monitoring

## Environment Configuration

### Local Development
- Uses `appsettings.Development.json` for local settings
- Local MySQL instance or Azure development database
- Swagger UI enabled at `/swagger`
- Hot reload enabled for rapid development

### Production Deployment
- Automated via GitHub Actions (`.github/workflows/main_apifat.yml`)
- Database migrations run automatically during deployment
- Health checks at `/health` endpoints
- Application Insights telemetry enabled

## Security Considerations

### Authentication & Authorization
- JWT token-based authentication
- Secure password handling for CETTPRO API integration
- HTTPS enforcement in production
- CORS configuration for allowed origins

### Data Protection
- Sensitive configuration via Azure Key Vault
- SQL injection protection via EF Core parameterized queries
- Input validation and sanitization
- Connection string encryption

## Monitoring & Health Checks

### Available Health Check Endpoints
- `/health` - Overall application health
- `/health/database` - Database connectivity
- `/health/cettpro` - CETTPRO API connectivity

### Logging Locations
- Console output for development
- File logs in `logs/` directory (rotating daily)
- Application Insights for production monitoring