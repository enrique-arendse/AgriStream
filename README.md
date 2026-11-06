# AgriStream IoT — CLDV7112/w Practicum starter implementation

This repository contains an ASP.NET Core MVC application and an Azure Functions .NET isolated worker project. It implements the four required function trigger types and uses Azure SQL, Blob Storage, Table Storage and Queue Storage.

## Solution layout
- `AgriStream.Web/` — MVC application (App Service).
- `AgriStream.Functions/` — **exactly four** Azure Functions.
- `sql/schema.sql` — relational schema.
- `load-test/agristream.jmx` — Apache JMeter starter plan.
- `docs/` — deployment checklist and submission evidence list.

## Prerequisites
Install Visual Studio 2022 (ASP.NET and web development, Azure development, and .NET 10 SDK workloads), .NET 10 SDK, Azure Functions Core Tools v4, Azure CLI, SQL Server extension/Azure Data Studio, and optionally Apache JMeter. Sign in to Visual Studio and Azure CLI with the subscription you are permitted to use.

## Open in Visual Studio
1. Open `AgriStream.Web/AgriStream.Web.csproj` in Visual Studio. Build and run locally.
2. Open `AgriStream.Functions/AgriStream.Functions.csproj` as a second project/window. Copy `local.settings.sample.json` to `local.settings.json`, set local storage and SQL values, then run.
3. `local.settings.json` is intentionally not committed. Never commit secrets or connection strings.
4. Create a solution from Visual Studio (File > New > Project > Blank Solution) and add both existing projects, or open the two project files separately.

## Azure resource creation (Portal)
Create a resource group (e.g. `rg-agristream-cldv7112`) in a region supported by your student subscription. Create:
1. Storage account (Standard GPv2). For the practical exercise LRS is economical; use GRS for important records where regional recovery is required. Create Blob container `farm-media`, tables `FarmTelemetry` and `ProcessingAudit`, and queue `irrigation-commands`.
2. Azure SQL logical server and database. Configure firewall access for your current IP during setup; remove broad access after testing. Run `sql/schema.sql` in Query Editor or SQL Server extension.
3. App Service Plan and Web App, runtime .NET 10, Windows or Linux according to the portal's supported stack. Enable HTTPS Only.
4. Function App, runtime .NET 10 isolated, Functions runtime v4. Use the current Visual Studio/Azure Functions tooling that supports .NET 10. The Functions project uses `Azure.Functions.Sdk` 1.0.0 and `Microsoft.Azure.Functions.Worker` 2.50.0 or later. Use the same storage account or a dedicated function host storage account. For .NET 10 on Linux, do not select the Linux Consumption plan; use Flex Consumption or another supported plan. Windows Consumption or Premium options may be available depending on region/subscription. Configure app settings: `FarmStorage`, `AzureWebJobsStorage`, `SqlConnection`, `TelemetryTable=FarmTelemetry`, `AuditTable=ProcessingAudit`, `IrrigationQueue=irrigation-commands`, `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`.
5. Application Insights and link it to both App Service and Function App.

## Configure MVC App Service settings
In Web App > Settings > Environment variables / Configuration, add:
- `ConnectionStrings__AgriStreamSql` = SQL connection string (prefer managed identity in production; for this academic demo use a least-privilege SQL login stored as a secret).
- `Azure__StorageConnection` = storage connection string (secret).
- `Azure__BlobContainer` = `farm-media`
- `Azure__TableName` = `FarmTelemetry`
- `Azure__FunctionsBaseUrl` = `https://YOUR-FUNCTION.azurewebsites.net`
- `Azure__FunctionsKey` = function key (secret)
Save and restart. Do not put secrets in source control.

## Deploy from Visual Studio
### MVC
Right-click `AgriStream.Web` > Publish > Azure > Azure App Service (Windows/Linux) > select/create Web App > Finish > Publish. Confirm the deployed URL loads. Official guide: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps

### Functions
Right-click `AgriStream.Functions` > Publish > Azure > Azure Function App > select/create Function App > Publish. In Function App settings, add the settings above. Restart and verify all four functions appear under Functions in the portal.

