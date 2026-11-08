# RaceDay API

A RESTful Web API for managing road running, walking, and cycling events in South Africa. Built with ASP.NET Core 8.0 and Entity Framework Core using a Code-First approach.

## Project Overview

The RaceDay API is the backend for the RaceDay event management system. It allows Participants to browse events, enrol in race categories, and view their personal results. It allows Organisers to create and manage events, categories, and record participant results. The API uses JWT-based authentication with role-based access control to secure endpoints.

The system is split into two main parts:
1. **Part 2:** The RESTful API (`RaceDay.API`).
2. **Part 3:** The MVC Frontend (`RaceDay.MVC`) that consumes the API, manages sessions, and handles image uploads.

## Technology Stack

| Technology | Purpose |
|---|---|
| ASP.NET Core 8.0 | Web API and MVC Framework |
| Entity Framework Core 8.0 | ORM (Code-First) |
| SQL Server (LocalDB) | Database |
| JWT (JSON Web Tokens) | Authentication |
| BCrypt.Net-Next | Password hashing |
| Swagger / Swashbuckle | API documentation |
| xUnit | Unit testing |
| Azure Blob Storage | Image storage (Part 3) |
| Docker | Containerization (Part 3) |
| GitHub Actions | CI/CD pipeline |

## Setup Instructions

Prerequisites: .NET 8.0 SDK or newer, Visual Studio 2022 with ASP.NET workload, SQL Server LocalDB (installed with Visual Studio).

Step 1 - Clone this repository to your local machine.

Step 2 - Configure the database: the connection string is already set in RaceDay.API/appsettings.json for LocalDB. If you use a different SQL Server instance, update the DefaultConnection value.

Step 3 - Run migrations: open the Package Manager Console (Tools > NuGet Package Manager > Package Manager Console) and run Update-Database. This creates the RaceDayDB database with all tables and seed data.

Step 4 - Run the API: press F5 in Visual Studio or run dotnet run --project RaceDay.API. The API will be available at Swagger UI localhost:7137/swagger and API base URL localhost:7137/api

Step 5 - Run the MVC App: right-click `RaceDay.MVC` in Visual Studio and select Debug > Start New Instance. The app will open in your browser.

Step 6 - Run the tests: run dotnet test or in Visual Studio go to Test > Run All Tests.

## API Endpoints

Auth:
POST /api/auth/register (Public)
POST /api/auth/login (Public)

Users:
GET /api/users/me (Authenticated)
PUT /api/users/me (Authenticated)

Events:
GET /api/events (Authenticated)
GET /api/events/{id} (Authenticated)
POST /api/events (Organiser)
PUT /api/events/{id} (Organiser, own)
DELETE /api/events/{id} (Organiser, own)

Categories:
GET /api/categories (Authenticated)
GET /api/events/{eventId}/categories (Authenticated)
POST /api/events/{eventId}/categories (Organiser, own)
PUT /api/categories/{id} (Organiser, own)
DELETE /api/categories/{id} (Organiser, own)

Enrolments:
POST /api/categories/{id}/enrol (Participant)
GET /api/enrolments (Participant / Organiser)
GET /api/enrolments/{id} (Participant own / Organiser)
DELETE /api/enrolments/{id} (Participant own)
GET /api/events/{eventId}/enrolments (Organiser own)

Results:
GET /api/results (Participant own / Organiser)
GET /api/events/{eventId}/results (Authenticated)
GET /api/enrolments/{enrolmentId}/result (Participant own / Organiser)
POST /api/enrolments/{enrolmentId}/result (Organiser own)
PUT /api/results/{id} (Organiser own)
DELETE /api/results/{id} (Organiser own)

## Unit Tests

The test project RaceDay.API.Tests contains xUnit tests covering user registration and login (valid, duplicate, invalid credentials), role-based access control (401 Unauthorized, 403 Forbidden), event creation as Organiser (201) vs Participant (403), and enrolment flow with conflict detection (409). Run all tests with dotnet test.

## CI/CD Pipeline

This project uses GitHub Actions for continuous integration. Every push to the main branch triggers checkout of the repository, setup of .NET 8.0, restore of NuGet packages, build in Release configuration for both API and MVC, and running all unit tests. The workflow file is located at .github/workflows/dotnet.yml.

---

## Part 3: MVC Frontend, Azure Blob & Docker

This section contains the details for Part 3, which adds the frontend and cloud storage to the RaceDay system.

### Features
- **Role-Based Dashboards:** Separate interfaces for Organisers and Participants.
- **Event Management:** Organisers can create events, upload banner images (via Azure Blob Storage), and manage race categories.
- **Enrolments & Results:** Participants can enrol in categories, and Organisers can capture their finish times.
- **Docker Integration:** The entire application (API, MVC, and Database) is containerized.

### How to Run with Docker
Ensure you have Docker Desktop installed and running. From the root directory of this repository, run:

```bash
docker-compose up --build
