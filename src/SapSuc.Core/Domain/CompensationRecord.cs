namespace SuccessFactorsLike.Domain;

public sealed class CompensationRecord
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployeeId { get; }
    public decimal BaseSalary { get; }
    public string Currency { get; }
    public DateTime EffectiveDate { get; }

    public CompensationRecord(Guid employeeId, decimal baseSalary, string currency, DateTime effectiveDate)
    {
        EmployeeId = employeeId;
        BaseSalary = baseSalary;
        Currency = currency;
        EffectiveDate = effectiveDate;
    }
}
