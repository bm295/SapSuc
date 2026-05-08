using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualBasic.FileIO;
using SuccessFactorsLike.Domain;
using SuccessFactorsLike.Services;

namespace SapSuc.Web.Pages;

public class IndexModel : PageModel
{
    private const string ProxiedEmployeeSessionKey = "SapSuc.ProxyMode.EmployeeId";
    private const int DefaultProxySlotCount = 3;
    private const string SapProxyDateFormat = "yyyy-MM-dd HH:mm";
    private static readonly DateTime OpenEndedProxyEndDate = DateTime.MaxValue;

    private readonly HrPlatformService _hrPlatform;

    public IndexModel(HrPlatformService hrPlatform)
    {
        _hrPlatform = hrPlatform;
    }

    [BindProperty]
    public ProxyAssignmentInput Input { get; set; } = new();

    [BindProperty]
    public ProxySessionInput ProxySession { get; set; } = new();

    [BindProperty]
    public ProxyImportInput Import { get; set; } = new();

    [BindProperty]
    public ProxySettingsInput Settings { get; set; } = new();

    [BindProperty]
    public ProxyRemoveInput Remove { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public string StatusFilter { get; private set; } = "all";
    public IReadOnlyList<SelectListItem> EmployeeOptions { get; private set; } = [];
    public IReadOnlyList<ProxyAssignmentRow> Assignments { get; private set; } = [];
    public ProxySummary Summary { get; private set; } = new(0, 0, 0, 0);
    public ProxyViewContext CurrentView { get; private set; } = ProxyViewContext.Admin(0, 0, 0);
    public bool ProxySelfServiceEnabled { get; private set; }
    public bool CanCreateProxyAssignments { get; private set; } = true;
    public bool CanImportProxyCsv { get; private set; } = true;
    public string AssignmentOwnerLabel { get; private set; } = "Selected account holder";
    public int ProxySlotCount => DefaultProxySlotCount;

    public void OnGet()
    {
        PreparePage(initializeInput: true);
    }

    public IActionResult OnPostAssign()
    {
        var employees = _hrPlatform.Employees.ToList();
        var proxiedEmployee = GetProxiedEmployee(employees);
        var selectedProxyEmployeeIds = ReadSelectedProxyEmployeeIds();
        Input.ProxyEmployeeIds = selectedProxyEmployeeIds;

        if (proxiedEmployee is not null)
        {
            if (!_hrPlatform.AllowEmployeeProxySelfService)
            {
                ModelState.AddModelError(string.Empty, "Proxy self-service is disabled. Only administrators can assign proxies.");
            }

            if (Input.DelegatorEmployeeId != proxiedEmployee.Id)
            {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.DelegatorEmployeeId)}", "You can only assign proxies for your own account.");
            }
        }

