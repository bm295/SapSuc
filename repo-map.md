# Repository Map

This repository is a small .NET 9 sample HR system inspired by SAP SuccessFactors. The main application is an ASP.NET Core Razor Pages web app for proxy management, backed by an in-memory HR service and domain model.

## Top-Level Layout

```text
.
|-- README.md
|-- SapSuc.sln
|-- src/
|   |-- SapSuc.Core/
|   |-- SapSuc.Web/
```

## Projects

### `SapSuc.sln`

Visual Studio solution containing the main two-project app:

- `src/SapSuc.Core/SapSuc.Core.csproj`
- `src/SapSuc.Web/SapSuc.Web.csproj`

### `src/SapSuc.Core`

Class library targeting `net9.0`. It contains the core HR domain entities and the in-memory application service.

Important files:

- `Domain/Employee.cs` - employee profile, employee number, assignment ID, department, job title.
- `Domain/ProxyAssignment.cs` - proxy delegation between employees, date-time validity window, all-tool access, per-tool permissions, leave approval capability.
- `Domain/Goal.cs` - performance goal and status.
- `Domain/PerformanceReview.cs` - review year, rating, summary.
- `Domain/LeaveRequest.cs` - leave request date range, pending/approved/rejected status, day count.
- `Domain/CompensationRecord.cs` - salary record with currency and effective date.
- `Services/HrPlatformService.cs` - in-memory data store and operations for employees, proxies, leave, goals, reviews, compensation, and proxy self-service settings.
- `Class1.cs` - placeholder file from the class library template.

### `src/SapSuc.Web`

ASP.NET Core Razor Pages app targeting `net9.0`. It references `SapSuc.Core`.

Important files:

- `Program.cs` - web app bootstrap, DI registration, session setup, and static asset/Razor Pages mapping.
- `HrPlatformSeeder.cs` - creation of the in-memory HR service and its sample data.
- `Pages/Index.cshtml` - main Proxy Management UI.
- `Pages/Index.cshtml.cs` - main page model and all proxy management handlers.
- `Pages/Shared/_Layout.cshtml` - SAP/Fiori-like shell layout.
- `Pages/Shared/_Layout.cshtml.css` - layout-specific styling.
- `wwwroot/css/site.css` - main visual styling for shell, dashboard, proxy panels, tables, forms, settings, and responsive behavior.
- `wwwroot/js/site.js` - default site script placeholder.
- `wwwroot/lib/*` - vendored Bootstrap, jQuery, and validation assets.
- `appsettings.json`, `appsettings.Development.json` - standard ASP.NET Core configuration files.

## Main Runtime Flow

1. `src/SapSuc.Web/Program.cs` creates the Razor Pages app.
2. `HrPlatformSeeder` creates an `HrPlatformService` with sample employees and proxy assignments, which is registered as a singleton.
3. Browser requests route to `Pages/Index.cshtml`.
4. `IndexModel` in `Pages/Index.cshtml.cs` reads the in-memory service, builds view models, and handles form posts.
5. Domain operations are delegated to `HrPlatformService`.

## Proxy Management Feature Map

The proxy management feature is centered in `Pages/Index.cshtml.cs` and `Pages/Index.cshtml`.

Page handlers:

- `OnGet` - prepares the dashboard and assignment list.
- `OnPostAssign` - creates one or more proxy assignments.
- `OnPostImport` - imports SAP SuccessFactors-style proxy CSV rows.
- `OnPostUpdateSettings` - admin-only toggle for employee proxy self-service.
- `OnPostRemove` - removes a proxy assignment, with admin/self-service authorization checks.
- `OnPostSwitchUser` - enters proxy/user view using session state.
- `OnPostClearProxy` - returns to administrator view.

Supporting logic:

- `PreparePage` - builds employee options, summary cards, current view context, and assignment rows.
- `ImportProxyCsv` - parses the proxy import CSV and applies assignments/removals.
- `SapProxyImportColumns` - maps required SAP-style CSV headers and tool/module columns.
- `ProxyAssignmentRow`, `ProxySummary`, `ProxyViewContext` - page-facing view models.

Supported proxy behaviors:

- Administrators can assign and remove proxies.
- Organization setting can allow or block employee self-service.
- Employees in proxy/user view can assign/remove only their own proxy assignments when self-service is enabled.
- CSV import is admin-only.
- CSV import supports `USERID`, `ASSIGNMENT_ID_USERID`, `PROXYID`, `ASSIGNMENT_ID_PROXYID`, `START_DATE(yyyy-MM-dd HH:mm)`, `END_DATE(yyyy-MM-dd HH:mm)`, `All`, `Remove All`, and tool/module columns.
- `PROXYID` supports multiple proxy IDs separated by `|`.

## Data Storage

There is no database. `HrPlatformService` stores all data in private in-memory collections:

- `_employees`
- `_goals`
- `_reviews`
- `_leaveRequests`
- `_compensations`
- `_leaveBalances`
- `_proxyAssignments`

Because the service is registered as a singleton, data persists only while the web process is running and resets on app restart.

## Styling

The app uses Bootstrap plus custom CSS. Most product-specific styling is in `src/SapSuc.Web/wwwroot/css/site.css`, with the shell layout in `Pages/Shared/_Layout.cshtml` and `Pages/Shared/_Layout.cshtml.css`.

The UI intentionally follows a restrained SAP/Fiori-like style: shell bar, side navigation, compact panels, table-driven workflows, muted colors, and small-radius controls.

## Run and Verify

Run the web app:

```bash
dotnet run --project src\SapSuc.Web\SapSuc.Web.csproj
```

Build:

```bash
dotnet build src\SapSuc.Web\SapSuc.Web.csproj
```

The web project currently has no dedicated test project. Running `dotnet test` against the web project validates that the project can be discovered by the test runner, but it does not execute meaningful unit tests unless tests are added.

## Change Hotspots

- Proxy rules and storage: `src/SapSuc.Core/Services/HrPlatformService.cs`
- Proxy assignment invariants: `src/SapSuc.Core/Domain/ProxyAssignment.cs`
- Main proxy UI behavior: `src/SapSuc.Web/Pages/Index.cshtml.cs`
- Main proxy markup: `src/SapSuc.Web/Pages/Index.cshtml`
- App styling: `src/SapSuc.Web/wwwroot/css/site.css`
- Seed data: `src/SapSuc.Web/HrPlatformSeeder.cs`
