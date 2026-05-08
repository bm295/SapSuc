namespace SuccessFactorsLike.Domain;

public sealed class ProxyAssignment
{
    private readonly HashSet<string> _toolPermissions;

    public Guid Id { get; } = Guid.NewGuid();
    public Guid DelegatorEmployeeId { get; }
    public Guid ProxyEmployeeId { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public bool CanApproveLeave { get; }
    public bool HasAllToolAccess { get; }
    public IReadOnlyCollection<string> ToolPermissions => _toolPermissions;

    public ProxyAssignment(
        Guid delegatorEmployeeId,
        Guid proxyEmployeeId,
        DateTime startDate,
        DateTime endDate,
        bool canApproveLeave,
        bool hasAllToolAccess = false,
        IEnumerable<string>? toolPermissions = null)
    {
        if (delegatorEmployeeId == proxyEmployeeId)
            throw new ArgumentException("Delegator and proxy must be different employees.");
        if (endDate < startDate)
            throw new ArgumentException("End date must be on or after start date.");

        DelegatorEmployeeId = delegatorEmployeeId;
        ProxyEmployeeId = proxyEmployeeId;
        StartDate = startDate;
        EndDate = endDate;
        HasAllToolAccess = hasAllToolAccess;
        _toolPermissions = toolPermissions is null
            ? []
            : new HashSet<string>(toolPermissions.Where(tool => !string.IsNullOrWhiteSpace(tool)), StringComparer.OrdinalIgnoreCase);
        CanApproveLeave = canApproveLeave || HasAllToolAccess || GrantsLeaveApproval(_toolPermissions);
    }

    public bool IsActiveOn(DateTime instant) => instant >= StartDate && instant <= EndDate;

    private static bool GrantsLeaveApproval(IEnumerable<string> toolPermissions)
    {
        return toolPermissions.Any(tool =>
            tool.Equals("Leave", StringComparison.OrdinalIgnoreCase) ||
            tool.Equals("Leave Approval", StringComparison.OrdinalIgnoreCase) ||
            tool.Equals("Time Off", StringComparison.OrdinalIgnoreCase));
    }
}
