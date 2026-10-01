# Market Readiness Trace: Role-Based Permissions

## Requirement Traceability

| Requirement ID | Business requirement | Source | Current application evidence | Current code evidence | Status |
| --- | --- | --- | --- | --- | --- |
| RBAC-EMP-PROFILE-001 | A Line Manager can view employee profiles only for employees in the manager's assigned team. | `docs/features/role-based-permissions.feature`, tagged `@RBAC-EMP-PROFILE-001` with per-scenario status tags. | `/AdminCenter` links to **Manage Permission Roles**; `/ManagePermissionRoles` lists **Manager Role**; `/ManagePermissionRoles/EditAdministrators?role=Manager%20Role` contains **Permission settings**, **Permission requiring target**, and **Grant this role to...** sections. | `Employee.Department` stores team membership; `HrPlatformService.AssignLineManagerToDepartment`, `GrantDirectReportsOnlyProfileAccess`, and `CanViewEmployeeProfile` implement the core access rule; `tests/SapSuc.Tests` covers allow, deny, and default-deny cases. | IMPLEMENTED_IN_CORE; employee profile permission selection and confirmation are implemented in the role editor; pending scenarios cover target configuration, role granting and saving, manager sign-in, application scenario data, and profile authorization with explicit denial and no profile disclosure. |

## First Acceptance Test

`LineManagerWithDirectReportsOnlyPermissionCanViewSalesEmployeeButCannotViewFinanceEmployee`

## Skill Context

This trace was produced from the market-readiness workflow for `RBAC-EMP-PROFILE-001`. The business requirements have been migrated to the Gherkin feature file, which preserves the user journey, business rules, and expected system behavior for the incomplete application workflow. The `@pending` tag describes implementation status; these scenarios are specifications and are not yet connected to an automated Gherkin runner.

## Permission Configuration Scenario

The Employee Profile category offers direct-reports-only read access. Done applies the selection to the current role draft and its summary. Reopening the dialog preserves the confirmed selection; Cancel, Escape, and backdrop dismissal discard unconfirmed changes. Check Tool selection remains independent. This implements the configuration scenario only; Save Changes persistence, role granting, and Web profile enforcement remain pending.
