namespace SuccessFactorsLike.Domain;

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected
}

public sealed class LeaveRequest
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployeeId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string Reason { get; }
    public LeaveStatus Status { get; private set; } = LeaveStatus.Pending;

    public LeaveRequest(Guid employeeId, DateTime startDate, DateTime endDate, string reason)
    {
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
    }

    public int Days => (EndDate.Date - StartDate.Date).Days + 1;

    public void Approve() => Status = LeaveStatus.Approved;
    public void Reject() => Status = LeaveStatus.Rejected;
}
