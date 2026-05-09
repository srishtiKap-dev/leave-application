# Leave & Expense Management Portal

Full-stack Employee Leave & Expense Management Portal with ASP.NET Core 10, Clean Architecture, SQLite for local development, PostgreSQL for Azure production, Identity/JWT, Hangfire, SendGrid, Azure Blob Storage, React 18, Vite, Zustand, TanStack Query, Tailwind, and Azure deployment assets.

## Structure

- `src/LeavePortal.Domain` - entities and enums.
- `src/LeavePortal.Application` - DTOs, validation, service contracts, business workflows.
- `src/LeavePortal.Infrastructure` - EF Core PostgreSQL, Identity, seed data, JWT refresh tokens, email, blob storage, Hangfire.
- `src/LeavePortal.API` - versioned REST API under `/api/v1`, built-in OpenAPI at `/openapi/v1.json`, auth, middleware.
- `frontend` - React 18 + TypeScript portal.
- `infra` - Azure Bicep for App Service, Static Web Apps, PostgreSQL Flexible Server, Blob Storage, Key Vault, Application Insights, CDN.

## Local Backend

1. Install .NET 10 SDK.
2. Local development uses SQLite via `Data Source=leaveportal.dev.db`, so PostgreSQL is not required.
3. Update `src/LeavePortal.API/appsettings.Development.json` or environment variables:
   - `ConnectionStrings__DefaultConnection`
   - `ConnectionStrings__Storage`
   - `Jwt__SigningKey`
   - `SendGrid__ApiKey`
   - `Cors__AllowedOrigins__0`
4. Run:

```bash
dotnet restore LeavePortal.slnx
dotnet build LeavePortal.slnx
dotnet run --project src/LeavePortal.API
```

On startup, the API applies migrations and seeds roles, leave types, holidays, sample users, balances, leave applications, and expense claims.

Seed logins:

- `admin@company.com` / `Admin@123!`
- `hr@company.com` / `Hr@12345!`
- managers use `Manager@123!`
- employees use `Employee@123!`

## Local Frontend

Node is required for the frontend.

```bash
cd frontend
npm install
npm run dev
```

Set `VITE_API_URL=https://localhost:7109/api/v1` in `frontend/.env.local`.

## Tests

```bash
dotnet test LeavePortal.slnx
```

The backend builds on `net10.0`. Unit tests cover application leave calculation logic; integration tests smoke-test API startup and built-in OpenAPI.

## Azure Deployment

Provision infrastructure:

```bash
az deployment sub create --location centralindia --template-file infra/main.bicep --parameters postgresAdminPassword='<strong-password>'
```

Configure GitHub secrets:

- `AZURE_API_APP_NAME`
- `AZURE_API_PUBLISH_PROFILE`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`
- `VITE_API_URL`

Required App Service settings:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__Storage`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Cors__AllowedOrigins__0`
- `Storage__ReceiptsContainer`
- `SendGrid__ApiKey`
- `SendGrid__FromEmail`
- `ApplicationInsights__ConnectionString`

Security defaults include 15-minute JWT access tokens, 7-day rotated refresh tokens, Identity password policy and lockout, role-based API authorization, private receipt storage with SAS read URLs, global exception handling, HTTPS, validation, and paginated list responses.
