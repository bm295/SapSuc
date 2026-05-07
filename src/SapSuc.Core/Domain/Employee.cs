namespace SuccessFactorsLike.Domain;

public sealed class Employee
{
    public Guid Id { get; }
    public string EmployeeNumber { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Department { get; private set; }
    public string JobTitle { get; private set; }
    public DateTime HireDate { get; }

    public Employee(string employeeNumber, string firstName, string lastName, string department, string jobTitle, DateTime hireDate)
    {
        Id = Guid.NewGuid();
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        Department = department;
        JobTitle = jobTitle;
        HireDate = hireDate;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void Transfer(string newDepartment, string newJobTitle)
    {
        Department = newDepartment;
        JobTitle = newJobTitle;
    }
}
