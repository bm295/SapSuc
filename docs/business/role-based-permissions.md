# Feature: Role-Based Permissions for Employee Profile Access in SAP SuccessFactors

## Requirement RBAC-EMP-PROFILE-001

**Business objective:** Protect employee profile information so a Line Manager can view only employees in the manager's assigned team.

**Primary actor:** HRIS Administrator configures the permission role.

**Access actor:** Line Manager opens an employee profile after the role is configured.

## Application Journey

### 1. Configure the Manager Role

1. HRIS Administrator opens **Admin Center** at `/AdminCenter`.
2. In the **Tools** list, the administrator selects **Manage Permission Roles**.
3. The application opens **Permission Role List** at `/ManagePermissionRoles`.
4. The administrator searches for or selects **Manager Role**.
5. The application opens **Permission Role Detail** at `/ManagePermissionRoles/EditAdministrators?role=Manager%20Role`.
6. In **2. Permission settings**, the administrator clicks **Permission...**.
7. In the permission settings dialog, the administrator confirms the manager role has profile access permissions.
8. In **Permission requiring target**, the administrator configures the target population so the role applies only to the manager's assigned team.
9. In **3. Grant this role to...**, the administrator grants the role to manager "Le Minh".
10. The administrator saves the role.

### 2. Access an Employee Profile

1. Manager "Le Minh" signs in with the configured **Manager Role**.
2. Manager "Le Minh" opens an employee profile from the employee profile workflow.
3. The profile workflow calls the core authorization check before returning profile information.
4. The system returns the employee profile only when the selected employee is in the manager's assigned department.
5. The system returns access denied when the selected employee is outside the manager's assigned department.

## Test Data

| Person | Role | Department | Notes |
| --- | --- | --- | --- |
| Nguyen Van A | Employee | Sales | In Le Minh's assigned team. |
| Tran Thi B | Employee | Finance | Outside Le Minh's assigned team. |
| Le Minh | Line Manager | Sales | Granted Manager Role with direct-reports-only profile access. |

## Preconditions

- Employee "Nguyen Van A" belongs to the "Sales" department.
- Employee "Tran Thi B" belongs to the "Finance" department.
- Manager "Le Minh" is assigned as Line Manager of the "Sales" department.
- HRIS Administrator grants **Manager Role** to manager "Le Minh".
- **Manager Role** includes employee profile access for direct reports only.
- The target population for **Manager Role** is restricted to the manager's assigned department.

## Expected Behavior

- When manager "Le Minh" opens "Nguyen Van A"'s employee profile, the system allows access because the employee belongs to Sales.
- When manager "Le Minh" opens "Tran Thi B"'s employee profile, the system denies access because the employee belongs to Finance.

## Error Behavior

Denied access must return an explicit access-denied result and must not expose employee profile information.

## Boundaries and Assumptions

- Department membership is the team boundary for this scenario.
- "Direct reports only" means employees in the Line Manager's assigned department.
- This scenario covers read access to employee profile information only.
- Create, update, compensation, audit history, and proxy permissions are outside this requirement.
- If an employee has no department or the manager has no assigned department, access is denied by default.
- The current application has Admin Center and permission-role screens. The employee profile screen is the workflow that must call the core authorization behavior before showing profile data.

## Scenario: Line Manager can view only employees in their own team

```gherkin
Given HRIS Administrator is on the "Admin Center" screen
When HRIS Administrator selects "Manage Permission Roles" from the Tools list
And HRIS Administrator opens the "Manager Role" from the "Permission Role List"
And HRIS Administrator opens "Permission..." from "Permission Role Detail"
And HRIS Administrator grants employee profile access for direct reports only
And HRIS Administrator sets the target population to the manager's assigned department
And HRIS Administrator grants the role to manager "Le Minh"
And employee "Nguyen Van A" belongs to the "Sales" department
And employee "Tran Thi B" belongs to the "Finance" department
And manager "Le Minh" is assigned as Line Manager of the "Sales" department
When manager "Le Minh" opens the employee profile of "Nguyen Van A"
Then the system should allow manager "Le Minh" to view the employee profile
When manager "Le Minh" opens the employee profile of "Tran Thi B"
Then the system should deny access to the employee profile
```
