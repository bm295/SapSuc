using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SuccessFactorsLike.Services;

namespace SapSuc.Web.Pages;

public class CompanySettingsModel : PageModel
{
    private readonly HrPlatformService _hrPlatform;

    public CompanySettingsModel(HrPlatformService hrPlatform)
    {
        _hrPlatform = hrPlatform;
    }

    [BindProperty]
    public CompanySettingsInput Settings { get; set; } = new();

    public bool CaseInsensitiveUsernamesLocked => _hrPlatform.CaseInsensitiveUsernamesEnabled;

    public void OnGet()
    {
        LoadSettings();
    }

    public IActionResult OnPost()
    {
        _hrPlatform.UpdateCompanySettings(
            Settings.AllowManagerAccessToDocumentRevisionHistory,
            Settings.GeneralDisplayNameEnabledByDefaultForNewCustomers,
            Settings.EnableCaseInsensitiveUsernames);
        TempData["SuccessMessage"] = "Company settings saved.";

        return RedirectToPage("/CompanySettings");
    }

    private void LoadSettings()
    {
        Settings = new CompanySettingsInput
        {
            AllowManagerAccessToDocumentRevisionHistory = _hrPlatform.AllowManagerAccessToDocumentRevisionHistory,
            GeneralDisplayNameEnabledByDefaultForNewCustomers = _hrPlatform.GeneralDisplayNameEnabledByDefaultForNewCustomers,
            EnableCaseInsensitiveUsernames = _hrPlatform.CaseInsensitiveUsernamesEnabled
        };
    }

    public sealed class CompanySettingsInput
    {
        [Display(Name = "Allow Manager Access to a Document's Revision History")]
        public bool AllowManagerAccessToDocumentRevisionHistory { get; set; }

        [Display(Name = "General Display Name Enabled by Default for New Customers")]
        public bool GeneralDisplayNameEnabledByDefaultForNewCustomers { get; set; }

        [Display(Name = "Enable Case-Insensitive Usernames")]
        public bool EnableCaseInsensitiveUsernames { get; set; }
    }
}
