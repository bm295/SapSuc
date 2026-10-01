@RBAC-EMP-PROFILE-001
Feature: Role-based permissions for employee profile read access
  HRIS Administrators configure Manager Role so Line Managers can read only
  employee profiles in their assigned department.
  Department membership defines the team boundary and "direct reports only".
  Create, update, compensation, audit history, and proxy permissions are outside scope.
  Scenario tags describe implementation status; some core rules already exist.

  Background:
    Given the application contains the following people:
      | Person       | Role         | Department |
      | Nguyen Van A | Employee     | Sales      |
      | Tran Thi B   | Employee     | Finance    |
      | Le Minh      | Line Manager | Sales      |
    And "Le Minh" is assigned as Line Manager of the "Sales" department
    And the permission role "Manager Role" exists

  @implemented
  Scenario: Configure employee profile access for direct reports only
    Given an HRIS Administrator opens Admin Center at "/AdminCenter"
    When the administrator selects "Manage Permission Roles" from the Tools list
    And opens "Manager Role" from Permission Role List at "/ManagePermissionRoles"
    And opens "Permission..." in "2. Permission settings" at "/ManagePermissionRoles/EditAdministrators?role=Manager%20Role"
    Then the permission settings dialog offers employee profile read access for direct reports only
    When the administrator enables employee profile read access for direct reports only
    And confirms the permission settings
    Then the role configuration includes employee profile read access for direct reports only

  @pending
  Scenario: Restrict the target population to the manager's assigned department
    Given an HRIS Administrator is editing "Manager Role"
    And employee profile read access for direct reports only is enabled
    When the administrator configures "Permission requiring target"
    And selects the manager's assigned department as the target population
    Then the role configuration restricts employee profile read access to that department

  @pending
  Scenario: Grant Manager Role to Le Minh and save its configuration
    Given an HRIS Administrator is editing "Manager Role"
    And employee profile read access for direct reports only is enabled
    And the target population is the manager's assigned department
    When the administrator selects "Le Minh" in "3. Grant this role to..."
    And clicks "Save Changes"
    And reopens "Manager Role"
    Then employee profile read access for direct reports only is still enabled
    And the target population is still the manager's assigned department
    And "Le Minh" is assigned "Manager Role"
    And the saved configuration is used by the core employee profile authorization check

  @pending
  Scenario: Sign in with the configured Manager Role
    Given an HRIS Administrator has saved "Manager Role" with employee profile read access for direct reports only
    And its target population is the manager's assigned department
    And the administrator has granted "Manager Role" to "Le Minh"
    When "Le Minh" signs in
    Then the application identifies the signed-in employee as "Le Minh"
    And uses the granted "Manager Role" for employee profile authorization

  @pending
  Scenario: Allow a manager to read a profile in the assigned department
    Given "Le Minh" is signed in with the saved "Manager Role"
    And the role grants employee profile read access for direct reports only
    And its target population is the manager's assigned department
    When "Le Minh" opens the employee profile of "Nguyen Van A"
    Then the profile workflow checks core authorization before returning any profile information
    And the system allows access
    And displays the employee profile of "Nguyen Van A"

  @pending
  Scenario: Deny a manager access to a profile outside the assigned department
    Given "Le Minh" is signed in with the saved "Manager Role"
    And the role grants employee profile read access for direct reports only
    And its target population is the manager's assigned department
    When "Le Minh" opens the employee profile of "Tran Thi B"
    Then the profile workflow checks core authorization before returning any profile information
    And the system returns an explicit access-denied result
    And no employee profile information for "Tran Thi B" is returned or displayed

  @pending
  Scenario Outline: Deny profile access by default when department information is missing
    Given "Le Minh" is signed in with the saved "Manager Role"
    And the role grants employee profile read access for direct reports only
    And its target population is the manager's assigned department
    And <missing_department>
    When "Le Minh" opens the employee profile of "Nguyen Van A"
    Then the profile workflow checks core authorization before returning any profile information
    And the system returns an explicit access-denied result
    And no employee profile information for "Nguyen Van A" is returned or displayed

    Examples:
      | missing_department                    |
      | "Nguyen Van A" has no department      |
      | "Le Minh" has no assigned department  |

  @pending
  Scenario: Deny profile access without the required profile permission
    Given "Le Minh" is signed in
    And "Le Minh" has not been granted employee profile read access for direct reports only
    When "Le Minh" opens the employee profile of "Nguyen Van A"
    Then the profile workflow checks core authorization before returning any profile information
    And the system returns an explicit access-denied result
    And no employee profile information for "Nguyen Van A" is returned or displayed