        if (selectedProxyEmployeeIds.Count == 0)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ProxyEmployeeIds)}", "Choose at least one proxy employee.");
        }

        if (selectedProxyEmployeeIds.Contains(Input.DelegatorEmployeeId))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ProxyEmployeeIds)}", "Choose proxy employees who are different from the delegator.");
        }

        if (Input.EndDate.Date < Input.StartDate.Date)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.EndDate)}", "End date must be on or after the start date.");
        }

        if (!ModelState.IsValid)
        {
            PreparePage();
            return Page();
        }

        try
        {
            var assignments = _hrPlatform.AssignProxies(
                Input.DelegatorEmployeeId,
                selectedProxyEmployeeIds,
                Input.StartDate,
                Input.EndDate,
                Input.CanApproveLeave);

            TempData["SuccessMessage"] = $"{assignments.Count} proxy assignment{(assignments.Count == 1 ? string.Empty : "s")} created.";
            return RedirectToPage(new { status = "all" });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PreparePage();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostImport()
    {
        if (GetProxiedEmployee(_hrPlatform.Employees.ToList()) is not null)
        {
            ModelState.AddModelError($"{nameof(Import)}.{nameof(Import.CsvFile)}", "CSV proxy import is only available to administrators.");
        }

        if (Import.CsvFile is null || Import.CsvFile.Length == 0)
        {
            ModelState.AddModelError($"{nameof(Import)}.{nameof(Import.CsvFile)}", "Choose a CSV file to import.");
        }
        else if (!string.Equals(Path.GetExtension(Import.CsvFile.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError($"{nameof(Import)}.{nameof(Import.CsvFile)}", "Upload a .csv file.");
        }

        if (!ModelState.IsValid)
        {
            PreparePage(initializeInput: true);
            return Page();
        }

        await using var stream = Import.CsvFile!.OpenReadStream();
        using var reader = new StreamReader(stream);

        var result = ImportProxyCsv(reader);

        if (result.Created == 0 && result.Removed == 0 && result.Errors.Count > 0)
        {
            ModelState.AddModelError($"{nameof(Import)}.{nameof(Import.CsvFile)}", result.Errors[0]);
            foreach (var error in result.Errors.Skip(1))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            PreparePage(initializeInput: true);
            return Page();
        }

        TempData["SuccessMessage"] = $"CSV import complete: {result.Created} assigned, {result.Removed} removed.";

        if (result.Errors.Count > 0)
        {
            TempData["WarningMessage"] = string.Join(" ", result.Errors.Take(3)) +
                (result.Errors.Count > 3 ? $" {result.Errors.Count - 3} more row errors." : string.Empty);
        }

        return RedirectToPage(new { status = "all" });
    }

    public IActionResult OnPostUpdateSettings()
    {
        if (GetProxiedEmployee(_hrPlatform.Employees.ToList()) is not null)
        {
            TempData["WarningMessage"] = "Return to administrator view to change proxy settings.";
            return RedirectToPage(new { status = NormalizeStatus(Status) });
        }

        _hrPlatform.UpdateProxySelfService(Settings.AllowEmployeeProxySelfService);
        TempData["SuccessMessage"] = Settings.AllowEmployeeProxySelfService
            ? "Employees can now assign and remove their own proxies."
            : "Proxy self-service is locked to administrators.";

        return RedirectToPage(new { status = NormalizeStatus(Status) });
    }

    public IActionResult OnPostRemove()
    {
        var employees = _hrPlatform.Employees.ToList();
        var proxiedEmployee = GetProxiedEmployee(employees);
        var assignment = _hrPlatform.ProxyAssignments.FirstOrDefault(candidate => candidate.Id == Remove.AssignmentId);

        if (assignment is null)
        {
            TempData["WarningMessage"] = "Proxy assignment could not be found.";
            return RedirectToPage(new { status = NormalizeStatus(Status) });
        }

        if (proxiedEmployee is not null)
        {
            if (!_hrPlatform.AllowEmployeeProxySelfService)
            {
                TempData["WarningMessage"] = "Proxy self-service is disabled. Only administrators can remove proxies.";
                return RedirectToPage(new { status = NormalizeStatus(Status) });
            }

            if (assignment.DelegatorEmployeeId != proxiedEmployee.Id)
            {
                TempData["WarningMessage"] = "You can only remove proxies from your own account.";
                return RedirectToPage(new { status = NormalizeStatus(Status) });
            }
        }

        TempData["SuccessMessage"] = _hrPlatform.RemoveProxyAssignment(assignment.Id)
            ? "Proxy assignment removed."
            : "Proxy assignment could not be removed.";

        return RedirectToPage(new { status = NormalizeStatus(Status) });
    }

    private ProxyImportResult ImportProxyCsv(TextReader reader)
    {
        var employees = _hrPlatform.Employees.ToList();
        var userIdLookup = CreateUserIdLookup(employees);
        var assignmentIdLookup = CreateAssignmentIdLookup(employees);
        var created = 0;
        var removed = 0;
        var errors = new List<string>();

        using var parser = new TextFieldParser(reader)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            return new ProxyImportResult(0, 0, ["CSV file is empty."]);
        }

        var headers = parser.ReadFields();
        if (headers is null)
        {
            return new ProxyImportResult(0, 0, ["CSV header is missing."]);
        }

        var columns = SapProxyImportColumns.From(headers);
        if (!columns.IsValid)
        {
            return new ProxyImportResult(0, 0, ["CSV must include USERID, ASSIGNMENT_ID_USERID, PROXYID, ASSIGNMENT_ID_PROXYID, START_DATE(yyyy-MM-dd HH:mm), END_DATE(yyyy-MM-dd HH:mm), All, and Remove All columns."]);
        }

        while (!parser.EndOfData)
        {
            var lineNumber = parser.LineNumber;
            string[]? fields;

            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException ex)
            {
                errors.Add($"Line {lineNumber}: malformed CSV row. {ex.Message}");
                continue;
            }

            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                var userId = GetOptionalField(fields, columns.UserIdIndex);
                var assignmentIdUserId = GetOptionalField(fields, columns.AssignmentIdUserIdIndex);
                var proxyId = GetOptionalField(fields, columns.ProxyIdIndex);
                var assignmentIdProxyId = GetOptionalField(fields, columns.AssignmentIdProxyIdIndex);
                var delegator = ResolveAccountHolder(userId, assignmentIdUserId, userIdLookup, assignmentIdLookup);
                var proxies = ResolveProxyEmployees(proxyId, assignmentIdProxyId, userId, assignmentIdUserId, userIdLookup, assignmentIdLookup);
                var startDate = ReadRequiredSapProxyDate(fields, columns.StartDateIndex);
                var endDate = ReadOptionalSapProxyDate(fields, columns.EndDateIndex) ?? OpenEndedProxyEndDate;
                var removeAll = IsYes(GetOptionalField(fields, columns.RemoveAllIndex));
                var hasAllToolAccess = IsYes(GetOptionalField(fields, columns.AllIndex));
                var toolPermissions = ReadToolPermissions(fields, columns.ToolColumns);

                if (removeAll)
                {
                    foreach (var proxy in proxies)
                    {
                        var removedForProxy = _hrPlatform.RemoveProxyAssignments(delegator.Id, proxy.Id, startDate, endDate == OpenEndedProxyEndDate ? null : endDate);

                        if (removedForProxy == 0)
                        {
                            errors.Add($"Line {lineNumber}: no matching assignment to remove for {proxy.EmployeeNumber}.");
                        }
                        else
                        {
                            removed += removedForProxy;
                        }
                    }

                    continue;
                }

                if (!hasAllToolAccess && toolPermissions.Count == 0)
                {
                    errors.Add($"Line {lineNumber}: enter YES in All, Remove All, or at least one module column.");
                    continue;
                }

                foreach (var proxy in proxies)
                {
                    _hrPlatform.AssignProxy(
                        delegator.Id,
                        proxy.Id,
                        startDate,
                        endDate,
                        canApproveLeave: false,
                        hasAllToolAccess,
                        toolPermissions);
                    created++;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
            }
        }

        return new ProxyImportResult(created, removed, errors);
    }

    private static Dictionary<string, Employee> CreateUserIdLookup(IEnumerable<Employee> employees)
    {
        var lookup = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var employee in employees)
        {
            lookup[employee.Id.ToString()] = employee;
            lookup[employee.EmployeeNumber] = employee;
        }

        return lookup;
    }

    private static Dictionary<string, Employee> CreateAssignmentIdLookup(IEnumerable<Employee> employees)
    {
        var lookup = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var employee in employees)
        {
            lookup[employee.AssignmentId] = employee;
        }

        return lookup;
    }

    private static Employee ResolveAccountHolder(
        string userId,
        string assignmentIdUserId,
        IReadOnlyDictionary<string, Employee> userIdLookup,
        IReadOnlyDictionary<string, Employee> assignmentIdLookup)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return ResolveEmployee(userId, userIdLookup, "USERID");
        }

        if (!string.IsNullOrWhiteSpace(assignmentIdUserId))
        {
            return ResolveEmployee(assignmentIdUserId, assignmentIdLookup, "ASSIGNMENT_ID_USERID");
        }

        throw new FormatException("USERID or ASSIGNMENT_ID_USERID is required.");
    }

    private static IReadOnlyList<Employee> ResolveProxyEmployees(
        string proxyId,
        string assignmentIdProxyId,
        string userId,
        string assignmentIdUserId,
        IReadOnlyDictionary<string, Employee> userIdLookup,
        IReadOnlyDictionary<string, Employee> assignmentIdLookup)
    {
        if (!string.IsNullOrWhiteSpace(proxyId) && !string.IsNullOrWhiteSpace(assignmentIdProxyId))
        {
            throw new FormatException("Use PROXYID or ASSIGNMENT_ID_PROXYID, not both.");
        }

        if (!string.IsNullOrWhiteSpace(assignmentIdProxyId))
        {
            if (!string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(assignmentIdUserId))
            {
                throw new FormatException("ASSIGNMENT_ID_PROXYID must be used with ASSIGNMENT_ID_USERID and blank USERID.");
            }

            return SplitPipeValues(assignmentIdProxyId)
                .Select(value => ResolveEmployee(value, assignmentIdLookup, "ASSIGNMENT_ID_PROXYID"))
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(proxyId))
        {
            throw new FormatException("PROXYID or ASSIGNMENT_ID_PROXYID is required.");
        }

        return SplitPipeValues(proxyId)
            .Select(value => ResolveEmployee(value, userIdLookup, "PROXYID"))
            .ToList();
    }

    private static Employee ResolveEmployee(string value, IReadOnlyDictionary<string, Employee> lookup, string fieldName)
    {
        if (lookup.TryGetValue(value, out var employee))
        {
            return employee;
        }

        throw new InvalidOperationException($"Unknown {fieldName} '{value}'.");
    }

    private static IReadOnlyList<string> SplitPipeValues(string value)
    {
        var values = value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (values.Count == 0)
        {
            throw new FormatException("Proxy list is empty.");
        }

        return values;
    }

    private static DateTime ReadRequiredSapProxyDate(IReadOnlyList<string> fields, int index)
    {
        var value = GetOptionalField(fields, index);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("START_DATE is required.");
        }

        return ParseSapProxyDate(value, "START_DATE");
    }

    private static DateTime? ReadOptionalSapProxyDate(IReadOnlyList<string> fields, int index)
    {
        var value = GetOptionalField(fields, index);
        return string.IsNullOrWhiteSpace(value) ? null : ParseSapProxyDate(value, "END_DATE");
    }

    private static DateTime ParseSapProxyDate(string value, string fieldName)
    {
        if (DateTime.TryParseExact(value, SapProxyDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        throw new FormatException($"{fieldName} must use {SapProxyDateFormat} format.");
    }

    private static IReadOnlyList<string> ReadToolPermissions(IReadOnlyList<string> fields, IEnumerable<SapProxyToolColumn> toolColumns)
    {
        return toolColumns
            .Where(column => IsYes(GetOptionalField(fields, column.Index)))
            .Select(column => column.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsYes(string value)
    {
        return value.Equals("YES", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOptionalField(IReadOnlyList<string> fields, int index)
    {
        return index >= 0 && index < fields.Count ? fields[index].Trim() : string.Empty;
    }

    private List<Guid> ReadSelectedProxyEmployeeIds()
    {
        var selectedProxyEmployeeIds = Input.ProxyEmployeeIds
            .Where(employeeId => employeeId != Guid.Empty)
            .ToList();

        if (selectedProxyEmployeeIds.Count > 0 ||
            !Request.HasFormContentType ||
            !Request.Form.TryGetValue($"{nameof(Input)}.{nameof(Input.ProxyEmployeeIds)}", out var rawProxyEmployeeIds))
        {
            return selectedProxyEmployeeIds.Distinct().ToList();
        }

        foreach (var rawProxyEmployeeId in rawProxyEmployeeIds)
        {
            if (string.IsNullOrWhiteSpace(rawProxyEmployeeId))
            {
                continue;
            }

            foreach (var candidate in rawProxyEmployeeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(candidate, out var proxyEmployeeId))
                {
                    selectedProxyEmployeeIds.Add(proxyEmployeeId);
                }
            }
        }

        return selectedProxyEmployeeIds.Distinct().ToList();
    }

    public IActionResult OnPostSwitchUser()
    {
        var employees = _hrPlatform.Employees.ToList();
        var selectedEmployee = employees.FirstOrDefault(employee => employee.Id == ProxySession.EmployeeId);

        if (selectedEmployee is null)
        {
            ModelState.AddModelError($"{nameof(ProxySession)}.{nameof(ProxySession.EmployeeId)}", "Choose an employee to proxy as.");
            PreparePage(initializeInput: true);
            return Page();
        }

        HttpContext.Session.SetString(ProxiedEmployeeSessionKey, selectedEmployee.Id.ToString());
        TempData["SuccessMessage"] = $"Proxy view switched to {selectedEmployee.FullName}.";

        return RedirectToPage(new { status = NormalizeStatus(Status) });
    }

    public IActionResult OnPostClearProxy()
    {
        HttpContext.Session.Remove(ProxiedEmployeeSessionKey);
        TempData["SuccessMessage"] = "Returned to administrator view.";

        return RedirectToPage(new { status = NormalizeStatus(Status) });
    }

    private void PreparePage(bool initializeInput = false)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var employees = _hrPlatform.Employees
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToList();

        EmployeeOptions = employees
            .Select(employee => new SelectListItem
            {
                Value = employee.Id.ToString(),
                Text = $"{employee.FullName} - {employee.EmployeeNumber} - {employee.AssignmentId}"
            })
            .ToList();

        var proxiedEmployee = GetProxiedEmployee(employees);
        ProxySelfServiceEnabled = _hrPlatform.AllowEmployeeProxySelfService;
        CanCreateProxyAssignments = proxiedEmployee is null || ProxySelfServiceEnabled;
        CanImportProxyCsv = proxiedEmployee is null;
        Settings = new ProxySettingsInput
        {
            AllowEmployeeProxySelfService = ProxySelfServiceEnabled
        };
        ProxySession = new ProxySessionInput
        {
            EmployeeId = proxiedEmployee?.Id ?? employees.FirstOrDefault()?.Id ?? Guid.Empty
        };
        AssignmentOwnerLabel = proxiedEmployee is null
            ? "Selected account holder"
            : $"{proxiedEmployee.FullName} - {proxiedEmployee.EmployeeNumber} - {proxiedEmployee.AssignmentId}";

        if (initializeInput)
        {
            Input = CreateDefaultInput(employees, today, proxiedEmployee);
        }
        else if (proxiedEmployee is not null)
        {
            Input.DelegatorEmployeeId = proxiedEmployee.Id;
        }

        var employeeLookup = employees.ToDictionary(employee => employee.Id);
        var allAssignments = _hrPlatform.ProxyAssignments
            .Select(assignment => MapAssignment(assignment, employeeLookup, now, proxiedEmployee, ProxySelfServiceEnabled))
            .OrderBy(row => row.SortRank)
            .ThenBy(row => row.EndDate)
            .ThenBy(row => row.DelegatorName)
            .ToList();

        var viewedAssignments = proxiedEmployee is null
            ? allAssignments
            : allAssignments
                .Where(row => row.DelegatorEmployeeId == proxiedEmployee.Id || row.ProxyEmployeeId == proxiedEmployee.Id)
                .ToList();

        Summary = new ProxySummary(
            viewedAssignments.Count(row => row.StatusFilter == "active"),
            viewedAssignments.Count(row => row.StatusFilter == "scheduled"),
            viewedAssignments.Count(row => row.StatusFilter == "active" && row.EndDate <= now.AddDays(7)),
            viewedAssignments.Count);

        CurrentView = CreateViewContext(proxiedEmployee, allAssignments, viewedAssignments);

        StatusFilter = NormalizeStatus(Status);
        Assignments = viewedAssignments
            .Where(row => StatusFilter == "all" || row.StatusFilter == StatusFilter)
            .ToList();
    }

    private Employee? GetProxiedEmployee(IReadOnlyList<Employee> employees)
    {
        var rawEmployeeId = HttpContext.Session.GetString(ProxiedEmployeeSessionKey);
        if (!Guid.TryParse(rawEmployeeId, out var employeeId))
        {
            return null;
        }

        var employee = employees.FirstOrDefault(candidate => candidate.Id == employeeId);
        if (employee is null)
        {
            HttpContext.Session.Remove(ProxiedEmployeeSessionKey);
        }

        return employee;
    }

    private static ProxyAssignmentInput CreateDefaultInput(IReadOnlyList<Employee> employees, DateTime today, Employee? proxiedEmployee)
    {
        var delegator = proxiedEmployee ?? employees.FirstOrDefault();
        var proxy = employees.FirstOrDefault(employee => employee.Id != delegator?.Id);
        var proxyEmployeeIds = Enumerable.Repeat(Guid.Empty, DefaultProxySlotCount).ToList();

        if (proxy is not null)
        {
            proxyEmployeeIds[0] = proxy.Id;
        }

        return new ProxyAssignmentInput
        {
            DelegatorEmployeeId = delegator?.Id ?? Guid.Empty,
            ProxyEmployeeIds = proxyEmployeeIds,
            StartDate = today,
            EndDate = today.AddDays(14),
            CanApproveLeave = true
        };
    }

    private static ProxyAssignmentRow MapAssignment(
        ProxyAssignment assignment,
        IReadOnlyDictionary<Guid, Employee> employees,
        DateTime now,
        Employee? proxiedEmployee,
        bool proxySelfServiceEnabled)
    {
        var delegator = employees[assignment.DelegatorEmployeeId];
        var proxy = employees[assignment.ProxyEmployeeId];
        var status = GetStatus(assignment, now);
        var duration = assignment.EndDate == OpenEndedProxyEndDate
            ? "Open-ended"
            : $"{(assignment.EndDate.Date - assignment.StartDate.Date).Days + 1} day{((assignment.EndDate.Date - assignment.StartDate.Date).Days == 0 ? string.Empty : "s")}";
        var canRemove = proxiedEmployee is null ||
            (proxySelfServiceEnabled && assignment.DelegatorEmployeeId == proxiedEmployee.Id);

        return new ProxyAssignmentRow(
            assignment.Id,
            assignment.DelegatorEmployeeId,
            assignment.ProxyEmployeeId,
            delegator.FullName,
            $"{delegator.EmployeeNumber} - {delegator.AssignmentId} - {delegator.JobTitle}",
            proxy.FullName,
            $"{proxy.EmployeeNumber} - {proxy.AssignmentId} - {proxy.Department}",
            $"{FormatDate(assignment.StartDate)} - {FormatDate(assignment.EndDate)}",
            duration,
            status.Label,
            status.CssClass,
            status.Filter,
            status.SortRank,
            assignment.EndDate,
            assignment.CanApproveLeave,
            assignment.HasAllToolAccess,
            CreateAccessLabel(assignment),
            canRemove);
    }

    private static ProxyViewContext CreateViewContext(
        Employee? proxiedEmployee,
        IReadOnlyList<ProxyAssignmentRow> allAssignments,
        IReadOnlyList<ProxyAssignmentRow> viewedAssignments)
    {
        if (proxiedEmployee is null)
        {
            return ProxyViewContext.Admin(
                allAssignments.Count,
                allAssignments.Count(row => row.StatusFilter == "active"),
                allAssignments.Count(row => row.CanApproveLeave));
        }

        return ProxyViewContext.Employee(
            proxiedEmployee.Id,
            proxiedEmployee.FullName,
            $"{proxiedEmployee.EmployeeNumber} - {proxiedEmployee.AssignmentId} - {proxiedEmployee.Department} - {proxiedEmployee.JobTitle}",
            viewedAssignments.Count(row => row.DelegatorEmployeeId == proxiedEmployee.Id),
            viewedAssignments.Count(row => row.ProxyEmployeeId == proxiedEmployee.Id),
            viewedAssignments.Count(row => row.ProxyEmployeeId == proxiedEmployee.Id && row.CanApproveLeave));
    }

    private static AssignmentStatus GetStatus(ProxyAssignment assignment, DateTime now)
    {
        if (assignment.IsActiveOn(now))
        {
            return new AssignmentStatus("Active", "active", "active", 0);
        }

        if (assignment.StartDate > now)
        {
            return new AssignmentStatus("Scheduled", "scheduled", "scheduled", 1);
        }

        return new AssignmentStatus("Expired", "expired", "expired", 2);
    }

    private static string NormalizeStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized is "active" or "scheduled" or "expired" ? normalized : "all";
    }

    private static string FormatDate(DateTime date)
    {
        if (date == OpenEndedProxyEndDate)
        {
            return "Open-ended";
        }

        if (date.TimeOfDay != TimeSpan.Zero)
        {
            return date.ToString(SapProxyDateFormat, CultureInfo.InvariantCulture);
        }

        return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    private static string CreateAccessLabel(ProxyAssignment assignment)
    {
        if (assignment.HasAllToolAccess)
        {
            return "All tools";
        }

        if (assignment.ToolPermissions.Count == 0)
        {
            return assignment.CanApproveLeave ? "Leave" : "Standard";
        }

        return string.Join(", ", assignment.ToolPermissions.Take(2)) +
            (assignment.ToolPermissions.Count > 2 ? $" +{assignment.ToolPermissions.Count - 2}" : string.Empty);
    }

    public sealed class ProxyAssignmentInput
    {
        [Required]
        [Display(Name = "Delegator")]
        public Guid DelegatorEmployeeId { get; set; }

        [Display(Name = "Proxies")]
        public List<Guid> ProxyEmployeeIds { get; set; } = [];

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Leave approvals")]
        public bool CanApproveLeave { get; set; } = true;
    }

    public sealed class ProxySessionInput
    {
        [Required]
        [Display(Name = "Proxy as")]
        public Guid EmployeeId { get; set; }
    }

    public sealed class ProxyImportInput
    {
        [Display(Name = "CSV file")]
        public IFormFile? CsvFile { get; set; }
    }

    public sealed class ProxySettingsInput
    {
        [Display(Name = "Employee self-service")]
        public bool AllowEmployeeProxySelfService { get; set; }
    }

    public sealed class ProxyRemoveInput
    {
        [Required]
        public Guid AssignmentId { get; set; }
    }

    public sealed record ProxyAssignmentRow(
        Guid AssignmentId,
        Guid DelegatorEmployeeId,
        Guid ProxyEmployeeId,
        string DelegatorName,
        string DelegatorDetail,
        string ProxyName,
        string ProxyDetail,
        string WindowLabel,
        string DurationLabel,
        string Status,
        string StatusClass,
        string StatusFilter,
        int SortRank,
        DateTime EndDate,
        bool CanApproveLeave,
        bool HasAllToolAccess,
        string AccessLabel,
        bool CanRemove);

    public sealed record ProxySummary(int Active, int Scheduled, int EndingSoon, int Total);

    public sealed record ProxyImportResult(int Created, int Removed, IReadOnlyList<string> Errors);

    private sealed record SapProxyToolColumn(string Name, int Index);

    private sealed record SapProxyImportColumns(
        int UserIdIndex,
        int AssignmentIdUserIdIndex,
        int ProxyIdIndex,
        int AssignmentIdProxyIdIndex,
        int StartDateIndex,
        int EndDateIndex,
        int AllIndex,
        int RemoveAllIndex,
        IReadOnlyList<SapProxyToolColumn> ToolColumns)
    {
        public bool IsValid =>
            UserIdIndex >= 0 &&
            AssignmentIdUserIdIndex >= 0 &&
            ProxyIdIndex >= 0 &&
            AssignmentIdProxyIdIndex >= 0 &&
            StartDateIndex >= 0 &&
            EndDateIndex >= 0 &&
            AllIndex >= 0 &&
            RemoveAllIndex >= 0;

        public static SapProxyImportColumns From(IReadOnlyList<string> headers)
        {
            var normalizedHeaders = headers
                .Select((header, index) => new { Name = NormalizeHeader(header), Index = index })
                .Where(header => !string.IsNullOrWhiteSpace(header.Name))
                .GroupBy(header => header.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

            var knownIndexes = new HashSet<int>
            {
                FindIndex(normalizedHeaders, "userid"),
                FindIndex(normalizedHeaders, "assignmentiduserid"),
                FindIndex(normalizedHeaders, "proxyid"),
                FindIndex(normalizedHeaders, "assignmentidproxyid"),
                FindIndex(normalizedHeaders, "startdateyyyymmddhhmm", "startdate"),
                FindIndex(normalizedHeaders, "enddateyyyymmddhhmm", "enddate"),
                FindIndex(normalizedHeaders, "all"),
                FindIndex(normalizedHeaders, "removeall")
            };

            knownIndexes.Remove(-1);

            var toolColumns = headers
                .Select((header, index) => new SapProxyToolColumn(header.Trim(), index))
                .Where(column => !knownIndexes.Contains(column.Index) && !string.IsNullOrWhiteSpace(column.Name))
                .ToList();

            return new SapProxyImportColumns(
                FindIndex(normalizedHeaders, "userid"),
                FindIndex(normalizedHeaders, "assignmentiduserid"),
                FindIndex(normalizedHeaders, "proxyid"),
                FindIndex(normalizedHeaders, "assignmentidproxyid"),
                FindIndex(normalizedHeaders, "startdateyyyymmddhhmm", "startdate"),
                FindIndex(normalizedHeaders, "enddateyyyymmddhhmm", "enddate"),
                FindIndex(normalizedHeaders, "all"),
                FindIndex(normalizedHeaders, "removeall"),
                toolColumns);
        }

        private static int FindIndex(IReadOnlyDictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(name, out var index))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string NormalizeHeader(string header)
        {
            return new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }

    public sealed record ProxyViewContext(
        Guid? EmployeeId,
        string DisplayName,
        string Detail,
        bool IsProxying,
        string ModeLabel,
        int PrimaryCount,
        string PrimaryLabel,
        int SecondaryCount,
        string SecondaryLabel,
        int ApprovalCount)
    {
        public static ProxyViewContext Admin(int totalAssignments, int activeAssignments, int approvalAssignments)
        {
            return new ProxyViewContext(
                null,
                "Administrator view",
                "All employees and proxy assignments",
                false,
                "Admin",
                totalAssignments,
                "Visible assignments",
                activeAssignments,
                "Active assignments",
                approvalAssignments);
        }

        public static ProxyViewContext Employee(
            Guid employeeId,
            string displayName,
            string detail,
            int delegatedByUser,
            int assignedToUser,
            int approvalAssignments)
        {
            return new ProxyViewContext(
                employeeId,
                displayName,
                detail,
                true,
                "Proxy active",
                delegatedByUser,
                "Delegated by user",
                assignedToUser,
                "Assigned to user",
                approvalAssignments);
        }
    }

    private sealed record AssignmentStatus(string Label, string CssClass, string Filter, int SortRank);
}
