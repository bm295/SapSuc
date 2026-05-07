namespace SuccessFactorsLike.Domain;

public enum GoalStatus
{
    NotStarted,
    InProgress,
    Completed
}

public sealed class Goal
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployeeId { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime DueDate { get; }
    public GoalStatus Status { get; private set; } = GoalStatus.NotStarted;

    public Goal(Guid employeeId, string title, string description, DateTime dueDate)
    {
        EmployeeId = employeeId;
        Title = title;
        Description = description;
        DueDate = dueDate;
    }

    public void UpdateStatus(GoalStatus status) => Status = status;
}
