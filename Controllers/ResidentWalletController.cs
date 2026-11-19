using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.ViewModels;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  ResidentWalletController	محفظة الساكن + كشف الحساب + OCR
// ============================================================
/// <summary>
/// Resident wallet: balance, account statement, payment submission and receipt image OCR.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>resident</c>.
/// </remarks>
[Authorize(Roles = "resident")]
public class ResidentWalletController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;
    private readonly TargetWalletReadService _targetWallet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidentWalletController"/> class.
    /// </summary>
    public ResidentWalletController(BuildingsService buildings, WalletService wallet, AuditLogService audit,
        TargetWalletReadService targetWallet)
    {
        _buildings = buildings; _wallet = wallet; _audit = audit; _targetWallet = targetWallet;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    /// <summary>
    /// Shows the main page of the resident wallet screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? month,
        bool allMonths = false,
        string? fromMonth = null,
        string? toMonth = null,
        string? typeFilter = "all",
        string? statusFilter = null,
        int page = 1,
        int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        var statementFrom = allMonths ? fromMonth : mk;
        var statementTo = allMonths ? toMonth : mk;
        var snapshot = await _targetWallet.GetApartmentAsync(BuildingId, ApartmentId,
            statementFrom, statementTo, cancellationToken: HttpContext.RequestAborted);
        var currentSnapshot = allMonths
            ? await _targetWallet.GetApartmentAsync(BuildingId, ApartmentId, mk, mk,
                cancellationToken: HttpContext.RequestAborted)
            : snapshot;
        var vm = BuildTargetViewModel(snapshot, mk, allMonths, fromMonth, toMonth,
            typeFilter, statusFilter, page, pageSize);

        // ✅ AJAX — ارجع Partial بس
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_StatementPartial", vm.Statement);
        }

        ViewBag.Building = building;
        ViewBag.Apartment = apt;
        ViewBag.Month = mk;
        ViewBag.AllMonths = allMonths;
        ViewBag.FromMonth = fromMonth;
        ViewBag.ToMonth = toMonth;
        ViewBag.TypeFilter = typeFilter;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.Balance = currentSnapshot.Balance;
        ViewBag.Transactions = vm.Statement.Transactions;
        ViewBag.PendingDeposits = vm.PendingDeposits;
        ViewBag.RouteValues = vm.Statement.RouteValues;

        return View();
    }

    /// <summary>Builds the resident statement exclusively from the canonical ledger projection.</summary>
    private static (StatementPartialViewModels Statement, PagedResult<Deposit> PendingDeposits) BuildTargetViewModel(
        TargetWalletSnapshot snapshot, string month, bool allMonths,
        string? fromMonth, string? toMonth, string? typeFilter, string? statusFilter,
        int page, int pageSize)
    {
        var transactions = snapshot.Transactions.AsEnumerable();
        var selectedTypes = (typeFilter ?? "all")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedTypes.Count > 0 && !selectedTypes.Contains("all"))
        {
            transactions = transactions.Where(item =>
                (selectedTypes.Contains("deposits") && item.Type == "deposit") ||
                (selectedTypes.Contains("expenses") && item.Type == "expense") ||
                (selectedTypes.Contains("revenues") && item.Type == "revenue") ||
                (selectedTypes.Contains("carry_over") && item.Type == "carry_over") ||
                (selectedTypes.Contains("adjustments") && item.Type == "adjustment") ||
                (selectedTypes.Contains("monthly_fee") && item.Type is "monthly_fee_due" or "monthly_fee_credit") ||
                (selectedTypes.Contains("success") && item.Status == "confirmed"));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
            transactions = transactions.Where(item =>
                string.Equals(item.Status, statusFilter, StringComparison.OrdinalIgnoreCase));

        var statement = new StatementPartialViewModels
        {
            Transactions = PagedResult<WalletTransactionVm>.Create(
                transactions.OrderByDescending(item => item.CreatedAt).ToList(), page, pageSize),
            AllMonths = allMonths,
            RouteValues = new Dictionary<string, string?>
            {
                ["month"] = month,
                ["allMonths"] = allMonths.ToString().ToLowerInvariant(),
                ["fromMonth"] = fromMonth,
                ["toMonth"] = toMonth,
                ["typeFilter"] = typeFilter,
                ["statusFilter"] = statusFilter,
                ["pageSize"] = pageSize.ToString()
            }
        };

        return (statement, PagedResult<Deposit>.Create(snapshot.PendingDeposits.ToList(), 1, pageSize));
    }

    // ============================================================
    // ✅ CreateDeposit
    // ============================================================
    /// <summary>
    /// Creates the deposit.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
    public async Task<IActionResult> CreateDeposit(string month, decimal amount, string? note,
        IFormFile? receiptImage,
        [FromServices] CloudinaryService cloudinary)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        if (amount > 0)
        {
            ReceiptData? receipt = null;
            if (receiptImage != null && receiptImage.Length > 0)
            {
                using var stream = receiptImage.OpenReadStream();
                if (!ImageSniffer.IsAllowedImage(stream))
                {
                    TempData["Error"] = Loc.T("Invalid_Image_File");
                    return RedirectToAction("Index", new { month = mk });
                }

                receipt = await cloudinary.UploadReceiptAsync(
                    stream,
                    receiptImage.FileName,
                    building.Id,
                    apt.Id);
            }

            var d = _wallet.CreateDeposit(building, apt.Id, amount, note, "resident", mk, receipt);

            var auditDetails = $"دفعة جديدة {d.Number} — {amount:0.##} ج.م";
            if (receipt != null) auditDetails += " (مع صورة إيصال)";

            _audit.Push(building, "deposit_create", auditDetails, "resident",
                User.Identity?.Name ?? "resident", apt.Number.ToString());

            await _buildings.SaveFullAsync(building);
            TempData["Message"] = Loc.T("The_Payment_Was_Submitted_Awaiting_Admin");
        }

        return RedirectToAction("Index", new { month = mk });
    }

    // ============================================================
    // ✅ ParseReceiptImage (OCR)
    // ============================================================
    /// <summary>
    /// Parses the receipt image.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
    public async Task<IActionResult> ParseReceiptImage(IFormFile image,
        [FromServices] OcrService ocr,
        [FromServices] TransactionParser parser)
    {
        if (image == null || image.Length == 0)
            return Json(new { success = false, error = Loc.T("No_Image_Was_Uploaded") });

        if (image.Length > 10 * 1024 * 1024)
            return Json(new { success = false, error = Loc.T("The_Image_Is_Too_Large_Maximum") });

        try
        {
            using var stream = image.OpenReadStream();
            if (!ImageSniffer.IsAllowedImage(stream))
                return Json(new { success = false, error = Loc.T("Invalid_Image_File") });

            var text = ocr.ExtractText(stream);
            var parsed = parser.Parse(text);

            if (parsed == null || parsed.Amount <= 0)
            {
                return Json(new
                {
                    success = false,
                    error = Loc.T("We_Couldn_T_Read_The_Image"),
                    rawText = text
                });
            }

            if (!parsed.IsSuccessful)
            {
                return Json(new
                {
                    success = false,
                    error = Loc.T("This_Image_Shows_A_Failed_Or"),
                    status = "FAILED",
                    rawText = text
                });
            }

            return Json(new
            {
                success = true,
                amount = parsed.Amount,
                reference = parsed.Reference,
                date = parsed.Date,
                status = "SUCCESS",
                from = parsed.From,
                recipientAccount = parsed.RecipientAccount,
                note = BuildNote(parsed)
            });
        }
        catch (Exception)
        {
            return Json(new { success = false, error = Loc.T("Error_Reading_The_Image") });
        }
    }

    private static string BuildNote(TransactionData d)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(d.Date)) parts.Add($"📅 {d.Date}");
        if (!string.IsNullOrEmpty(d.Reference)) parts.Add($"🔢 Ref: {d.Reference}");
        if (!string.IsNullOrEmpty(d.RecipientAccount)) parts.Add($"🏦 Acc: {d.RecipientAccount}");
        if (!string.IsNullOrEmpty(d.From)) parts.Add($"👤 {d.From}");
        return string.Join(" | ", parts);
    }
}