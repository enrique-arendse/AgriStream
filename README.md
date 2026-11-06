# AgriStream IoT — CLDV7112/w Practicum

## Project Overview

AgriStream IoT is a cloud-based agricultural monitoring and irrigation management system developed using ASP.NET Core MVC, .NET 10 and Microsoft Azure.

The system enables farmers and administrators to monitor sensor telemetry, manage agricultural devices, upload farm-related media, and submit irrigation commands through a centralized web application.

The solution integrates Azure SQL Database, Blob Storage, Table Storage, Queue Storage and Azure Functions to provide cloud-based data management and event-driven processing.

## Technology Stack

- **Frontend & Backend:** ASP.NET Core MVC, C#
- **Framework:** .NET 10
- **Cloud Platform:** Microsoft Azure
- **Web Hosting:** Azure App Service
- **Serverless Computing:** Azure Functions v4 (.NET Isolated)
- **Database:** Azure SQL Database
- **Object Storage:** Azure Blob Storage
- **NoSQL Storage:** Azure Table Storage
- **Messaging:** Azure Queue Storage
- **Monitoring:** Application Insights
- **Load Testing:** Apache JMeter

## Solution Structure

```text
AgriStream_IoT_Submission/
│
├── AgriStream.Web/          # ASP.NET Core MVC application
├── AgriStream.Functions/    # Azure Functions (4 triggers)
├── sql/
│   └── schema.sql           # Database schema
├── load-test/
│   └── agristream.jmx       # JMeter test plan
└── docs/
    └── EVIDENCE_CHECKLIST.md
```

## Core Features

- **User Authentication:** Secure cookie-based login with Farmer, Agronomist and Admin roles.
- **Agricultural Dashboard:** Retrieves sensor telemetry, including soil moisture and temperature readings.
- **Media Management:** Uploads agricultural images to Azure Blob Storage.
- **Irrigation Management:** Submits irrigation commands through HTTP endpoints and processes them asynchronously.
- **Device Registry:** Maintains device and irrigation command information in Azure SQL Database.
- **Processing Auditing:** Records media processing activity in Azure Table Storage.

Passwords are protected using PBKDF2-SHA256 hashing with randomly generated salts.

## Azure Functions

The Functions application implements exactly four Azure Functions using the .NET 10 isolated worker model.

| Function | Trigger | Purpose |
|---|---|---|
| Telemetry Function | HTTP GET | Retrieves sensor readings. |
| Irrigation Function | HTTP POST | Submits irrigation commands to a queue. |
| Queue Function | Queue Trigger | Processes commands and updates the device registry. |
| Media Function | Blob Trigger | Processes uploads and records audit information. |

## Azure Storage and Database

| Azure Service | Implementation |
|---|---|
| Azure SQL Database | Users and DeviceRegistry |
| Blob Storage | `farm-media` container |
| Table Storage | `FarmTelemetry` and `ProcessingAudit` |
| Queue Storage | `irrigation-commands` |

## Deployment

The solution uses separate Azure hosting environments:

- **AgriStream.Web:** Azure App Service.
- **AgriStream.Functions:** Azure Function App.
- **Database:** Azure SQL Database.
- **Storage:** Azure Storage Account.
- **Monitoring:** Application Insights.

Application configuration and sensitive connection strings are maintained through environment variables and Azure application settings rather than committed source files.

The Azure Functions application uses Functions runtime v4 with the .NET isolated worker model.

## Testing

The repository includes an Apache JMeter test plan for evaluating web application responsiveness.

The main application workflows cover authentication, telemetry retrieval, media uploads, blob-triggered processing, irrigation command submission, queue processing, and device registry updates.

Deployment results and actual performance measurements are maintained separately as submission evidence.

## Security

The implementation incorporates password hashing, cookie-based authentication, role classification, SQL parameterization, HTTPS hosting, and environment-based secret configuration.

Additional production security measures include managed identities, Azure RBAC, network restrictions, and enhanced monitoring.

## Conclusion

AgriStream IoT demonstrates the integration of ASP.NET Core MVC, .NET 10 and Microsoft Azure to deliver a cloud-based agricultural management solution.

The project showcases cloud hosting, serverless computing, relational and NoSQL databases, object storage, and asynchronous message processing within a practical agricultural application.
