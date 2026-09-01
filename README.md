# Planning Balancer (AssesmentAGIT)

**Candidate Token:** `VEH-Arief_Achmadi`  
**Track:** Full-stack

## Prerequisites / Runtime Versions
- **.NET SDK:** 10.0 (or matching runtime)
- **Database:** PostgreSQL (Ensure you have a local instance running)

## How to Run the Application

1. **Configure Database Connection**
   Open `AssesmentAGIT.Api/appsettings.Development.json` and ensure the `DefaultConnection` string matches your local PostgreSQL credentials. Make sure to create new database on pgadmin or any other postgres tool

2. **Run Database Migrations**
   Open a terminal in the root folder and run:
   ```bash
   cd AssesmentAGIT.Api
   dotnet ef database update --project ../AssesmentAGIT.Infrastructure/AssesmentAGIT.Infrastructure.csproj
   ```

3. **Start the API & Web Server**
   ```bash
   dotnet run
   ```
   The application will start. Open the printed localhost URL (e.g., `http://localhost:5000` or `https://localhost:5001`) in your browser to view the Minimal UI.

4. **Run Automated Tests**
   Open a terminal in the root folder and run:
   ```bash
   dotnet test
   ```
   This will run all 16 tests, covering the unit tests for the balancing logic and the integration tests for the API.

## Quality Requirements Met

- **Persistence/API Flow Tests:** The `AssesmentAGIT.Tests` project includes `PlanningApiIntegrationTests.cs`, which uses `WebApplicationFactory` and an InMemory database to verify the complete API flow, idempotency, data persistence, and historical ordering.
- **Frontend States:** The UI (located in `AssesmentAGIT.Api/wwwroot`) is implemented in pure vanilla JS/HTML/CSS and handles all required states:
  - **Loading:** Submit button shows a loading state and disables during API calls.
  - **Empty:** History tab displays a "No submissions yet" message when the database is empty.
  - **Success:** Detailed balance results and positive badges are rendered upon successful processing.
  - **Error:** Validation errors (e.g., negative values) and API rejection messages are displayed in a clear error panel.

## Assumptions & Trade-offs

### Assumptions
- **Idempotency Behavior:** If the same `RequestCode` is submitted twice, the system assumes the original transaction is the source of truth and returns the existing record rather than creating a duplicate or updating it.
- **Data Integrity:** The `RequestCode` is enforced as `UNIQUE` at the database schema level to guarantee there are never duplicate plans.
- **Inactive Days:** Days with an initial plan of 0 are considered inactive. They cannot receive balanced quantities, but they are still saved to the database with `IsActive = false` to preserve the full 7-day record.

### Trade-offs
- **Architecture (Minimal UI):** To fulfill the "Minimal UI" requirement while reducing friction for the assessor, the frontend is built using static HTML/JS/CSS housed within the `wwwroot` folder of the API project. While a strict Clean Architecture might isolate the frontend into a separate Node.js project, co-hosting it simplifies the setup process (a single `dotnet run` command launches everything). 
- **Vanilla JavaScript:** Decided against using a frontend framework (like React or Vite) to keep the repository extremely lightweight, dependency-free, and easy to review line-by-line.
- **Integration Test Database:** Used EF Core's InMemory provider for integration tests instead of Testcontainers or a live PostgreSQL database. This ensures the tests run fast and reliably on any assessor's machine without requiring Docker, though it required conditional logic in the service layer to bypass transaction wrapping (since InMemory does not support DB transactions).

## Project Structure
- `AssesmentAGIT.Domain` - Core entities, DTOs, and the pure function `PlanningBalancer`.
- `AssesmentAGIT.Infrastructure` - Data access, `AppDbContext`, and EF Core mappings.
- `AssesmentAGIT.Api` - REST API controllers and static web assets (`wwwroot/`).
- `AssesmentAGIT.Tests` - xUnit tests covering business logic and API integration.
- `Case3.sql` - Standalone SQL script addressing Case 3 requirements.
