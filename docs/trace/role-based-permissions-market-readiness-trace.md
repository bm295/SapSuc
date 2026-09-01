# Market Readiness Trace: Role-Based Permissions

## Requirement Traceability

| Requirement ID | Business requirement | Source | Current application evidence | Current code evidence | Status |
| --- | --- | --- | --- | --- | --- |
| RBAC-EMP-PROFILE-001 | A Line Manager can view employee profiles only for employees in the manager's assigned team. | `docs/business/role-based-permissions.md`, "Requirement RBAC-EMP-PROFILE-001" and scenario. | `/AdminCenter` links to **Manage Permission Roles**; `/ManagePermissionRoles` lists **Manager Role**; `/ManagePermissionRoles/EditAdministrators?role=Manager%20Role` contains **Permission settings**, **Permission requiring target**, and **Grant this role to...** sections. | `Employee.Department` stores team membership; `HrPlatformService.AssignLineManagerToDepartment`, `GrantDirectReportsOnlyProfileAccess`, and `CanViewEmployeeProfile` implement the core access rule; `tests/SapSuc.Tests` covers allow, deny, and default-deny cases. | IMPLEMENTED_IN_CORE; UI profile workflow still needs to call the core check when an employee profile page is added or wired. |

## First Acceptance Test

`LineManagerWithDirectReportsOnlyPermissionCanViewSalesEmployeeButCannotViewFinanceEmployee`

## Skill Context

This trace was produced from the market-readiness workflow for `RBAC-EMP-PROFILE-001`. It is intentionally separated from the business scenario so the business document remains focused on user journey, business rules, and expected system behavior.
