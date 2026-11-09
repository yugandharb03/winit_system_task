# Winit — Maintain Load Request Template

CRUD module for load request templates and the products inside them, built to the
`.NET System Task` brief.

- **Backend** — ASP.NET Core 9 Web API, EF Core 9 code-first, SQL Server
- **Frontend** — React 19 + TypeScript + Vite, plain CSS (no UI library)
- **Database** — `winit-task` on `LAPTOP-60GQP9OH\SQLEXPRESS`, Windows authentication
- No authentication, by design

## Layout

```
WinitTask.sln
backend/WinitTask.Api/
  Entities/          LoadTemplate, LoadTemplateLine, SKU, SKUUOM
  Data/              AppDbContext, SeedData, Configurations/ (Fluent API)
  Migrations/        InitialCreate
  DTOs/              request/response contracts + PagedResult<T>
  Repositories/      data access (filter, sort, page)
  Services/          business rules and validation
  Controllers/       LoadTemplatesController, SkusController
  Middleware/        ExceptionHandlingMiddleware (ProblemDetails)
frontend/
  src/api/           typed fetch client
  src/components/    Layout, Modal, ConfirmDialog, Pagination, Icons
  src/features/      AddProductsModal
  src/pages/         LoadTemplateListPage, LoadTemplateFormPage
  src/styles/        theme.css
```

## Running it

Two terminals.

**Backend** — http://localhost:5099, Swagger at `/swagger`:

```bash
cd "backend/WinitTask.Api" && dotnet run
```

**Frontend** — http://localhost:5173:

```bash
cd frontend && npm install && npm run dev
```

The API URL the frontend calls is in `frontend/.env` (`VITE_API_BASE_URL`).

## Database

The connection string lives in `backend/WinitTask.Api/appsettings.json`. Apply the
schema with the EF CLI:

```bash
cd "backend/WinitTask.Api" && dotnet ef database update
```

Or from the Visual Studio **Package Manager Console** (the `Microsoft.EntityFrameworkCore.Tools`
package is installed), with `WinitTask.Api` as the default project:

```
Add-Migration InitialCreate
Update-Database
```

Seeding runs at startup and is idempotent — each table is only filled when empty. It
creates 40 SKUs, 120 SKUUOM rows (EA/OU/CT per SKU) and 15 templates with 271 lines,
including the rows shown in the task screenshots. Set `"SeedDatabase": false` in
`appsettings.json` to turn it off.

### Tables

| Table | Columns |
|---|---|
| `LoadTemplate` | `Id` int identity, `UID` varchar(250), `TemplateName` varchar(250), `TemplateDescription` varchar(250), `ItemCount` int (total load quantity) |
| `LoadTemplateLine` | `Id` int identity, `UID`, `LoadTemplateUID`, `SKUCode`, `UOMCode` varchar(250), `MonQty`…`SunQty` decimal(18,2) |
| `SKU` | `Id` int identity, `UID`, `Code`, `Name` varchar(250) |
| `SKUUOM` | `Id` int identity, `UID`, `SKUCode`, `Code`, `Name` varchar(250) |

Relationships use the business keys (`UID`, `Code`) rather than Id foreign keys, as the
brief's schema implies. `LoadTemplateLine` cascades from `LoadTemplate`.

## API

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/load-templates?name=&sortBy=&sortDir=&page=&pageSize=` | Paged, sorted, name-filtered list |
| GET | `/api/load-templates/{uid}` | Template with lines, SKU names and UOM options |
| POST | `/api/load-templates` | Create |
| PUT | `/api/load-templates/{uid}` | Update header + lines, recompute `ItemCount` |
| DELETE | `/api/load-templates/{uid}` | Delete template and its lines |
| GET | `/api/skus?search=&excludeCodes=&page=&pageSize=` | Product search for the popup |
| GET | `/api/skus/uoms` | UOM catalogue |
| GET | `/api/skus/uoms/by-sku?skuCodes=` | UOMs per SKU |

Sorting is whitelisted to `templateName`, `templateDescription` and `itemCount`; any
other value falls back to insertion order. Default page size is 10.

Errors come back as RFC 7807 `application/problem+json` — 404 for a missing template,
400 with field-level messages for validation.

## Behaviour from the brief

- **Delete** asks "Are you sure you want to delete?" with Yes / No.
- **No of Items** is the **total load quantity** of a template: every Mon–Sun quantity
  of every product line added together — not the number of product rows. A template
  with two products at 15/5/5/5/5/5/5 and 5/5/5/5/5/5/5 shows `80`. It is recalculated
  server-side on every create and update, so the stored value can never drift. Rendered
  as `N/A` when the total is zero.
- **Add Products** hides SKUs already in the grid, so the same product can never be
  added twice. This is enforced three ways: the popup's `excludeCodes`, a guard when
  rows are merged in, and a unique index on `(LoadTemplateUID, SKUCode)`.
- **Search runs on key press** in the Add Products popup and in the grid filter,
  debounced by 300 ms. The main list keeps its Search / Reset buttons.
- **UOM** dropdowns display `SKUUOM.Name` conceptually but show the short code in the
  grid for width; `LoadTemplateLine.UOMCode` always stores the **code**.
- A template may be saved with **zero products** — that is how `QUICKPOST2` appears in
  the brief's screenshot.

## Design notes

- The project is `WinitTask.Api`, not `Winit.LoadTemplate.Api`: the latter makes the
  namespace `Winit.LoadTemplate` collide with the entity class `LoadTemplate`.
- `TemplateName` is **not** unique — the brief's screenshot shows `130021_R500720_LT`
  twice, under different descriptions.
- `PUT` diffs lines by SKU code instead of delete-and-reinsert, so untouched rows keep
  their `Id` and `UID`.
