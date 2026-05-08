namespace SuccessFactorsLike.Domain;

public sealed class ProxyAssignment
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid DelegatorEmployeeId { get; }
    public Guid ProxyEmployeeId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public bool CanApproveLeave { get; }

    public ProxyAssignment(Guid delegatorEmployeeId, Guid proxyEmployeeId, DateTime startDate, DateTime endDate, bool canApproveLeave)
    {
        if (delegatorEmployeeId == proxyEmployeeId)
            throw new ArgumentException("Delegator and proxy must be different employees.");
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("End date must be on or after start date.");

        DelegatorEmployeeId = delegatorEmployeeId;
        ProxyEmployeeId = proxyEmployeeId;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        CanApproveLeave = canApproveLeave;
    }

    public bool IsActiveOn(DateTime day) => day.Date >= StartDate && day.Date <= EndDate;
}
