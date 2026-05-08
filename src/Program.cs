using SuccessFactorsLike.Domain;
using SuccessFactorsLike.Services;

var platform = new HrPlatformService();

var employee = platform.HireEmployee(
    employeeNumber: "E-1001",
    firstName: "Ava",
    lastName: "Miller",
    department: "Engineering",
    title: "Software Engineer",
    hireDate: new DateTime(2024, 2, 12),
    yearlyLeaveEntitlement: 20);

platform.SetCompensation(employee.Id, 120000m, "USD", DateTime.UtcNow.Date);

var proxy = platform.HireEmployee(
    employeeNumber: "E-2001",
    firstName: "Noah",
    lastName: "Davis",
    department: "Engineering",
    title: "Engineering Manager",
    hireDate: new DateTime(2020, 8, 5),
    yearlyLeaveEntitlement: 25);

var goal = platform.AddGoal(
    employee.Id,
    "Launch onboarding portal",
    "Deliver MVP onboarding portal for all new hires.",
    DateTime.UtcNow.Date.AddMonths(3));
goal.UpdateStatus(GoalStatus.InProgress);

platform.AddReview(employee.Id, 2026, 4.5m, "Strong delivery and collaboration.");

var leave = platform.RequestLeave(
    employee.Id,
    DateTime.UtcNow.Date.AddDays(14),
    DateTime.UtcNow.Date.AddDays(16),
    "Family event");
platform.AssignProxy(employee.Id, proxy.Id, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(30));
platform.ApproveLeaveAs(proxy.Id, leave.Id);

Console.WriteLine("=== SuccessFactors-Style HR Snapshot ===");
Console.WriteLine($"Employee: {employee.EmployeeNumber} - {employee.FullName}");
Console.WriteLine($"Org: {employee.Department} / {employee.JobTitle}");
Console.WriteLine($"Goal: {goal.Title} [{goal.Status}] due {goal.DueDate:yyyy-MM-dd}");
Console.WriteLine($"Leave request: {leave.Status}, days={leave.Days}, remaining={platform.GetLeaveBalance(employee.Id)}");
Console.WriteLine($"Proxy approver: {proxy.FullName}");
Console.WriteLine("Compensation records:");
foreach (var comp in platform.Compensations.Where(c => c.EmployeeId == employee.Id))
{
    Console.WriteLine($"- {comp.BaseSalary} {comp.Currency} effective {comp.EffectiveDate:yyyy-MM-dd}");
}
