# Smart Medi - Healthcare Management System Backend

![.NET 9](https://img.shields.io/badge/.NET-9-512BD4?style=flat&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9-512BD4?style=flat&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green)
![Repository](https://img.shields.io/badge/GitHub-Smart--Medc-181717?style=flat&logo=github)

## Overview

**Smart Medi** is a comprehensive healthcare management system backend that bridges patients and healthcare organizations through intelligent appointment scheduling, medical record management, real-time communications, and advanced data sharing capabilities. Built with cutting-edge .NET 9 technology, it provides a robust, scalable, and secure solution for modern healthcare delivery.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [API Documentation](#api-documentation)
- [Key Features Deep Dive](#key-features-deep-dive)
- [Authentication & Authorization](#authentication--authorization)
- [Background Jobs](#background-jobs)
- [Real-time Communication](#real-time-communication)
- [Contributing](#contributing)
- [Support](#support)

## Features

### 👤 User Management
- **Multi-role Support**: Patient, Doctor, Organization Admin, and System Admin roles
- **Advanced Authentication**: JWT with refresh tokens, Two-Factor Authentication (2FA) via TOTP, and email verification
- **Secure Password Management**: Password reset with OTP verification, secure password change functionality

### 📅 Appointment System
- **Smart Scheduling**: View organization availability, book appointments with intelligent slot management
- **Reschedule & Cancel**: Full control over appointment lifecycle with automatic reminders
- **Status Tracking**: Real-time appointment status updates (Pending → Confirmed → In Progress → Completed)
- **Appointment History**: Comprehensive audit trail of all status changes with reasons
- **Multi-doctor Support**: Support for doctor assignments and specializations

### 📋 Medical Records Management
- **HIPAA-Compliant Storage**: Secure storage of medical records with multiple document types
- **Advanced Querying**: Filter by type, date range, and patient health conditions
- **Audit Trail**: Complete tracking of all medical record access and modifications
- **PDF Export**: Generate professional PDF exports of medical records
- **Document Versioning**: Track multiple versions of medical records

### 🔐 Data Sharing & Privacy
- **Share Codes**: Generate time-limited, one-time use codes to securely share medical data
- **Access Control**: Granular control over which records can be shared
- **Access Logging**: Complete audit trail of all data access events
- **Expiration Policies**: Support for both time-based and count-based sharing limits
- **Selective Sharing**: Choose specific records or categories to share

### 💊 Medication Management
- **Medication Tracking**: Comprehensive medication profiles with dosage and frequency
- **Smart Reminders**: Multi-channel reminders (email, SMS, in-app) for medication adherence
- **Adherence Logging**: Track medication compliance with detailed historical records
- **Interaction Detection**: Identify potential drug interactions
- **Status Management**: Track medication active status with prescribed dates

### 📔 Health Journal
- **Personal Health Journal**: Patients can document health observations and symptoms
- **Photo Attachments**: Support for photo documentation of health conditions
- **Tagging System**: Organize journal entries with custom tags
- **Date Range Filtering**: Query historical entries with flexible date ranges
- **Statistics**: Generate insights on journal activity and health trends

### 🤖 AI Chat Integration
- **Intelligent Assistant**: AI-powered health advice and information assistant
- **Session Management**: Maintain conversation context across multiple sessions
- **Document Attachments**: Share medical documents in conversations
- **Message History**: Complete conversation history for reference

### 🔔 Notifications System
- **Real-time Notifications**: WebSocket-based real-time push notifications via SignalR
- **Multi-channel Delivery**: Email, in-app, and SMS notifications
- **User Preferences**: Customizable notification preferences per user
- **Scheduled Reminders**: Appointment and medication reminders
- **Notification History**: Track all sent notifications

### 🏥 Organization Management
- **Profile Management**: Comprehensive organization profiles with specializations
- **Operating Hours**: Define working hours with exceptions and holidays
- **Availability Slots**: Configure appointment slots with custom durations
- **Consultation Fees**: Support for multiple consultation fee structures
- **Document Verification**: Track and verify organization documents (licenses, certifications)
- **Photo Gallery**: Manage organization photos and branding

### 👨‍💼 Admin Dashboard
- **Organization Approval**: Review and approve healthcare organization registrations
- **User Management**: Monitor patient and organization accounts
- **System Statistics**: Dashboard with key metrics and analytics
- **Admin Controls**: System-wide settings and management capabilities

## Architecture

The backend follows a **Clean Architecture** pattern with clear separation of concerns across multiple layers:

### Architecture Layers

```
┌─────────────────────────────────────┐
│     Smart-Medc.API (Presentation)   │
│  Controllers, Hubs, Filters, DTOs   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Smart-Medc.Application (Business) │
│ Services, Mappings, DTOs, Interfaces│
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│Smart-Medc.Infrastructure (Persistence)│
│ EF Core, Repositories, DbContext    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Smart-Medc.Domain (Core Logic)    │
│   Entities, Enums, Interfaces       │
└─────────────────────────────────────┘
```

### Design Patterns

- **Repository Pattern**: Data access abstraction with Unit of Work pattern
- **Service Layer**: Business logic encapsulation
- **DTO Pattern**: Data transfer between layers with AutoMapper
- **Dependency Injection**: Loose coupling through DI container
- **Authorization Policies**: Custom resource owner and organization verification handlers

## Technology Stack

### Core Framework
- **.NET 9** - Latest .NET runtime with performance improvements
- **ASP.NET Core 9** - Modern web API framework
- **Entity Framework Core 9** - ORM for database operations
- **SQL Server** - Primary database solution

### Libraries & Frameworks

| Category | Technologies |
|----------|---|
| **API** | ASP.NET Core REST APIs, SignalR (real-time communication) |
| **Authentication** | JWT, Identity Framework, Google Authenticator (TOTP), OTP |
| **Mapping** | AutoMapper (DTO mapping) |
| **Data Access** | Entity Framework Core, Unit of Work, Repository Pattern |
| **Background Jobs** | Hangfire (job scheduling and execution) |
| **File Storage** | Cloudflare R2, Local storage with abstraction |
| **Export** | iText Sharp (PDF generation) |
| **Email** | SMTP (Gmail), SendGrid integration |
| **Validation** | FluentValidation, Custom validators |
| **Logging** | Serilog, Application Insights |
| **Health Checks** | AspNetCore.Diagnostics.HealthChecks |
| **Caching** | Distributed caching support |
| **API Documentation** | Swagger/OpenAPI |

## Project Structure

### Smart-Medc.Domain
Core business logic and domain entities layer.

```
Smart-Medc.Domain/
├── Entities/                    # Domain models
│   ├── Identity/               # User entities
│   ├── PatientModels/          # Patient-related entities
│   ├── OrganizationModels/     # Organization-related entities
│   ├── AppointmentModels/      # Appointment entities
│   ├── DataSharing/            # Data sharing entities
│   ├── AI/                     # AI chat entities
│   └── Notification/           # Notification entities
├── Enums/                      # Domain enumerations
├── Interfaces/
│   ├── Repositories/           # Repository contracts
│   └── Services/               # Service contracts
└── Services/                   # Domain services
```

### Smart-Medc.Infrastructure
Persistence and external services layer.

```
Smart-Medc.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs         # EF Core context
│   ├── Migrations/             # Database migrations
│   ├── Repositories/           # Repository implementations
│   ├── Seeders/                # Database seed data
│   └── UnitOfWork.cs          # Unit of Work pattern
├── ServiceCollectionExtension/ # DI registration
└── obj/                        # Build artifacts
```

### Smart-Medc.Application
Business logic and application services layer.

```
Smart-Medc.Application/
├── DTOs/                       # Data Transfer Objects
│   ├── Auth/                   # Authentication DTOs
│   ├── Appointment/            # Appointment DTOs
│   ├── Patient/                # Patient DTOs
│   ├── Organization/           # Organization DTOs
│   ├── MedicalRecord/          # Medical record DTOs
│   ├── Medications/            # Medication DTOs
│   ├── Journal/                # Journal DTOs
│   ├── DataSharing/            # Data sharing DTOs
│   ├── AI/                     # AI chat DTOs
│   ├── Notifications/          # Notification DTOs
│   ├── Admin/                  # Admin DTOs
│   └── MedicalRecord/          # Record DTOs
├── Services/                   # Business logic services
│   ├── Auth/                   # Authentication services
│   ├── Patient/                # Patient services
│   ├── Organization/           # Organization services
│   ├── Notifications/          # Notification services
│   ├── DataSharing/            # Data sharing services
│   ├── AI/                     # AI chat services
│   ├── Admin/                  # Admin services
│   └── Storage/                # File storage services
├── Mappings/                   # AutoMapper profiles
├── Interfaces/                 # Service contracts
├── BackgroundJobs/             # Hangfire background jobs
├── Configuration/              # Settings models
├── Common/
│   ├── Constants/              # App-wide constants
│   ├── Auth/                   # Authorization handlers
│   ├── Results/                # Result patterns
│   └── Helpers/                # Utility functions
└── ServiceCollectionExtension/ # DI registration
```

### Smart-Medc.API
REST API and presentation layer.

```
Smart-Medc.API/
├── Controllers/                # API endpoints
│   ├── AuthController          # Authentication endpoints
│   ├── AppointmentsController  # Appointment endpoints
│   ├── PatientProfileController# Patient profile endpoints
│   ├── MedicalRecordsController# Medical records endpoints
│   ├── OrganizationsController # Organization endpoints
│   ├── DataSharingController   # Data sharing endpoints
│   ├── MedicationsController   # Medication endpoints
│   ├── JournalController       # Journal endpoints
│   ├── AIChatController        # AI chat endpoints
│   ├── PatientNotifications    # Patient notifications
│   ├── OrganizationNotifications# Org notifications
│   ├── AdminController         # Admin endpoints
│   └── SharedDataAccessController# Data access endpoints
├── Hubs/                       # SignalR hubs
│   ├── NotificationHub         # Notification hub
│   ├── ChatHub                 # Chat hub
│   ├── ChatHubDispatcher       # Hub message dispatcher
│   └── NotificationHubPusher   # Notification pusher
├── Filters/                    # Custom filters
│   └── GlobalValidationFilter  # Global validation
├── ServiceCollectionExtension/ # API service registration
├── Program.cs                  # Application startup
├── appsettings.json            # Configuration
└── obj/                        # Build artifacts
```

## Getting Started

### Prerequisites

- **.NET 9 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **SQL Server 2019** or later (or compatible version)
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Git** for version control

### Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Smart-Medc/Smart-Medi-Backend.git
   cd Smart-Medi-Backend
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Update Configuration**
   ```bash
   # Update Smart-Medc.API/appsettings.json with your settings
   # See Configuration section below
   ```

4. **Apply Migrations**
   ```bash
   dotnet ef database update --project Smart-Medc.Infrastructure
   ```

5. **Build the Solution**
   ```bash
   dotnet build
   ```

6. **Run the Application**
   ```bash
   cd Smart-Medc.API
   dotnet run
   ```

The API will be available at `https://localhost:7000` (or the configured port).

## Configuration

### appsettings.json

Configure the following sections in `Smart-Medc.API/appsettings.json`:

#### Database Connection
```json
{
  "ConnectionStrings": {
    "cs": "Server=YOUR_SERVER;Database=SmartMedi;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=False;MultipleActiveResultSets=True;"
  }
}
```

#### JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "your-secure-secret-key-min-32-characters",
    "Issuer": "SmartMedicAPI",
    "Audience": "SmartMedicClient",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

#### Email Configuration
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Smart Medi",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-specific-password",
    "EnableSsl": true
  }
}
```

#### Cloudflare R2 (Optional, for file storage)
```json
{
  "CloudflareR2": {
    "AccountId": "YOUR_ACCOUNT_ID",
    "AccessKeyId": "YOUR_ACCESS_KEY",
    "SecretAccessKey": "YOUR_SECRET_KEY",
    "BucketName": "your-bucket",
    "PublicUrl": "https://your-bucket.r2.cloudflarecontent.com"
  }
}
```

#### AI Service Configuration
```json
{
  "AIService": {
    "ApiKey": "your-ai-api-key",
    "Endpoint": "https://your-ai-endpoint.com"
  }
}
```

#### Admin Settings
```json
{
  "AdminSettings": {
    "AdminEmails": [
      "admin1@example.com",
      "admin2@example.com"
    ]
  }
}
```

### Environment Variables

For production, use environment variables instead of hardcoding sensitive values:

```bash
# Linux/Mac
export ConnectionStrings__cs="your-connection-string"
export JwtSettings__SecretKey="your-secret-key"
export EmailSettings__SmtpPassword="your-password"

# Windows PowerShell
$env:ConnectionStrings__cs="your-connection-string"
$env:JwtSettings__SecretKey="your-secret-key"
$env:EmailSettings__SmtpPassword="your-password"
```

## Database Setup

### Migrations

The project uses Entity Framework Core migrations for database management.

#### Create a New Migration
```bash
cd Smart-Medc.Infrastructure
dotnet ef migrations add "DescriptionOfChange" --startup-project ../Smart-Medc.API
```

#### Apply Pending Migrations
```bash
dotnet ef database update --startup-project ../Smart-Medc.API
```

#### View Migration Status
```bash
dotnet ef migrations list --startup-project ../Smart-Medc.API
```

#### Revert Last Migration
```bash
dotnet ef migrations remove --startup-project ../Smart-Medc.API
```

### Database Schema

Key entities include:
- **ApplicationUser** - Base user entity with identity
- **Patient** - Patient profiles linked to users
- **Organization** - Healthcare organizations
- **Doctor** - Doctors within organizations
- **Appointment** - Appointment bookings and management
- **MedicalRecord** - Patient medical records
- **Medication** - Patient medications
- **JournalEntry** - Patient health journal entries
- **DataShareCode** - Secure data sharing codes
- **AIChatSession** - AI chat conversation sessions
- **Notification** - System notifications (Patient & Organization)

## API Documentation

### Swagger/OpenAPI

API documentation is automatically generated and available at:
```
https://localhost:7000/swagger
```

Interactive API testing is available through the Swagger UI.

### Base URL
```
https://localhost:7000/api
```

### Authentication

Most endpoints require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Key API Endpoints

#### Authentication
- `POST /api/auth/register-patient` - Patient registration
- `POST /api/auth/register-organization` - Organization registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/verify-email` - Verify email
- `POST /api/auth/forgot-password` - Initiate password reset
- `POST /api/auth/reset-password` - Complete password reset
- `POST /api/auth/enable-2fa` - Enable two-factor authentication
- `POST /api/auth/verify-2fa-login` - Verify 2FA during login

#### Appointments
- `GET /api/appointments` - List user appointments
- `GET /api/appointments/{id}` - Get appointment details
- `POST /api/appointments` - Book appointment
- `PATCH /api/appointments/{id}/confirm` - Confirm appointment
- `PATCH /api/appointments/{id}/reschedule` - Reschedule appointment
- `PATCH /api/appointments/{id}/cancel` - Cancel appointment
- `PATCH /api/appointments/{id}/complete` - Mark appointment complete
- `GET /api/appointments/{id}/history` - Get appointment status history

#### Medical Records
- `GET /api/medical-records` - List medical records
- `GET /api/medical-records/{id}` - Get record details
- `POST /api/medical-records` - Create medical record
- `PUT /api/medical-records/{id}` - Update medical record
- `DELETE /api/medical-records/{id}` - Delete medical record
- `GET /api/medical-records/{id}/export-pdf` - Export as PDF

#### Medications
- `GET /api/medications` - List medications
- `POST /api/medications` - Add medication
- `PUT /api/medications/{id}` - Update medication
- `DELETE /api/medications/{id}` - Remove medication
- `POST /api/medications/{id}/reminders` - Create reminder
- `POST /api/medications/{id}/log-adherence` - Log medication adherence

#### Data Sharing
- `POST /api/data-sharing/generate-code` - Generate share code
- `POST /api/data-sharing/validate-code` - Validate share code
- `GET /api/data-sharing/shared-records` - Get shared records
- `GET /api/data-sharing/access-history` - View access logs

#### Organizations
- `GET /api/organizations/search` - Search organizations
- `GET /api/organizations/{id}` - Get organization details
- `GET /api/organizations/{id}/availability` - Get availability slots
- `GET /api/organizations/{id}/operating-hours` - Get operating hours

#### Health Journal
- `GET /api/journal` - List journal entries
- `POST /api/journal` - Create journal entry
- `PUT /api/journal/{id}` - Update journal entry
- `DELETE /api/journal/{id}` - Delete journal entry
- `GET /api/journal/statistics` - Get journal statistics

#### AI Chat
- `POST /api/ai-chat/sessions` - Create chat session
- `POST /api/ai-chat/send-message` - Send message
- `GET /api/ai-chat/sessions/{id}` - Get session history
- `GET /api/ai-chat/sessions/{id}/messages` - List messages

#### Notifications
- `GET /api/notifications` - List notifications
- `PATCH /api/notifications/{id}/read` - Mark as read
- `PUT /api/notifications/preferences` - Update preferences
- `GET /api/notifications/preferences` - Get notification preferences

#### Admin
- `GET /api/admin/organizations/pending` - List pending organizations
- `POST /api/admin/organizations/{id}/approve` - Approve organization
- `POST /api/admin/organizations/{id}/reject` - Reject organization
- `GET /api/admin/dashboard/stats` - Get dashboard statistics

## Key Features Deep Dive

### Appointment Workflow

```
Patient Search Organization
    ↓
View Available Slots
    ↓
Book Appointment
    ↓
Pending Status
    ↓
Organization Confirms/Rejects
    ↓
Confirmed → In Progress → Completed
                ↓
        Save Completion Notes
                ↓
        Notification to Patient
```

### Medical Records Management

Secure storage with multiple types:
- **Lab Results** - Laboratory test results
- **Prescriptions** - Doctor prescriptions
- **Diagnosis** - Diagnosis documentation
- **Imaging** - Medical imaging reports
- **Vaccination** - Vaccination records
- **Allergies** - Allergy documentation
- **Surgery** - Surgical procedure records
- **Other** - General medical documents

### Data Sharing Security

1. **Generate Code**: Patient generates a unique share code
2. **Set Permissions**: Select which records to share
3. **Provide Code**: Share code with authorized recipient
4. **Validate Code**: Recipient validates code to access records
5. **Access Logged**: All access attempts are audited
6. **Auto Expiry**: Codes can expire after time or usage limit

### Medication Adherence Tracking

- **Reminder System**: Multi-channel reminders (email, SMS, push)
- **Adherence Logging**: Log when medications are taken
- **Compliance Reports**: Track adherence over time
- **Health Insights**: Monitor medication compliance patterns

## Authentication & Authorization

### JWT-Based Authentication

- **Access Token**: Short-lived token (default 60 minutes) for API requests
- **Refresh Token**: Long-lived token (default 7 days) for obtaining new access tokens
- **Token Claims**: Include user ID, roles, and custom claims

### Two-Factor Authentication (2FA)

- **TOTP Support**: Time-based One-Time Password using Google Authenticator
- **Backup Codes**: Generated codes for account recovery
- **Login Flow**: Email + OTP, then 2FA verification if enabled

### Authorization Policies

Custom authorization handlers:

```csharp
// Resource Owner Policy
// Patients can only access their own data
[Authorize(Policy = "ResourceOwner")]

// Verified Organization Policy
// Only verified organizations can perform certain operations
[Authorize(Policy = "VerifiedOrganization")]

// Role-Based Access
[Authorize(Roles = "Admin,OrganizationAdmin")]
```

## Background Jobs

Hangfire-powered background job scheduling:

### AppointmentReminderJob
- **Schedule**: Runs before appointment time
- **Action**: Sends reminder notifications to patients
- **Frequency**: Configurable (default 24 hours before)

### MedicationReminderJob
- **Schedule**: Based on medication schedule
- **Action**: Sends medication adherence reminders
- **Frequency**: Multiple times daily as configured

### AutoRejectAppointmentJob
- **Schedule**: After confirmation deadline
- **Action**: Auto-rejects unconfirmed appointments
- **Frequency**: Configured by organization

### NotificationCleanupJob
- **Schedule**: Daily cleanup
- **Action**: Removes old notifications based on retention policy
- **Frequency**: Daily at specified time

### Job Management

Access Hangfire dashboard at:
```
https://localhost:7000/hangfire
```

## Real-time Communication

### SignalR Hubs

#### NotificationHub
Push real-time notifications to clients:
```csharp
// Client receives notification
public async Task ReceiveNotification(PatientNotificationDto notification)
{
    // Handle notification
}
```

#### ChatHub
Real-time messaging for chat:
```csharp
// Send message
await hubConnection.InvokeAsync("SendMessage", message);

// Receive message
hubConnection.On<ChatMessageDto>("ReceiveMessage", (message) =>
{
    // Handle incoming message
});
```

### Connection Management
- Automatic connection handling
- Graceful disconnection
- Connection state tracking
- User-specific message routing

## Logging & Monitoring

### Serilog Integration
Structured logging with multiple sinks:
- **Console**: Development debugging
- **File**: Production logging
- **Application Insights**: Cloud monitoring (optional)

### Health Checks
Monitor application health at:
```
https://localhost:7000/health
```

Includes checks for:
- Database connectivity
- Memory usage
- CPU usage
- Custom application health

## Error Handling

### Global Exception Handling
- **GlobalValidationFilter**: Validates requests and formats responses
- **Exception Middleware**: Catches unhandled exceptions
- **Detailed Error Responses**: Helpful error messages with status codes

### Standard Response Format
```json
{
  "success": true/false,
  "message": "Operation result message",
  "data": {
    // Response data
  },
  "errors": [
    {
      "field": "fieldName",
      "message": "Error message"
    }
  ]
}
```

## Performance Considerations

### Caching
- Distributed caching for frequently accessed data
- Cache invalidation strategies
- User-specific cache keys

### Query Optimization
- EF Core eager loading to prevent N+1 queries
- Pagination for large result sets
- Indexed database columns

### Async Operations
- Async/await throughout for non-blocking operations
- Async file uploads to external storage
- Async email sending via background jobs

## Security Best Practices

✅ **Implemented:**
- JWT tokens with expiration
- Password hashing with bcrypt
- HTTPS enforcement
- CORS configuration
- Input validation and sanitization
- SQL injection prevention via EF Core
- HIPAA-compliant data handling
- Secure password reset with OTP
- Two-Factor Authentication (2FA)
- Data access audit logging

⚠️ **Recommendations:**
- Use environment-specific secrets management
- Enable HTTPS in production
- Implement rate limiting for API endpoints
- Regular security audits and updates
- Implement Web Application Firewall (WAF)
- Monitor access logs for suspicious activity
- Keep dependencies updated

## Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test Smart-Medc.Application.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Test Structure
- Unit Tests: Service and business logic testing
- Integration Tests: Database and service integration
- API Tests: Endpoint and contract testing

## Deployment

### Docker Deployment
```bash
# Build Docker image
docker build -t smart-medi-backend .

# Run container
docker run -p 5000:80 smart-medi-backend
```

### Azure Deployment
```bash
# Publish to Azure App Service
dotnet publish -c Release
az webapp deployment source config-zip --resource-group myResourceGroup --name myAppService --src publish.zip
```

### Environment Variables for Production
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
ConnectionStrings__cs=<production-db-connection>
JwtSettings__SecretKey=<production-secret-key>
EmailSettings__SmtpPassword=<email-password>
```

## Troubleshooting

### Database Connection Issues
```bash
# Test connection
dotnet ef dbcontext validate --startup-project Smart-Medc.API

# Check migration status
dotnet ef migrations list --startup-project Smart-Medc.API
```

### Port Already in Use
```bash
# Windows: Kill process using port 7000
netstat -ano | findstr :7000
taskkill /PID <PID> /F

# Linux/Mac
lsof -i :7000
kill -9 <PID>
```

### Migration Rollback
```bash
# Revert to previous migration
dotnet ef database update <MigrationName> --startup-project Smart-Medc.API
```

### Clear Application Cache
```bash
# Remove build artifacts
dotnet clean

# Restore dependencies
dotnet restore
```

## Code Quality

### Code Standards
- **C# Coding Conventions**: PascalCase for public members, camelCase for private
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
- **DRY Principle**: Don't Repeat Yourself
- **Clean Code**: Self-documenting code with minimal comments

### Code Analysis
```bash
# Run static code analysis
dotnet analyze

# Format code
dotnet format
```

## Dependencies

Key NuGet packages:
- `Microsoft.EntityFrameworkCore` - Data access
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT auth
- `AutoMapper` - Object mapping
- `Hangfire` - Background jobs
- `Serilog` - Logging
- `FluentValidation` - Data validation
- `iTextSharp` - PDF generation
- `AspNetCore.SignalR` - Real-time communication
- `Azure.Storage.Blobs` - Cloud storage

## Contributing

Contributions are welcome! Please follow these steps:

1. **Fork the Repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/Smart-Medi-Backend.git
   ```

2. **Create Feature Branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

3. **Commit Changes**
   ```bash
   git commit -m "Add amazing feature"
   ```

4. **Push to Branch**
   ```bash
   git push origin feature/amazing-feature
   ```

5. **Open Pull Request**
   - Describe your changes clearly
   - Reference related issues
   - Ensure tests pass

### Code Style Guidelines
- Follow existing code structure
- Write meaningful commit messages
- Add tests for new functionality
- Update documentation as needed

## License

This project is licensed under the **MIT License** - see the LICENSE file for details.

## Support

### Getting Help

- **Documentation**: Check this README and inline code comments
- **Issues**: Report bugs on [GitHub Issues](https://github.com/Smart-Medc/Smart-Medi-Backend/issues)
- **Discussions**: Join [GitHub Discussions](https://github.com/Smart-Medc/Smart-Medi-Backend/discussions)
- **Email**: Contact the development team

### Reporting Security Issues

⚠️ **Do not** open public issues for security vulnerabilities. Instead, email security concerns to the maintainers.

## Changelog

### Version 1.0.0 (Initial Release)
- Core appointment scheduling system
- Medical records management
- Patient-doctor communication
- Medication tracking
- Data sharing with access control
- AI health assistant
- Real-time notifications
- Admin dashboard

### Planned Features
- Mobile app integration
- Advanced analytics and reporting
- Telemedicine video consultation
- Electronic health records (EHR) integration
- Insurance integration
- Multi-language support
- Advanced predictive health analytics

## Roadmap

- **Q1 2024**: Telemedicine features
- **Q2 2024**: Mobile app optimization
- **Q3 2024**: Advanced analytics dashboard
- **Q4 2024**: EHR integrations

## Related Projects

- **Smart-Medi-Frontend**: [React-based patient portal](https://github.com/Smart-Medc/Smart-Medi-Frontend)
- **Smart-Medi-Mobile**: [Mobile application](https://github.com/Smart-Medc/Smart-Medi-Mobile)

## Acknowledgments

- Built with [.NET 9](https://dotnet.microsoft.com/)
- Database powered by [SQL Server](https://www.microsoft.com/en-us/sql-server/)
- Real-time features by [SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- Job scheduling via [Hangfire](https://www.hangfire.io/)
- Data mapping with [AutoMapper](https://automapper.org/)

## Contact

- **GitHub**: [@Smart-Medc](https://github.com/Smart-Medc)
- **Email**: smart-medi@example.com
- **Website**: https://smartmedi.example.com

---

**Made with ❤️ by the Smart Medi Team**

Last Updated: 2026  
Repository: [Smart-Medc/Smart-Medi-Backend](https://github.com/Smart-Medc/Smart-Medi-Backend)
