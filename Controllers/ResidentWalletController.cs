﻿using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Data;
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
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetDepositWorkflowService _depositWorkflow;
    private readonly TargetAccountIdentityResolver _identityResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidentWalletController"/> class.
    /// </summary>
    public ResidentWalletController(
        TargetWalletReadService targetWallet,
        TargetDepositWorkflowService depositWorkflow,
        TargetAccountIdentityResolver identityResolver)
    {
        _targetWallet = targetWallet;
        _depositWorkflow = depositWorkflow;
        _identityResolver = identityResolver;
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
        var building = await _targetWallet.GetBuildingShellAsync(BuildingId, ApartmentId, HttpContext.RequestAborted);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month)
            ? await _targetWallet.GetCurrentPeriodKeyAsync(BuildingId, cancellationToken: HttpContext.RequestAborted)
            : month;

        var vm = await BuildViewModelAsync(building, apt, mk, allMonths, fromMonth, toMonth,
            typeFilter, statusFilter, page, pageSize);

        // âœ… AJAX â€” ارجع Partial بس
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
        ViewBag.Balance = vm.Balance;
        ViewBag.Transactions = vm.Statement.Transactions;
        ViewBag.PendingDeposits = vm.PendingDeposits;
        ViewBag.RouteValues = vm.Statement.RouteValues;

        return View();
    }

    // ============================================================
    // âœ… Helper: بناء ViewModel
    // ============================================================
    private async Task<(StatementPartialViewModels Statement, PagedResult<Deposit> PendingDeposits, decimal Balance)> BuildViewModelAsync(
        Building building, Apartment apt, string mk,
        bool allMonths, string? fromMonth, string? toMonth,
        string? typeFilter, string? statusFilter,
        int page, int pageSize)
    {
        var snapshot = await _targetWallet.GetApartmentAsync(
            BuildingId,
            apt.Id,
            allMonths ? fromMonth : mk,
            allMonths ? toMonth : mk);

        var allTxs = snapshot.Transactions.ToList();
        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            var selectedTypes = typeFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList();

            if (!selectedTypes.Contains("all") && selectedTypes.Count > 0)
            {
                allTxs = allTxs.Where(t =>
                    (selectedTypes.Contains("deposits") && t.Type == "deposit") ||
                    (selectedTypes.Contains("expenses") && t.Type == "expense") ||
                    (selectedTypes.Contains("revenues") && t.Type == "revenue") ||
                    (selectedTypes.Contains("carry_over") && t.Type == "carry_over") ||
                    (selectedTypes.Contains("adjustments") && t.Type == "adjustment") ||
                    (selectedTypes.Contains("monthly_fee") &&
                        (t.Type == "monthly_fee_due" || t.Type == "monthly_fee_credit")) ||
                    (selectedTypes.Contains("success") && t.Status == "confirmed") ||
                    (selectedTypes.Contains("failed") && t.Status == "cancelled") ||
                    (selectedTypes.Contains("pending") && t.Status == "pending")
                ).ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
            allTxs = allTxs.Where(t => string.Equals(t.Status, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        var pagedTxs = PagedResult<WalletTransactionVm>.Create(allTxs, page, pageSize);
        var pendingList = snapshot.PendingDeposits.ToList();
        var pagedPending = PagedResult<Deposit>.Create(pendingList, 1, pageSize);

        var statementVm = new StatementPartialViewModels
        {
            Transactions = pagedTxs,
            AllMonths = allMonths,
            RouteValues = new Dictionary<string, string?>
            {
                ["month"] = mk,
                ["allMonths"] = allMonths.ToString().ToLowerInvariant(),
                ["fromMonth"] = fromMonth,
                ["toMonth"] = toMonth,
                ["typeFilter"] = typeFilter,
                ["statusFilter"] = statusFilter,
                ["pageSize"] = pageSize.ToString()
            }
        };

        return (statementVm, pagedPending, snapshot.Balance);
    }

    // ============================================================
    // âœ… CreateDeposit
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
        Guid commandId, IFormFile? receiptImage,
        [FromServices] CloudinaryService cloudinary)
    {
        var building = await _targetWallet.GetBuildingShellAsync(BuildingId, ApartmentId, HttpContext.RequestAborted);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month)
            ? await _targetWallet.GetCurrentPeriodKeyAsync(BuildingId, cancellationToken: HttpContext.RequestAborted)
            : month;
        if (!DateOnly.TryParseExact(mk + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
        {
            TempData["Error"] = "الفترة المالية غير صالحة.";
            return RedirectToAction("Index");
        }
        if (commandId == Guid.Empty)
        {
            TempData["Error"] = "انتهت صلاحية نموذج الإيداع. حدّث الصفحة وحاول مرة أخرى.";
            return RedirectToAction("Index", new { month = mk });
        }
        if (string.IsNullOrWhiteSpace(note) == false && note.Length > 1000)
        {
            TempData["Error"] = "الملاحظة يجب ألا تتجاوز 1000 حرف.";
            return RedirectToAction("Index", new { month = mk });
        }
        if (amount <= 0)
        {
            TempData["Error"] = "يجب إدخال مبلغ أكبر من صفر.";
            return RedirectToAction("Index", new { month = mk });
        }
        if (amount > 999999999999999m || decimal.Round(amount, 4, MidpointRounding.AwayFromZero) != amount)
        {
            TempData["Error"] = "قيمة المبلغ غير صالحة.";
            return RedirectToAction("Index", new { month = mk });
        }

        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر ربط حسابك بهوية النظام الجديدة. لم يتم تسجيل الدفعة.";
            return RedirectToAction("Index", new { month = mk });
        }

        var submissionContext = await _targetWallet.GetSubmissionContextAsync(
            BuildingId, mk, HttpContext.RequestAborted);
        if (submissionContext is null)
        {
            TempData["Error"] = "لا توجد فترة مالية مستهدفة لهذا الشهر.";
            return RedirectToAction("Index", new { month = mk });
        }

        ReceiptData? receipt = null;
        if (receiptImage != null && receiptImage.Length > 0)
        {
            if (receiptImage.Length > 12 * 1024 * 1024)
            {
                TempData["Error"] = "حجم صورة الإيصال أكبر من الحد المسموح.";
                return RedirectToAction("Index", new { month = mk });
            }

            using var stream = receiptImage.OpenReadStream();
            if (!ImageSniffer.IsAllowedImage(stream))
            {
                TempData["Error"] = Loc.T("Invalid_Image_File");
                return RedirectToAction("Index", new { month = mk });
            }

            receipt = await cloudinary.UploadReceiptAsync(
                stream, receiptImage.FileName, building.Id, apt.Id);
            if (receipt is null || !receipt.Success || string.IsNullOrWhiteSpace(receipt.Url) ||
                string.IsNullOrWhiteSpace(receipt.Path))
            {
                TempData["Error"] = "تعذر رفع صورة الإيصال. لم يتم تسجيل الدفعة.";
                return RedirectToAction("Index", new { month = mk });
            }
        }

        try
        {
            await _depositWorkflow.CreatePendingAsync(
                BuildingId,
                ApartmentId,
                submissionContext.PeriodId,
                actorAccountId.Value,
                amount,
                submissionContext.CurrencyCode,
                commandId.ToString("N"),
                HttpContext.RequestAborted,
                note,
                receipt is null ? null : JsonSerializer.Serialize(receipt));

            TempData["Message"] = Loc.T("The_Payment_Was_Submitted_Awaiting_Admin");
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = "تعذر تسجيل الدفعة. لم يتم تغيير رصيد المحفظة.";
        }

        return RedirectToAction("Index", new { month = mk });
    }

    // ============================================================
    // âœ… ParseReceiptImage (OCR)
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

            if (parsed.IsFailed)
            {
                return Json(new
                {
                    success = false,
                    error = Loc.T("This_Image_Shows_A_Failed_Or"),
                    status = parsed.Status
                });
            }

            // "success" here means fields were extracted for the resident to review.
            // It never means the transfer is verified or the deposit is approved.
            return Json(new
            {
                success = true,
                amount = parsed.Amount,
                reference = parsed.Reference,
                date = parsed.Date,
                status = parsed.Status,
                from = parsed.From,
                recipientAccount = parsed.RecipientAccount,
                note = BuildNote(parsed),
                requiresManualReview = parsed.Status is "UNKNOWN" or "PENDING"
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
        if (!string.IsNullOrEmpty(d.Date)) parts.Add($"ًں“… {d.Date}");
        if (!string.IsNullOrEmpty(d.Reference)) parts.Add($"ًں”¢ Ref: {d.Reference}");
        if (!string.IsNullOrEmpty(d.RecipientAccount)) parts.Add($"ًںڈ¦ Acc: {d.RecipientAccount}");
        if (!string.IsNullOrEmpty(d.From)) parts.Add($"ًں‘¤ {d.From}");
        return string.Join(" | ", parts);
    }
}
