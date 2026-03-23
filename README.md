# Library Book Tracking API

An HTTP-based REST API for tracking library books, members, and checkouts — built with **ASP.NET Core 8**, Swagger/OpenAPI, and xUnit.

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Running the API

```bash
cd LibraryApi
dotnet run
```

The API starts on `http://localhost:5000`.  
**Swagger UI** opens at the root: [http://localhost:5000](http://localhost:5000)

---

## Running the Tests

```bash
dotnet test
```

Runs **unit tests** (service layer) + **integration tests** (full HTTP stack via `WebApplicationFactory`). Tests are serialised so each class gets an isolated in-memory store.

---

## Endpoints

### Books

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/books` | List all books with availability status |
| `GET`  | `/books/{isbn}` | Get a book by ISBN |
| `POST` | `/books` | Add a new book |

### Members

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/members` | List all members |
| `GET`  | `/members/{memberId}` | Get a member by GUID |
| `POST` | `/members` | Register a new member |
| `GET`  | `/members/{memberId}/books/checkedout` | Books currently checked out by a member |
| `POST` | `/members/{memberId}/books/hire` | Check out a book |
| `POST` | `/members/{memberId}/books/return` | Return a book |

### Checkouts (admin view)

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/checkouts` | All checkout records (add `?activeOnly=true` to filter) |
| `GET`  | `/checkouts/{checkoutId}` | Single checkout record by ID |

### System

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/health` | Health probe |

---

## Sample Requests

### List all books
```bash
curl http://localhost:5000/books
```

### Check out a book
```bash
curl -X POST http://localhost:5000/members/22222222-0000-0000-0000-000000000002/books/hire \
  -H "Content-Type: application/json" \
  -d '{"bookISBN": "978-0-06-112008-4"}'
```

### View a member's checked-out books
```bash
curl http://localhost:5000/members/11111111-0000-0000-0000-000000000001/books/checkedout
```

### Return a book
```bash
curl -X POST http://localhost:5000/members/11111111-0000-0000-0000-000000000001/books/return \
  -H "Content-Type: application/json" \
  -d '{"bookISBN": "978-0-7432-7356-5"}'
```

### View all active checkouts (admin)
```bash
curl "http://localhost:5000/checkouts?activeOnly=true"
```

---

## Seed Data

The store is pre-loaded with **5 books** and **3 members**. Alice has *1984* already checked out.

| Member | ID |
|--------|----|
| Alice Johnson | `11111111-0000-0000-0000-000000000001` |
| Bob Smith     | `22222222-0000-0000-0000-000000000002` |
| Carol White   | `33333333-0000-0000-0000-000000000003` |

---

## Assumptions & Design Decisions

- **Mock Data** — state is lost on restart; intentional per spec.
- **ISBN as natural key** for books; GUID for members.
- **One active checkout per book** at a time; a member may hold multiple books.
- **Return endpoint** — `POST /members/{id}/books/return` added as the natural complement to hire.
- **Book status is computed** at query time from active checkouts — never stored redundantly.
- **`/checkouts` admin endpoint** — gives staff a flat, filterable view of all checkout history.
- **Validation** — `[Required]`, `[EmailAddress]`, `[Range]`, `[StringLength]` via DataAnnotations on all request records; invalid requests return `400` with a structured problem-details body.
- **Swagger at `/`** — `RoutePrefix = ""` so Swagger UI loads immediately on launch.
- **No authentication** — out of scope for this exercise.


---
 
## Roadmap
 
The following areas are planned for future iterations to bring this to production quality.
 
### Authentication & Authorisation — MS Identity + Azure App Registration
- Register the API as an App Registration in Azure Entra ID
- Protect all endpoints with JWT bearer token validation (`Microsoft.Identity.Web`)
- Define app roles: `Library.Admin`, `Library.Member`
- Admins can manage books and view all checkouts; members can only manage their own checkouts
- Add Swagger UI support for OAuth2 / Bearer token input
- Store the `MemberId` claim from the token rather than accepting it as a URL parameter (prevents members acting on behalf of others)
 
### Database — Azure SQL Server + EF Core
- Replace `InMemoryStore` with EF Core `DbContext` backed by Azure SQL
- Add migrations from day one (`dotnet ef migrations add InitialCreate`)
- Use `IDbContextFactory` or scoped `DbContext` rather than singleton
- Connection string via `appsettings.Production.json` + Azure Key Vault secret reference
- Add indexes on `Checkouts.MemberId`, `Checkouts.BookISBN`, and `Checkouts.ReturnedAt` for query performance
- Soft-delete pattern for books and members (set `DeletedAt` rather than removing rows)
 
### Domain Gaps
- **Due dates** — checkouts should have a `DueDate` (e.g. 2 weeks from checkout); expose overdue status on the checkout record
- **Fines** — calculate and record fines for overdue returns
- **Reservations** — allow a member to reserve a book that is currently checked out; notify when it becomes available
- **Multiple copies** — a book (ISBN) can have more than one physical copy; track copies independently so the same ISBN can be checked out by multiple members simultaneously
- **Book search** — filter `GET /books` by title, author, genre, and availability
 
### API Quality
- **Pagination** — all list endpoints (`/books`, `/members`, `/checkouts`) need `?page=` and `?pageSize=` parameters; unbounded lists will not scale
- **Sorting** — `?sortBy=title&order=asc` on list endpoints
- **PATCH endpoints** — update a book's details or a member's profile without replacing the whole record
- **DELETE endpoints** — remove books from the catalogue and deregister members
- **RFC 7807 problem details** — consistent error response shape across all 4xx/5xx responses
- **API versioning** — route-based (`/v1/books`) so future breaking changes don't affect existing clients
- **ETags** — support conditional GET requests for client-side caching
 
### Operational & Infrastructure
- **Structured logging** — Serilog with Application Insights sink; log all requests, errors, and checkout events
- **Azure Application Insights** — distributed tracing, performance monitoring, alerting
- **Rate limiting** — protect the API from abuse using ASP.NET Core's built-in rate limiting middleware
- **Docker** — `Dockerfile` + `docker-compose.yml` for local development with SQL Server in a container
- **CI/CD** — GitHub Actions pipeline: build → test → publish → deploy to Azure App Service
- **Environment config** — `appsettings.Production.json` with Key Vault references for secrets; never commit connection strings
- **Health checks** — extend `/health` to check database connectivity and report degraded status