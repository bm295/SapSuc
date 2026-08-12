using SuccessFactorsLike.Services;

internal static class HrPlatformSeeder
{
    public static HrPlatformService Create()
    {
        var service = new HrPlatformService();
        var today = DateTime.Today;

        var aisha = service.HireEmployee("SF-1001", "Aisha", "Nguyen", "People Operations", "HR Director", today.AddYears(-7), 24, "ASG-9001");
        var minh = service.HireEmployee("SF-1014", "Minh", "Tran", "Engineering", "Engineering Manager", today.AddYears(-5), 20, "ASG-9014");
        var sofia = service.HireEmployee("SF-1022", "Sofia", "Martinez", "Finance", "Payroll Lead", today.AddYears(-4), 18, "ASG-9022");
        var daniel = service.HireEmployee("SF-1038", "Daniel", "Kim", "Customer Success", "Regional Manager", today.AddYears(-6), 22, "ASG-9038");
        var priya = service.HireEmployee("SF-1045", "Priya", "Shah", "Legal", "Compliance Counsel", today.AddYears(-3), 18, "ASG-9045");
        var ethan = service.HireEmployee("SF-1057", "Ethan", "Reed", "Operations", "Workforce Analyst", today.AddYears(-2), 16, "ASG-9057");

        service.AssignProxy(aisha.Id, ethan.Id, today.AddDays(-5), today.AddDays(9), canApproveLeave: true);
        service.AssignProxy(minh.Id, sofia.Id, today.AddDays(4), today.AddDays(18), canApproveLeave: true);
        service.AssignProxy(priya.Id, aisha.Id, today.AddDays(-12), today.AddDays(2), canApproveLeave: false);
        service.AssignProxy(daniel.Id, minh.Id, today.AddDays(-38), today.AddDays(-11), canApproveLeave: true);

        return service;
    }
}
