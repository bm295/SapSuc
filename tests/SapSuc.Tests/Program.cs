using SuccessFactorsLike.Services;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("LineManagerWithDirectReportsOnlyPermissionCanViewSalesEmployeeButCannotViewFinanceEmployee", LineManagerWithDirectReportsOnlyPermissionCanViewSalesEmployeeButCannotViewFinanceEmployee),
            ("LineManagerWithoutAssignedDepartmentCannotViewEmployeeProfile", LineManagerWithoutAssignedDepartmentCannotViewEmployeeProfile),
            ("LineManagerWithoutDirectReportsOnlyPermissionCannotViewEmployeeProfile", LineManagerWithoutDirectReportsOnlyPermissionCannotViewEmployeeProfile)
        };

        var failures = new List<string>();

        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{test.Name}: {ex.Message}");
                Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"{failures.Count} test(s) failed.");
            return 1;
        }

        Console.WriteLine($"{tests.Length} test(s) passed.");
        return 0;
    }

    private static void LineManagerWithDirectReportsOnlyPermissionCanViewSalesEmployeeButCannotViewFinanceEmployee()
    {
        var service = new HrPlatformService();
        var today = DateTime.Today;
        var salesEmployee = service.HireEmployee("SF-2001", "Nguyen", "Van A", "Sales", "Sales Executive", today.AddYears(-2), 18);
        var financeEmployee = service.HireEmployee("SF-2002", "Tran", "Thi B", "Finance", "Financial Analyst", today.AddYears(-2), 18);
        var manager = service.HireEmployee("SF-2003", "Le", "Minh", "Sales", "Line Manager", today.AddYears(-5), 20);

        service.AssignLineManagerToDepartment(manager.Id, "Sales");
        service.GrantDirectReportsOnlyProfileAccess(manager.Id);

        AssertTrue(service.CanViewEmployeeProfile(manager.Id, salesEmployee.Id), "Line manager should view an employee in the assigned department.");
        AssertFalse(service.CanViewEmployeeProfile(manager.Id, financeEmployee.Id), "Line manager should not view an employee outside the assigned department.");
    }

    private static void LineManagerWithoutAssignedDepartmentCannotViewEmployeeProfile()
    {
        var service = new HrPlatformService();
        var today = DateTime.Today;
        var employee = service.HireEmployee("SF-2011", "Nguyen", "Van A", "Sales", "Sales Executive", today.AddYears(-2), 18);
        var manager = service.HireEmployee("SF-2012", "Le", "Minh", "Sales", "Line Manager", today.AddYears(-5), 20);

        service.GrantDirectReportsOnlyProfileAccess(manager.Id);

        AssertFalse(service.CanViewEmployeeProfile(manager.Id, employee.Id), "Manager without an assigned department should be denied by default.");
    }

    private static void LineManagerWithoutDirectReportsOnlyPermissionCannotViewEmployeeProfile()
    {
        var service = new HrPlatformService();
        var today = DateTime.Today;
        var employee = service.HireEmployee("SF-2021", "Nguyen", "Van A", "Sales", "Sales Executive", today.AddYears(-2), 18);
        var manager = service.HireEmployee("SF-2022", "Le", "Minh", "Sales", "Line Manager", today.AddYears(-5), 20);

        service.AssignLineManagerToDepartment(manager.Id, "Sales");

        AssertFalse(service.CanViewEmployeeProfile(manager.Id, employee.Id), "Manager without direct-reports-only profile permission should be denied.");
    }

    private static void AssertTrue(bool actual, string message)
    {
        if (!actual)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertFalse(bool actual, string message)
    {
        if (actual)
        {
            throw new InvalidOperationException(message);
        }
    }
}
