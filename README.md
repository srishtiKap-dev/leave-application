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
   - `JwtSettings__Secret`
   - `JwtSettings__Issuer`
   - `JwtSettings__Audience`
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
- `hr@company.com` / `Hr@123!`
- `manager1@company.com` / `Manager@123!`
- `manager2@company.com` / `Manager@123!`
- `emp001@company.com` through `emp005@company.com` / `Emp@123!`

## Local Frontend

Node is required for the frontend.

```bash
cd frontend
npm install
npm run dev
```

Set `VITE_API_URL=http://localhost:5088/api/v1` in `frontend/.env.local` if you want to override the frontend default.

## Tests

```bash
dotnet test LeavePortal.slnx
```

The backend builds on `net10.0`. Unit tests cover application leave calculation logic; integration tests smoke-test API startup and built-in OpenAPI.

## Deployment

Azure CLI is installed in the current terminal, but provisioning requires an authenticated Azure account. Run `az login` before deploying. If Azure CLI is missing on another machine, install it first: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

Live application URLs after deployment:

- Frontend: `https://thankful-smoke-0f212b60f.7.azurestaticapps.net`
- API: `https://app-leaveportal-api-prod.azurewebsites.net`
- API Docs: `https://app-leaveportal-api-prod.azurewebsites.net/openapi/v1.json`

Deploy to Azure:

```bash
az group create --name rg-leaveportal-prod --location eastus
az deployment group create \
  --resource-group rg-leaveportal-prod \
  --template-file infra/main.bicep \
  --parameters appName=leaveportal environment=prod postgresAdminPassword='<strong-password>'
```

Configure GitHub secrets:

- `AZURE_WEBAPP_PUBLISH_PROFILE`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`

Push to the `main` branch after the secrets are configured. GitHub Actions deploys the backend and frontend automatically.

Required App Service settings:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret`
- `JwtSettings__Issuer`
- `JwtSettings__Audience`
- `JwtSettings__AccessTokenExpirationMinutes`
- `JwtSettings__RefreshTokenExpirationDays`
- `AllowedOrigins__0`
- `FileStorage__Provider`
- `FileStorage__ConnectionString`
- `FileStorage__ContainerName`
- `Email__ApiKey`
- `ApplicationInsights__ConnectionString`

Example:

```bash
az webapp config appsettings set \
  --resource-group rg-leaveportal-prod \
  --name app-leaveportal-api-prod \
  --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  "ConnectionStrings__DefaultConnection=Host=psql-leaveportal-prod.postgres.database.azure.com;Database=leaveportaldb;Username=leaveportal_admin;Password=${DB_PASSWORD};SSL Mode=Require;" \
  "JwtSettings__Secret=${JWT_SECRET}" \
  "JwtSettings__Issuer=LeavePortal" \
  "JwtSettings__Audience=LeavePortalUsers" \
  "JwtSettings__AccessTokenExpirationMinutes=15" \
  "JwtSettings__RefreshTokenExpirationDays=7" \
  "FileStorage__Provider=AzureBlob" \
  "FileStorage__ConnectionString=${STORAGE_CONNECTION_STRING}" \
  "FileStorage__ContainerName=receipts" \
  "ApplicationInsights__ConnectionString=${APPINSIGHTS_CONNECTION_STRING}" \
  "AllowedOrigins__0=https://thankful-smoke-0f212b60f.7.azurestaticapps.net"
```

After deployment, verify:

```bash
curl https://app-leaveportal-api-prod.azurewebsites.net/health
curl https://app-leaveportal-api-prod.azurewebsites.net/openapi/v1.json
```

Security defaults include 15-minute JWT access tokens, 7-day rotated refresh tokens, Identity password policy and lockout, role-based API authorization, private receipt storage with SAS read URLs, global exception handling, HTTPS, validation, and paginated list responses.