## Seed a demonstration user
The `Users` table stores a PBKDF2-SHA256 hash and random salt, not a plaintext password. Generate a 16-byte cryptographic salt and derive a 32-byte hash using PBKDF2-SHA256 with 210,000 iterations, then insert the byte arrays as SQL binary parameters. Set role to Farmer, Agronomist or Admin. A simple standalone C# snippet can use `RandomNumberGenerator.GetBytes(16)` and `Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA256, 32)`. Do not use a real personal password for the demonstration.

## Add sample telemetry
In Storage account > Tables > `FarmTelemetry`, add entities with PartitionKey=device ID (e.g. `sensor-001`), RowKey=UTC timestamp sortable format (`2026-11-06T08:30:00.0000000Z`), and numeric `Moisture` and `Temperature` properties. The GET function returns the latest records.

## Verify end-to-end
1. Sign in to MVC using the seeded user.
2. Upload a small test image from Media. Verify it appears in Blob container `farm-media`; verify an audit entity appears in `ProcessingAudit`.
3. Add sample telemetry to `FarmTelemetry`; refresh dashboard and verify readings.
4. Submit an irrigation command; verify a queue message is processed and `DeviceRegistry` has the device and command status.
5. Function queue trigger is at-least-once delivery: production implementations should make SQL updates idempotent using CommandId and should add poison-queue alerting. For the practicum, capture successful processing.

## Load test and scale-out
Use the supplied JMeter plan as a starting point. Replace host with the actual Web App hostname. Start conservatively (e.g. 10 users, 30-second ramp-up), then increase to 50–100 only if your student quota and App Service tier permit. The sample test calls the login page without authenticating; it measures front-door response, not authenticated dashboard/SQL throughput. For a full authenticated dashboard test, add a JMeter cookie manager and login form/anti-forgery token handling. Alternatively create an Azure Load Testing URL-based test against a safe public page.
In App Service Plan > Scale out (App Service plan) > Custom autoscale, create a rule such as CPU > 70% for 10 minutes: increase instance count by 1; CPU < 40% for 10 minutes: decrease by 1. Set minimum 1, maximum 3 (or within quota). Autoscale requires a tier that supports it (typically Standard or above); confirm current subscription eligibility and pricing before enabling. Run test and capture the instance count/scale history and metrics. Never claim scale-out occurred unless the portal shows it.

## Security and production hardening
- Use managed identities and Azure RBAC where possible; rotate keys if connection strings were exposed.
- Use HTTPS, least privilege, secure cookies, input validation and SQL parameterization.
- This teaching sample has a simple cookie sign-in and does not include registration, password reset, MFA, CSRF-safe upload size scanning, or production-grade authorization workflows.
- Use private endpoints/network restrictions for sensitive production systems.
- Consider separate storage accounts where Blob media, telemetry, and audit data have different redundancy, retention, or security requirements.
- Add retry policies, dead-letter/poison handling, idempotency and alerting before production use.

## Important implementation note
The Azure Functions GET reads Table Storage directly; POST queues an irrigation command; Queue trigger writes the master device registry to SQL; Blob trigger records the upload audit row in Table Storage. Telemetry ingestion is demonstrated by adding sensor entities to Table Storage. If your lecturer expects a separate ingestion endpoint or simulated sensor producer, add one as a non-Function MVC/service component—do not add a fifth Function because the brief explicitly requires exactly four.

## Submission evidence
See `docs/EVIDENCE_CHECKLIST.md`. Replace every placeholder in the Word submission with genuine screenshots, your real GitHub repository URL, deployed Azure URL, and observed test results.

## .NET 10 Azure Functions hosting note
The Functions project targets .NET 10 using the isolated worker model and Functions runtime 4.x. Microsoft documents .NET 10 support for the isolated worker model, not the in-process model. Hosting plan/OS combinations matter: .NET 10 is not supported on Linux Consumption; choose Flex Consumption or another supported plan and verify the current region/subscription availability. On the deployed Function App, ensure `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`, the stack is set to .NET 10, and the app uses a 64-bit worker where required. See https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide.
