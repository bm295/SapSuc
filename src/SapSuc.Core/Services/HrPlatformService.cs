using SuccessFactorsLike.Domain;

namespace SuccessFactorsLike.Services;

public sealed class HrPlatformService
{
    private readonly List<Employee> _employees = [];
    private readonly List<Goal> _goals = [];
    private readonly List<PerformanceReview> _reviews = [];
    private readonly List<LeaveRequest> _leaveRequests = [];
    private readonly List<CompensationRecord> _compensations = [];
    private readonly Dictionary<Guid, int> _leaveBalances = [];
    private readonly List<ProxyAssignment> _proxyAssignments = [];

    public Employee HireEmployee(string employeeNumber, string firstName, string lastName, string department, string title, DateTime hireDate, int yearlyLeaveEntitlement)
    {
        var employee = new Employee(employeeNumber, firstName, lastName, department, title, hireDate);
        _employees.Add(employee);
        _leaveBalances[employee.Id] = yearlyLeaveEntitlement;
        return employee;
    }

    public Goal AddGoal(Guid employeeId, string title, string description, DateTime dueDate)
    {
        var goal = new Goal(employeeId, title, description, dueDate);
        _goals.Add(goal);
        return goal;
    }

    public PerformanceReview AddReview(Guid employeeId, int year, decimal rating, string summary)
    {
        var review = new PerformanceReview(employeeId, year, rating, summary);
        _reviews.Add(review);
        return review;
    }

    public LeaveRequest RequestLeave(Guid employeeId, DateTime startDate, DateTime endDate, string reason)
    {
        var req = new LeaveRequest(employeeId, startDate, endDate, reason);
        _leaveRequests.Add(req);
        return req;
    }

    public bool ApproveLeave(Guid leaveRequestId)
    {
        var request = _leaveRequests.FirstOrDefault(x => x.Id == leaveRequestId);
        if (request is null || request.Status != LeaveStatus.Pending)
            return false;

        return ApproveLeaveInternal(request);
    }

    public bool ApproveLeaveAs(Guid actingEmployeeId, Guid leaveRequestId, DateTime? actionDate = null)
    {
        var request = _leaveRequests.FirstOrDefault(x => x.Id == leaveRequestId);
        if (request is null || request.Status != LeaveStatus.Pending)
            return false;

        var currentDate = (actionDate ?? DateTime.UtcNow).Date;
        var canApproveAsProxy = _proxyAssignments.Any(x =>
            x.ProxyEmployeeId == actingEmployeeId &&
            x.DelegatorEmployeeId == request.EmployeeId &&
            x.CanApproveLeave &&
            x.IsActiveOn(currentDate));

        if (!canApproveAsProxy)
            return false;

        return ApproveLeaveInternal(request);
    }

    private bool ApproveLeaveInternal(LeaveRequest request)
    {
        var remaining = _leaveBalances.GetValueOrDefault(request.EmployeeId, 0);
        if (remaining < request.Days)
        {
            request.Reject();
            return false;
        }

        _leaveBalances[request.EmployeeId] = remaining - request.Days;
        request.Approve();
        return true;
    }


    public ProxyAssignment AssignProxy(Guid delegatorEmployeeId, Guid proxyEmployeeId, DateTime startDate, DateTime endDate, bool canApproveLeave = true)
    {
        EnsureEmployeeExists(delegatorEmployeeId);
        EnsureEmployeeExists(proxyEmployeeId);

        var assignment = new ProxyAssignment(delegatorEmployeeId, proxyEmployeeId, startDate, endDate, canApproveLeave);
        _proxyAssignments.Add(assignment);
        return assignment;
    }

    public CompensationRecord SetCompensation(Guid employeeId, decimal baseSalary, string currency, DateTime effectiveDate)
    {
        var record = new CompensationRecord(employeeId, baseSalary, currency, effectiveDate);
        _compensations.Add(record);
        return record;
    }

    public IEnumerable<Employee> Employees => _employees;
    public IEnumerable<Goal> Goals => _goals;
    public IEnumerable<PerformanceReview> Reviews => _reviews;
    public IEnumerable<LeaveRequest> LeaveRequests => _leaveRequests;
    public IEnumerable<CompensationRecord> Compensations => _compensations;
    public IEnumerable<ProxyAssignment> ProxyAssignments => _proxyAssignments;

    public int GetLeaveBalance(Guid employeeId) => _leaveBalances.GetValueOrDefault(employeeId, 0);

    private void EnsureEmployeeExists(Guid employeeId)
    {
        if (_employees.All(x => x.Id != employeeId))
            throw new InvalidOperationException($"Employee {employeeId} does not exist.");
    }
}
