namespace SuccessFactorsLike.Domain;

public sealed class PerformanceReview
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployeeId { get; }
    public int Year { get; }
    public decimal Rating { get; }
    public string Summary { get; }

    public PerformanceReview(Guid employeeId, int year, decimal rating, string summary)
    {
        EmployeeId = employeeId;
        Year = year;
        Rating = rating;
        Summary = summary;
    }
}
