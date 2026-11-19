# Phase 53 â€” Refined legacy runtime reference audit

- Run: 2026-11-19T05:13:03.6535023+02:00
- Mode: READ ONLY repository scan; no database connection and no database changes.
- Runtime excludes tests, documentation/schema snapshots, generated migrations, build output and package folders.
- Text references are review leads, not proof of an EF mapping or live call path.

## Summary

- Runtime references: 2401
- Test references: 322
- Documentation/schema references: 110
- Generated migration references: 1052

| Legacy table | Runtime refs | Test refs | Docs/schema refs | Migration refs |
|---|---:|---:|---:|---:|
| __EFMigrationsHistory | 56 | 0 | 3 | 0 |
| Apartments | 246 | 35 | 17 | 109 |
| AuditLog | 60 | 13 | 2 | 22 |
| BackupHistory | 24 | 0 | 2 | 18 |
| BuildingAdmins | 93 | 9 | 6 | 49 |
| Buildings | 245 | 74 | 6 | 87 |
| BuildingSettings | 30 | 0 | 2 | 22 |
| Cameras | 73 | 0 | 6 | 48 |
| DepositEditHistory | 28 | 0 | 2 | 21 |
| Deposits | 116 | 54 | 3 | 37 |
| Dvrs | 68 | 0 | 2 | 29 |
| Expenses | 148 | 34 | 2 | 35 |
| FinancialCategories | 19 | 0 | 2 | 34 |
| Floors | 86 | 8 | 6 | 48 |
| GlobalSettings | 30 | 0 | 2 | 18 |
| LoginThrottle | 8 | 4 | 0 | 0 |
| MaintenanceRecords | 24 | 0 | 2 | 24 |
| MonthlyApartmentShares | 38 | 0 | 3 | 31 |
| Notifications | 84 | 2 | 2 | 31 |
| PollOptions | 24 | 0 | 2 | 30 |
| Polls | 94 | 2 | 2 | 28 |
| PollVotes | 23 | 0 | 2 | 34 |
| Presence | 73 | 9 | 3 | 22 |
| QrTokens | 29 | 0 | 2 | 31 |
| QrUsages | 28 | 0 | 2 | 14 |
| Residents | 219 | 23 | 8 | 58 |
| Revenues | 125 | 18 | 2 | 35 |
| ScheduledBackupSettings | 45 | 7 | 2 | 18 |
| SyncPendingChanges | 4 | 0 | 2 | 14 |
| UserPermissions | 27 | 0 | 2 | 24 |
| Users | 201 | 26 | 8 | 48 |
| WalletAdjustments | 33 | 4 | 3 | 33 |

## Runtime reference locations

| Table | File | Line | Reference |
|---|---|---:|---|
| LoginThrottle | Program.cs | 170 | builder.Services.AddSingleton<LoginThrottle>(); |
| Residents | Controllers\AccountController.cs | 15 | /// Handles sign-in for residents, admins and super admins: unified login, account selection, QR quick login, set-PIN links, language-neutral lookups for the login page, and sign-out. |
| LoginThrottle | Controllers\AccountController.cs | 28 | private readonly LoginThrottle _throttle; |
| Buildings | Controllers\AccountController.cs | 50 | BuildingsService buildings, |
| Users | Controllers\AccountController.cs | 51 | UsersService users, |
| Residents | Controllers\AccountController.cs | 52 | ResidentsService residents, |
| LoginThrottle | Controllers\AccountController.cs | 53 | LoginThrottle throttle, |
| Buildings | Controllers\AccountController.cs | 60 | _buildings = buildings; |
| Users | Controllers\AccountController.cs | 61 | _users = users; |
| Residents | Controllers\AccountController.cs | 62 | _residents = residents; |
| Buildings | Controllers\AccountController.cs | 86 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 103 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 111 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 122 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 143 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 160 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 168 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AccountController.cs | 178 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Apartments | Controllers\AccountController.cs | 788 | // AJAX: Get Floors / Apartments |
| Floors | Controllers\AccountController.cs | 788 | // AJAX: Get Floors / Apartments |
| Floors | Controllers\AccountController.cs | 791 | /// Gets the floors. |
| Floors | Controllers\AccountController.cs | 808 | var floors = building.Floors |
| Floors | Controllers\AccountController.cs | 818 | return Json(floors); |
| Apartments | Controllers\AccountController.cs | 822 | /// Gets the apartments. |
| Apartments | Controllers\AccountController.cs | 839 | var apts = building.Apartments |
| Apartments | Controllers\AccountController.cs | 1042 | var apt = building.Apartments.FirstOrDefault(a => a.Id == payload.AptId); |
| Apartments | Controllers\AccountController.cs | 1074 | var apt = building.Apartments.FirstOrDefault(a => a.Id == payload.AptId); |
| Residents | Controllers\AccountController.cs | 1078 | var residents = await _residents.GetByApartmentAsync(payload.BuildingId, payload.AptId); |
| Residents | Controllers\AccountController.cs | 1079 | var resident = residents.FirstOrDefault(r => r.IsPrimary && !r.IsDisabled && r.IsActive) |
| Residents | Controllers\AccountController.cs | 1080 | ?? residents.FirstOrDefault(r => !r.IsDisabled && r.IsActive); |
| Apartments | Controllers\AdminAptsController.cs | 14 | /// Admin screens for managing the apartments of the current building: listing, editing, opening/closing, enabling/disabling, welcome messages and quick-login QR codes. |
| Buildings | Controllers\AdminAptsController.cs | 36 | BuildingsService buildings, |
| Residents | Controllers\AdminAptsController.cs | 37 | ResidentsService residents, |
| Buildings | Controllers\AdminAptsController.cs | 45 | _buildings = buildings; |
| Residents | Controllers\AdminAptsController.cs | 46 | _residents = residents; |
| Apartments | Controllers\AdminAptsController.cs | 64 | /// Shows the main page of the admin apartments screen. |
| Apartments | Controllers\AdminAptsController.cs | 90 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Floors | Controllers\AdminAptsController.cs | 93 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Apartments | Controllers\AdminAptsController.cs | 137 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Floors | Controllers\AdminAptsController.cs | 140 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Apartments | Controllers\AdminAptsController.cs | 214 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\AdminAptsController.cs | 264 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\AdminAptsController.cs | 320 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\AdminAptsController.cs | 365 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\AdminAptsController.cs | 398 | /// Edits the admin apartments edit page. |
| Apartments | Controllers\AdminAptsController.cs | 409 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Floors | Controllers\AdminAptsController.cs | 412 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Apartments | Controllers\AdminAptsController.cs | 424 | /// Handles the edit form submission of the admin apartments screen. |
| Apartments | Controllers\AdminAptsController.cs | 444 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Buildings | Controllers\AdminHomeController.cs | 25 | public AdminHomeController(BuildingsService buildings, BuildingMapService map) |
| Buildings | Controllers\AdminHomeController.cs | 27 | _buildings = buildings; |
| Residents | Controllers\AdminResidentsController.cs | 15 | /// Admin screens for managing the residents of the current building (create, edit, enable/disable, delete, PIN reset). |
| Residents | Controllers\AdminResidentsController.cs | 31 | ResidentsService residents, |
| Buildings | Controllers\AdminResidentsController.cs | 32 | BuildingsService buildings) |
| Residents | Controllers\AdminResidentsController.cs | 34 | _residents = residents; |
| Buildings | Controllers\AdminResidentsController.cs | 35 | _buildings = buildings; |
| Residents | Controllers\AdminResidentsController.cs | 51 | /// Shows the main page of the admin residents screen. |
| Residents | Controllers\AdminResidentsController.cs | 73 | var residents = await _residents.GetByBuildingAsync(bid); |
| Residents | Controllers\AdminResidentsController.cs | 79 | residents = residents |
| Residents | Controllers\AdminResidentsController.cs | 92 | return View(residents); |
| Residents | Controllers\AdminResidentsController.cs | 100 | /// Shows the details page of the admin residents screen. |
| Apartments | Controllers\AdminResidentsController.cs | 115 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId); |
| Residents | Controllers\AdminResidentsController.cs | 128 | /// Creates the admin residents create page. |
| Residents | Controllers\AdminResidentsController.cs | 153 | /// Handles the create form submission of the admin residents screen. |
| Residents | Controllers\AdminResidentsController.cs | 201 | /// Edits the admin residents edit page. |
| Apartments | Controllers\AdminResidentsController.cs | 216 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId); |
| Residents | Controllers\AdminResidentsController.cs | 229 | /// Handles the edit form submission of the admin residents screen. |
| Apartments | Controllers\AdminResidentsController.cs | 265 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId); |
| Residents | Controllers\AdminResidentsController.cs | 311 | /// Handles the disable form submission of the admin residents screen. |
| Residents | Controllers\AdminResidentsController.cs | 333 | /// Handles the enable form submission of the admin residents screen. |
| Residents | Controllers\AdminResidentsController.cs | 359 | /// Handles the delete form submission of the admin residents screen. |
| Residents | Controllers\AdminResidentsController.cs | 385 | /// Migrates the admin residents data. |
| Buildings | Controllers\AdminResidentsController.cs | 394 | var buildings = await _buildings.GetSummariesWithCountsAsync(); |
| Buildings | Controllers\AdminResidentsController.cs | 395 | ViewBag.Buildings = buildings; |
| Buildings | Controllers\AdminsController.cs | 14 | /// Super admin screens for creating and managing admin accounts, their buildings and PINs. |
| Buildings | Controllers\AdminsController.cs | 29 | public AdminsController(AdminManagementService admins, BuildingsService buildings) |
| Buildings | Controllers\AdminsController.cs | 32 | _buildings = buildings; |
| Buildings | Controllers\AdminsController.cs | 43 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AdminsController.cs | 68 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AdminsController.cs | 88 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AdminsController.cs | 102 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AdminsController.cs | 120 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AdminsController.cs | 146 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Buildings | Controllers\AuditLogController.cs | 15 | /// Shows and exports the audit log. The scope (all buildings, one building, one apartment) depends on the signed-in role. |
| Buildings | Controllers\AuditLogController.cs | 29 | public AuditLogController(AuditLogQueryService audit, BuildingsService buildings) |
| Buildings | Controllers\AuditLogController.cs | 32 | _buildings = buildings; |
| Buildings | Controllers\AuditLogController.cs | 106 | ViewBag.Buildings = IsSuperAdmin |
| Buildings | Controllers\BackupController.cs | 41 | BuildingsService buildings, |
| Users | Controllers\BackupController.cs | 42 | UsersService users, |
| Buildings | Controllers\BackupController.cs | 50 | _buildings = buildings; |
| Users | Controllers\BackupController.cs | 51 | _users = users; |
| Buildings | Controllers\BackupController.cs | 75 | var buildings = await _buildings.GetSummariesWithCountsAsync(); |
| Buildings | Controllers\BackupController.cs | 81 | ViewBag.Buildings = buildings; |
| Buildings | Controllers\BackupController.cs | 90 | ViewBag.TotalBuildings = buildings.Count; |
| ScheduledBackupSettings | Controllers\BackupController.cs | 221 | public async Task<IActionResult> SaveScheduledSettings(ScheduledBackupSettings model) |
| Apartments | Controllers\BackupController.cs | 405 | if (data == null \|\| data.Apartments.Count == 0) |
| Apartments | Controllers\BackupController.cs | 430 | var floorsGrouped = data.Apartments |
| Floors | Controllers\BackupController.cs | 441 | building.Floors.Add(new Floor |
| Apartments | Controllers\BackupController.cs | 457 | building.Apartments.Add(new Apartment |
| Apartments | Controllers\BackupController.cs | 488 | data.Name, data.Apartments.Count); |
| Buildings | Controllers\BackupController.cs | 489 | return RedirectToAction("Details", "Buildings", new { id = buildingId }); |
| Buildings | Controllers\BackupController.cs | 534 | var buildings = await _buildings.GetSummariesWithCountsAsync(); |
| Buildings | Controllers\BackupController.cs | 540 | count = buildings.Count, |
| Buildings | Controllers\BackupController.cs | 541 | buildings |
| Buildings | Controllers\BackupController.cs | 551 | var fileName = $"all-buildings-{DateTime.Now:yyyyMMdd-HHmm}.json"; |
| Apartments | Controllers\BackupController.cs | 583 | var aptsSheet = workbook.Worksheets.Add("Floors & Apartments"); |
| Floors | Controllers\BackupController.cs | 583 | var aptsSheet = workbook.Worksheets.Add("Floors & Apartments"); |
| Floors | Controllers\BackupController.cs | 594 | foreach (var floor in building.Floors.OrderBy(f => f.Order)) |
| Apartments | Controllers\BackupController.cs | 596 | var apts = building.Apartments.Where(a => a.FloorId == floor.Id).OrderBy(a => a.Number).ToList(); |
| Apartments | Controllers\BuildingsController.cs | 11 | /// Super admin screens for managing buildings, floors, apartments, QR code sheets and the entry points for admin/resident impersonation. |
| Buildings | Controllers\BuildingsController.cs | 11 | /// Super admin screens for managing buildings, floors, apartments, QR code sheets and the entry points for admin/resident impersonation. |
| Floors | Controllers\BuildingsController.cs | 11 | /// Super admin screens for managing buildings, floors, apartments, QR code sheets and the entry points for admin/resident impersonation. |
| Buildings | Controllers\BuildingsController.cs | 35 | /// Shows the main page of the buildings screen. |
| Buildings | Controllers\BuildingsController.cs | 59 | /// Creates the buildings create page. |
| Buildings | Controllers\BuildingsController.cs | 69 | /// Handles the create form submission of the buildings screen. |
| Buildings | Controllers\BuildingsController.cs | 138 | /// Shows the details page of the buildings screen. |
| Buildings | Controllers\BuildingsController.cs | 200 | /// Handles the delete form submission of the buildings screen. |
| Apartments | Controllers\BuildingsController.cs | 297 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Floors | Controllers\BuildingsController.cs | 300 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Residents | Controllers\CamerasController.cs | 14 | /// Live camera viewing for residents/admins and camera management for a building. |
| Buildings | Controllers\CamerasController.cs | 34 | BuildingsService buildings, |
| Buildings | Controllers\CamerasController.cs | 40 | _buildings = buildings; |
| Dvrs | Controllers\CamerasController.cs | 61 | return RedirectToAction("Index", "Dvrs"); |
| Cameras | Controllers\CamerasController.cs | 81 | var cameras = await db.Cameras |
| Cameras | Controllers\CamerasController.cs | 90 | return View(cameras); |
| Cameras | Controllers\CamerasController.cs | 106 | var cam = await db.Cameras |
| Buildings | Controllers\CategoriesController.cs | 30 | public CategoriesController(CategoriesService categories, BuildingsService buildings) |
| Buildings | Controllers\CategoriesController.cs | 33 | _buildings = buildings; |
| Buildings | Controllers\CategoriesController.cs | 54 | var buildings = await _buildings.GetSummariesWithCountsAsync(); |
| Buildings | Controllers\CategoriesController.cs | 55 | return View("ChooseBuilding", buildings); |
| Cameras | Controllers\DvrsController.cs | 13 | /// Management of DVR devices and the cameras attached to them (connection settings are stored encrypted). |
| Dvrs | Controllers\DvrsController.cs | 31 | DvrService dvrs, |
| Buildings | Controllers\DvrsController.cs | 32 | BuildingsService buildings, |
| Dvrs | Controllers\DvrsController.cs | 36 | _dvrs = dvrs; |
| Buildings | Controllers\DvrsController.cs | 37 | _buildings = buildings; |
| Dvrs | Controllers\DvrsController.cs | 52 | // Index — كل DVRs العمارة |
| Dvrs | Controllers\DvrsController.cs | 55 | /// Shows the main page of the dvrs screen. |
| Dvrs | Controllers\DvrsController.cs | 71 | var dvrs = await _dvrs.GetByBuildingAsync(bid); |
| Dvrs | Controllers\DvrsController.cs | 75 | return View(dvrs); |
| Dvrs | Controllers\DvrsController.cs | 82 | /// Creates the dvrs create page. |
| Dvrs | Controllers\DvrsController.cs | 100 | /// Handles the create form submission of the dvrs screen. |
| Dvrs | Controllers\DvrsController.cs | 128 | /// Edits the dvrs edit page. |
| Dvrs | Controllers\DvrsController.cs | 145 | /// Handles the edit form submission of the dvrs screen. |
| Cameras | Controllers\DvrsController.cs | 170 | // Details + Cameras |
| Dvrs | Controllers\DvrsController.cs | 173 | /// Shows the details page of the dvrs screen. |
| Dvrs | Controllers\DvrsController.cs | 259 | /// Handles the delete form submission of the dvrs screen. |
| Cameras | Controllers\DvrsController.cs | 271 | foreach (var camera in dvr.Cameras) |
| Buildings | Controllers\ExpensesController.cs | 32 | BuildingsService buildings, |
| Expenses | Controllers\ExpensesController.cs | 34 | ExpensesService expenses, |
| Buildings | Controllers\ExpensesController.cs | 38 | _buildings = buildings; |
| Expenses | Controllers\ExpensesController.cs | 40 | _expenses = expenses; |
| Expenses | Controllers\ExpensesController.cs | 48 | /// Shows the main page of the expenses screen. |
| Expenses | Controllers\ExpensesController.cs | 63 | var allExpenses = m.Expenses.OrderByDescending(e => e.Date).ToList(); |
| Expenses | Controllers\ExpensesController.cs | 72 | ViewBag.Total = m.Expenses.Sum(e => e.Amount); |
| Expenses | Controllers\ExpensesController.cs | 83 | /// Handles the add form submission of the expenses screen. |
| Expenses | Controllers\ExpensesController.cs | 117 | /// Handles the delete form submission of the expenses screen. |
| Buildings | Controllers\ImpersonationController.cs | 28 | BuildingsService buildings, |
| Buildings | Controllers\ImpersonationController.cs | 32 | _buildings = buildings; |
| Apartments | Controllers\ImpersonationController.cs | 66 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\ImpersonationController.cs | 104 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Buildings | Controllers\InvoiceController.cs | 28 | public InvoiceController(BuildingsService buildings, InvoiceService invoice, |
| Buildings | Controllers\InvoiceController.cs | 31 | _buildings = buildings; |
| Apartments | Controllers\InvoiceController.cs | 59 | var foundApt = foundBuilding?.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\InvoiceController.cs | 77 | var targetApt = building.Apartments.FirstOrDefault(a => a.Id == targetAptId); |
| Buildings | Controllers\MaintenanceController.cs | 29 | public MaintenanceController(BuildingsService buildings, MaintenanceService maint, AuditLogService audit, NotificationsService notify) |
| Buildings | Controllers\MaintenanceController.cs | 31 | _buildings = buildings; _maint = maint; _audit = audit; _notify = notify; |
| Expenses | Controllers\MaintenanceController.cs | 150 | /// Adds the to expenses. |
| Buildings | Controllers\NotificationsController.cs | 25 | public NotificationsController(BuildingsService buildings, NotificationsService notify) |
| Buildings | Controllers\NotificationsController.cs | 27 | _buildings = buildings; _notify = notify; |
| Notifications | Controllers\NotificationsController.cs | 33 | /// Shows the main page of the notifications screen. |
| Buildings | Controllers\PermissionsController.cs | 29 | public PermissionsController(AdminManagementService admins, BuildingsService buildings) |
| Buildings | Controllers\PermissionsController.cs | 32 | _buildings = buildings; |
| Buildings | Controllers\PermissionsController.cs | 46 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| Polls | Controllers\PollsController.cs | 12 | /// Admin side of polls: create, close, reopen, delete and view results. |
| Buildings | Controllers\PollsController.cs | 28 | public PollsController(BuildingsService buildings, PollsService polls, AuditLogService audit) |
| Polls | Controllers\PollsController.cs | 28 | public PollsController(BuildingsService buildings, PollsService polls, AuditLogService audit) |
| Buildings | Controllers\PollsController.cs | 30 | _buildings = buildings; _polls = polls; _audit = audit; |
| Polls | Controllers\PollsController.cs | 30 | _buildings = buildings; _polls = polls; _audit = audit; |
| Polls | Controllers\PollsController.cs | 36 | /// Shows the main page of the polls screen. |
| Polls | Controllers\PollsController.cs | 61 | /// Handles the create form submission of the polls screen. |
| Polls | Controllers\PollsController.cs | 88 | /// Handles the close form submission of the polls screen. |
| Polls | Controllers\PollsController.cs | 108 | /// Handles the reopen form submission of the polls screen. |
| Polls | Controllers\PollsController.cs | 128 | /// Handles the delete form submission of the polls screen. |
| Users | Controllers\PresenceController.cs | 11 | /// Heartbeat endpoint used to show which users are currently online. |
| Presence | Controllers\PresenceController.cs | 25 | public PresenceController(PresenceService presence, ILogger<PresenceController> logger) |
| Presence | Controllers\PresenceController.cs | 27 | _presence = presence; |
| Presence | Controllers\PresenceController.cs | 32 | // POST /Presence/Heartbeat |
| Buildings | Controllers\QrAccessController.cs | 28 | public QrAccessController(QrAuthService qrAuth, QrTokenStoreService qrStore, BuildingsService buildings) |
| Buildings | Controllers\QrAccessController.cs | 30 | _qrAuth = qrAuth; _qrStore = qrStore; _buildings = buildings; |
| Apartments | Controllers\QrAccessController.cs | 77 | var apartment = building?.Apartments.FirstOrDefault(a => a.Id == apt); |
| Apartments | Controllers\QrController.cs | 12 | /// Generation of QR codes and temporary login links for apartments and admins. |
| Buildings | Controllers\QrController.cs | 30 | public QrController(BuildingsService buildings, QrGeneratorService qrGen, QrAuthService qrAuth, |
| Buildings | Controllers\QrController.cs | 33 | _buildings = buildings; _qrGen = qrGen; _qrAuth = qrAuth; _qrStore = qrStore; _audit = audit; |
| Apartments | Controllers\QrController.cs | 64 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Floors | Controllers\QrController.cs | 66 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Apartments | Controllers\QrController.cs | 87 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Buildings | Controllers\ReportsController.cs | 30 | public ReportsController(BuildingsService buildings, ReportsService reports, WalletService wallet, IAuditLogger audit) |
| Buildings | Controllers\ReportsController.cs | 32 | _buildings = buildings; _reports = reports; _wallet = wallet; _audit = audit; |
| Expenses | Controllers\ReportsController.cs | 85 | Row(Loc.T("Month"), Loc.T("Collected_2"), Loc.T("Revenues_2"), Loc.T("Expenses"), Loc.T("Net")); |
| Expenses | Controllers\ReportsController.cs | 87 | Row(mp.Month, mp.Collected.ToString("0.##"), mp.Revenues.ToString("0.##"), mp.Expenses.ToString("0.##"), |
| Revenues | Controllers\ReportsController.cs | 87 | Row(mp.Month, mp.Collected.ToString("0.##"), mp.Revenues.ToString("0.##"), mp.Expenses.ToString("0.##"), |
| Expenses | Controllers\ReportsController.cs | 88 | (mp.Collected + mp.Revenues - mp.Expenses).ToString("0.##")); |
| Revenues | Controllers\ReportsController.cs | 88 | (mp.Collected + mp.Revenues - mp.Expenses).ToString("0.##")); |
| Apartments | Controllers\ReportsController.cs | 93 | foreach (var a in building.Apartments) |
| Buildings | Controllers\ResidentHomeController.cs | 30 | BuildingsService buildings, |
| Polls | Controllers\ResidentHomeController.cs | 32 | PollsService polls, |
| Buildings | Controllers\ResidentHomeController.cs | 36 | _buildings = buildings; |
| Polls | Controllers\ResidentHomeController.cs | 38 | _polls = polls; |
| Apartments | Controllers\ResidentHomeController.cs | 58 | apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\ResidentHomeController.cs | 61 | apt = building.Apartments.FirstOrDefault(a => a.Number.ToString() == aptNumber); |
| Notifications | Controllers\ResidentHomeController.cs | 139 | var notifications = _notify.GetForResident(building, apt.Id, 3) |
| Notifications | Controllers\ResidentHomeController.cs | 205 | ViewBag.Notifications = notifications; |
| Residents | Controllers\ResidentMaintenanceController.cs | 11 | /// Read-only view of maintenance records for residents. |
| Buildings | Controllers\ResidentMaintenanceController.cs | 25 | public ResidentMaintenanceController(BuildingsService buildings, MaintenanceService maint) |
| Buildings | Controllers\ResidentMaintenanceController.cs | 27 | _buildings = buildings; _maint = maint; |
| Polls | Controllers\ResidentPollsController.cs | 11 | /// Resident view of active polls and voting (one vote per apartment). |
| Buildings | Controllers\ResidentPollsController.cs | 26 | public ResidentPollsController(BuildingsService buildings, PollsService polls, AuditLogService audit) |
| Polls | Controllers\ResidentPollsController.cs | 26 | public ResidentPollsController(BuildingsService buildings, PollsService polls, AuditLogService audit) |
| Buildings | Controllers\ResidentPollsController.cs | 28 | _buildings = buildings; _polls = polls; _audit = audit; |
| Polls | Controllers\ResidentPollsController.cs | 28 | _buildings = buildings; _polls = polls; _audit = audit; |
| Polls | Controllers\ResidentPollsController.cs | 35 | /// Shows the main page of the resident polls screen. |
| Apartments | Controllers\ResidentPollsController.cs | 46 | var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId); |
| Polls | Controllers\ResidentPollsController.cs | 62 | /// Handles the vote form submission of the resident polls screen. |
| Apartments | Controllers\ResidentPollsController.cs | 73 | var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId); |
| Polls | Controllers\ResidentPollsController.cs | 78 | var poll = building.Polls.First(p => p.Id == pollId); |
| Buildings | Controllers\ResidentWalletController.cs | 28 | public ResidentWalletController(BuildingsService buildings, WalletService wallet, AuditLogService audit) |
| Buildings | Controllers\ResidentWalletController.cs | 30 | _buildings = buildings; _wallet = wallet; _audit = audit; |
| Apartments | Controllers\ResidentWalletController.cs | 56 | var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId); |
| Deposits | Controllers\ResidentWalletController.cs | 118 | (selectedTypes.Contains("deposits") && t.Type == "deposit") \|\| |
| Expenses | Controllers\ResidentWalletController.cs | 119 | (selectedTypes.Contains("expenses") && t.Type == "expense") \|\| |
| Revenues | Controllers\ResidentWalletController.cs | 120 | (selectedTypes.Contains("revenues") && t.Type == "revenue") \|\| |
| Apartments | Controllers\ResidentWalletController.cs | 182 | var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId); |
| Buildings | Controllers\RevenuesController.cs | 32 | BuildingsService buildings, |
| Revenues | Controllers\RevenuesController.cs | 34 | RevenuesService revenues, |
| Buildings | Controllers\RevenuesController.cs | 38 | _buildings = buildings; |
| Revenues | Controllers\RevenuesController.cs | 40 | _revenues = revenues; |
| Revenues | Controllers\RevenuesController.cs | 48 | /// Shows the main page of the revenues screen. |
| Revenues | Controllers\RevenuesController.cs | 62 | var allRevenues = m.Revenues.OrderByDescending(r => r.Date).ToList(); |
| Revenues | Controllers\RevenuesController.cs | 69 | ViewBag.Total = m.Revenues.Sum(r => r.Amount); |
| Revenues | Controllers\RevenuesController.cs | 80 | /// Handles the add form submission of the revenues screen. |
| Revenues | Controllers\RevenuesController.cs | 114 | /// Handles the delete form submission of the revenues screen. |
| Buildings | Controllers\SettingsController.cs | 35 | BuildingsService buildings, |
| Users | Controllers\SettingsController.cs | 36 | UsersService users, |
| Buildings | Controllers\SettingsController.cs | 41 | _buildings = buildings; |
| Users | Controllers\SettingsController.cs | 42 | _users = users; |
| GlobalSettings | Controllers\SettingsController.cs | 105 | public async Task<IActionResult> Global(GlobalSettings model) |
| Buildings | Controllers\SettingsController.cs | 140 | ViewBag.Buildings = await _buildings.GetSummariesAsync(); |
| BuildingSettings | Controllers\SettingsController.cs | 173 | public async Task<IActionResult> Building(string buildingId, BuildingSettings model) |
| Apartments | Controllers\SettingsController.cs | 215 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == CurrentApartmentId); |
| Buildings | Controllers\SuperAdminHomeController.cs | 10 | /// Super admin dashboard with the buildings overview. |
| Buildings | Controllers\SuperAdminHomeController.cs | 22 | public SuperAdminHomeController(BuildingsService buildings) => _buildings = buildings; |
| Buildings | Controllers\SuperAdminHomeController.cs | 29 | var buildings = await _buildings.GetSummariesWithCountsAsync(); |
| Buildings | Controllers\SuperAdminHomeController.cs | 30 | return View(buildings); |
| Buildings | Controllers\WalletController.cs | 31 | BuildingsService buildings, |
| Buildings | Controllers\WalletController.cs | 36 | _buildings = buildings; |
| Floors | Controllers\WalletController.cs | 59 | var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order); |
| Apartments | Controllers\WalletController.cs | 62 | var allBalances = building.Apartments |
| Apartments | Controllers\WalletController.cs | 121 | var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId); |
| Apartments | Controllers\WalletController.cs | 163 | var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId); |
| Deposits | Controllers\WalletController.cs | 238 | var dep = m.Deposits.FirstOrDefault(d => d.Id == depositId); |
| Apartments | Controllers\WalletController.cs | 277 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Apartments | Controllers\WhatsAppController.cs | 12 | /// Builds WhatsApp message drafts (welcome, reminders, payment notices) from templates for apartments. |
| Buildings | Controllers\WhatsAppController.cs | 27 | public WhatsAppController(BuildingsService buildings, WhatsAppTemplateService templates) |
| Buildings | Controllers\WhatsAppController.cs | 29 | _buildings = buildings; _templates = templates; |
| Apartments | Controllers\WhatsAppController.cs | 61 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Users | Data\AppDbContext.cs | 51 | /// Gets the users. |
| Users | Data\AppDbContext.cs | 53 | public DbSet<UserEntity> Users => Set<UserEntity>(); |
| UserPermissions | Data\AppDbContext.cs | 57 | public DbSet<UserPermissionEntity> UserPermissions => Set<UserPermissionEntity>(); |
| Buildings | Data\AppDbContext.cs | 59 | /// Gets the buildings. |
| Buildings | Data\AppDbContext.cs | 61 | public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>(); |
| BuildingSettings | Data\AppDbContext.cs | 65 | public DbSet<BuildingSettingsEntity> BuildingSettings => Set<BuildingSettingsEntity>(); |
| BuildingAdmins | Data\AppDbContext.cs | 69 | public DbSet<BuildingAdminEntity> BuildingAdmins => Set<BuildingAdminEntity>(); |
| Floors | Data\AppDbContext.cs | 71 | /// Gets the floors. |
| Floors | Data\AppDbContext.cs | 73 | public DbSet<FloorEntity> Floors => Set<FloorEntity>(); |
| Apartments | Data\AppDbContext.cs | 75 | /// Gets the apartments. |
| Apartments | Data\AppDbContext.cs | 77 | public DbSet<ApartmentEntity> Apartments => Set<ApartmentEntity>(); |
| FinancialCategories | Data\AppDbContext.cs | 81 | public DbSet<FinancialCategoryEntity> FinancialCategories => Set<FinancialCategoryEntity>(); |
| Expenses | Data\AppDbContext.cs | 83 | /// Gets the expenses. |
| Expenses | Data\AppDbContext.cs | 85 | public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>(); |
| Revenues | Data\AppDbContext.cs | 87 | /// Gets the revenues. |
| Revenues | Data\AppDbContext.cs | 89 | public DbSet<RevenueEntity> Revenues => Set<RevenueEntity>(); |
| MonthlyApartmentShares | Data\AppDbContext.cs | 93 | public DbSet<MonthlyApartmentShareEntity> MonthlyApartmentShares => Set<MonthlyApartmentShareEntity>(); |
| Deposits | Data\AppDbContext.cs | 95 | /// Gets the deposits. |
| Deposits | Data\AppDbContext.cs | 97 | public DbSet<DepositEntity> Deposits => Set<DepositEntity>(); |
| DepositEditHistory | Data\AppDbContext.cs | 101 | public DbSet<DepositEditEntity> DepositEditHistory => Set<DepositEditEntity>(); |
| WalletAdjustments | Data\AppDbContext.cs | 105 | public DbSet<WalletAdjustmentEntity> WalletAdjustments => Set<WalletAdjustmentEntity>(); |
| AuditLog | Data\AppDbContext.cs | 110 | public DbSet<AuditLogEntity> AuditLog => Set<AuditLogEntity>(); |
| Notifications | Data\AppDbContext.cs | 112 | /// Gets the notifications. |
| Notifications | Data\AppDbContext.cs | 114 | public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>(); |
| Polls | Data\AppDbContext.cs | 116 | /// Gets the polls. |
| Polls | Data\AppDbContext.cs | 118 | public DbSet<PollEntity> Polls => Set<PollEntity>(); |
| PollOptions | Data\AppDbContext.cs | 122 | public DbSet<PollOptionEntity> PollOptions => Set<PollOptionEntity>(); |
| PollVotes | Data\AppDbContext.cs | 126 | public DbSet<PollVoteEntity> PollVotes => Set<PollVoteEntity>(); |
| MaintenanceRecords | Data\AppDbContext.cs | 130 | public DbSet<MaintenanceRecordEntity> MaintenanceRecords => Set<MaintenanceRecordEntity>(); |
| QrTokens | Data\AppDbContext.cs | 134 | public DbSet<QrTokenEntity> QrTokens => Set<QrTokenEntity>(); |
| QrUsages | Data\AppDbContext.cs | 138 | public DbSet<QrUsageEntity> QrUsages => Set<QrUsageEntity>();   // ✅ جديد |
| Presence | Data\AppDbContext.cs | 140 | /// Gets the presence. |
| Presence | Data\AppDbContext.cs | 142 | public DbSet<PresenceEntity> Presence => Set<PresenceEntity>(); |
| GlobalSettings | Data\AppDbContext.cs | 146 | public DbSet<GlobalSettingsEntity> GlobalSettings => Set<GlobalSettingsEntity>(); |
| ScheduledBackupSettings | Data\AppDbContext.cs | 150 | public DbSet<ScheduledBackupSettingsEntity> ScheduledBackupSettings => Set<ScheduledBackupSettingsEntity>(); |
| BackupHistory | Data\AppDbContext.cs | 154 | public DbSet<BackupHistoryEntity> BackupHistory => Set<BackupHistoryEntity>(); |
| SyncPendingChanges | Data\AppDbContext.cs | 162 | public DbSet<SyncPendingChangeEntity> SyncPendingChanges => Set<SyncPendingChangeEntity>(); |
| Dvrs | Data\AppDbContext.cs | 164 | /// Gets the dvrs. |
| Dvrs | Data\AppDbContext.cs | 166 | public DbSet<DvrEntity> Dvrs => Set<DvrEntity>(); |
| Cameras | Data\AppDbContext.cs | 168 | /// Gets the cameras. |
| Cameras | Data\AppDbContext.cs | 170 | public DbSet<CameraEntity> Cameras => Set<CameraEntity>(); |
| Residents | Data\AppDbContext.cs | 172 | /// Gets the residents. |
| Residents | Data\AppDbContext.cs | 174 | public DbSet<ResidentEntity> Residents => Set<ResidentEntity>(); |
| Users | Data\AppDbContext.cs | 321 | e.ToTable("Users"); |
| UserPermissions | Data\AppDbContext.cs | 327 | e.ToTable("UserPermissions"); |
| Buildings | Data\AppDbContext.cs | 336 | e.ToTable("Buildings"); |
| BuildingSettings | Data\AppDbContext.cs | 344 | e.ToTable("BuildingSettings"); |
| BuildingAdmins | Data\AppDbContext.cs | 353 | e.ToTable("BuildingAdmins"); |
| BuildingAdmins | Data\AppDbContext.cs | 357 | e.HasOne(x => x.User).WithMany(u => u.BuildingAdmins) |
| Floors | Data\AppDbContext.cs | 363 | e.ToTable("Floors"); |
| Floors | Data\AppDbContext.cs | 365 | e.HasOne(x => x.Building).WithMany(b => b.Floors) |
| Apartments | Data\AppDbContext.cs | 371 | e.ToTable("Apartments"); |
| Apartments | Data\AppDbContext.cs | 374 | e.HasOne(x => x.Building).WithMany(b => b.Apartments) |
| Apartments | Data\AppDbContext.cs | 376 | e.HasOne(x => x.Floor).WithMany(f => f.Apartments) |
| FinancialCategories | Data\AppDbContext.cs | 383 | e.ToTable("FinancialCategories"); |
| Expenses | Data\AppDbContext.cs | 391 | e.ToTable("Expenses"); |
| Revenues | Data\AppDbContext.cs | 401 | e.ToTable("Revenues"); |
| MonthlyApartmentShares | Data\AppDbContext.cs | 411 | e.ToTable("MonthlyApartmentShares"); |
| Deposits | Data\AppDbContext.cs | 421 | e.ToTable("Deposits"); |
| DepositEditHistory | Data\AppDbContext.cs | 431 | e.ToTable("DepositEditHistory"); |
| WalletAdjustments | Data\AppDbContext.cs | 440 | e.ToTable("WalletAdjustments"); |
| AuditLog | Data\AppDbContext.cs | 453 | e.ToTable("AuditLog"); |
| Notifications | Data\AppDbContext.cs | 466 | e.ToTable("Notifications"); |
| Polls | Data\AppDbContext.cs | 478 | e.ToTable("Polls"); |
| PollOptions | Data\AppDbContext.cs | 486 | e.ToTable("PollOptions"); |
| PollVotes | Data\AppDbContext.cs | 494 | e.ToTable("PollVotes"); |
| MaintenanceRecords | Data\AppDbContext.cs | 507 | e.ToTable("MaintenanceRecords"); |
| QrTokens | Data\AppDbContext.cs | 516 | e.ToTable("QrTokens"); |
| QrUsages | Data\AppDbContext.cs | 526 | // ✅ جديد: QrUsages |
| QrUsages | Data\AppDbContext.cs | 529 | e.ToTable("QrUsages"); |
| Presence | Data\AppDbContext.cs | 539 | e.ToTable("Presence"); |
| GlobalSettings | Data\AppDbContext.cs | 547 | e.ToTable("GlobalSettings"); |
| ScheduledBackupSettings | Data\AppDbContext.cs | 553 | e.ToTable("ScheduledBackupSettings"); |
| BackupHistory | Data\AppDbContext.cs | 559 | e.ToTable("BackupHistory"); |
| SyncPendingChanges | Data\AppDbContext.cs | 572 | e.ToTable("SyncPendingChanges"); |
| Dvrs | Data\AppDbContext.cs | 581 | e.ToTable("Dvrs"); |
| Cameras | Data\AppDbContext.cs | 591 | e.ToTable("Cameras"); |
| Cameras | Data\AppDbContext.cs | 595 | e.HasOne(x => x.Dvr).WithMany(d => d.Cameras) |
| Residents | Data\AppDbContext.cs | 600 | // ✅ Residents — جدول الساكنين الجديد |
| Residents | Data\AppDbContext.cs | 604 | e.ToTable("Residents"); |
| AuditLog | Data\BuildingSqlMapper.cs | 13 | //     ما عدا AuditLog: Append-only (بيضيف الجديد بس). |
| Buildings | Data\BuildingSqlMapper.cs | 126 | var be = await db.Buildings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); |
| Floors | Data\BuildingSqlMapper.cs | 129 | var floors = await db.Floors.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Apartments | Data\BuildingSqlMapper.cs | 130 | var apts = await db.Apartments.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| FinancialCategories | Data\BuildingSqlMapper.cs | 131 | var cats = await db.FinancialCategories.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Expenses | Data\BuildingSqlMapper.cs | 132 | var expenses = await db.Expenses.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Revenues | Data\BuildingSqlMapper.cs | 133 | var revenues = await db.Revenues.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| MonthlyApartmentShares | Data\BuildingSqlMapper.cs | 134 | var shares = await db.MonthlyApartmentShares.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Deposits | Data\BuildingSqlMapper.cs | 135 | var deposits = await db.Deposits.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| DepositEditHistory | Data\BuildingSqlMapper.cs | 136 | var edits = await db.DepositEditHistory.AsNoTracking().Where(x => x.BuildingId == id) |
| WalletAdjustments | Data\BuildingSqlMapper.cs | 138 | var adjustments = await db.WalletAdjustments.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Notifications | Data\BuildingSqlMapper.cs | 139 | var notifs = await db.Notifications.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| Polls | Data\BuildingSqlMapper.cs | 140 | var polls = await db.Polls.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| PollOptions | Data\BuildingSqlMapper.cs | 141 | var options = await db.PollOptions.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| PollVotes | Data\BuildingSqlMapper.cs | 142 | var votes = await db.PollVotes.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| MaintenanceRecords | Data\BuildingSqlMapper.cs | 143 | var maint = await db.MaintenanceRecords.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct); |
| BuildingAdmins | Data\BuildingSqlMapper.cs | 144 | var admins = await db.BuildingAdmins.AsNoTracking().Where(x => x.BuildingId == id) |
| AuditLog | Data\BuildingSqlMapper.cs | 148 | var audit = await db.AuditLog.AsNoTracking().Where(x => x.BuildingId == id && x.SourceId != null) |
| Floors | Data\BuildingSqlMapper.cs | 172 | Floors = floors.OrderBy(f => f.SortOrder) |
| Apartments | Data\BuildingSqlMapper.cs | 174 | Apartments = apts.Select(a => new Apartment |
| WalletAdjustments | Data\BuildingSqlMapper.cs | 197 | WalletAdjustments = adjustments.OrderBy(a => a.CreatedAt).Select(a => new WalletAdjustment |
| Notifications | Data\BuildingSqlMapper.cs | 207 | Notifications = notifs.OrderBy(n => n.Ts).Select(n => new Notification |
| AuditLog | Data\BuildingSqlMapper.cs | 235 | AuditLog = audit.Select(a => new AuditLogEntry |
| Expenses | Data\BuildingSqlMapper.cs | 258 | foreach (var e in expenses.OrderBy(x => x.ExpenseDate).ThenBy(x => x.Id)) |
| Expenses | Data\BuildingSqlMapper.cs | 259 | Month(e.MonthKey).Expenses.Add(new Expense |
| Revenues | Data\BuildingSqlMapper.cs | 269 | foreach (var r in revenues.OrderBy(x => x.RevenueDate).ThenBy(x => x.Id)) |
| Revenues | Data\BuildingSqlMapper.cs | 270 | Month(r.MonthKey).Revenues.Add(new Revenue |
| Deposits | Data\BuildingSqlMapper.cs | 289 | foreach (var d in deposits.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)) |
| Deposits | Data\BuildingSqlMapper.cs | 333 | Month(d.MonthKey).Deposits.Add(dep); |
| Polls | Data\BuildingSqlMapper.cs | 339 | b.Polls = polls.OrderBy(p => p.CreatedAt).Select(p => new Poll |
| Buildings | Data\BuildingSqlMapper.cs | 405 | var be = await db.Buildings.FirstOrDefaultAsync(x => x.Id == bid, ct); |
| Buildings | Data\BuildingSqlMapper.cs | 409 | db.Buildings.Add(be); |
| Buildings | Data\BuildingSqlMapper.cs | 415 | // A child-only aggregate change would otherwise leave the Buildings row untouched, |
| Users | Data\BuildingSqlMapper.cs | 433 | // ---------------- 2) الأدمنز (لازم يكونوا موجودين في Users) ---------------- |
| Users | Data\BuildingSqlMapper.cs | 435 | var knownUids = await db.Users.Where(u => uids.Contains(u.Uid)).Select(u => u.Uid).ToListAsync(ct); |
| Users | Data\BuildingSqlMapper.cs | 437 | warn.Add($"admin uid '{u}' مش موجود في جدول Users - اتخطّى"); |
| BuildingAdmins | Data\BuildingSqlMapper.cs | 438 | var adminsExisting = await db.BuildingAdmins.Where(x => x.BuildingId == bid).ToListAsync(ct); |
| Floors | Data\BuildingSqlMapper.cs | 444 | var floors = (src.Floors ?? new()) |
| Floors | Data\BuildingSqlMapper.cs | 450 | var floorIds = floors.Select(f => f.Id).ToHashSet(); |
| Floors | Data\BuildingSqlMapper.cs | 451 | var floorsExisting = await db.Floors.Where(x => x.BuildingId == bid).ToListAsync(ct); |
| Floors | Data\BuildingSqlMapper.cs | 452 | Sync(db, floorsExisting, floors, x => x.Id, warn, "floors"); |
| Apartments | Data\BuildingSqlMapper.cs | 455 | var apartments = new List<ApartmentEntity>(); |
| Apartments | Data\BuildingSqlMapper.cs | 457 | foreach (var a in src.Apartments ?? new()) |
| Apartments | Data\BuildingSqlMapper.cs | 468 | apartments.Add(new ApartmentEntity |
| Apartments | Data\BuildingSqlMapper.cs | 495 | var aptIds = apartments.Select(a => a.Id).ToHashSet(); |
| Apartments | Data\BuildingSqlMapper.cs | 496 | var aptsExisting = await db.Apartments.Where(x => x.BuildingId == bid).ToListAsync(ct); |
| Apartments | Data\BuildingSqlMapper.cs | 497 | Sync(db, aptsExisting, apartments, x => x.Id, warn, "apartments"); |
| FinancialCategories | Data\BuildingSqlMapper.cs | 517 | var catsExisting = await db.FinancialCategories.Where(x => x.BuildingId == bid).ToListAsync(ct); |
| Expenses | Data\BuildingSqlMapper.cs | 521 | var expenses = new List<ExpenseEntity>(); |
| Revenues | Data\BuildingSqlMapper.cs | 522 | var revenues = new List<RevenueEntity>(); |
| Deposits | Data\BuildingSqlMapper.cs | 524 | var deposits = new List<DepositEntity>(); |
| Expenses | Data\BuildingSqlMapper.cs | 542 | foreach (var e in m.Expenses ?? new()) |
| Expenses | Data\BuildingSqlMapper.cs | 550 | expenses.Add(new ExpenseEntity |
| Revenues | Data\BuildingSqlMapper.cs | 559 | foreach (var r in m.Revenues ?? new()) |
| Revenues | Data\BuildingSqlMapper.cs | 567 | revenues.Add(new RevenueEntity |
| Deposits | Data\BuildingSqlMapper.cs | 579 | foreach (var d in m.Deposits ?? new()) |
| Deposits | Data\BuildingSqlMapper.cs | 591 | deposits.Add(new DepositEntity |
| Expenses | Data\BuildingSqlMapper.cs | 620 | Sync(db, await db.Expenses.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Expenses | Data\BuildingSqlMapper.cs | 621 | expenses, x => x.Id, warn, "expenses"); |
| Revenues | Data\BuildingSqlMapper.cs | 622 | Sync(db, await db.Revenues.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Revenues | Data\BuildingSqlMapper.cs | 623 | revenues, x => x.Id, warn, "revenues"); |
| MonthlyApartmentShares | Data\BuildingSqlMapper.cs | 624 | Sync(db, await db.MonthlyApartmentShares.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Deposits | Data\BuildingSqlMapper.cs | 626 | Sync(db, await db.Deposits.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Deposits | Data\BuildingSqlMapper.cs | 627 | deposits, x => x.Id, warn, "deposits"); |
| DepositEditHistory | Data\BuildingSqlMapper.cs | 630 | var editsExisting = (await db.DepositEditHistory.Where(x => x.BuildingId == bid) |
| DepositEditHistory | Data\BuildingSqlMapper.cs | 638 | db.DepositEditHistory.RemoveRange(cur); |
| DepositEditHistory | Data\BuildingSqlMapper.cs | 639 | db.DepositEditHistory.AddRange(incoming); |
| DepositEditHistory | Data\BuildingSqlMapper.cs | 643 | db.DepositEditHistory.RemoveRange(cur); |
| WalletAdjustments | Data\BuildingSqlMapper.cs | 647 | foreach (var a in src.WalletAdjustments ?? new()) |
| WalletAdjustments | Data\BuildingSqlMapper.cs | 661 | Sync(db, await db.WalletAdjustments.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| WalletAdjustments | Data\BuildingSqlMapper.cs | 662 | adjustments, x => x.Id, warn, "walletAdjustments"); |
| Notifications | Data\BuildingSqlMapper.cs | 666 | foreach (var n in src.Notifications ?? new()) |
| Notifications | Data\BuildingSqlMapper.cs | 686 | Sync(db, await db.Notifications.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Notifications | Data\BuildingSqlMapper.cs | 687 | notifs, x => x.Id, warn, "notifications"); |
| Polls | Data\BuildingSqlMapper.cs | 693 | foreach (var p in src.Polls ?? new()) |
| Polls | Data\BuildingSqlMapper.cs | 719 | Sync(db, await db.Polls.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| Polls | Data\BuildingSqlMapper.cs | 720 | pollsIn, x => x.Id, warn, "polls"); |
| PollOptions | Data\BuildingSqlMapper.cs | 721 | Sync(db, await db.PollOptions.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| PollOptions | Data\BuildingSqlMapper.cs | 722 | optsIn, x => (x.PollId, x.Id), warn, "pollOptions"); |
| PollVotes | Data\BuildingSqlMapper.cs | 723 | Sync(db, await db.PollVotes.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| PollVotes | Data\BuildingSqlMapper.cs | 724 | votesIn, x => (x.PollId, x.ApartmentId), warn, "pollVotes"); |
| MaintenanceRecords | Data\BuildingSqlMapper.cs | 745 | Sync(db, await db.MaintenanceRecords.Where(x => x.BuildingId == bid).ToListAsync(ct), |
| AuditLog | Data\BuildingSqlMapper.cs | 749 | var knownAudit = (await db.AuditLog.Where(x => x.BuildingId == bid && x.SourceId != null) |
| AuditLog | Data\BuildingSqlMapper.cs | 751 | foreach (var e in src.AuditLog ?? new()) |
| AuditLog | Data\BuildingSqlMapper.cs | 755 | db.AuditLog.Add(new AuditLogEntity |
| Apartments | Data\BuildingSqlMapper.cs | 763 | ? src.Apartments?.FirstOrDefault(a => a.Number == e.AptNumber.Value)?.Id |
| Buildings | Data\BuildingSqlStore.cs | 6 | //  Optimistic concurrency is enforced by Buildings.RowVersion for every aggregate save. |
| Buildings | Data\BuildingSqlStore.cs | 16 | /// Loads and saves buildings (the whole aggregate) from SQL Server through EF Core. |
| Buildings | Data\BuildingSqlStore.cs | 42 | // استعلام خفيف على Buildings بس. |
| Buildings | Data\BuildingSqlStore.cs | 49 | var ids = await db.Buildings.AsNoTracking() |
| Buildings | Data\BuildingSqlStore.cs | 70 | var rows = await db.Buildings.AsNoTracking() |
| Buildings | Data\BuildingSqlStore.cs | 92 | var query = db.Buildings.AsNoTracking().OrderBy(b => b.BuildingNumber); |
| Apartments | Data\BuildingSqlStore.cs | 108 | //  مفيدة للـ Views اللي بتستخدم @b.Apartments.Count و @b.Floors.Count. |
| Floors | Data\BuildingSqlStore.cs | 108 | //  مفيدة للـ Views اللي بتستخدم @b.Apartments.Count و @b.Floors.Count. |
| Buildings | Data\BuildingSqlStore.cs | 118 | var rows = await db.Buildings.AsNoTracking() |
| Apartments | Data\BuildingSqlStore.cs | 125 | ApartmentsCount = b.Apartments.Count(), |
| Floors | Data\BuildingSqlStore.cs | 126 | FloorsCount = b.Floors.Count() |
| Apartments | Data\BuildingSqlStore.cs | 146 | return await db.Apartments.AsNoTracking() |
| Buildings | Data\BuildingSqlStore.cs | 169 | await db.Buildings.Where(x => x.Id == id).ExecuteDeleteAsync(); |
| BuildingSettings | Data\BuildingSqlStore.cs | 183 | var entity = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == buildingId); |
| BuildingSettings | Data\BuildingSqlStore.cs | 187 | db.BuildingSettings.Add(entity); |
| BuildingAdmins | Data\Entities.cs | 95 | public List<BuildingAdminEntity> BuildingAdmins { get; set; } = new(); |
| Floors | Data\Entities.cs | 183 | /// Gets or sets the floors. |
| Floors | Data\Entities.cs | 185 | public List<FloorEntity> Floors { get; set; } = new(); |
| Apartments | Data\Entities.cs | 187 | /// Gets or sets the apartments. |
| Apartments | Data\Entities.cs | 189 | public List<ApartmentEntity> Apartments { get; set; } = new(); |
| Apartments | Data\Entities.cs | 298 | /// Gets or sets the apartments. |
| Apartments | Data\Entities.cs | 300 | public List<ApartmentEntity> Apartments { get; set; } = new(); |
| Expenses | Data\Entities.cs | 547 | /// An apartment's share of one month's expenses/revenues together with the balance carried over from the previous month. |
| Revenues | Data\Entities.cs | 547 | /// An apartment's share of one month's expenses/revenues together with the balance carried over from the previous month. |
| Presence | Data\Entities.cs | 1178 | /// Database entity for the last-seen presence of a user. |
| Polls | Data\Entities.cs | 1235 | /// Gets or sets a value indicating whether enable polls is enabled. |
| Apartments | Data\Entities.cs | 1380 | /// Keyless entity mapped to a database view that lists the wallet movements of apartments. |
| Deposits | Data\Entities.cs | 1397 | /// Gets or sets the confirmed deposits. |
| Cameras | Data\Entities.cs | 1508 | /// Gets or sets the cameras. |
| Cameras | Data\Entities.cs | 1510 | public List<CameraEntity> Cameras { get; set; } = new(); |
| Deposits | Data\FinanceEntities.cs | 86 | /// <summary>New deposit workflow record. Legacy deposits remain available as source evidence.</summary> |
| Residents | Data\IdentityModelConfiguration.cs | 301 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Users | Data\IdentityModelConfiguration.cs | 301 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Residents | Data\IdentityModelConfiguration.cs | 310 | "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountId] IS NULL))"); |
| Users | Data\IdentityModelConfiguration.cs | 310 | "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountId] IS NULL))"); |
| Residents | Data\ResidentSqlStore.cs | 41 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 53 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 65 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 80 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 95 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 117 | return await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 136 | var row = await db.Residents.FirstOrDefaultAsync(r => r.Id == resident.Id, ct); |
| Residents | Data\ResidentSqlStore.cs | 140 | db.Residents.Add(row); |
| Residents | Data\ResidentSqlStore.cs | 176 | IEnumerable<ResidentEntity> residents, CancellationToken ct = default) |
| Residents | Data\ResidentSqlStore.cs | 179 | foreach (var r in residents) |
| Residents | Data\ResidentSqlStore.cs | 197 | var affected = await db.Residents.Where(r => r.Id == id).ExecuteDeleteAsync(ct); |
| Residents | Data\ResidentSqlStore.cs | 212 | await db.Residents |
| Residents | Data\ResidentSqlStore.cs | 227 | var query = db.Residents.AsQueryable(); |
| Users | Data\SyncReport.cs | 12 | /// Gets or sets the users. |
| Users | Data\SyncReport.cs | 14 | public int Users { get; set; } |
| Buildings | Data\SyncReport.cs | 16 | /// Gets or sets the buildings. |
| Buildings | Data\SyncReport.cs | 18 | public int Buildings { get; set; } |
| BuildingAdmins | Data\UserSqlMapper.cs | 64 | BuildingIds = e.BuildingAdmins?.Select(ba => ba.BuildingId).ToList() ?? new() |
| Users | Data\UserSqlStore.cs | 2 | //  UserSqlStore - ظ…ط±ط¢ط© SQL ظ„ظ…ط³طھظ†ط¯ط§طھ users (ط§ظ„ط£ط¯ظ…ظ†ط² ظˆط§ظ„ط³ظˆط¨ط± ط£ط¯ظ…ظ†) |
| Users | Data\UserSqlStore.cs | 47 | var row = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, ct); |
| Users | Data\UserSqlStore.cs | 51 | db.Users.Add(row); |
| UserPermissions | Data\UserSqlStore.cs | 72 | var have = await db.UserPermissions.Where(x => x.Uid == uid).ToListAsync(ct); |
| UserPermissions | Data\UserSqlStore.cs | 73 | db.UserPermissions.RemoveRange(have.Where(h => !want.Contains(h.Permission))); |
| UserPermissions | Data\UserSqlStore.cs | 75 | db.UserPermissions.Add(new UserPermissionEntity { Uid = uid, Permission = p }); |
| Buildings | Data\UserSqlStore.cs | 80 | var inSql = await db.Buildings.Where(b => ids.Contains(b.Id)).Select(b => b.Id).ToListAsync(ct); |
| BuildingAdmins | Data\UserSqlStore.cs | 81 | var linked = await db.BuildingAdmins.Where(x => x.AdminUid == uid) |
| BuildingAdmins | Data\UserSqlStore.cs | 84 | db.BuildingAdmins.Add(new BuildingAdminEntity { BuildingId = bid, AdminUid = uid }); |
| Users | Data\UserSqlStore.cs | 102 | return await db.Users |
| BuildingAdmins | Data\UserSqlStore.cs | 104 | .Include(u => u.BuildingAdmins) |
| Users | Data\UserSqlStore.cs | 118 | return await db.Users |
| BuildingAdmins | Data\UserSqlStore.cs | 120 | .Include(u => u.BuildingAdmins) |
| Users | Data\UserSqlStore.cs | 134 | return await db.Users |
| BuildingAdmins | Data\UserSqlStore.cs | 136 | .Include(u => u.BuildingAdmins) |
| Users | Data\UserSqlStore.cs | 152 | return await db.Users |
| BuildingAdmins | Data\UserSqlStore.cs | 154 | .Include(u => u.BuildingAdmins) |
| BuildingAdmins | Data\UserSqlStore.cs | 156 | .Where(u => u.BuildingAdmins.Any(ba => ba.BuildingId == buildingId)) |
| Buildings | Models\AdminPermissions.cs | 12 | /// The manage buildings constant. |
| Buildings | Models\AdminPermissions.cs | 14 | public const string ManageBuildings = "buildings.manage"; |
| Residents | Models\AdminPermissions.cs | 20 | /// The manage residents constant. |
| Residents | Models\AdminPermissions.cs | 22 | public const string ManageResidents = "residents.manage"; |
| Dvrs | Models\AdminPermissions.cs | 32 | /// The manage dvrs constant. |
| Dvrs | Models\AdminPermissions.cs | 34 | public const string ManageDvrs = "dvrs.manage"; |
| Cameras | Models\AdminPermissions.cs | 36 | /// The view cameras constant. |
| Cameras | Models\AdminPermissions.cs | 38 | public const string ViewCameras = "cameras.view"; |
| Cameras | Models\AdminPermissions.cs | 40 | /// The manage cameras constant. |
| Cameras | Models\AdminPermissions.cs | 42 | public const string ManageCameras = "cameras.manage"; |
| Expenses | Models\AdminPermissions.cs | 60 | /// The manage expenses constant. |
| Expenses | Models\AdminPermissions.cs | 62 | public const string ManageExpenses = "expenses.manage"; |
| Revenues | Models\AdminPermissions.cs | 64 | /// The manage revenues constant. |
| Revenues | Models\AdminPermissions.cs | 66 | public const string ManageRevenues = "revenues.manage"; |
| Polls | Models\AdminPermissions.cs | 80 | /// The manage polls constant. |
| Polls | Models\AdminPermissions.cs | 82 | public const string ManagePolls = "polls.manage"; |
| Notifications | Models\AdminPermissions.cs | 88 | /// The send notifications constant. |
| Notifications | Models\AdminPermissions.cs | 90 | public const string SendNotifications = "notifications.send"; |
| Apartments | Models\Building.cs | 9 | /// Aggregate root of the domain: floors, apartments, categories, monthly financial data, wallet adjustments and the audit trail of one building. |
| Floors | Models\Building.cs | 9 | /// Aggregate root of the domain: floors, apartments, categories, monthly financial data, wallet adjustments and the audit trail of one building. |
| Floors | Models\Building.cs | 47 | /// Gets or sets the floors. |
| Floors | Models\Building.cs | 49 | public List<Floor> Floors { get; set; } = new(); |
| Apartments | Models\Building.cs | 51 | /// Gets or sets the apartments. |
| Apartments | Models\Building.cs | 53 | public List<Apartment> Apartments { get; set; } = new(); |
| WalletAdjustments | Models\Building.cs | 65 | public List<WalletAdjustment> WalletAdjustments { get; set; } = new(); |
| AuditLog | Models\Building.cs | 81 | public List<AuditLogEntry> AuditLog { get; set; } = new(); |
| Notifications | Models\Building.cs | 87 | /// Gets or sets the notifications. |
| Notifications | Models\Building.cs | 89 | public List<Notification> Notifications { get; set; } = new(); |
| Polls | Models\Building.cs | 91 | /// Gets or sets the polls. |
| Polls | Models\Building.cs | 93 | public List<Poll> Polls { get; set; } = new(); |
| Residents | Models\Building.cs | 222 | /// Transfer details (bank account, wallet number, ...) shown to residents and printed on invoices. |
| Deposits | Models\Finance.cs | 5 | /// All financial data of one month: expenses, revenues, deposits, per-apartment shares and carried-over balances. |
| Expenses | Models\Finance.cs | 5 | /// All financial data of one month: expenses, revenues, deposits, per-apartment shares and carried-over balances. |
| Revenues | Models\Finance.cs | 5 | /// All financial data of one month: expenses, revenues, deposits, per-apartment shares and carried-over balances. |
| Expenses | Models\Finance.cs | 14 | /// Gets or sets the expenses. |
| Expenses | Models\Finance.cs | 16 | public List<Expense> Expenses { get; set; } = new(); |
| Deposits | Models\Finance.cs | 18 | /// Gets or sets the deposits. |
| Deposits | Models\Finance.cs | 20 | public List<Deposit> Deposits { get; set; } = new(); |
| Revenues | Models\Finance.cs | 22 | /// Gets or sets the revenues. |
| Revenues | Models\Finance.cs | 24 | public List<Revenue> Revenues { get; set; } = new(); |
| Presence | Models\Presence.cs | 4 | /// Last-seen presence information of a user. |
| Presence | Models\Presence.cs | 6 | public class Presence |
| ScheduledBackupSettings | Models\ScheduledBackupSettings.cs | 6 | public class ScheduledBackupSettings |
| GlobalSettings | Models\SettingsModels.cs | 9 | public class GlobalSettings |
| Polls | Models\SettingsModels.cs | 63 | /// Gets or sets a value indicating whether enable polls is enabled. |
| BuildingSettings | Models\SettingsModels.cs | 105 | public class BuildingSettings |
| Notifications | Resources\Shared.Designer.cs | 226 | ///   Looks up a localized string similar to 4 permissions — maintenance and notifications. |
| Expenses | Resources\Shared.Designer.cs | 352 | ///   Looks up a localized string similar to &lt;strong&gt;Account statement&lt;/strong&gt; — all transactions (payments, expenses, revenues, adjustments). |
| Revenues | Resources\Shared.Designer.cs | 352 | ///   Looks up a localized string similar to &lt;strong&gt;Account statement&lt;/strong&gt; — all transactions (payments, expenses, revenues, adjustments). |
| Expenses | Resources\Shared.Designer.cs | 523 | ///   Looks up a localized string similar to Add/edit/delete maintenance records + add the cost to expenses. |
| Expenses | Resources\Shared.Designer.cs | 532 | ///   Looks up a localized string similar to Add/edit/delete monthly expenses. |
| Revenues | Resources\Shared.Designer.cs | 541 | ///   Looks up a localized string similar to Add/edit/delete monthly revenues. |
| Residents | Resources\Shared.Designer.cs | 559 | ///   Looks up a localized string similar to Add/edit/disable residents + reset PINs. |
| Apartments | Resources\Shared.Designer.cs | 604 | ///   Looks up a localized string similar to &lt;strong&gt;Add floors and apartments&lt;/strong&gt; — with automatic Firebase Auth creation. |
| Floors | Resources\Shared.Designer.cs | 604 | ///   Looks up a localized string similar to &lt;strong&gt;Add floors and apartments&lt;/strong&gt; — with automatic Firebase Auth creation. |
| Expenses | Resources\Shared.Designer.cs | 676 | ///   Looks up a localized string similar to Add to expenses. |
| Expenses | Resources\Shared.Designer.cs | 685 | ///   Looks up a localized string similar to Added to expenses. |
| Buildings | Resources\Shared.Designer.cs | 910 | ///   Looks up a localized string similar to — All buildings —. |
| Buildings | Resources\Shared.Designer.cs | 919 | ///   Looks up a localized string similar to 🏢 All buildings. |
| Buildings | Resources\Shared.Designer.cs | 928 | ///   Looks up a localized string similar to ← All buildings. |
| Apartments | Resources\Shared.Designer.cs | 1288 | ///   Looks up a localized string similar to Apartments. |
| Apartments | Resources\Shared.Designer.cs | 1290 | internal static string Apartments { |
| Apartments | Resources\Shared.Designer.cs | 1292 | return ResourceManager.GetString("Apartments", resourceCulture); |
| Apartments | Resources\Shared.Designer.cs | 1297 | ///   Looks up a localized string similar to 🏠 Apartments ({0}). |
| Apartments | Resources\Shared.Designer.cs | 1306 | ///   Looks up a localized string similar to 🏠 Apartments of {0} ({1}). |
| Apartments | Resources\Shared.Designer.cs | 1315 | ///   Looks up a localized string similar to 🟢 Apartments with the highest balance. |
| AuditLog | Resources\Shared.Designer.cs | 1396 | ///   Looks up a localized string similar to auditLog is not a list. |
| Buildings | Resources\Shared.Designer.cs | 1486 | ///   Looks up a localized string similar to ← Back to buildings. |
| Buildings | Resources\Shared.Designer.cs | 1702 | ///   Looks up a localized string similar to Building (leave empty = all buildings). |
| Apartments | Resources\Shared.Designer.cs | 1765 | ///   Looks up a localized string similar to ✅ Building &apos;{0}&apos; imported successfully ({1} apartments). |
| Buildings | Resources\Shared.Designer.cs | 1873 | ///   Looks up a localized string similar to Buildings. |
| Buildings | Resources\Shared.Designer.cs | 1875 | internal static string Buildings { |
| Buildings | Resources\Shared.Designer.cs | 1877 | return ResourceManager.GetString("Buildings", resourceCulture); |
| Buildings | Resources\Shared.Designer.cs | 1882 | ///   Looks up a localized string similar to — Buildings. |
| Buildings | Resources\Shared.Designer.cs | 1891 | ///   Looks up a localized string similar to Buildings:. |
| Buildings | Resources\Shared.Designer.cs | 1900 | ///   Looks up a localized string similar to Buildings ({0}). |
| Residents | Resources\Shared.Designer.cs | 1945 | ///   Looks up a localized string similar to &lt;strong&gt;By number of residents&lt;/strong&gt; — a share based on the number of people in the apartment. |
| Residents | Resources\Shared.Designer.cs | 2210 | ///   Looks up a localized string similar to Check Firebase Auth for residents. |
| Expenses | Resources\Shared.Designer.cs | 2426 | ///   Looks up a localized string similar to Collected vs. expenses by month. |
| Apartments | Resources\Shared.Designer.cs | 2642 | ///   Looks up a localized string similar to Contact the admin. They can set a new PIN for you from the apartments page or from their settings.. |
| AuditLog | Resources\Shared.Designer.cs | 2660 | ///   Looks up a localized string similar to Convert &lt;code&gt;building.AuditLog&lt;/code&gt; → &lt;code&gt;auditLogs&lt;/code&gt; collection. |
| AuditLog | Resources\Shared.Designer.cs | 2669 | ///   Looks up a localized string similar to Convert the old building log (building.AuditLog) to the auditLogs collection. |
| Expenses | Resources\Shared.Designer.cs | 2741 | ///   Looks up a localized string similar to Cost added to expenses. |
| Polls | Resources\Shared.Designer.cs | 2840 | ///   Looks up a localized string similar to Create/close/delete polls + follow results. |
| Apartments | Resources\Shared.Designer.cs | 2849 | ///   Looks up a localized string similar to Create/edit/delete buildings + manage the structure (floors + apartments). |
| Buildings | Resources\Shared.Designer.cs | 2849 | ///   Looks up a localized string similar to Create/edit/delete buildings + manage the structure (floors + apartments). |
| Floors | Resources\Shared.Designer.cs | 2849 | ///   Looks up a localized string similar to Create/edit/delete buildings + manage the structure (floors + apartments). |
| Deposits | Resources\Shared.Designer.cs | 3299 | ///   Looks up a localized string similar to + Deposits this month. |
| Users | Resources\Shared.Designer.cs | 3380 | ///   Looks up a localized string similar to 👥 Detect duplicate users. |
| Expenses | Resources\Shared.Designer.cs | 3434 | ///   Looks up a localized string similar to Disable/enable the apartment (resident cannot log in, but expenses and invoices are still calculated). |
| Expenses | Resources\Shared.Designer.cs | 3443 | ///   Looks up a localized string similar to Disabling the apartment means the resident cannot log in, but expenses and invoices are still calculated.. |
| Apartments | Resources\Shared.Designer.cs | 3515 | ///   Looks up a localized string similar to Do you have an Excel file with all the data (floors + apartments + categories)? Use the ready-made template and upload it.. |
| Floors | Resources\Shared.Designer.cs | 3515 | ///   Looks up a localized string similar to Do you have an Excel file with all the data (floors + apartments + categories)? Use the ready-made template and upload it.. |
| Buildings | Resources\Shared.Designer.cs | 3596 | ///   Looks up a localized string similar to Download a full backup of all buildings as JSON.. |
| Apartments | Resources\Shared.Designer.cs | 3992 | ///   Looks up a localized string similar to &lt;strong&gt;Equally&lt;/strong&gt; — equally across all open apartments. |
| Expenses | Resources\Shared.Designer.cs | 4199 | ///   Looks up a localized string similar to Expenses. |
| Expenses | Resources\Shared.Designer.cs | 4201 | internal static string Expenses { |
| Expenses | Resources\Shared.Designer.cs | 4203 | return ResourceManager.GetString("Expenses", resourceCulture); |
| Expenses | Resources\Shared.Designer.cs | 4208 | ///   Looks up a localized string similar to — Expenses. |
| Expenses | Resources\Shared.Designer.cs | 4217 | ///   Looks up a localized string similar to Expenses. |
| Expenses | Resources\Shared.Designer.cs | 4226 | ///   Looks up a localized string similar to 💸 Expenses and revenues. |
| Revenues | Resources\Shared.Designer.cs | 4226 | ///   Looks up a localized string similar to 💸 Expenses and revenues. |
| Apartments | Resources\Shared.Designer.cs | 4235 | ///   Looks up a localized string similar to &lt;strong&gt;Expenses are distributed automatically&lt;/strong&gt; equally across all open apartments.. |
| Expenses | Resources\Shared.Designer.cs | 4235 | ///   Looks up a localized string similar to &lt;strong&gt;Expenses are distributed automatically&lt;/strong&gt; equally across all open apartments.. |
| Expenses | Resources\Shared.Designer.cs | 4244 | ///   Looks up a localized string similar to Expenses by item. |
| Expenses | Resources\Shared.Designer.cs | 4253 | ///   Looks up a localized string similar to 💰 Expenses of {0}. |
| Buildings | Resources\Shared.Designer.cs | 4280 | ///   Looks up a localized string similar to ⬇️ Export all buildings (JSON). |
| Buildings | Resources\Shared.Designer.cs | 4298 | ///   Looks up a localized string similar to Export all data (buildings + users) as a ZIP file. |
| Users | Resources\Shared.Designer.cs | 4298 | ///   Looks up a localized string similar to Export all data (buildings + users) as a ZIP file. |
| Expenses | Resources\Shared.Designer.cs | 4559 | ///   Looks up a localized string similar to 💰 &lt;strong&gt;Finance&lt;/strong&gt; — wallets, expenses, revenues, reports. |
| Revenues | Resources\Shared.Designer.cs | 4559 | ///   Looks up a localized string similar to 💰 &lt;strong&gt;Finance&lt;/strong&gt; — wallets, expenses, revenues, reports. |
| Expenses | Resources\Shared.Designer.cs | 4604 | ///   Looks up a localized string similar to Financial summary (collected, expenses, revenues, net). |
| Revenues | Resources\Shared.Designer.cs | 4604 | ///   Looks up a localized string similar to Financial summary (collected, expenses, revenues, net). |
| Apartments | Resources\Shared.Designer.cs | 4658 | ///   Looks up a localized string similar to Fix Firebase Auth accounts for all apartments?. |
| Floors | Resources\Shared.Designer.cs | 4748 | ///   Looks up a localized string similar to Floors. |
| Floors | Resources\Shared.Designer.cs | 4750 | internal static string Floors { |
| Floors | Resources\Shared.Designer.cs | 4752 | return ResourceManager.GetString("Floors", resourceCulture); |
| Residents | Resources\Shared.Designer.cs | 4793 | ///   Looks up a localized string similar to For residents. |
| Residents | Resources\Shared.Designer.cs | 4802 | ///   Looks up a localized string similar to 🏠 For residents. |
| Expenses | Resources\Shared.Designer.cs | 5234 | ///   Looks up a localized string similar to In &lt;code&gt;/Expenses&lt;/code&gt; and &lt;code&gt;/Revenues&lt;/code&gt;, you can:. |
| Revenues | Resources\Shared.Designer.cs | 5234 | ///   Looks up a localized string similar to In &lt;code&gt;/Expenses&lt;/code&gt; and &lt;code&gt;/Revenues&lt;/code&gt;, you can:. |
| Expenses | Resources\Shared.Designer.cs | 5578 | ///• The apartment will be added to the monthly expenses |
| Notifications | Resources\Shared.Designer.cs | 5710 | ///   Looks up a localized string similar to Language, notifications, payment method. |
| Apartments | Resources\Shared.Designer.cs | 6070 | ///   Looks up a localized string similar to Manage apartments. |
| Apartments | Resources\Shared.Designer.cs | 6079 | ///   Looks up a localized string similar to 🏠 Manage apartments. |
| Buildings | Resources\Shared.Designer.cs | 6088 | ///   Looks up a localized string similar to Manage buildings. |
| Expenses | Resources\Shared.Designer.cs | 6106 | ///   Looks up a localized string similar to Manage expenses. |
| Polls | Resources\Shared.Designer.cs | 6124 | ///   Looks up a localized string similar to Manage polls. |
| Residents | Resources\Shared.Designer.cs | 6133 | ///   Looks up a localized string similar to Manage residents. |
| Revenues | Resources\Shared.Designer.cs | 6142 | ///   Looks up a localized string similar to Manage revenues. |
| Buildings | Resources\Shared.Designer.cs | 6169 | ///   Looks up a localized string similar to 🏢 &lt;strong&gt;Management&lt;/strong&gt; — buildings, admins, residents. |
| Residents | Resources\Shared.Designer.cs | 6169 | ///   Looks up a localized string similar to 🏢 &lt;strong&gt;Management&lt;/strong&gt; — buildings, admins, residents. |
| Buildings | Resources\Shared.Designer.cs | 6187 | ///   Looks up a localized string similar to 🏢 Managing buildings. |
| Apartments | Resources\Shared.Designer.cs | 6376 | ///   Looks up a localized string similar to ✅ Migration done: {0} buildings, {1} months, {2} apartments updated. |
| Buildings | Resources\Shared.Designer.cs | 6376 | ///   Looks up a localized string similar to ✅ Migration done: {0} buildings, {1} months, {2} apartments updated. |
| Expenses | Resources\Shared.Designer.cs | 6439 | ///   Looks up a localized string similar to &lt;strong&gt;Month summary&lt;/strong&gt; — collected, expenses, revenues, wallet total. |
| Revenues | Resources\Shared.Designer.cs | 6439 | ///   Looks up a localized string similar to &lt;strong&gt;Month summary&lt;/strong&gt; — collected, expenses, revenues, wallet total. |
| Apartments | Resources\Shared.Designer.cs | 6493 | ///   Looks up a localized string similar to 🔴 Most indebted apartments. |
| Apartments | Resources\Shared.Designer.cs | 6664 | ///   Looks up a localized string similar to {0} apartments. |
| Apartments | Resources\Shared.Designer.cs | 6673 | ///   Looks up a localized string similar to {0} apartments — each resident scans their QR and the details are filled in automatically. |
| Buildings | Resources\Shared.Designer.cs | 6691 | ///   Looks up a localized string similar to — {0} buildings. |
| Apartments | Resources\Shared.Designer.cs | 6763 | ///   Looks up a localized string similar to 🏢 {0} floors \| 🏠 {1} apartments ({2} open. |
| Floors | Resources\Shared.Designer.cs | 6763 | ///   Looks up a localized string similar to 🏢 {0} floors \| 🏠 {1} apartments ({2} open. |
| Apartments | Resources\Shared.Designer.cs | 6808 | ///   Looks up a localized string similar to 📅 {0}: new distribution = {1} apartments. |
| Apartments | Resources\Shared.Designer.cs | 6907 | ///   Looks up a localized string similar to Apartments. |
| Buildings | Resources\Shared.Designer.cs | 6934 | ///   Looks up a localized string similar to Buildings. |
| Expenses | Resources\Shared.Designer.cs | 6952 | ///   Looks up a localized string similar to Expenses. |
| Notifications | Resources\Shared.Designer.cs | 7024 | ///   Looks up a localized string similar to Notifications. |
| Polls | Resources\Shared.Designer.cs | 7042 | ///   Looks up a localized string similar to Polls. |
| Revenues | Resources\Shared.Designer.cs | 7051 | ///   Looks up a localized string similar to Revenues. |
| Polls | Resources\Shared.Designer.cs | 7249 | ///   Looks up a localized string similar to No active polls at the moment.. |
| Buildings | Resources\Shared.Designer.cs | 7276 | ///   Looks up a localized string similar to No buildings found.. |
| Buildings | Resources\Shared.Designer.cs | 7285 | ///   Looks up a localized string similar to No buildings yet.. |
| Expenses | Resources\Shared.Designer.cs | 7321 | ///   Looks up a localized string similar to No expenses for this month.. |
| Notifications | Resources\Shared.Designer.cs | 7393 | ///   Looks up a localized string similar to No notifications.. |
| AuditLog | Resources\Shared.Designer.cs | 7402 | ///   Looks up a localized string similar to No old auditLog found in the building. |
| Polls | Resources\Shared.Designer.cs | 7411 | ///   Looks up a localized string similar to No polls yet.. |
| Revenues | Resources\Shared.Designer.cs | 7438 | ///   Looks up a localized string similar to No revenues for this month.. |
| Notifications | Resources\Shared.Designer.cs | 7528 | ///   Looks up a localized string similar to — Notifications. |
| Notifications | Resources\Shared.Designer.cs | 7530 | internal static string Notifications { |
| Notifications | Resources\Shared.Designer.cs | 7532 | return ResourceManager.GetString("Notifications", resourceCulture); |
| Notifications | Resources\Shared.Designer.cs | 7537 | ///   Looks up a localized string similar to 🔔 Notifications. |
| Notifications | Resources\Shared.Designer.cs | 7546 | ///   Looks up a localized string similar to Notifications. |
| Notifications | Resources\Shared.Designer.cs | 7555 | ///   Looks up a localized string similar to 🔔 Notifications ({0}). |
| Buildings | Resources\Shared.Designer.cs | 7600 | ///   Looks up a localized string similar to Number of buildings. |
| Notifications | Resources\Shared.Designer.cs | 7690 | ///   Looks up a localized string similar to On the &lt;code&gt;/Notifications&lt;/code&gt; page, you will find all notifications addressed to you:. |
| Polls | Resources\Shared.Designer.cs | 7699 | ///   Looks up a localized string similar to On the &lt;code&gt;/ResidentPolls&lt;/code&gt; page, you will find the active polls. Each apartment has one vote per poll.. |
| Apartments | Resources\Shared.Designer.cs | 7771 | ///   Looks up a localized string similar to &lt;strong&gt;Open and disabled apartments only&lt;/strong&gt; — closed ones are excluded. |
| Apartments | Resources\Shared.Designer.cs | 8077 | ///   Looks up a localized string similar to Payments linked to apartments that don&apos;t exist. |
| Polls | Resources\Shared.Designer.cs | 8266 | ///   Looks up a localized string similar to — Polls. |
| Polls | Resources\Shared.Designer.cs | 8268 | internal static string Polls { |
| Polls | Resources\Shared.Designer.cs | 8270 | return ResourceManager.GetString("Polls", resourceCulture); |
| Polls | Resources\Shared.Designer.cs | 8275 | ///   Looks up a localized string similar to 🗳️ Polls. |
| Polls | Resources\Shared.Designer.cs | 8284 | ///   Looks up a localized string similar to Polls. |
| Polls | Resources\Shared.Designer.cs | 8293 | ///   Looks up a localized string similar to 🗳️ Polls ({0}). |
| Polls | Resources\Shared.Designer.cs | 8302 | ///   Looks up a localized string similar to 🗳️ Polls of {0} ({1}). |
| Presence | Resources\Shared.Designer.cs | 8338 | ///   Looks up a localized string similar to Presence. |
| Presence | Resources\Shared.Designer.cs | 8340 | internal static string Presence { |
| Presence | Resources\Shared.Designer.cs | 8342 | return ResourceManager.GetString("Presence", resourceCulture); |
| Apartments | Resources\Shared.Designer.cs | 8644 | ///   Looks up a localized string similar to Recreate Firebase accounts for all apartments?. |
| Residents | Resources\Shared.Designer.cs | 8833 | ///   Looks up a localized string similar to Residents. |
| Residents | Resources\Shared.Designer.cs | 8835 | internal static string Residents { |
| Residents | Resources\Shared.Designer.cs | 8837 | return ResourceManager.GetString("Residents", resourceCulture); |
| Apartments | Resources\Shared.Designer.cs | 8842 | ///   Looks up a localized string similar to 🏠 Residents ({0} apartments in {1} buildings). |
| Buildings | Resources\Shared.Designer.cs | 8842 | ///   Looks up a localized string similar to 🏠 Residents ({0} apartments in {1} buildings). |
| Residents | Resources\Shared.Designer.cs | 8842 | ///   Looks up a localized string similar to 🏠 Residents ({0} apartments in {1} buildings). |
| Revenues | Resources\Shared.Designer.cs | 8977 | ///   Looks up a localized string similar to Revenues. |
| Revenues | Resources\Shared.Designer.cs | 8979 | internal static string Revenues { |
| Revenues | Resources\Shared.Designer.cs | 8981 | return ResourceManager.GetString("Revenues", resourceCulture); |
| Revenues | Resources\Shared.Designer.cs | 8986 | ///   Looks up a localized string similar to Revenues. |
| Revenues | Resources\Shared.Designer.cs | 8995 | ///   Looks up a localized string similar to 📈 Revenues of {0}. |
| Apartments | Resources\Shared.Designer.cs | 9301 | ///   Looks up a localized string similar to Secret access (impersonation) to buildings and apartments. |
| Buildings | Resources\Shared.Designer.cs | 9301 | ///   Looks up a localized string similar to Secret access (impersonation) to buildings and apartments. |
| Notifications | Resources\Shared.Designer.cs | 9427 | ///   Looks up a localized string similar to Send in-app notifications to admins and residents. |
| Residents | Resources\Shared.Designer.cs | 9427 | ///   Looks up a localized string similar to Send in-app notifications to admins and residents. |
| Notifications | Resources\Shared.Designer.cs | 9445 | ///   Looks up a localized string similar to Send notifications. |
| Notifications | Resources\Shared.Designer.cs | 9499 | ///   Looks up a localized string similar to 🛎️ &lt;strong&gt;Services&lt;/strong&gt; — polls, maintenance, notifications, WhatsApp. |
| Polls | Resources\Shared.Designer.cs | 9499 | ///   Looks up a localized string similar to 🛎️ &lt;strong&gt;Services&lt;/strong&gt; — polls, maintenance, notifications, WhatsApp. |
| Expenses | Resources\Shared.Designer.cs | 9544 | ///   Looks up a localized string similar to Share of expenses. |
| Revenues | Resources\Shared.Designer.cs | 9553 | ///   Looks up a localized string similar to Share of revenues. |
| Apartments | Resources\Shared.Designer.cs | 9904 | ///   Looks up a localized string similar to Sync Firebase passwords for all apartments?. |
| Residents | Resources\Shared.Designer.cs | 9931 | ///   Looks up a localized string similar to 🏠 Sync residents. |
| Buildings | Resources\Shared.Designer.cs | 10165 | ///   Looks up a localized string similar to The &lt;code&gt;/Buildings&lt;/code&gt; page gives you:. |
| Notifications | Resources\Shared.Designer.cs | 10318 | ///   Looks up a localized string similar to The name shown on invoices and notifications. |
| Residents | Resources\Shared.Designer.cs | 10408 | ///   Looks up a localized string similar to The percentage of residents required for a vote to be valid. |
| Apartments | Resources\Shared.Designer.cs | 10579 | ///   Looks up a localized string similar to There are no apartments in the file. Fill in the data starting from row 6.. |
| Expenses | Resources\Shared.Designer.cs | 10813 | ///   Looks up a localized string similar to Total expenses. |
| Revenues | Resources\Shared.Designer.cs | 10831 | ///   Looks up a localized string similar to Total revenues. |
| Users | Resources\Shared.Designer.cs | 11083 | ///   Looks up a localized string similar to Users. |
| Users | Resources\Shared.Designer.cs | 11085 | internal static string Users { |
| Users | Resources\Shared.Designer.cs | 11087 | return ResourceManager.GetString("Users", resourceCulture); |
| Users | Resources\Shared.Designer.cs | 11092 | ///   Looks up a localized string similar to &lt;strong&gt;users.json&lt;/strong&gt; — all admins. |
| Users | Resources\Shared.Designer.cs | 11101 | ///   Looks up a localized string similar to Users with the same mobile number. |
| Buildings | Resources\Shared.Designer.cs | 11209 | ///   Looks up a localized string similar to View all records across all buildings (Super Admin). |
| Deposits | Resources\Shared.Designer.cs | 11398 | ///   Looks up a localized string similar to Wallet summary (balance, deposits, share of expenses and revenues). |
| Expenses | Resources\Shared.Designer.cs | 11398 | ///   Looks up a localized string similar to Wallet summary (balance, deposits, share of expenses and revenues). |
| Revenues | Resources\Shared.Designer.cs | 11398 | ///   Looks up a localized string similar to Wallet summary (balance, deposits, share of expenses and revenues). |
| Expenses | Resources\Shared.Designer.cs | 11641 | ///   Looks up a localized string similar to 💡 When you open the apartment, it will be automatically added to the monthly expenses.. |
| Expenses | Resources\Shared.Designer.cs | 11857 | ///   Looks up a localized string similar to - Your share of expenses. |
| Revenues | Resources\Shared.Designer.cs | 11866 | ///   Looks up a localized string similar to + Your share of revenues. |
| Revenues | Resources\Shared.Designer.cs | 11875 | ///   Looks up a localized string similar to Your share of the building&apos;s revenues. |
| Expenses | Resources\Shared.Designer.cs | 11884 | ///   Looks up a localized string similar to Your share of this month&apos;s expenses. |
| __EFMigrationsHistory | scripts\Apply-Phase08-To-RestoreTest.ps1 | 70 | $history = Invoke-Table $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;" |
| __EFMigrationsHistory | scripts\Apply-Phase08-To-RestoreTest.ps1 | 118 | $historyBefore = Invoke-Table $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;" |
| __EFMigrationsHistory | scripts\Apply-Phase09-To-RestoreTest.ps1 | 43 | $hist=Invoke-Sql $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;" |
| __EFMigrationsHistory | scripts\Apply-Phase09-To-RestoreTest.ps1 | 60 | $postHist=Invoke-Sql $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;" |
| Deposits | scripts\Generate-Phase08-DepositResolutionMigration.ps1 | 94 | "The model now requires ResolutionReason to be NULL for Pending and Confirmed deposits, and non-empty for Cancelled and Reversed deposits. The generated migration replaces only CK_PaymentDeposits_Resoâ€¦ |
| __EFMigrationsHistory | scripts\Generate-Phase08-DepositResolutionSql.ps1 | 31 | $hist=Invoke-Phase08DepositSqlQuery -DatabaseName $Database -SqlText "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;" |
| __EFMigrationsHistory | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 16 | foreach($t in @('Notifications','NotificationMessages','NotificationRecipients','NotificationReadStates','LegacyDataMigrationMap','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'Uâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 16 | foreach($t in @('Notifications','NotificationMessages','NotificationRecipients','NotificationReadStates','LegacyDataMigrationMap','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'Uâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 17 | $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 19 | $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.Notifications").Rows[0].N |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 20 | $old=[int](Query $c "SELECT COUNT(*) N FROM dbo.NotificationMessages WHERE Origin='legacy.notifications.v1'").Rows[0].N |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 21 | $pending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Notifications s WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationMessages t WHERE t.LegacyNotificationId=CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 29 | ;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.v1\|',BuildingId,N'\|',Id)),1,16)) Mid,CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(Builâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 31 | SELECT Mid,BuildingId,LKey,Title,Body,0,N'legacy.notifications.v1',Ts,NULL FROM S |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 33 | ;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.recipient.v1\|',BuildingId,N'\|',Id)),1,16)) Rid,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 37 | ;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.recipient.v1\|',BuildingId,N'\|',Id)),1,16)) Rid,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 41 | ;WITH S AS (SELECT n.*,CONCAT(BuildingId,N'/',Id) SourceKey,CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(BuildingId,N'\|',Id)),2) LKey,HASHBYTES('SHA2_256',CONCAT(BuildingId,N'\|',Id,N'\|',CONVERT(â€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 43 | SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.map.v1\|',BuildingId,N'\|',Id)),1,16)),N'BuildingManagement.Legacy',N'Notifications',SourceKey,N'Notificatiâ€¦ |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 44 | FROM S WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'Notifications' AND x.SourceKey=S.SourceKey); |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 48 | $after=[int](Query $c "SELECT COUNT(*) N FROM dbo.NotificationMessages WHERE Origin='legacy.notifications.v1'").Rows[0].N |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 49 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Notifications'").Rows[0].N |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 58 | $lines=@("# Phase 10 - Notification Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.Notifications","","## Results") |
| Notifications | scripts\Invoke-Phase10-NotificationBackfill.ps1 | 65 | try{git add -- scripts/Invoke-Phase10-NotificationBackfill.ps1 $report;if((git status --short -- scripts/Invoke-Phase10-NotificationBackfill.ps1 $report).Count -gt 0){git commit --only -m "feat: backfâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 25 | foreach($t in @('MaintenanceRecords','MaintenanceRequests','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 25 | foreach($t in @('MaintenanceRecords','MaintenanceRequests','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 29 | $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 31 | $tooLong=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRecords WHERE LEN(Title)>200").Rows[0].N |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 33 | $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRecords").Rows[0].N |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 37 | FROM dbo.MaintenanceRecords s |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 55 | FROM dbo.MaintenanceRecords m |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 73 | FROM dbo.MaintenanceRecords m |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 81 | N'BuildingManagement.Legacy',N'MaintenanceRecords',s.SourceKey, |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 92 | AND x.SourceTable=N'MaintenanceRecords' AND x.SourceKey=s.SourceKey |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 100 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'MaintenanceRecords'").Rows[0].N |
| MaintenanceRecords | scripts\Invoke-Phase11-MaintenanceBackfill.ps1 | 109 | $lines=@("# Phase 11 - Maintenance Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.MaintenanceRecords","","## Results") |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 10 | $report=Join-Path $dir ("Phase12-AuditLog-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md") |
| __EFMigrationsHistory | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 23 | foreach($t in @('AuditLog','AuditEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 23 | foreach($t in @('AuditLog','AuditEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 27 | $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 29 | $badActor=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog WHERE LEN(Actor)>160").Rows[0].N |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 30 | $badLabel=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog WHERE LEN(Label)>200").Rows[0].N |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 32 | $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog").Rows[0].N |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 33 | $existing=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditEvents e INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),e.AuditEventId) WHERE x.SourceSystem=N'BuildingManagementâ€¦ |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 35 | SELECT COUNT(*) N FROM dbo.AuditLog s |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 37 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog' |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 52 | FROM dbo.AuditLog a |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 69 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog' |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 76 | FROM dbo.AuditLog a |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 83 | N'BuildingManagement.Legacy',N'AuditLog',s.SourceKey,N'AuditEvents', |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 86 | N';MonthKey=',s.MonthKey,N';Details retained in source AuditLog.'),500) |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 89 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog' |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 95 | $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditEvents e INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),e.AuditEventId) WHERE x.SourceSystem=N'BuildingManagement.Lâ€¦ |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 96 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'AuditLog'").Rows[0].N |
| AuditLog | scripts\Invoke-Phase12-AuditLogBackfill.ps1 | 105 | $lines=@("# Phase 12 - AuditLog Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.AuditLog","","## Results") |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 10 | $report=Join-Path $dir ("Phase13-BackupHistory-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md") |
| __EFMigrationsHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 23 | foreach($t in @('BackupHistory','BackupJobs','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 23 | foreach($t in @('BackupHistory','BackupJobs','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 27 | $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 29 | $badSize=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory WHERE FileSize<0").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 30 | $badDuration=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory WHERE DurationSeconds<0").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 32 | $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 33 | $existing=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'BackupHistory'").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 35 | SELECT COUNT(*) N FROM dbo.BackupHistory s |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 37 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory' |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 51 | FROM dbo.BackupHistory b |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 58 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory' AND x.SourceKey=s.Id |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 64 | FROM dbo.BackupHistory b |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 68 | N'BuildingManagement.Legacy',N'BackupHistory',s.Id,N'BackupJobs',CONVERT(nvarchar(36),s.JobId),s.Fingerprint,1,N'MigratedWithLegacyMetadata', |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 74 | WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory' AND x.SourceKey=s.Id); |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 79 | $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupJobs j INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),j.BackupJobId) WHERE x.SourceSystem=N'BuildingManagement.Legâ€¦ |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 80 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'BackupHistory'").Rows[0].N |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 89 | $lines=@("# Phase 13 - BackupHistory Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.BackupHistory","","## Results") |
| BackupHistory | scripts\Invoke-Phase13-BackupHistoryBackfill.ps1 | 95 | "- No BackupArtifacts are fabricated: file existence, storage location, and SHA-256 content hash are not verifiable from BackupHistory metadata.", |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 11 | $report = Join-Path $reportDir ("Phase14-ScheduledBackupSettings-Backfill-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md") |
| __EFMigrationsHistory | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 30 | foreach ($tableName in @('ScheduledBackupSettings', 'BackupScheduleConfigurations', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) { |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 30 | foreach ($tableName in @('ScheduledBackupSettings', 'BackupScheduleConfigurations', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) { |
| __EFMigrationsHistory | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 35 | $migrationCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261119140000_Phase14_BackupScheduleConfiguration'").Rows[0].N |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 40 | FROM dbo.ScheduledBackupSettings |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 53 | if ($invalid -gt 0) { throw "Preflight blocked: $invalid ScheduledBackupSettings row(s) contain invalid or unsupported values. No normalization or truncation is performed." } |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 55 | $sourceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.ScheduledBackupSettings").Rows[0].N |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 56 | $mappedCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'ScheduledBackupSettings'").Rows[0].N |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 59 | FROM dbo.ScheduledBackupSettings s |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 63 | AND m.SourceTable=N'ScheduledBackupSettings' |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 91 | FROM dbo.ScheduledBackupSettings s |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 104 | AND m.SourceTable=N'ScheduledBackupSettings' |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 117 | FROM dbo.ScheduledBackupSettings s |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 124 | N'BuildingManagement.Legacy', N'ScheduledBackupSettings', s.Id, |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 132 | AND m.SourceTable=N'ScheduledBackupSettings' |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 154 | AND m.SourceTable=N'ScheduledBackupSettings'; |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 156 | $provenanceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'ScheduledBackupSettings'").Rows[0].N |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 173 | '# Phase 14 - ScheduledBackupSettings Backfill', |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 177 | '- Source rows retained: dbo.ScheduledBackupSettings', |
| ScheduledBackupSettings | scripts\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 | 189 | '- The source ScheduledBackupSettings row is never updated or deleted.' |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 11 | $report = Join-Path $reportDir ("Phase15-Presence-Backfill-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md") |
| __EFMigrationsHistory | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 30 | foreach ($tableName in @('Presence', 'PresenceSessions', 'PresenceHeartbeats', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) { |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 30 | foreach ($tableName in @('Presence', 'PresenceSessions', 'PresenceHeartbeats', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) { |
| __EFMigrationsHistory | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 34 | $migrationCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119150000_Phase15_PresenceLegacyObservation'").Rows[0].N |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 39 | FROM dbo.Presence |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 45 | if ($invalid -gt 0) { throw "Preflight blocked: $invalid Presence row(s) have invalid keys, scope lengths, or missing timestamps." } |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 47 | $sourceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.Presence").Rows[0].N |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 48 | $mappedCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Presence'").Rows[0].N |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 50 | SELECT COUNT(*) AS N FROM dbo.Presence s |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 52 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid); |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 68 | CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.session.v1\|',p.Uid))),1,16)) AS SessionId, |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 70 | FROM dbo.Presence p |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 79 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 84 | CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.session.v1\|',p.Uid))),1,16)) AS SessionId, |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 86 | FROM dbo.Presence p |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 90 | SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.map.v1\|',s.Uid))),1,16)), |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 91 | N'BuildingManagement.Legacy',N'Presence',s.Uid,N'PresenceSessions',CONVERT(nvarchar(36),s.SessionId), |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 97 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 111 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence'; |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 113 | $provenanceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Presence'").Rows[0].N |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 118 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence'; |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 135 | '# Phase 15 - Presence Backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode", |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 136 | '- Source rows retained: dbo.Presence','','## Results' |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 145 | '- The source Presence table is never updated or deleted.' |
| Presence | scripts\Invoke-Phase15-PresenceBackfill.ps1 | 155 | git commit --only -m "feat: backfill legacy presence observations" -- $scriptPath $report |
| __EFMigrationsHistory | scripts\Invoke-Phase16-QRBackfill.ps1 | 19 | foreach($t in @('QrTokens','QrUsages','QrCapabilities','QrCapabilityUses','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 19 | foreach($t in @('QrTokens','QrUsages','QrCapabilities','QrCapabilityUses','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 19 | foreach($t in @('QrTokens','QrUsages','QrCapabilities','QrCapabilityUses','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase16-QRBackfill.ps1 | 23 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 26 | SELECT COUNT(*) N FROM dbo.QrTokens |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 34 | $badUsage=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrUsages WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>128 OR TokenId IS NULL OR LEN(TokenId)>128 OR UseCount<0").Rows[0].N |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 36 | $tokens=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrTokens").Rows[0].N |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 37 | $usage=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrUsages").Rows[0].N |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 39 | SELECT COUNT(*) N FROM dbo.QrTokens s WHERE NOT EXISTS |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 40 | (SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid); |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 54 | FROM dbo.QrTokens q |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 62 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid); |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 68 | FROM dbo.QrTokens q |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 72 | N'BuildingManagement.Legacy',N'QrTokens',s.Uid,N'QrCapabilities',CONVERT(nvarchar(36),s.CapabilityId),s.Fingerprint,1,N'MigratedWithLegacyMetadata', |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 75 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid); |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 80 | FROM dbo.QrUsages u |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 84 | N'BuildingManagement.Legacy',N'QrUsages',s.Id,N'QrCapabilityUses',NULL,s.Fingerprint,1,N'PreservedAsAggregateEvidence', |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 87 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrUsages' AND m.SourceKey=s.Id); |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 93 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens'; |
| QrTokens | scripts\Invoke-Phase16-QRBackfill.ps1 | 95 | $tokenMap=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'QrTokens'").Rows[0].N |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 96 | $usageMap=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'QrUsages'").Rows[0].N |
| QrUsages | scripts\Invoke-Phase16-QRBackfill.ps1 | 111 | '- QrUsages are aggregate counters, not event logs. They are retained as provenance evidence and never expanded into fabricated individual uses.', |
| __EFMigrationsHistory | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 19 | foreach($t in @('Polls','PollOptions','PollVotes','PollLifecycleEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 19 | foreach($t in @('Polls','PollOptions','PollVotes','PollLifecycleEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 19 | foreach($t in @('Polls','PollOptions','PollVotes','PollLifecycleEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 19 | foreach($t in @('Polls','PollOptions','PollVotes','PollLifecycleEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 23 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119170000_Phase17_LegacyPollEvidence'").Rows[0].N |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 26 | SELECT COUNT(*) N FROM dbo.Polls |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 32 | SELECT COUNT(*) N FROM dbo.PollOptions o |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 33 | WHERE NOT EXISTS(SELECT 1 FROM dbo.Polls p WHERE p.BuildingId=o.BuildingId AND p.Id=o.PollId); |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 36 | SELECT COUNT(*) N FROM dbo.PollVotes v |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 37 | WHERE NOT EXISTS(SELECT 1 FROM dbo.Polls p WHERE p.BuildingId=v.BuildingId AND p.Id=v.PollId) |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 38 | OR NOT EXISTS(SELECT 1 FROM dbo.PollOptions o WHERE o.BuildingId=v.BuildingId AND o.PollId=v.PollId AND o.Id=v.OptionId); |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 41 | $polls=[int](Query $c "SELECT COUNT(*) N FROM dbo.Polls").Rows[0].N |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 42 | $options=[int](Query $c "SELECT COUNT(*) N FROM dbo.PollOptions").Rows[0].N |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 43 | $votes=[int](Query $c "SELECT COUNT(*) N FROM dbo.PollVotes").Rows[0].N |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 45 | SELECT COUNT(*) N FROM dbo.Polls p WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=p.BuildingIdâ€¦ |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 47 | Log "Database: $db";Log "Source polls: $polls";Log "Source options: $options";Log "Source apartment votes: $votes";Log "Pending poll lifecycle records: $pendingPolls" |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 58 | FROM dbo.Polls p |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 64 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=s.BuildingId+N'\|'+s.Id); |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 70 | FROM dbo.Polls p |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 74 | N'BuildingManagement.Legacy',N'Polls',s.BuildingId+N'\|'+s.Id,N'PollLifecycleEvents',CONVERT(nvarchar(36),s.EventId),s.Fingerprint,1,N'MigratedWithLegacyMetadata', |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 77 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=s.BuildingId+N'\|'+s.Id); |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 81 | FROM dbo.PollOptions o |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 85 | N'BuildingManagement.Legacy',N'PollOptions',s.BuildingId+N'\|'+s.PollId+N'\|'+s.Id,N'LegacyDataMigrationMap',NULL,s.Fingerprint,1,N'PreservedAsLegacyOptionEvidence', |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 88 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'PollOptions' AND m.SourceKey=s.BuildingId+N'\|'+s.PollId+N'\|'+s.Id); |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 92 | FROM dbo.PollVotes v |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 96 | N'BuildingManagement.Legacy',N'PollVotes',s.BuildingId+N'\|'+s.PollId+N'\|'+s.ApartmentId,N'LegacyDataMigrationMap',NULL,s.Fingerprint,1,N'PreservedAsApartmentVoteEvidence', |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 99 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'PollVotes' AND m.SourceKey=s.BuildingId+N'\|'+s.PollId+N'\|'+s.Apartmeâ€¦ |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 105 | WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls'; |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 107 | $pollMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Polls'").Rows[0].N |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 108 | $optionMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'PollOptions'").Rows[0].N |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 109 | $voteMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'PollVotes'").Rows[0].N |
| Polls | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 111 | if($events -ne $polls -or $pollMaps -ne $polls -or $optionMaps -ne $options -or $voteMaps -ne $votes){throw "Reconciliation failed: polls=$polls events=$events pollMap=$pollMaps options=$options optioâ€¦ |
| PollOptions | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 122 | '- PollOptions and PollVotes are retained as fingerprinted legacy evidence in LegacyDataMigrationMap; source rows are not changed.', |
| PollVotes | scripts\Invoke-Phase17-PollEvidenceBackfill.ps1 | 122 | '- PollOptions and PollVotes are retained as fingerprinted legacy evidence in LegacyDataMigrationMap; source rows are not changed.', |
| __EFMigrationsHistory | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 19 | foreach($t in @('Dvrs','Cameras','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 19 | foreach($t in @('Dvrs','Cameras','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 19 | foreach($t in @('Dvrs','Cameras','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 23 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119180000_Phase18_MediaInventoryEvidence'").Rows[0].N |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 27 | (SELECT COUNT(*) FROM dbo.Dvrs WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>256 OR LEN(BuildingId)>64 OR LEN(Name)>500 OR LEN(IpAddress)>500 OR Port<1 OR Port>65535) |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 28 | +(SELECT COUNT(*) FROM dbo.Cameras WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>256 OR LEN(DvrId)>256 OR LEN(BuildingId)>64 OR LEN(Name)>500 OR Channel<0) N; |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 31 | $dvr=[int](Query $c "SELECT COUNT(*) N FROM dbo.Dvrs").Rows[0].N |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 32 | $cam=[int](Query $c "SELECT COUNT(*) N FROM dbo.Cameras").Rows[0].N |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 33 | $dvrPending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Dvrs s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Dvrs' â€¦ |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 34 | $camPending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Cameras s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Camâ€¦ |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 45 | FROM dbo.Dvrs d |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 49 | N'BuildingManagement.Legacy',N'Dvrs',d.Id,d.BuildingId,d.Payload, |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 52 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Dvrs' AND t.SourceKey=d.Id); |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 57 | FROM dbo.Cameras c |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 61 | N'BuildingManagement.Legacy',N'Cameras',c.Id,c.BuildingId,c.Payload, |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 64 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Cameras' AND t.SourceKey=c.Id); |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 69 | CASE WHEN t.SourceTable=N'Dvrs' THEN N'Encrypted password and username intentionally excluded; credential rotation and vault re-provisioning required.' |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 72 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Dvrs',N'Cameras') |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 72 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Dvrs',N'Cameras') |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 77 | $dvrTarget=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Dvrs'").Rows[0].N |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 78 | $camTarget=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Cameras'").Rows[0].N |
| Cameras | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 79 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable IN(N'Dvrs',N'Cameras')").Rows[0].N |
| Dvrs | scripts\Invoke-Phase18-MediaInventoryBackfill.ps1 | 79 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable IN(N'Dvrs',N'Cameras')").Rows[0].N |
| __EFMigrationsHistory | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 19 | foreach($t in @('UserPermissions','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 19 | foreach($t in @('UserPermissions','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 23 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119190000_Phase19_UserPermissionEvidence'").Rows[0].N |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 25 | $bad=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128 OR Permission IS NULL OR LEN(Permission)=0 OR LEN(Permission)>256 OR LEN(Uid)+1+LEN(Permâ€¦ |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 27 | $source=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions").Rows[0].N |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 28 | $pending=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=â€¦ |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 39 | FROM dbo.UserPermissions p |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 43 | N'BuildingManagement.Legacy',N'UserPermissions',s.SourceKey,NULL,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload)) |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 45 | WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'UserPermissions' AND t.SourceKey=s.SourceKey); |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 52 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'UserPermissions' |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 57 | $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'UserPermissions'").Rows[0].N |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 58 | $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'UserPermissions'").Rows[0].N |
| UserPermissions | scripts\Invoke-Phase19-UserPermissionEvidenceBackfill.ps1 | 70 | '- UserPermissions are account/user-scoped legacy permission entries; they are not role-level RolePermissions.', |
| __EFMigrationsHistory | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 19 | foreach($t in @('Users','Residents','BuildingAdmins','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 19 | foreach($t in @('Users','Residents','BuildingAdmins','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 19 | foreach($t in @('Users','Residents','BuildingAdmins','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 19 | foreach($t in @('Users','Residents','BuildingAdmins','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 23 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119200000_Phase20_IdentityProfileEvidence'").Rows[0].N |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 25 | $invalidUsers=[int](Query $c "SELECT COUNT(*) N FROM dbo.Users WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128").Rows[0].N |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 26 | $invalidResidents=[int](Query $c "SELECT COUNT(*) N FROM dbo.Residents WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>128 OR BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64 OR ApartmentId IS Nâ€¦ |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 27 | $invalidAdmins=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingAdmins WHERE BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64 OR AdminUid IS NULL OR LEN(AdminUid)=0 OR LEN(AdminUid)>128â€¦ |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 28 | if($invalidUsers -or $invalidResidents -or $invalidAdmins){throw "Preflight blocked: invalid Users=$invalidUsers Residents=$invalidResidents BuildingAdmins=$invalidAdmins."} |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 28 | if($invalidUsers -or $invalidResidents -or $invalidAdmins){throw "Preflight blocked: invalid Users=$invalidUsers Residents=$invalidResidents BuildingAdmins=$invalidAdmins."} |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 28 | if($invalidUsers -or $invalidResidents -or $invalidAdmins){throw "Preflight blocked: invalid Users=$invalidUsers Residents=$invalidResidents BuildingAdmins=$invalidAdmins."} |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 30 | foreach($t in @('Users','Residents','BuildingAdmins')){$counts[$t]=[int](Query $c "SELECT COUNT(*) N FROM dbo.$t").Rows[0].N} |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 30 | foreach($t in @('Users','Residents','BuildingAdmins')){$counts[$t]=[int](Query $c "SELECT COUNT(*) N FROM dbo.$t").Rows[0].N} |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 30 | foreach($t in @('Users','Residents','BuildingAdmins')){$counts[$t]=[int](Query $c "SELECT COUNT(*) N FROM dbo.$t").Rows[0].N} |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 32 | foreach($t in @('Users','Residents','BuildingAdmins')){Log "$t source rows: $($counts[$t])"} |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 32 | foreach($t in @('Users','Residents','BuildingAdmins')){Log "$t source rows: $($counts[$t])"} |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 32 | foreach($t in @('Users','Residents','BuildingAdmins')){Log "$t source rows: $($counts[$t])"} |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 44 | FROM dbo.Users u |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 48 | N'BuildingManagement.Legacy',N'Users',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload)) |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 49 | FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Users' AND t.SourceKey=s.SourceKey); |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 56 | FROM dbo.Residents r |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 60 | N'BuildingManagement.Legacy',N'Residents',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload)) |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 61 | FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Residents' AND t.SourceKey=s.SourceKey); |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 66 | FROM dbo.BuildingAdmins a |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 70 | N'BuildingManagement.Legacy',N'BuildingAdmins',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload)) |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 71 | FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'BuildingAdmins' AND t.SourceKey=s.SourceKey); |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 77 | WHEN N'Users' THEN N'Profile and account state retained; PIN, MFA secrets and recovery codes excluded. No Person or UserAccount linkage inferred.' |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 78 | WHEN N'Residents' THEN N'Resident profile and apartment scope retained; PIN/password hashes excluded. IsOwner and IsPrimary are not converted to ownership or occupancy history.' |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 82 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Users',N'Residents',N'BuildingAdmins') |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 82 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Users',N'Residents',N'BuildingAdmins') |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 82 | WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Users',N'Residents',N'BuildingAdmins') |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 88 | foreach($t in @('Users','Residents','BuildingAdmins')){ |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 88 | foreach($t in @('Users','Residents','BuildingAdmins')){ |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 88 | foreach($t in @('Users','Residents','BuildingAdmins')){ |
| BuildingAdmins | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 106 | '- Users, Residents, and BuildingAdmins are preserved as unresolved legacy evidence; this phase does not create Persons, UserAccounts, credentials, or role assignments.', |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 106 | '- Users, Residents, and BuildingAdmins are preserved as unresolved legacy evidence; this phase does not create Persons, UserAccounts, credentials, or role assignments.', |
| Users | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 106 | '- Users, Residents, and BuildingAdmins are preserved as unresolved legacy evidence; this phase does not create Persons, UserAccounts, credentials, or role assignments.', |
| Residents | scripts\Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1 | 108 | '- Residents.IsOwner and IsPrimary remain source facts; no legal ownership shares or effective-dated occupancy periods are fabricated.', |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 4 | $report=Join-Path $dir ("Phase21-GlobalSettings-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md");$log=[System.Collections.Generic.List[string]]::new() |
| __EFMigrationsHistory | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 10 | foreach($t in @('GlobalSettings','GlobalApplicationSettings','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "Missing â€¦ |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 10 | foreach($t in @('GlobalSettings','GlobalApplicationSettings','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "Missing â€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 11 | $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119210000_Phase21_GlobalApplicationSettings'").Rows[0].N;if($m -ne 1){throw 'Phase21 migration not applied'} |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 12 | $source=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings").Rows[0].N;if($source -ne 1){throw "Expected exactly one GlobalSettings row, found $source"} |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 13 | $bad=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings WHERE Id IS NULL OR LEN(Id)>64 OR LEN(AppName)>200 OR LEN(DefaultLanguage)>16 OR SessionTimeoutDays NOT BETWEEN 1 AND 365 OR MaxPinAttempâ€¦ |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 23 | FROM dbo.GlobalSettings s |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 29 | $match=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings s JOIN dbo.GlobalApplicationSettings t ON t.SettingsId=s.Id AND t.AppName=s.AppName AND t.DefaultLanguage=s.DefaultLanguage AND t.Enablâ€¦ |
| GlobalSettings | scripts\Invoke-Phase21-GlobalSettingsBackfill.ps1 | 37 | $lines+=@('','## Secret handling','- WhatsApp API token and Cloudinary API key are not copied.','- Provider credentials require explicit vault encryption/key-version handling and rotation; no plaintexâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 10 | foreach($t in @('Buildings','BuildingSettings','FinancialCategories','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "â€¦ |
| Buildings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 10 | foreach($t in @('Buildings','BuildingSettings','FinancialCategories','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "â€¦ |
| BuildingSettings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 10 | foreach($t in @('Buildings','BuildingSettings','FinancialCategories','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "â€¦ |
| FinancialCategories | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 10 | foreach($t in @('Buildings','BuildingSettings','FinancialCategories','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "â€¦ |
| Buildings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 11 | $badSettings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings s WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=s.BuildingId)").Rows[0].N |
| BuildingSettings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 11 | $badSettings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings s WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=s.BuildingId)").Rows[0].N |
| Buildings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 12 | $badCategories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories f WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=f.BuildingId)").Rows[0].N |
| FinancialCategories | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 12 | $badCategories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories f WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=f.BuildingId)").Rows[0].N |
| BuildingSettings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 13 | $settings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings").Rows[0].N;$categories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories").Rows[0].N |
| FinancialCategories | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 13 | $settings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings").Rows[0].N;$categories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories").Rows[0].N |
| BuildingSettings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 14 | Log "Database: $db";Log "Canonical BuildingSettings rows: $settings";Log "Canonical FinancialCategories rows: $categories";Log "Orphan BuildingSettings: $badSettings";Log "Orphan FinancialCategories: â€¦ |
| FinancialCategories | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 14 | Log "Database: $db";Log "Canonical BuildingSettings rows: $settings";Log "Canonical FinancialCategories rows: $categories";Log "Orphan BuildingSettings: $badSettings";Log "Orphan FinancialCategories: â€¦ |
| Buildings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 21 | $lines+=@('','## Mapping decision','- Buildings, BuildingSettings, and FinancialCategories remain the canonical shared tables; creating duplicate copies would split reference identity.','- This audit â€¦ |
| BuildingSettings | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 21 | $lines+=@('','## Mapping decision','- Buildings, BuildingSettings, and FinancialCategories remain the canonical shared tables; creating duplicate copies would split reference identity.','- This audit â€¦ |
| FinancialCategories | scripts\Invoke-Phase22-CanonicalReferenceAudit.ps1 | 21 | $lines+=@('','## Mapping decision','- Buildings, BuildingSettings, and FinancialCategories remain the canonical shared tables; creating duplicate copies would split reference identity.','- This audit â€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 16 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','LegacyOperationalRecords','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 16 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','LegacyOperationalRecords','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 16 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','LegacyOperationalRecords','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 20 | $users=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Users") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 21 | $residents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 22 | $overlap=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT DISTINCT u.Uid FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE u.Uid IS NOT NULL AND LEN(u.Uid)>0) q") |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 22 | $overlap=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT DISTINCT u.Uid FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE u.Uid IS NOT NULL AND LEN(u.Uid)>0) q") |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 23 | $duplicateUsers=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Users WHERE Uid IS NOT NULL AND LEN(Uid)>0 GROUP BY Uid HAVING COUNT(*)>1) q") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 24 | $duplicateResidents=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Residents WHERE Uid IS NOT NULL AND LEN(Uid)>0 GROUP BY Uid HAVING COUNT(*)>1) q") |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 25 | $userMaps=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Users'") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 26 | $residentMaps=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Residents'") |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 27 | $linkedUsers=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 28 | $linkedResidents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL") |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 32 | Log "Users source rows: $users; Residents source rows: $residents" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 32 | Log "Users source rows: $users; Residents source rows: $residents" |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 33 | Log "Exact Users/Residents UID overlap: $overlap" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 33 | Log "Exact Users/Residents UID overlap: $overlap" |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 34 | Log "Duplicate Users UIDs: $duplicateUsers; duplicate Residents UIDs: $duplicateResidents" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 34 | Log "Duplicate Users UIDs: $duplicateUsers; duplicate Residents UIDs: $duplicateResidents" |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 35 | Log "LegacyIdentityMap rows: Users=$userMaps; Residents=$residentMaps" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 35 | Log "LegacyIdentityMap rows: Users=$userMaps; Residents=$residentMaps" |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 36 | Log "Linked identity dispositions: Users=$linkedUsers; Residents=$linkedResidents" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 36 | Log "Linked identity dispositions: Users=$linkedUsers; Residents=$linkedResidents" |
| Residents | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 38 | Log "Unresolved Users map rows: $($userMaps-$linkedUsers); unresolved Residents map rows: $($residentMaps-$linkedResidents)" |
| Users | scripts\Invoke-Phase23-IdentityLinkageReadinessAudit.ps1 | 38 | Log "Unresolved Users map rows: $($userMaps-$linkedUsers); unresolved Residents map rows: $($residentMaps-$linkedResidents)" |
| __EFMigrationsHistory | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 16 | foreach($t in @('Residents','Apartments','Buildings','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Apartments | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 16 | foreach($t in @('Residents','Apartments','Buildings','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Buildings | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 16 | foreach($t in @('Residents','Apartments','Buildings','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 16 | foreach($t in @('Residents','Apartments','Buildings','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 20 | $residents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 21 | $owners=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsOwner=1") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 22 | $primary=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsPrimary=1") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 23 | $ownerWithNoUid=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsOwner=1 AND (Uid IS NULL OR LEN(Uid)=0)") |
| Apartments | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 24 | $badApartment=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents r WHERE NOT EXISTS(SELECT 1 FROM dbo.Apartments a WHERE a.BuildingId=r.BuildingId AND a.Id=r.ApartmentId)") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 24 | $badApartment=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents r WHERE NOT EXISTS(SELECT 1 FROM dbo.Apartments a WHERE a.BuildingId=r.BuildingId AND a.Id=r.ApartmentId)") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 25 | $multiplePrimary=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT BuildingId,ApartmentId FROM dbo.Residents WHERE IsPrimary=1 GROUP BY BuildingId,ApartmentId HAVING COUNT(*)>1) q") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 28 | $exists=Scalar $c "SELECT COL_LENGTH(N'dbo.Residents',N'$col')" |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 30 | $n=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE [$col] IS NOT NULL AND LEN(CONVERT(nvarchar(max),[$col]))>0") |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 35 | Log "Residents source rows: $residents" |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 36 | Log "Residents marked IsOwner: $owners; IsPrimary: $primary" |
| Apartments | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 39 | Log "Apartments with multiple IsPrimary residents: $multiplePrimary" |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 39 | Log "Apartments with multiple IsPrimary residents: $multiplePrimary" |
| Residents | scripts\Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1 | 43 | if($multiplePrimary -gt 0){Log 'PRIMARY OCCUPANCY REVIEW REQUIRED: multiple primary residents exist for one apartment.'} |
| __EFMigrationsHistory | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 16 | foreach($t in @('Residents','Users','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}} |
| Residents | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 16 | foreach($t in @('Residents','Users','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}} |
| Users | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 16 | foreach($t in @('Residents','Users','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}} |
| Residents | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 18 | SELECT N'Residents.PinHash' SourceColumn, |
| Residents | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 22 | FROM dbo.Residents |
| Residents | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 24 | SELECT N'Residents.PasswordHash', |
| Residents | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 28 | FROM dbo.Residents |
| Users | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 32 | $users=(Query $c "SELECT COUNT(*) N FROM dbo.Users").Rows[0].N |
| Users | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 33 | $hasPin=(Query $c "SELECT CASE WHEN COL_LENGTH(N'dbo.Users',N'Pin') IS NULL THEN 0 ELSE 1 END N").Rows[0].N |
| Users | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 34 | $hasMfa=(Query $c "SELECT CASE WHEN COL_LENGTH(N'dbo.Users',N'TwoFactorSecretEncrypted') IS NULL THEN 0 ELSE 1 END N").Rows[0].N |
| Users | scripts\Invoke-Phase25-CredentialFormatProfile.ps1 | 35 | Log "Users rows: $users; legacy Users.Pin column present: $hasPin; encrypted MFA column present: $hasMfa" |
| __EFMigrationsHistory | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 16 | foreach($t in @('BuildingAdmins','Users','Buildings','UserAccounts','LegacyIdentityMap','UserRoleAssignments','__EFMigrationsHistory')){ |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 16 | foreach($t in @('BuildingAdmins','Users','Buildings','UserAccounts','LegacyIdentityMap','UserRoleAssignments','__EFMigrationsHistory')){ |
| Buildings | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 16 | foreach($t in @('BuildingAdmins','Users','Buildings','UserAccounts','LegacyIdentityMap','UserRoleAssignments','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 16 | foreach($t in @('BuildingAdmins','Users','Buildings','UserAccounts','LegacyIdentityMap','UserRoleAssignments','__EFMigrationsHistory')){ |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 21 | $v=Scalar $c "SELECT COL_LENGTH(N'dbo.BuildingAdmins',N'$col')" |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 22 | if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.BuildingAdmins.$col"} |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 24 | $rows=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins") |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 25 | $distinctAdmins=[int](Scalar $c "SELECT COUNT(DISTINCT AdminUid) FROM dbo.BuildingAdmins WHERE AdminUid IS NOT NULL") |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 26 | $distinctBuildings=[int](Scalar $c "SELECT COUNT(DISTINCT BuildingId) FROM dbo.BuildingAdmins WHERE BuildingId IS NOT NULL") |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 27 | $unmatchedBuildings=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId))") |
| Buildings | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 27 | $unmatchedBuildings=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId))") |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 28 | $unmapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.Adminâ€¦ |
| Users | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 28 | $unmapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.Adminâ€¦ |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 29 | $mapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.AdminUid) Aâ€¦ |
| Users | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 29 | $mapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.AdminUid) Aâ€¦ |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 32 | Log "BuildingAdmins source rows: $rows; distinct AdminUid: $distinctAdmins; distinct BuildingId: $distinctBuildings" |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 38 | else{Log 'STRUCTURAL READINESS ONLY: explicit BuildingAdmins rows and account mappings exist; role semantics and active status still require review.'} |
| BuildingAdmins | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 45 | $lines+=@('','## Decision','- BuildingAdmins rows are the only source for building-scoped administrator scope.','- No system-wide role is inferred from a building admin row.','- No UserRoleAssignmentsâ€¦ |
| Users | scripts\Invoke-Phase26-BuildingAdminScopeReadiness.ps1 | 45 | $lines+=@('','## Decision','- BuildingAdmins rows are the only source for building-scoped administrator scope.','- No system-wide role is inferred from a building admin row.','- No UserRoleAssignmentsâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 16 | foreach($t in @('LegacyDataMigrationMap','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}} |
| Apartments | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| AuditLog | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| BackupHistory | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| BuildingAdmins | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Buildings | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| BuildingSettings | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Cameras | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| DepositEditHistory | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Deposits | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Dvrs | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Expenses | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| FinancialCategories | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Floors | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| GlobalSettings | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| LoginThrottle | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| MaintenanceRecords | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| MonthlyApartmentShares | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Notifications | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| PollOptions | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Polls | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| PollVotes | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Presence | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| QrTokens | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| QrUsages | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Residents | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Revenues | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| ScheduledBackupSettings | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| SyncPendingChanges | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| UserPermissions | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| Users | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| WalletAdjustments | scripts\Invoke-Phase27-FullLegacyReconciliationAudit.ps1 | 17 | $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions'â€¦ |
| DepositEditHistory | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Expenses | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| MonthlyApartmentShares | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Revenues | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| WalletAdjustments | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 15 | $sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| __EFMigrationsHistory | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 21 | foreach($t in @($sourceTables)+@($targetTables)+@('__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 25 | $migration=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261118213732_Phase08_FinancialLedgerFoundation'").Rows[0].N |
| DepositEditHistory | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Expenses | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| MonthlyApartmentShares | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Revenues | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| WalletAdjustments | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 72 | WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments') |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 79 | foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){ |
| Expenses | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 79 | foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){ |
| MonthlyApartmentShares | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 79 | foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){ |
| Revenues | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 79 | foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){ |
| WalletAdjustments | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 79 | foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){ |
| MonthlyApartmentShares | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 87 | $dt=Query $c "SELECT BuildingId,MonthKey,COUNT_BIG(*) AS Rows,SUM(CONVERT(decimal(38,4),ExpenseShare)) AS ExpenseShareTotal,SUM(CONVERT(decimal(38,4),RevenueShare)) AS RevenueShareTotal,SUM(CONVERT(deâ€¦ |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 92 | if(((Query $c "SELECT COUNT(*) N FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Deposits') AND name='Status'").Rows[0].N) -gt 0){ |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 93 | Log '### Deposits by status' |
| Deposits | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 94 | $dt=Query $c "SELECT CONVERT(nvarchar(80),Status) AS StatusValue,COUNT_BIG(*) AS Rows,SUM(CONVERT(decimal(38,4),Amount)) AS AmountTotal FROM dbo.Deposits GROUP BY Status ORDER BY Status" |
| DepositEditHistory | scripts\Invoke-Phase28-FinanceMigrationReadiness.ps1 | 104 | Log '- DepositEditHistory requires a separate immutable-history mapping; it must not be discarded or folded into current deposit state.' |
| __EFMigrationsHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 25 | foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) { |
| __EFMigrationsHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 30 | $n = [int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'$m'").Rows[0].N |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 43 | SELECT N'Deposits',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 47 | FROM dbo.Deposits d; |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 49 | SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId),BuildingId, |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 52 | FROM dbo.DepositEditHistory h; |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 54 | SELECT N'Expenses',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 57 | FROM dbo.Expenses e; |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 59 | SELECT N'Revenues',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 62 | FROM dbo.Revenues r; |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 64 | SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'\|',MonthKey,N'\|',ApartmentId),BuildingId, |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 67 | FROM dbo.MonthlyApartmentShares s; |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 69 | SELECT N'WalletAdjustments',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 72 | FROM dbo.WalletAdjustments w; |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 113 | SELECT N'Deposits',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 116 | FROM dbo.Deposits d; |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 118 | SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId),BuildingId, |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 121 | FROM dbo.DepositEditHistory h; |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 123 | SELECT N'Expenses',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 126 | FROM dbo.Expenses e; |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 128 | SELECT N'Revenues',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 131 | FROM dbo.Revenues r; |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 133 | SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'\|',MonthKey,N'\|',ApartmentId),BuildingId, |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 136 | FROM dbo.MonthlyApartmentShares s; |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 138 | SELECT N'WalletAdjustments',CONCAT(BuildingId,N'\|',Id),BuildingId, |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 141 | FROM dbo.WalletAdjustments w; |
| Deposits | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 166 | SELECT N'Deposits' SourceTable,CONCAT(BuildingId,N'\|',Id) SourceKey FROM dbo.Deposits |
| DepositEditHistory | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 167 | UNION ALL SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId) FROM dbo.DepositEditHistory |
| Expenses | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 168 | UNION ALL SELECT N'Expenses',CONCAT(BuildingId,N'\|',Id) FROM dbo.Expenses |
| Revenues | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 169 | UNION ALL SELECT N'Revenues',CONCAT(BuildingId,N'\|',Id) FROM dbo.Revenues |
| MonthlyApartmentShares | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 170 | UNION ALL SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'\|',MonthKey,N'\|',ApartmentId) FROM dbo.MonthlyApartmentShares |
| WalletAdjustments | scripts\Invoke-Phase29-FinanceEvidenceBackfill.ps1 | 171 | UNION ALL SELECT N'WalletAdjustments',CONCAT(BuildingId,N'\|',Id) FROM dbo.WalletAdjustments |
| __EFMigrationsHistory | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| Apartments | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| BuildingAdmins | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| Buildings | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 21 | foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 26 | Users=@('Uid','Role','IsDisabled','IsActive') |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 27 | Residents=@('Id','BuildingId','ApartmentId','Uid','IsActive','IsDisabled') |
| BuildingAdmins | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 28 | BuildingAdmins=@('AdminUid','BuildingId') |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 38 | FROM dbo.Users GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(Role)),N''),N'(blank)') |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 42 | SELECT r.BuildingId,COUNT(*) Residents, |
| Apartments | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 44 | COUNT(DISTINCT r.ApartmentId) Apartments |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 45 | FROM dbo.Residents r GROUP BY r.BuildingId ORDER BY r.BuildingId; |
| Apartments | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 49 | COUNT(DISTINCT r.ApartmentId) Apartments |
| Buildings | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 50 | FROM dbo.Buildings b LEFT JOIN dbo.Residents r ON r.BuildingId=b.Id |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 50 | FROM dbo.Buildings b LEFT JOIN dbo.Residents r ON r.BuildingId=b.Id |
| BuildingAdmins | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 57 | FROM dbo.BuildingAdmins ba |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 58 | LEFT JOIN dbo.Users u ON u.Uid=ba.AdminUid |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 66 | FROM dbo.Users; |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 68 | $overlap=[int](Query $c "SELECT COUNT(*) N FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE NULLIF(u.Uid,N'') IS NOT NULL").Rows[0].N |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 68 | $overlap=[int](Query $c "SELECT COUNT(*) N FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE NULLIF(u.Uid,N'') IS NOT NULL").Rows[0].N |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 69 | $mapUsers=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND AccountId IS NOT NULL").Rows[0].N |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 70 | $mapResidents=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS Nâ€¦ |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 75 | Log "Users total: $($roleCounts.Rows[0].TotalUsers); SuperAdmins by role: $($roleCounts.Rows[0].SuperAdmins); Admins by role: $($roleCounts.Rows[0].Admins)" |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 76 | foreach($r in $userSummary.Rows){Log "Users role '$($r.RoleName)': total=$($r.Total); inactive/disabled=$($r.InactiveOrDisabled)"} |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 77 | Log "Residents total: $((Query $c 'SELECT COUNT(*) N FROM dbo.Residents').Rows[0].N)" |
| Apartments | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 78 | foreach($r in $residentSummary.Rows){Log "Residents BuildingId=$($r.BuildingId): residents=$($r.Residents); apartments=$($r.Apartments); inactive/disabled=$($r.InactiveOrDisabled)"} |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 78 | foreach($r in $residentSummary.Rows){Log "Residents BuildingId=$($r.BuildingId): residents=$($r.Residents); apartments=$($r.Apartments); inactive/disabled=$($r.InactiveOrDisabled)"} |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 79 | Log "Users/Residents exact UID overlap: $overlap" |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 79 | Log "Users/Residents exact UID overlap: $overlap" |
| BuildingAdmins | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 80 | foreach($r in $adminSummary.Rows){Log "BuildingAdmins BuildingId=$($r.BuildingId): links=$($r.AdminLinks); distinct AdminUid=$($r.DistinctAdminUids); missing Users UID=$($r.UidsMissingFromUsers); roleâ€¦ |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 80 | foreach($r in $adminSummary.Rows){Log "BuildingAdmins BuildingId=$($r.BuildingId): links=$($r.AdminLinks); distinct AdminUid=$($r.DistinctAdminUids); missing Users UID=$($r.UidsMissingFromUsers); roleâ€¦ |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 81 | Log "Target Persons=$persons; UserAccounts=$accounts; verified Users mappings=$mapUsers; verified Resident mappings=$mapResidents; UserRoleAssignments=$assignments" |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 89 | '- Residents are residents only, not administrators or super administrators.', |
| Buildings | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 90 | '- Expected resident distribution: 4, 15, and 60 across three buildings.', |
| Users | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 91 | '- Users expected: 2 SuperAdmins and 3 building Admins, one Admin per building.', |
| Residents | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 95 | '- Resident login accounts will be created only after exact identity/account policy is confirmed; residents are not assigned administrative roles.', |
| BuildingAdmins | scripts\Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1 | 96 | '- Admin role assignments must be scoped to the exact BuildingId verified from BuildingAdmins; a missing third building-admin link must not be fabricated.', |
| BuildingAdmins | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 20 | WHERE t.name IN(N'Users',N'Residents',N'BuildingAdmins',N'Persons',N'UserAccounts',N'UserCredentials',N'LegacyIdentityMap') |
| Residents | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 20 | WHERE t.name IN(N'Users',N'Residents',N'BuildingAdmins',N'Persons',N'UserAccounts',N'UserCredentials',N'LegacyIdentityMap') |
| Users | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 20 | WHERE t.name IN(N'Users',N'Residents',N'BuildingAdmins',N'Persons',N'UserAccounts',N'UserCredentials',N'LegacyIdentityMap') |
| BuildingAdmins | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 24 | foreach($table in @('Users','Residents','BuildingAdmins','Persons','UserAccounts','UserCredentials','LegacyIdentityMap')){ |
| Residents | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 24 | foreach($table in @('Users','Residents','BuildingAdmins','Persons','UserAccounts','UserCredentials','LegacyIdentityMap')){ |
| Users | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 24 | foreach($table in @('Users','Residents','BuildingAdmins','Persons','UserAccounts','UserCredentials','LegacyIdentityMap')){ |
| Users | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 31 | (SELECT COUNT(*) FROM dbo.Users) UsersCount, |
| Residents | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 32 | (SELECT COUNT(*) FROM dbo.Residents) ResidentsCount, |
| BuildingAdmins | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 33 | (SELECT COUNT(*) FROM dbo.BuildingAdmins) BuildingAdminLinks, |
| Buildings | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 34 | (SELECT COUNT(*) FROM dbo.Buildings) BuildingsCount, |
| Users | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 38 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMapRows, |
| Residents | scripts\Invoke-Phase31-IdentitySchemaProfile.ps1 | 39 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMapRows; |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 18 | (SELECT COUNT(*) FROM dbo.Users) UsersTotal, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 19 | (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL) UsersWithEmail, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 20 | (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NULL) UsersWithoutEmail, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 21 | (SELECT COUNT(*) FROM (SELECT LOWER(LTRIM(RTRIM(Email))) E FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL GROUP BY LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*)>1) d) DuplicateUserEmailâ€¦ |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 22 | (SELECT COUNT(*) FROM dbo.Users WHERE LEN(LTRIM(RTRIM(Email)))>254) UserEmailsOver254, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 23 | (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Name)),N'') IS NULL) UsersWithoutName, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 24 | (SELECT COUNT(*) FROM dbo.Users WHERE IsDisabled=1 OR IsActive=0) InactiveUsers, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 25 | (SELECT COUNT(*) FROM dbo.Residents) ResidentsTotal, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 26 | (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL) ResidentsWithEmail, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 27 | (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NULL) ResidentsWithoutEmail, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 28 | (SELECT COUNT(*) FROM (SELECT LOWER(LTRIM(RTRIM(Email))) E FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL GROUP BY LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*)>1) d) DuplicateResidâ€¦ |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 29 | (SELECT COUNT(*) FROM dbo.Residents WHERE LEN(LTRIM(RTRIM(Email)))>254) ResidentEmailsOver254, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 30 | (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Name)),N'') IS NULL) ResidentsWithoutName, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 31 | (SELECT COUNT(*) FROM dbo.Residents WHERE IsDisabled=1 OR IsActive=0) InactiveResidents, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 32 | (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Uid)),N'') IS NULL) ResidentsWithoutUid, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 33 | (SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Residents GROUP BY Uid HAVING COUNT(*)>1) d) DuplicateResidentUidGroups, |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 34 | (SELECT COUNT(*) FROM dbo.BuildingAdmins) BuildingAdminLinks, |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 35 | (SELECT COUNT(DISTINCT AdminUid) FROM dbo.BuildingAdmins) DistinctLinkedAdminUids, |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 36 | (SELECT COUNT(DISTINCT BuildingId) FROM dbo.BuildingAdmins) DistinctLinkedBuildings, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 40 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMappingCandidates, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 41 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMappingCandidates, |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 42 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NOT NULL) VerifiedUserMappings, |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 43 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NULL) VerifiedResidentPersonMappâ€¦ |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 50 | SELECT COUNT(*) FROM dbo.Users u |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 52 | AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid); |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 55 | Log "Admin users without an explicit BuildingAdmins row: $missing" |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 55 | Log "Admin users without an explicit BuildingAdmins row: $missing" |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 57 | SELECT COUNT(*) FROM dbo.BuildingAdmins ba |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 58 | WHERE NOT EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Uid=ba.AdminUid AND LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')); |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 61 | Log "BuildingAdmins rows without an exact Admin source user: $invalid" |
| BuildingAdmins | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 69 | $lines+=@('','## Decision','- No identity, account, credential, role, ownership, occupancy, or apartment-access rows are created by this audit.','- Users and Residents remain distinct source populatioâ€¦ |
| Residents | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 69 | $lines+=@('','## Decision','- No identity, account, credential, role, ownership, occupancy, or apartment-access rows are created by this audit.','- Users and Residents remain distinct source populatioâ€¦ |
| Users | scripts\Invoke-Phase32-IdentityMigrationReadiness.ps1 | 69 | $lines+=@('','## Decision','- No identity, account, credential, role, ownership, occupancy, or apartment-access rows are created by this audit.','- Users and Residents remain distinct source populatioâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 20 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','__EFMigrationsHistory')){ |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 20 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','__EFMigrationsHistory')){ |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 20 | foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','__EFMigrationsHistory')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 24 | $migration=[int](Scalar $c "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119200000_Phase20_IdentityProfileEvidence'") |
| __EFMigrationsHistory | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 26 | $reviewMigration=[int](Scalar $c "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119220000_Phase34_SystemReviewedIdentityEvidence'") |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 30 | (SELECT COUNT(*) FROM dbo.Users u WHERE LEN(LTRIM(RTRIM(u.Name)))>160 OR LEN(NULLIF(LTRIM(RTRIM(u.Phone)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(u.Whatsapp)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(u.PhotoUrl)â€¦ |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 31 | (SELECT COUNT(*) FROM dbo.Residents r WHERE LEN(LTRIM(RTRIM(r.Name)))>160 OR LEN(NULLIF(LTRIM(RTRIM(r.Phone)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(r.Whatsapp)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(r.Photoâ€¦ |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 32 | (SELECT COUNT(*) FROM dbo.Users u WHERE NULLIF(LTRIM(RTRIM(u.Email)),N'') IS NULL) UsersMissingEmail, |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 33 | (SELECT COUNT(*) FROM dbo.Users u WHERE NULLIF(LTRIM(RTRIM(u.Name)),N'') IS NULL) UsersMissingName, |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 34 | (SELECT COUNT(*) FROM dbo.Residents r WHERE NULLIF(LTRIM(RTRIM(r.Name)),N'') IS NULL) ResidentsMissingName, |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 35 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND (MappingStatus<>0 OR PersonId IS NOT NULL OR AccountId IS NOT NULL)) NonPendingUserMappings, |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 36 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND (MappingStatus<>0 OR PersonId IS NOT NULL OR AccountId IS NOT NULL)) NonPendingResidentMapâ€¦ |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 37 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMaps, |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 38 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMaps, |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 39 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap m WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Residents' |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 40 | AND (SELECT COUNT(*) FROM dbo.Residents r WHERE m.SourceKey=r.Id OR m.SourceKey=CONCAT(r.BuildingId,N'\|',r.Id) OR m.SourceKey=r.Uid)<>1) UnmatchedOrAmbiguousResidentMapKeys, |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 41 | (SELECT COUNT(*) FROM dbo.LegacyIdentityMap m WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 42 | AND (SELECT COUNT(*) FROM dbo.Users u WHERE m.SourceKey=u.Uid)<>1) UnmatchedOrAmbiguousUserMapKeys; |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 49 | if([int]$r.UserMaps -ne 5 -or [int]$r.ResidentMaps -ne 79){throw 'Expected exact identity mapping candidate counts Users=5 and Residents=79.'} |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 49 | if([int]$r.UserMaps -ne 5 -or [int]$r.ResidentMaps -ne 79){throw 'Expected exact identity mapping candidate counts Users=5 and Residents=79.'} |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 65 | JOIN dbo.Residents r ON m.SourceKey=r.Id OR m.SourceKey=CONCAT(r.BuildingId,N'\|',r.Id) OR m.SourceKey=r.Uid |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 66 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Residents' |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 78 | EvidenceReference=N'Phase30 user-confirmed Residents are residents only; Phase32 exact source profile', |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 79 | ReviewReason=N'User explicitly confirmed all Residents rows are residents, not administrative identities. Person only; no login account or credential inferred.', |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 81 | WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND SourceKey=@sourceKey AND MappingStatus=0 AND PersonId IS NULL AND AccountId IS NULL; |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 90 | JOIN dbo.Users u ON m.SourceKey=u.Uid |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 91 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 105 | EvidenceReference=N'Phase30 exact Users role classification; Phase32 unique email and exact UID source row', |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 108 | WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND SourceKey=@sourceKey AND MappingStatus=0 AND PersonId IS NULL AND AccountId IS NULL; |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 119 | "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NOT NULL", |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 120 | "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=2 AND PersonId IS NOT NULL AND AccountId IS NULL", |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 127 | Log ("Verified Users mappings: "+(Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND Accouâ€¦ |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 128 | Log ("Separate Resident person mappings: "+(Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=2 AND PersonId IS NOT â€¦ |
| Residents | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 140 | '- Residents are mapped as separate Persons only, based on the explicit user confirmation; no Resident login accounts or credentials are created.', |
| Users | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 141 | '- Five administrative Users become distinct Persons and pending UserAccounts; AccountStatus=0 prevents sign-in until later controlled activation.', |
| BuildingAdmins | scripts\Invoke-Phase33-IdentityPersonAccountBackfill.ps1 | 144 | '- The third Admin remains without a building role until an exact source BuildingAdmins link is available.', |
| __EFMigrationsHistory | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 28 | $required = @('Expenses','Revenues','MonthlyApartmentShares','LegacyOperationalRecords','FinancialReconciliationRuns','FinancialReconciliationDifferences','__EFMigrationsHistory') |
| Expenses | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 28 | $required = @('Expenses','Revenues','MonthlyApartmentShares','LegacyOperationalRecords','FinancialReconciliationRuns','FinancialReconciliationDifferences','__EFMigrationsHistory') |
| MonthlyApartmentShares | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 28 | $required = @('Expenses','Revenues','MonthlyApartmentShares','LegacyOperationalRecords','FinancialReconciliationRuns','FinancialReconciliationDifferences','__EFMigrationsHistory') |
| Revenues | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 28 | $required = @('Expenses','Revenues','MonthlyApartmentShares','LegacyOperationalRecords','FinancialReconciliationRuns','FinancialReconciliationDifferences','__EFMigrationsHistory') |
| __EFMigrationsHistory | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 33 | $migration = [int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261118213732_Phase08_FinancialLedgerFoundation'").Rows[0].N |
| Expenses | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 39 | FROM dbo.Expenses e GROUP BY e.BuildingId,e.MonthKey |
| Revenues | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 42 | FROM dbo.Revenues r GROUP BY r.BuildingId,r.MonthKey |
| MonthlyApartmentShares | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 47 | FROM dbo.MonthlyApartmentShares s GROUP BY s.BuildingId,s.MonthKey |
| Expenses | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 75 | AND SourceTable IN (N'Expenses',N'Revenues',N'MonthlyApartmentShares'); |
| MonthlyApartmentShares | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 75 | AND SourceTable IN (N'Expenses',N'Revenues',N'MonthlyApartmentShares'); |
| Revenues | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 75 | AND SourceTable IN (N'Expenses',N'Revenues',N'MonthlyApartmentShares'); |
| Apartments | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 110 | N'Preserve as a separate historical reconciliation variance. Do not allocate to apartments, post to ledger, change wallet balances, or modify source records.',SYSUTCDATETIME(),NULL,NULL), |
| Apartments | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 113 | N'Preserve as a separate historical reconciliation variance. Do not allocate to apartments, post to ledger, change wallet balances, or modify source records.',SYSUTCDATETIME(),NULL,NULL); |
| Expenses | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 137 | '- Expenses: source total 4,700.00; existing apartment shares 4,700.22; variance +0.22.', |
| Revenues | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 138 | '- Revenues: source total 27,173.00; existing apartment shares 27,173.04; variance +0.04.', |
| Apartments | scripts\Invoke-Phase36-LegacyFinanceVarianceRegister.ps1 | 140 | 'Both differences are separate reconciliation items classified as LegacyRoundingVariance. They are not assigned to apartments and are not posted to the ledger or wallet.', |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 30 | IF OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.BuildingAdmins',N'U') IS NULL |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 30 | IF OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.BuildingAdmins',N'U') IS NULL |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 31 | OR OBJECT_ID(N'dbo.Buildings',N'U') IS NULL OR OBJECT_ID(N'dbo.LegacyIdentityMap',N'U') IS NULL |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 36 | IF COL_LENGTH(N'dbo.Users',N'Uid') IS NULL OR COL_LENGTH(N'dbo.Users',N'Role') IS NULL |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 37 | OR COL_LENGTH(N'dbo.Users',N'IsDisabled') IS NULL OR COL_LENGTH(N'dbo.Users',N'IsActive') IS NULL |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 38 | OR COL_LENGTH(N'dbo.BuildingAdmins',N'AdminUid') IS NULL OR COL_LENGTH(N'dbo.BuildingAdmins',N'BuildingId') IS NULL |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 44 | SELECT @sourceAdminCount=COUNT(*) FROM dbo.Users |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 46 | SELECT @buildingCount=COUNT(*) FROM dbo.Buildings; |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 47 | SELECT @explicitLinkCount=COUNT(*) FROM dbo.BuildingAdmins; |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 50 | IF @buildingCount<>3 THROW 51037, 'Expected exactly three target Buildings; refusing ordinal or guessed mapping.', 1; |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 51 | IF @explicitLinkCount<>2 THROW 51037, 'Expected exactly two explicit BuildingAdmins links; source changed or links are ambiguous.', 1; |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 52 | IF EXISTS(SELECT 1 FROM dbo.BuildingAdmins GROUP BY AdminUid HAVING COUNT(*)<>1) |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 53 | OR EXISTS(SELECT 1 FROM dbo.BuildingAdmins GROUP BY BuildingId HAVING COUNT(*)<>1) |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 54 | THROW 51037, 'Explicit BuildingAdmins links are not one-to-one.', 1; |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 56 | SELECT 1 FROM dbo.BuildingAdmins ba |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 57 | LEFT JOIN dbo.Users u ON u.Uid=ba.AdminUid |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 58 | LEFT JOIN dbo.Buildings b ON CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId) |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 63 | SELECT 1 FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 69 | SELECT 1 FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 72 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 75 | ) THROW 51037, 'Every Admin must have exactly one verified Users identity/account mapping.', 1; |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 78 | SELECT 1 FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 79 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 87 | SELECT CONVERT(nvarchar(256),u.Uid),m.AccountId,CONVERT(nvarchar(64),ba.BuildingId),N'Explicit BuildingAdmins' |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 88 | FROM dbo.BuildingAdmins ba |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 89 | JOIN dbo.Users u ON u.Uid=ba.AdminUid |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 90 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 95 | SELECT 1 FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 96 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 99 | AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid) |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 102 | -- Owner-confirmed rule: the sole Admin without a legacy BuildingAdmins row is Building 3. |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 103 | IF (SELECT COUNT(*) FROM dbo.Users u |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 105 | AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid))<>1 |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 107 | IF (SELECT COUNT(*) FROM dbo.Buildings b |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 108 | WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 110 | THROW 51037, 'Expected exactly one building without an explicit BuildingAdmins link.', 1; |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 114 | FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 115 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 117 | CROSS APPLY (SELECT CONVERT(nvarchar(64),b.Id) BuildingId FROM dbo.Buildings b |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 118 | WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 121 | AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid); |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 127 | THROW 51037, 'Resolved Admin-to-Building map is not exactly three unique accounts and three unique buildings.', 1; |
| Cameras | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 155 | 'residents.manage','apartment.disable','dvrs.manage','cameras.view','cameras.manage', |
| Dvrs | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 155 | 'residents.manage','apartment.disable','dvrs.manage','cameras.view','cameras.manage', |
| Residents | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 155 | 'residents.manage','apartment.disable','dvrs.manage','cameras.view','cameras.manage', |
| Polls | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 156 | 'reports.view','polls.manage','maintenance.manage','audit.view.building', |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 164 | Log 'System-only permissions are deliberately excluded: buildings/admins management, global audit/settings, logs, identity mapping, backup/restore/delete, secrets, impersonation, and maintenance execuâ€¦ |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 177 | FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 178 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 180 | JOIN dbo.BuildingAdmins ba ON ba.AdminUid=u.Uid |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 181 | JOIN dbo.Buildings b ON CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId) |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 186 | FROM dbo.Users u |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 187 | JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 189 | CROSS APPLY (SELECT b.Id FROM dbo.Buildings b |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 190 | WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 193 | AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid); |
| Cameras | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 214 | (N'residents.manage'),(N'apartment.disable'),(N'dvrs.manage'),(N'cameras.view'),(N'cameras.manage'), |
| Dvrs | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 214 | (N'residents.manage'),(N'apartment.disable'),(N'dvrs.manage'),(N'cameras.view'),(N'cameras.manage'), |
| Residents | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 214 | (N'residents.manage'),(N'apartment.disable'),(N'dvrs.manage'),(N'cameras.view'),(N'cameras.manage'), |
| Polls | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 215 | (N'reports.view'),(N'polls.manage'),(N'maintenance.manage'),(N'audit.view.building'), |
| Cameras | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 235 | (N'residents.manage',N'apartment.disable',N'dvrs.manage',N'cameras.view',N'cameras.manage', |
| Dvrs | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 235 | (N'residents.manage',N'apartment.disable',N'dvrs.manage',N'cameras.view',N'cameras.manage', |
| Residents | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 235 | (N'residents.manage',N'apartment.disable',N'dvrs.manage',N'cameras.view',N'cameras.manage', |
| Polls | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 236 | N'reports.view',N'polls.manage',N'maintenance.manage',N'audit.view.building', |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 249 | N'Phase37: exact legacy Admin identity map; explicit BuildingAdmins link or owner-confirmed unique remaining Building 3 scope',NULL,NULL,NULL |
| Buildings | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 327 | '- Three legacy Users rows with role Admin/buildingadmin must resolve to three distinct target UserAccounts and three distinct Buildings.', |
| Users | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 327 | '- Three legacy Users rows with role Admin/buildingadmin must resolve to three distinct target UserAccounts and three distinct Buildings.', |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 328 | '- Two administrators use exact BuildingAdmins source links.', |
| BuildingAdmins | scripts\Invoke-Phase37-ThreeBuildingAdminScope.ps1 | 329 | '- The sole unlinked administrator is assigned to the sole building without a BuildingAdmins link, per the owner-approved Building 3 decision; no row ordering or guessed ID is used.', |
| __EFMigrationsHistory | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 33 | foreach ($table in @('Deposits','Apartments','Buildings','LegacyIdentityMap','UserAccounts','PaymentDeposits','FinancialPeriods','__EFMigrationsHistory')) { |
| Apartments | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 33 | foreach ($table in @('Deposits','Apartments','Buildings','LegacyIdentityMap','UserAccounts','PaymentDeposits','FinancialPeriods','__EFMigrationsHistory')) { |
| Buildings | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 33 | foreach ($table in @('Deposits','Apartments','Buildings','LegacyIdentityMap','UserAccounts','PaymentDeposits','FinancialPeriods','__EFMigrationsHistory')) { |
| Deposits | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 33 | foreach ($table in @('Deposits','Apartments','Buildings','LegacyIdentityMap','UserAccounts','PaymentDeposits','FinancialPeriods','__EFMigrationsHistory')) { |
| __EFMigrationsHistory | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 43 | $applied = [int](Scalar $connection "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'$migration'") |
| Deposits | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 52 | FROM dbo.Deposits d |
| Users | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 59 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 66 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Apartments | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 70 | CASE WHEN EXISTS (SELECT 1 FROM dbo.Apartments otherA WHERE otherA.Id=d.ApartmentId AND otherA.BuildingId<>d.BuildingId) THEN 1 ELSE 0 END ApartmentBuildingMismatch, |
| Apartments | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 94 | LEFT JOIN dbo.Apartments a ON a.BuildingId=D.BuildingId AND a.Id=D.ApartmentId; |
| Users | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 106 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 113 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Deposits | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 117 | FROM dbo.Deposits d |
| Buildings | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 136 | FROM dbo.Buildings b |
| Users | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 146 | Log 'Actor resolution rule: exact legacy Users UID only, with reviewed MappingStatus=1 and a real UserAccounts row. Display-name/email/fuzzy matching is deliberately excluded.' |
| Presence | scripts\Invoke-Phase41-DepositReadinessAudit.ps1 | 169 | '- Source DateTime fields are inspected for null/presence, but timezone meaning is not inferred.', |
| DepositEditHistory | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 33 | foreach ($table in @('Deposits','Users','LegacyIdentityMap','UserAccounts','DepositEditHistory')) { |
| Deposits | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 33 | foreach ($table in @('Deposits','Users','LegacyIdentityMap','UserAccounts','DepositEditHistory')) { |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 33 | foreach ($table in @('Deposits','Users','LegacyIdentityMap','UserAccounts','DepositEditHistory')) { |
| Deposits | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 42 | FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.ConfirmedBy)),N'') IS NOT NULL |
| Deposits | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 45 | FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.CancelledBy)),N'') IS NOT NULL |
| Deposits | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 48 | FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.CreatedBy)),N'') IS NOT NULL |
| Deposits | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 51 | FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.UpdatedBy)),N'') IS NOT NULL |
| DepositEditHistory | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 54 | FROM dbo.DepositEditHistory h WHERE NULLIF(LTRIM(RTRIM(h.ByUser)),N'') IS NOT NULL |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 64 | (SELECT COUNT(*) FROM dbo.Users u WHERE u.Uid=a.ActorValue) ExactUidMatches, |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 65 | (SELECT COUNT(*) FROM dbo.Users u WHERE u.Name=a.ActorValue) ExactNameMatches, |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 66 | (SELECT COUNT(*) FROM dbo.Users u WHERE u.Email=a.ActorValue) ExactEmailMatches, |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 67 | (SELECT COUNT(*) FROM dbo.Users u WHERE LTRIM(RTRIM(u.Name))=a.ActorValue) TrimmedNameMatches, |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 68 | (SELECT COUNT(*) FROM dbo.Users u WHERE LTRIM(RTRIM(u.Email))=a.ActorValue) TrimmedEmailMatches, |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 71 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 76 | FROM dbo.Users u |
| Buildings | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 102 | Log ("Actor: Field={0}; Value={1}; Occurrences={2}; Buildings={3}; ExactUid={4}; ExactName={5}; ExactEmail={6}; TrimmedName={7}; TrimmedEmail={8}; ReviewedUidAccount={9}; CandidateUids={10}" -f $row.Aâ€¦ |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 105 | $users = ReadTable $connection @" |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 110 | WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users' |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 114 | FROM dbo.Users u |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 117 | $phoneMatches = @($users \| Where-Object { |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 122 | Log "Users matching supplied phone in Phone/Whatsapp: $($phoneMatches.Count)" |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 126 | Log "Legacy administrative users profiled: $($users.Rows.Count)" |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 127 | foreach ($row in $users.Rows) { |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 147 | ($users \| Format-Table -AutoSize -Wrap \| Out-String).Trim(), |
| Users | scripts\Invoke-Phase42-DepositActorForensics.ps1 | 156 | '- The supplied phone is compared to Users.Phone and Users.Whatsapp after removing non-digit formatting; a match is not used to assign historical actors.', |
| Apartments | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 26 | IF OBJECT_ID(N'dbo.Deposits',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 26 | IF OBJECT_ID(N'dbo.Deposits',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL |
| Buildings | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 27 | OR OBJECT_ID(N'dbo.Buildings',N'U') IS NULL OR OBJECT_ID(N'dbo.UserRoleAssignments',N'U') IS NULL |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 34 | IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 36 | IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status))) NOT IN(N'confirmed',N'cancelled')) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 38 | IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE Amount<=0 OR Amount<>ROUND(Amount,2)) |
| Apartments | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 40 | IF EXISTS(SELECT 1 FROM dbo.Deposits d LEFT JOIN dbo.Apartments a |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 40 | IF EXISTS(SELECT 1 FROM dbo.Deposits d LEFT JOIN dbo.Apartments a |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 43 | IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status)))=N'confirmed' AND ConfirmedAt IS NULL) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 45 | IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status)))=N'cancelled' AND COALESCE(CancelledAt,CreatedAt) IS NULL) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 50 | FROM dbo.Deposits d |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 60 | FROM dbo.Deposits d |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 62 | AND e.SourceTable=N'Deposits' AND e.SourceKey=CONCAT(d.BuildingId,N'\|',d.Id) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 102 | FROM dbo.Deposits d |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 104 | AND e.SourceTable=N'Deposits' AND e.SourceKey=CONCAT(d.BuildingId,N'\|',d.Id) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 114 | IF (SELECT COUNT(*) FROM #S)<>(SELECT COUNT(*) FROM dbo.Deposits) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 120 | SELECT N'Deposits' SourceType,COUNT(*) SourceRows, |
| Buildings | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 125 | COUNT(DISTINCT BuildingId) Buildings,COUNT(DISTINCT MonthKey) Months |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 142 | CONCAT(N'legacy-deposit:',s.SourceKey),N'BuildingManagement.Legacy',N'Deposits',s.SourceKey, |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 146 | AND NOT EXISTS(SELECT 1 FROM dbo.LedgerTransactions t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey); |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 158 | N'BuildingManagement.Legacy',N'Deposits',s.SourceKey,s.LedgerTransactionId,s.SourceFingerprint |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 161 | AND NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Deposits' AND l.SourceKey=s.SourceKey); |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 176 | (NOT EXISTS(SELECT 1 FROM dbo.LedgerTransactions t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey AND t.Status=1) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 178 | OR NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Deposits' AND l.SourceKey=s.SourceKey))) |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 195 | (SELECT COUNT(*) FROM dbo.LedgerTransactions t JOIN #S s ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey) ConfirmedLedgerTransactions, |
| Buildings | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 208 | Log ("Source deposits={0}; confirmed={1}; cancelled={2}; confirmed amount={3}; cancelled amount={4}; buildings={5}; months={6}" -f $r.SourceRows,$r.ConfirmedRows,$r.CancelledRows,$r.ConfirmedAmount,$râ€¦ |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 208 | Log ("Source deposits={0}; confirmed={1}; cancelled={2}; confirmed amount={3}; cancelled amount={4}; buildings={5}; months={6}" -f $r.SourceRows,$r.ConfirmedRows,$r.CancelledRows,$r.ConfirmedAmount,$râ€¦ |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 211 | Log ("Target deposits={0}; confirmed ledger transactions={1}; confirmed ledger entries={2}; target periods={3}" -f $r.TargetDeposits,$r.ConfirmedLedgerTransactions,$r.ConfirmedLedgerEntries,$r.TargetPâ€¦ |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 217 | Log 'Cancellation disposition: owner-approved initial system testing reason; cancelled deposits create no ledger movement.' |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 230 | '- Confirmed deposits create one posted deposit transaction, one positive apartment ledger entry, and one source fingerprint link.', |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 231 | '- Cancelled deposits remain cancelled, have no ledger posting, and carry the owner-approved initial-testing disposition.', |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 233 | '- Source Deposits, Phase29 evidence payloads, hashes, and historical edit rows are never updated or deleted.', |
| Deposits | scripts\Invoke-Phase43-DepositLedgerCutover.ps1 | 243 | git commit --only -m "feat(finance): cut over legacy deposits to ledger" -- $PSCommandPath $report |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 26 | IF OBJECT_ID(N'dbo.Expenses',N'U') IS NULL OR OBJECT_ID(N'dbo.Revenues',N'U') IS NULL |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 26 | IF OBJECT_ID(N'dbo.Expenses',N'U') IS NULL OR OBJECT_ID(N'dbo.Revenues',N'U') IS NULL |
| Apartments | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 27 | OR OBJECT_ID(N'dbo.MonthlyApartmentShares',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 27 | OR OBJECT_ID(N'dbo.MonthlyApartmentShares',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL |
| Apartments | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 36 | IF EXISTS(SELECT 1 FROM dbo.MonthlyApartmentShares s LEFT JOIN dbo.Apartments a |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 36 | IF EXISTS(SELECT 1 FROM dbo.MonthlyApartmentShares s LEFT JOIN dbo.Apartments a |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 42 | SELECT BuildingId FROM dbo.Expenses WHERE MonthKey IN(N'2026-09',N'2026-10') |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 43 | UNION SELECT BuildingId FROM dbo.Revenues WHERE MonthKey IN(N'2026-09',N'2026-10') |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 44 | UNION SELECT BuildingId FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10') |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 54 | SELECT N'Expenses' SourceType,CONCAT(e.BuildingId,N'\|',e.Id) SourceKey |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 55 | FROM dbo.Expenses e LEFT JOIN dbo.LegacyOperationalRecords p |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 56 | ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Expenses' |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 60 | SELECT N'Revenues',CONCAT(r.BuildingId,N'\|',r.Id) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 61 | FROM dbo.Revenues r LEFT JOIN dbo.LegacyOperationalRecords p |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 62 | ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Revenues' |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 83 | FROM dbo.Expenses e |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 98 | FROM dbo.Revenues r |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 114 | FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10') GROUP BY BuildingId,MonthKey |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 117 | FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10') GROUP BY BuildingId,MonthKey; |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 169 | CASE WHEN o.Kind=2 THEN N'Legacy monthly expenses cutover using preserved apartment shares' |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 170 | ELSE N'Legacy monthly revenues cutover using preserved apartment shares' END, |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 188 | FROM #Ops o JOIN dbo.MonthlyApartmentShares s ON s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 197 | N'BuildingManagement.Legacy',N'Expenses',CONCAT(e.BuildingId,N'\|',e.Id),o.TransactionId,p.SourceFingerprint |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 198 | FROM dbo.Expenses e JOIN #Ops o ON o.BuildingId=e.BuildingId AND o.MonthKey=e.MonthKey AND o.Kind=2 |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 199 | JOIN dbo.LegacyOperationalRecords p ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Expenses' AND p.SourceKey=CONCAT(e.BuildingId,N'\|',e.Id) |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 200 | WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND l.SourceKey=CONCAT(e.BuildingId,N'\|',e.Id)); |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 204 | N'BuildingManagement.Legacy',N'Revenues',CONCAT(r.BuildingId,N'\|',r.Id),o.TransactionId,p.SourceFingerprint |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 205 | FROM dbo.Revenues r JOIN #Ops o ON o.BuildingId=r.BuildingId AND o.MonthKey=r.MonthKey AND o.Kind=3 |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 206 | JOIN dbo.LegacyOperationalRecords p ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Revenues' AND p.SourceKey=CONCAT(r.BuildingId,N'\|',r.Id) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 207 | WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND l.SourceKey=CONCAT(r.BuildingId,N'\|',r.Id)); |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 211 | < (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares s WHERE s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 215 | IF EXISTS(SELECT 1 FROM dbo.Expenses e JOIN #Ops o ON o.BuildingId=e.BuildingId AND o.MonthKey=e.MonthKey AND o.Kind=2 |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 216 | WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND l.SourceKey=CONCAT(e.BuildingId,N'\|',e.Id))) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 217 | OR EXISTS(SELECT 1 FROM dbo.Revenues r JOIN #Ops o ON o.BuildingId=r.BuildingId AND o.MonthKey=r.MonthKey AND o.Kind=3 |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 218 | WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND l.SourceKey=CONCAT(r.BuildingId,N'\|',r.Id))) |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 229 | (SELECT COUNT(*) FROM dbo.Expenses) ExpenseSourceRows, |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 230 | (SELECT COUNT(*) FROM dbo.Revenues) RevenueSourceRows, |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 231 | (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares) MonthlyShareSourceRows, |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 232 | (SELECT COUNT(*) FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND EXISTS(SELECT 1 FROM dbo.Expenses e WHERE e.MonthKey IN(N'2026-09',Nâ€¦ |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 233 | (SELECT COUNT(*) FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND EXISTS(SELECT 1 FROM dbo.Revenues r WHERE r.MonthKey IN(N'2026-09',Nâ€¦ |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 235 | (SELECT COUNT(*) FROM dbo.Expenses WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeExpenses, |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 236 | (SELECT COUNT(*) FROM dbo.Revenues WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeRevenues, |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 237 | (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeShareRows; |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 252 | Log ("All source rows: expenses={0}; revenues={1}; monthly shares={2}" -f $r.ExpenseSourceRows,$r.RevenueSourceRows,$r.MonthlyShareSourceRows) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 252 | Log ("All source rows: expenses={0}; revenues={1}; monthly shares={2}" -f $r.ExpenseSourceRows,$r.RevenueSourceRows,$r.MonthlyShareSourceRows) |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 253 | Log ("September/October links: expenses={0}; revenues={1}; share ledger entries={2}" -f $r.LinkedExpenseRows,$r.LinkedRevenueRows,$r.CreatedShareLedgerEntries) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 253 | Log ("September/October links: expenses={0}; revenues={1}; share ledger entries={2}" -f $r.LinkedExpenseRows,$r.LinkedRevenueRows,$r.CreatedShareLedgerEntries) |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 254 | Log ("Outside posting scope (retained in immutable evidence): expenses={0}; revenues={1}; monthly shares={2}" -f $r.OutOfScopeExpenses,$r.OutOfScopeRevenues,$r.OutOfScopeShareRows) |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 254 | Log ("Outside posting scope (retained in immutable evidence): expenses={0}; revenues={1}; monthly shares={2}" -f $r.OutOfScopeExpenses,$r.OutOfScopeRevenues,$r.OutOfScopeShareRows) |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 259 | Log 'Allocation policy: use original MonthlyApartmentShares only; no historical reallocation or invented apartment attribution.' |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 273 | '- Expenses and revenues are represented by building/month ledger transactions and linked to every original source row using immutable fingerprints.', |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 273 | '- Expenses and revenues are represented by building/month ledger transactions and linked to every original source row using immutable fingerprints.', |
| MonthlyApartmentShares | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 274 | '- Apartment wallet entries are generated only from the exact historical MonthlyApartmentShares rows; no historical reallocation is inferred.', |
| Apartments | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 276 | '- Registered legacy rounding variances are preserved and are never assigned to apartments or balanced with invented entries.', |
| Expenses | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 287 | git commit --only -m "feat(finance): migrate legacy expenses revenues and apartment shares" -- $PSCommandPath $report |
| Revenues | scripts\Invoke-Phase44-ExpenseRevenueShareCutover.ps1 | 287 | git commit --only -m "feat(finance): migrate legacy expenses revenues and apartment shares" -- $PSCommandPath $report |
| __EFMigrationsHistory | scripts\Invoke-Phase47-RowLevelMigrationReconciliation.ps1 | 166 | '- __EFMigrationsHistory naturally contains target migration rows absent from the legacy source and must be classified separately.', |
| Presence | scripts\Invoke-Phase48-ChangedRowDiagnostic.ps1 | 50 | foreach($table in @('Presence','ScheduledBackupSettings')){ |
| ScheduledBackupSettings | scripts\Invoke-Phase48-ChangedRowDiagnostic.ps1 | 50 | foreach($table in @('Presence','ScheduledBackupSettings')){ |
| __EFMigrationsHistory | scripts\Invoke-Phase49-LegacyTableRetirement.ps1 | 88 | if([string]$s.TableName -eq '__EFMigrationsHistory') { |
| __EFMigrationsHistory | scripts\Invoke-Phase49-LegacyTableRetirement.ps1 | 104 | $legacyValues=($sourceTables.Rows \| Where-Object { $_.TableName -ne '__EFMigrationsHistory' } \| ForEach-Object { "(N'$($_.SchemaName.Replace("'","''"))',N'$($_.TableName.Replace("'","''"))')" }) -joâ€¦ |
| __EFMigrationsHistory | scripts\Invoke-Phase49-LegacyTableRetirement.ps1 | 141 | '- __EFMigrationsHistory is deliberately retained as Entity Framework deployment metadata.', |
| __EFMigrationsHistory | scripts\Invoke-Phase51-LegacyDependencyRetirementAudit.ps1 | 119 | '- The 32-table source inventory includes infrastructure metadata and operational data; preserve __EFMigrationsHistory and any runtime-owned objects until their owners are explicitly migrated.', |
| __EFMigrationsHistory | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| Apartments | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| AuditLog | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| BackupHistory | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| BuildingAdmins | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| Buildings | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| BuildingSettings | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 9 | '__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings', |
| Cameras | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| DepositEditHistory | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| Deposits | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| Dvrs | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| Expenses | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| FinancialCategories | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| Floors | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| GlobalSettings | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 10 | 'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings', |
| LoginThrottle | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| MaintenanceRecords | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| MonthlyApartmentShares | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| Notifications | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| PollOptions | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| Polls | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| PollVotes | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 11 | 'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes', |
| Presence | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| QrTokens | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| QrUsages | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| Residents | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| Revenues | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| ScheduledBackupSettings | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| SyncPendingChanges | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 12 | 'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges', |
| UserPermissions | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 13 | 'UserPermissions','Users','WalletAdjustments') |
| Users | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 13 | 'UserPermissions','Users','WalletAdjustments') |
| WalletAdjustments | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 13 | 'UserPermissions','Users','WalletAdjustments') |
| Apartments | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 89 | $out.Add('- Buildings, Floors and Apartments are foundational and excluded from retirement regardless of counts.') |
| Buildings | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 89 | $out.Add('- Buildings, Floors and Apartments are foundational and excluded from retirement regardless of counts.') |
| Floors | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 89 | $out.Add('- Buildings, Floors and Apartments are foundational and excluded from retirement regardless of counts.') |
| __EFMigrationsHistory | scripts\Invoke-Phase53-RefinedLegacyRuntimeReferenceAudit.ps1 | 90 | $out.Add('- __EFMigrationsHistory is infrastructure metadata and is never part of legacy-domain retirement.') |
| __EFMigrationsHistory | scripts\Run-Phase08-MigrationSql-Review.ps1 | 76 | FROM dbo.__EFMigrationsHistory |
| __EFMigrationsHistory | scripts\Run-Phase08-RestoreTest-Preflight.ps1 | 77 | FROM dbo.__EFMigrationsHistory |
| Users | Services\AdminManagementService.cs | 23 | UsersService users, |
| Users | Services\AdminManagementService.cs | 29 | _users = users; |
| BuildingAdmins | Services\AdminManagementService.cs | 222 | await db.BuildingAdmins.Where(ba => ba.AdminUid == uid).ExecuteDeleteAsync(); |
| UserPermissions | Services\AdminManagementService.cs | 223 | await db.UserPermissions.Where(p => p.Uid == uid).ExecuteDeleteAsync(); |
| Users | Services\AdminManagementService.cs | 224 | await db.Users.Where(u => u.Uid == uid).ExecuteDeleteAsync(); |
| Apartments | Services\ApartmentAccessGrantService.cs | 70 | var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync( |
| AuditLog | Services\ApartmentAccessGrantService.cs | 100 | db.AuditLog.Add(CreateAudit( |
| AuditLog | Services\ApartmentAccessGrantService.cs | 158 | db.AuditLog.Add(CreateAudit( |
| Apartments | Services\ApartmentOwnershipService.cs | 58 | var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync( |
| AuditLog | Services\ApartmentOwnershipService.cs | 122 | db.AuditLog.Add(new AuditLogEntity |
| Users | Services\ApplicationLogQueryService.cs | 46 | @"(?<![\w:])/(?:var\|opt\|home\|etc\|usr\|tmp\|mnt\|app\|root\|product\|srv\|data\|workspace\|lib\|bin\|sbin\|boot\|dev\|proc\|sys\|run\|media\|storage\|Users\|Volumes)(?:/[^""'<>\|\r\n]*)?", |
| Buildings | Services\AuditLogger.cs | 46 | var exists = await db.Buildings.AnyAsync(b => b.Id == buildingId); |
| Apartments | Services\AuditLogger.cs | 55 | apartmentNumber = await db.Apartments.AsNoTracking() |
| AuditLog | Services\AuditLogger.cs | 64 | db.AuditLog.Add(new AuditLogEntity |
| AuditLog | Services\AuditLogger.cs | 103 | var query = db.AuditLog.AsNoTracking().AsQueryable(); |
| AuditLog | Services\AuditLogger.cs | 152 | var query = db.AuditLog.AsNoTracking().AsQueryable(); |
| Deposits | Services\AuditLogQueryService.cs | 71 | // Deposits |
| Expenses | Services\AuditLogQueryService.cs | 74 | // Expenses / Revenues |
| Revenues | Services\AuditLogQueryService.cs | 74 | // Expenses / Revenues |
| Polls | Services\AuditLogQueryService.cs | 83 | // Polls |
| Residents | Services\AuditLogQueryService.cs | 89 | // Residents |
| Buildings | Services\AuditLogQueryService.cs | 92 | // Buildings |
| Apartments | Services\AuditLogQueryService.cs | 99 | // Apartments |
| AuditLog | Services\AuditLogService.cs | 17 | building.AuditLog ??= new List<AuditLogEntry>(); |
| AuditLog | Services\AuditLogService.cs | 37 | building.AuditLog.Add(entry); |
| AuditLog | Services\AuditLogService.cs | 39 | if (building.AuditLog.Count > 1000) |
| AuditLog | Services\AuditLogService.cs | 40 | building.AuditLog = building.AuditLog.Skip(building.AuditLog.Count - 1000).ToList(); |
| Buildings | Services\AuthorizationEvaluator.cs | 178 | AuthorizationScopeType.Building => await db.Buildings.AsNoTracking() |
| Apartments | Services\AuthorizationEvaluator.cs | 180 | AuthorizationScopeType.Apartment => await db.Apartments.AsNoTracking() |
| Buildings | Services\AuthorizationPermissionCatalog.cs | 48 | ["buildings.manage"] = Define("buildings.manage", AuthorizationScopeType.System), |
| Residents | Services\AuthorizationPermissionCatalog.cs | 50 | ["residents.manage"] = Define("residents.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true), |
| Dvrs | Services\AuthorizationPermissionCatalog.cs | 52 | ["dvrs.manage"] = Define("dvrs.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true), |
| Cameras | Services\AuthorizationPermissionCatalog.cs | 53 | ["cameras.view"] = Define("cameras.view", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true), |
| Cameras | Services\AuthorizationPermissionCatalog.cs | 54 | ["cameras.manage"] = Define("cameras.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true), |
| Polls | Services\AuthorizationPermissionCatalog.cs | 56 | ["polls.manage"] = Define("polls.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true), |
| GlobalSettings | Services\BackupService.cs | 47 | ["globalSettings"] = Loc.T("Backup_Collection_globalSettings"), |
| ScheduledBackupSettings | Services\BackupService.cs | 48 | ["scheduledBackupSettings"] = Loc.T("Backup_Collection_scheduledBackupSettings"), |
| Users | Services\BackupService.cs | 49 | ["users"] = Loc.T("Backup_Collection_users"), |
| Buildings | Services\BackupService.cs | 50 | ["buildings"] = Loc.T("Backup_Collection_buildings"), |
| BuildingSettings | Services\BackupService.cs | 51 | ["buildingSettings"] = Loc.T("Backup_Collection_buildingSettings"), |
| BuildingAdmins | Services\BackupService.cs | 52 | ["buildingAdmins"] = Loc.T("Backup_Collection_buildingAdmins"), |
| Floors | Services\BackupService.cs | 53 | ["floors"] = Loc.T("Backup_Collection_floors"), |
| Apartments | Services\BackupService.cs | 54 | ["apartments"] = Loc.T("Backup_Collection_apartments"), |
| Residents | Services\BackupService.cs | 55 | ["residents"] = Loc.T("Backup_Collection_residents"), |
| FinancialCategories | Services\BackupService.cs | 56 | ["financialCategories"] = Loc.T("Backup_Collection_financialCategories"), |
| Expenses | Services\BackupService.cs | 57 | ["expenses"] = Loc.T("Backup_Collection_expenses"), |
| Revenues | Services\BackupService.cs | 58 | ["revenues"] = Loc.T("Backup_Collection_revenues"), |
| MonthlyApartmentShares | Services\BackupService.cs | 59 | ["monthlyApartmentShares"] = Loc.T("Backup_Collection_monthlyApartmentShares"), |
| Deposits | Services\BackupService.cs | 60 | ["deposits"] = Loc.T("Backup_Collection_deposits"), |
| DepositEditHistory | Services\BackupService.cs | 61 | ["depositEditHistory"] = Loc.T("Backup_Collection_depositEditHistory"), |
| WalletAdjustments | Services\BackupService.cs | 62 | ["walletAdjustments"] = Loc.T("Backup_Collection_walletAdjustments"), |
| UserPermissions | Services\BackupService.cs | 63 | ["userPermissions"] = Loc.T("Backup_Collection_userPermissions"), |
| AuditLog | Services\BackupService.cs | 64 | ["auditLog"] = Loc.T("Backup_Collection_auditLog"), |
| Notifications | Services\BackupService.cs | 65 | ["notifications"] = Loc.T("Backup_Collection_notifications"), |
| Polls | Services\BackupService.cs | 66 | ["polls"] = Loc.T("Backup_Collection_polls"), |
| PollOptions | Services\BackupService.cs | 67 | ["pollOptions"] = Loc.T("Backup_Collection_pollOptions"), |
| PollVotes | Services\BackupService.cs | 68 | ["pollVotes"] = Loc.T("Backup_Collection_pollVotes"), |
| MaintenanceRecords | Services\BackupService.cs | 69 | ["maintenanceRecords"] = Loc.T("Backup_Collection_maintenanceRecords"), |
| Dvrs | Services\BackupService.cs | 70 | ["dvrs"] = Loc.T("Backup_Collection_dvrs"), |
| Cameras | Services\BackupService.cs | 71 | ["cameras"] = Loc.T("Backup_Collection_cameras"), |
| QrTokens | Services\BackupService.cs | 72 | ["qrTokens"] = Loc.T("Backup_Collection_qrTokens"), |
| QrUsages | Services\BackupService.cs | 73 | ["qrUsages"] = Loc.T("Backup_Collection_qrUsages") |
| GlobalSettings | Services\BackupService.cs | 136 | "globalSettings" => await db.GlobalSettings.CountAsync(), |
| ScheduledBackupSettings | Services\BackupService.cs | 137 | "scheduledBackupSettings" => await db.ScheduledBackupSettings.CountAsync(), |
| Users | Services\BackupService.cs | 138 | "users" => await db.Users.CountAsync(), |
| Buildings | Services\BackupService.cs | 139 | "buildings" => await db.Buildings.CountAsync(), |
| BuildingSettings | Services\BackupService.cs | 140 | "buildingSettings" => await db.BuildingSettings.CountAsync(), |
| BuildingAdmins | Services\BackupService.cs | 141 | "buildingAdmins" => await db.BuildingAdmins.CountAsync(), |
| Floors | Services\BackupService.cs | 142 | "floors" => await db.Floors.CountAsync(), |
| Apartments | Services\BackupService.cs | 143 | "apartments" => await db.Apartments.CountAsync(), |
| Residents | Services\BackupService.cs | 144 | "residents" => await db.Residents.CountAsync(), |
| FinancialCategories | Services\BackupService.cs | 145 | "financialCategories" => await db.FinancialCategories.CountAsync(), |
| Expenses | Services\BackupService.cs | 146 | "expenses" => await db.Expenses.CountAsync(), |
| Revenues | Services\BackupService.cs | 147 | "revenues" => await db.Revenues.CountAsync(), |
| MonthlyApartmentShares | Services\BackupService.cs | 148 | "monthlyApartmentShares" => await db.MonthlyApartmentShares.CountAsync(), |
| Deposits | Services\BackupService.cs | 149 | "deposits" => await db.Deposits.CountAsync(), |
| DepositEditHistory | Services\BackupService.cs | 150 | "depositEditHistory" => await db.DepositEditHistory.CountAsync(), |
| WalletAdjustments | Services\BackupService.cs | 151 | "walletAdjustments" => await db.WalletAdjustments.CountAsync(), |
| UserPermissions | Services\BackupService.cs | 152 | "userPermissions" => await db.UserPermissions.CountAsync(), |
| AuditLog | Services\BackupService.cs | 153 | "auditLog" => await db.AuditLog.CountAsync(), |
| Notifications | Services\BackupService.cs | 154 | "notifications" => await db.Notifications.CountAsync(), |
| Polls | Services\BackupService.cs | 155 | "polls" => await db.Polls.CountAsync(), |
| PollOptions | Services\BackupService.cs | 156 | "pollOptions" => await db.PollOptions.CountAsync(), |
| PollVotes | Services\BackupService.cs | 157 | "pollVotes" => await db.PollVotes.CountAsync(), |
| MaintenanceRecords | Services\BackupService.cs | 158 | "maintenanceRecords" => await db.MaintenanceRecords.CountAsync(), |
| Dvrs | Services\BackupService.cs | 159 | "dvrs" => await db.Dvrs.CountAsync(), |
| Cameras | Services\BackupService.cs | 160 | "cameras" => await db.Cameras.CountAsync(), |
| QrTokens | Services\BackupService.cs | 161 | "qrTokens" => await db.QrTokens.CountAsync(), |
| QrUsages | Services\BackupService.cs | 162 | "qrUsages" => await db.QrUsages.CountAsync(), |
| GlobalSettings | Services\BackupService.cs | 291 | case "globalSettings": |
| GlobalSettings | Services\BackupService.cs | 292 | foreach (var g in await db.GlobalSettings.AsNoTracking().ToListAsync()) |
| ScheduledBackupSettings | Services\BackupService.cs | 295 | case "scheduledBackupSettings": |
| ScheduledBackupSettings | Services\BackupService.cs | 296 | foreach (var s in await db.ScheduledBackupSettings.AsNoTracking().ToListAsync()) |
| Users | Services\BackupService.cs | 299 | case "users": |
| Users | Services\BackupService.cs | 300 | foreach (var u in await db.Users.AsNoTracking().ToListAsync()) |
| Buildings | Services\BackupService.cs | 303 | case "buildings": |
| Buildings | Services\BackupService.cs | 304 | foreach (var b in await db.Buildings.AsNoTracking().ToListAsync()) |
| BuildingSettings | Services\BackupService.cs | 307 | case "buildingSettings": |
| BuildingSettings | Services\BackupService.cs | 308 | foreach (var bs in await db.BuildingSettings.AsNoTracking().ToListAsync()) |
| BuildingAdmins | Services\BackupService.cs | 311 | case "buildingAdmins": |
| BuildingAdmins | Services\BackupService.cs | 313 | foreach (var ba in await db.BuildingAdmins.AsNoTracking().ToListAsync()) |
| Floors | Services\BackupService.cs | 316 | case "floors": |
| Floors | Services\BackupService.cs | 318 | foreach (var f in await db.Floors.AsNoTracking().ToListAsync()) |
| Apartments | Services\BackupService.cs | 321 | case "apartments": |
| Apartments | Services\BackupService.cs | 323 | foreach (var a in await db.Apartments.AsNoTracking().ToListAsync()) |
| Residents | Services\BackupService.cs | 326 | case "residents": |
| Residents | Services\BackupService.cs | 327 | foreach (var r in await db.Residents.AsNoTracking().ToListAsync()) |
| FinancialCategories | Services\BackupService.cs | 330 | case "financialCategories": |
| FinancialCategories | Services\BackupService.cs | 332 | foreach (var c in await db.FinancialCategories.AsNoTracking().ToListAsync()) |
| Expenses | Services\BackupService.cs | 335 | case "expenses": |
| Expenses | Services\BackupService.cs | 337 | foreach (var e in await db.Expenses.AsNoTracking().ToListAsync()) |
| Revenues | Services\BackupService.cs | 340 | case "revenues": |
| Revenues | Services\BackupService.cs | 342 | foreach (var r in await db.Revenues.AsNoTracking().ToListAsync()) |
| MonthlyApartmentShares | Services\BackupService.cs | 345 | case "monthlyApartmentShares": |
| MonthlyApartmentShares | Services\BackupService.cs | 347 | foreach (var s in await db.MonthlyApartmentShares.AsNoTracking().ToListAsync()) |
| Deposits | Services\BackupService.cs | 350 | case "deposits": |
| Deposits | Services\BackupService.cs | 352 | foreach (var d in await db.Deposits.AsNoTracking().ToListAsync()) |
| DepositEditHistory | Services\BackupService.cs | 355 | case "depositEditHistory": |
| DepositEditHistory | Services\BackupService.cs | 356 | foreach (var de in await db.DepositEditHistory.AsNoTracking().ToListAsync()) |
| WalletAdjustments | Services\BackupService.cs | 359 | case "walletAdjustments": |
| WalletAdjustments | Services\BackupService.cs | 361 | foreach (var w in await db.WalletAdjustments.AsNoTracking().ToListAsync()) |
| UserPermissions | Services\BackupService.cs | 364 | case "userPermissions": |
| UserPermissions | Services\BackupService.cs | 366 | foreach (var p in await db.UserPermissions.AsNoTracking().ToListAsync()) |
| AuditLog | Services\BackupService.cs | 369 | case "auditLog": |
| AuditLog | Services\BackupService.cs | 370 | foreach (var a in await db.AuditLog.AsNoTracking().ToListAsync()) |
| Notifications | Services\BackupService.cs | 373 | case "notifications": |
| Notifications | Services\BackupService.cs | 375 | foreach (var n in await db.Notifications.AsNoTracking().ToListAsync()) |
| Polls | Services\BackupService.cs | 378 | case "polls": |
| Polls | Services\BackupService.cs | 380 | foreach (var p in await db.Polls.AsNoTracking().ToListAsync()) |
| PollOptions | Services\BackupService.cs | 383 | case "pollOptions": |
| PollOptions | Services\BackupService.cs | 385 | foreach (var o in await db.PollOptions.AsNoTracking().ToListAsync()) |
| PollVotes | Services\BackupService.cs | 388 | case "pollVotes": |
| PollVotes | Services\BackupService.cs | 390 | foreach (var v in await db.PollVotes.AsNoTracking().ToListAsync()) |
| MaintenanceRecords | Services\BackupService.cs | 393 | case "maintenanceRecords": |
| MaintenanceRecords | Services\BackupService.cs | 395 | foreach (var m in await db.MaintenanceRecords.AsNoTracking().ToListAsync()) |
| Dvrs | Services\BackupService.cs | 398 | case "dvrs": |
| Dvrs | Services\BackupService.cs | 399 | foreach (var d in await db.Dvrs.AsNoTracking().ToListAsync()) |
| Cameras | Services\BackupService.cs | 402 | case "cameras": |
| Cameras | Services\BackupService.cs | 403 | foreach (var c in await db.Cameras.AsNoTracking().ToListAsync()) |
| QrTokens | Services\BackupService.cs | 406 | case "qrTokens": |
| QrTokens | Services\BackupService.cs | 407 | foreach (var q in await db.QrTokens.AsNoTracking().ToListAsync()) |
| QrUsages | Services\BackupService.cs | 410 | case "qrUsages": |
| QrUsages | Services\BackupService.cs | 411 | foreach (var q in await db.QrUsages.AsNoTracking().ToListAsync()) |
| GlobalSettings | Services\BackupService.cs | 524 | "globalSettings", |
| ScheduledBackupSettings | Services\BackupService.cs | 525 | "scheduledBackupSettings", |
| Users | Services\BackupService.cs | 526 | "users", |
| Buildings | Services\BackupService.cs | 527 | "buildings", |
| BuildingSettings | Services\BackupService.cs | 528 | "buildingSettings", |
| BuildingAdmins | Services\BackupService.cs | 529 | "buildingAdmins", |
| Floors | Services\BackupService.cs | 530 | "floors", |
| Apartments | Services\BackupService.cs | 531 | "apartments", |
| Residents | Services\BackupService.cs | 532 | "residents", |
| FinancialCategories | Services\BackupService.cs | 533 | "financialCategories", |
| Expenses | Services\BackupService.cs | 534 | "expenses", |
| Revenues | Services\BackupService.cs | 535 | "revenues", |
| MonthlyApartmentShares | Services\BackupService.cs | 536 | "monthlyApartmentShares", |
| Deposits | Services\BackupService.cs | 537 | "deposits", |
| DepositEditHistory | Services\BackupService.cs | 538 | "depositEditHistory", |
| WalletAdjustments | Services\BackupService.cs | 539 | "walletAdjustments", |
| UserPermissions | Services\BackupService.cs | 540 | "userPermissions", |
| AuditLog | Services\BackupService.cs | 541 | "auditLog", |
| Notifications | Services\BackupService.cs | 542 | "notifications", |
| Polls | Services\BackupService.cs | 543 | "polls", |
| PollOptions | Services\BackupService.cs | 544 | "pollOptions", |
| PollVotes | Services\BackupService.cs | 545 | "pollVotes", |
| MaintenanceRecords | Services\BackupService.cs | 546 | "maintenanceRecords", |
| Dvrs | Services\BackupService.cs | 547 | "dvrs", |
| Cameras | Services\BackupService.cs | 548 | "cameras", |
| QrTokens | Services\BackupService.cs | 549 | "qrTokens", |
| QrUsages | Services\BackupService.cs | 550 | "qrUsages" |
| GlobalSettings | Services\BackupService.cs | 738 | case "globalSettings": |
| GlobalSettings | Services\BackupService.cs | 741 | var existing = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == id); |
| GlobalSettings | Services\BackupService.cs | 748 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.GlobalSettings, existing, entity); } |
| ScheduledBackupSettings | Services\BackupService.cs | 752 | case "scheduledBackupSettings": |
| ScheduledBackupSettings | Services\BackupService.cs | 754 | var existing = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == id); |
| ScheduledBackupSettings | Services\BackupService.cs | 761 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.ScheduledBackupSettings, existing, entity); } |
| Users | Services\BackupService.cs | 765 | case "users": |
| Users | Services\BackupService.cs | 767 | var existing = await db.Users.FirstOrDefaultAsync(x => x.Uid == id); |
| Users | Services\BackupService.cs | 774 | if (entity != null) { entity.Uid = id; ApplyOrAdd(db, db.Users, existing, entity); } |
| Buildings | Services\BackupService.cs | 778 | case "buildings": |
| Buildings | Services\BackupService.cs | 780 | var existing = await db.Buildings.FirstOrDefaultAsync(x => x.Id == id); |
| Buildings | Services\BackupService.cs | 787 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Buildings, existing, entity); } |
| BuildingSettings | Services\BackupService.cs | 791 | case "buildingSettings": |
| BuildingSettings | Services\BackupService.cs | 793 | var existing = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == id); |
| BuildingSettings | Services\BackupService.cs | 800 | if (entity != null) { entity.BuildingId = id; ApplyOrAdd(db, db.BuildingSettings, existing, entity); } |
| BuildingAdmins | Services\BackupService.cs | 804 | case "buildingAdmins": |
| BuildingAdmins | Services\BackupService.cs | 812 | var existing = await db.BuildingAdmins |
| BuildingAdmins | Services\BackupService.cs | 824 | ApplyOrAdd(db, db.BuildingAdmins, existing, entity); |
| Floors | Services\BackupService.cs | 829 | case "floors": |
| Floors | Services\BackupService.cs | 837 | var existing = await db.Floors |
| Floors | Services\BackupService.cs | 849 | ApplyOrAdd(db, db.Floors, existing, entity); |
| Apartments | Services\BackupService.cs | 854 | case "apartments": |
| Apartments | Services\BackupService.cs | 862 | var existing = await db.Apartments |
| Apartments | Services\BackupService.cs | 874 | ApplyOrAdd(db, db.Apartments, existing, entity); |
| Residents | Services\BackupService.cs | 879 | case "residents": |
| Residents | Services\BackupService.cs | 881 | var existing = await db.Residents.FirstOrDefaultAsync(x => x.Id == id); |
| Residents | Services\BackupService.cs | 888 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Residents, existing, entity); } |
| FinancialCategories | Services\BackupService.cs | 892 | case "financialCategories": |
| FinancialCategories | Services\BackupService.cs | 901 | var existing = await db.FinancialCategories |
| FinancialCategories | Services\BackupService.cs | 914 | ApplyOrAdd(db, db.FinancialCategories, existing, entity); |
| Expenses | Services\BackupService.cs | 919 | case "expenses": |
| Expenses | Services\BackupService.cs | 927 | var existing = await db.Expenses |
| Expenses | Services\BackupService.cs | 939 | ApplyOrAdd(db, db.Expenses, existing, entity); |
| Revenues | Services\BackupService.cs | 944 | case "revenues": |
| Revenues | Services\BackupService.cs | 952 | var existing = await db.Revenues |
| Revenues | Services\BackupService.cs | 964 | ApplyOrAdd(db, db.Revenues, existing, entity); |
| MonthlyApartmentShares | Services\BackupService.cs | 969 | case "monthlyApartmentShares": |
| MonthlyApartmentShares | Services\BackupService.cs | 978 | var existing = await db.MonthlyApartmentShares |
| MonthlyApartmentShares | Services\BackupService.cs | 991 | ApplyOrAdd(db, db.MonthlyApartmentShares, existing, entity); |
| Deposits | Services\BackupService.cs | 996 | case "deposits": |
| Deposits | Services\BackupService.cs | 1004 | var existing = await db.Deposits |
| Deposits | Services\BackupService.cs | 1016 | ApplyOrAdd(db, db.Deposits, existing, entity); |
| DepositEditHistory | Services\BackupService.cs | 1021 | case "depositEditHistory": |
| DepositEditHistory | Services\BackupService.cs | 1024 | var existing = await db.DepositEditHistory.FirstOrDefaultAsync(x => x.EditId == long.Parse(id)); |
| DepositEditHistory | Services\BackupService.cs | 1031 | if (entity != null) { if (existing == null) entity.EditId = 0; ApplyOrAdd(db, db.DepositEditHistory, existing, entity); } |
| WalletAdjustments | Services\BackupService.cs | 1035 | case "walletAdjustments": |
| WalletAdjustments | Services\BackupService.cs | 1043 | var existing = await db.WalletAdjustments |
| WalletAdjustments | Services\BackupService.cs | 1055 | ApplyOrAdd(db, db.WalletAdjustments, existing, entity); |
| UserPermissions | Services\BackupService.cs | 1060 | case "userPermissions": |
| UserPermissions | Services\BackupService.cs | 1068 | var existing = await db.UserPermissions |
| UserPermissions | Services\BackupService.cs | 1080 | ApplyOrAdd(db, db.UserPermissions, existing, entity); |
| AuditLog | Services\BackupService.cs | 1085 | case "auditLog": |
| AuditLog | Services\BackupService.cs | 1089 | if (entity != null) { entity.AuditId = 0; db.AuditLog.Add(entity); }   // الـ id بيتولّد من الداتابيز (identity) |
| Notifications | Services\BackupService.cs | 1093 | case "notifications": |
| Notifications | Services\BackupService.cs | 1101 | var existing = await db.Notifications |
| Notifications | Services\BackupService.cs | 1113 | ApplyOrAdd(db, db.Notifications, existing, entity); |
| Polls | Services\BackupService.cs | 1118 | case "polls": |
| Polls | Services\BackupService.cs | 1126 | var existing = await db.Polls |
| Polls | Services\BackupService.cs | 1138 | ApplyOrAdd(db, db.Polls, existing, entity); |
| PollOptions | Services\BackupService.cs | 1143 | case "pollOptions": |
| PollOptions | Services\BackupService.cs | 1152 | var existing = await db.PollOptions |
| PollOptions | Services\BackupService.cs | 1165 | ApplyOrAdd(db, db.PollOptions, existing, entity); |
| PollVotes | Services\BackupService.cs | 1170 | case "pollVotes": |
| PollVotes | Services\BackupService.cs | 1179 | var existing = await db.PollVotes |
| PollVotes | Services\BackupService.cs | 1192 | ApplyOrAdd(db, db.PollVotes, existing, entity); |
| MaintenanceRecords | Services\BackupService.cs | 1197 | case "maintenanceRecords": |
| MaintenanceRecords | Services\BackupService.cs | 1205 | var existing = await db.MaintenanceRecords |
| MaintenanceRecords | Services\BackupService.cs | 1217 | ApplyOrAdd(db, db.MaintenanceRecords, existing, entity); |
| Dvrs | Services\BackupService.cs | 1222 | case "dvrs": |
| Dvrs | Services\BackupService.cs | 1224 | var existing = await db.Dvrs.FirstOrDefaultAsync(x => x.Id == id); |
| Dvrs | Services\BackupService.cs | 1231 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Dvrs, existing, entity); } |
| Cameras | Services\BackupService.cs | 1235 | case "cameras": |
| Cameras | Services\BackupService.cs | 1237 | var existing = await db.Cameras.FirstOrDefaultAsync(x => x.Id == id); |
| Cameras | Services\BackupService.cs | 1244 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Cameras, existing, entity); } |
| QrTokens | Services\BackupService.cs | 1248 | case "qrTokens": |
| QrTokens | Services\BackupService.cs | 1250 | var existing = await db.QrTokens.FirstOrDefaultAsync(x => x.Uid == id); |
| QrTokens | Services\BackupService.cs | 1257 | if (entity != null) { entity.Uid = id; ApplyOrAdd(db, db.QrTokens, existing, entity); } |
| QrUsages | Services\BackupService.cs | 1261 | case "qrUsages": |
| QrUsages | Services\BackupService.cs | 1263 | var existing = await db.QrUsages.FirstOrDefaultAsync(x => x.Id == id); |
| QrUsages | Services\BackupService.cs | 1270 | if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.QrUsages, existing, entity); } |
| Floors | Services\BuildingMapService.cs | 8 | /// Builds the building map view models (floors, apartment cards, statistics and apartment details). |
| Presence | Services\BuildingMapService.cs | 18 | public BuildingMapService(WalletService wallet, PresenceService presence) |
| Presence | Services\BuildingMapService.cs | 21 | _presence = presence; |
| Presence | Services\BuildingMapService.cs | 35 | // 1. جيب الـ Presence للعمارة |
| Floors | Services\BuildingMapService.cs | 41 | FloorsCount = building.Floors.Count, |
| Apartments | Services\BuildingMapService.cs | 42 | AptsCount = building.Apartments.Count, |
| Apartments | Services\BuildingMapService.cs | 43 | OpenAptsCount = building.Apartments.Count(a => !a.Closed), |
| Apartments | Services\BuildingMapService.cs | 44 | ClosedAptsCount = building.Apartments.Count(a => a.Closed) |
| Floors | Services\BuildingMapService.cs | 48 | foreach (var floor in building.Floors.OrderBy(f => f.Order)) |
| Apartments | Services\BuildingMapService.cs | 57 | var apts = building.Apartments |
| Apartments | Services\BuildingMapService.cs | 65 | floorVm.Apartments.Add(card); |
| Floors | Services\BuildingMapService.cs | 75 | vm.Floors.Add(floorVm); |
| Presence | Services\BuildingMapService.cs | 89 | Dictionary<string, Presence> presenceMap) |
| Deposits | Services\BuildingMapService.cs | 98 | pending = m.Deposits.Count(d => d.AptId == apt.Id && d.Status == "pending"); |
| Presence | Services\BuildingMapService.cs | 110 | // Presence |
| Presence | Services\BuildingMapService.cs | 111 | if (presenceMap.TryGetValue(apt.Id, out var presence)) |
| Presence | Services\BuildingMapService.cs | 113 | card.IsOnline = PresenceService.IsOnline(presence); |
| Presence | Services\BuildingMapService.cs | 114 | card.IsRecent = PresenceService.IsRecent(presence); |
| Presence | Services\BuildingMapService.cs | 115 | card.LastSeenText = PresenceService.TimeAgo(presence); |
| Apartments | Services\BuildingMapService.cs | 196 | var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId); |
| Floors | Services\BuildingMapService.cs | 199 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Deposits | Services\BuildingMapService.cs | 232 | var aptDeposits = m.Deposits.Where(d => d.AptId == apt.Id).ToList(); |
| Presence | Services\BuildingMapService.cs | 256 | // Presence |
| Presence | Services\BuildingMapService.cs | 258 | if (presenceMap.TryGetValue(apt.Id, out var presence)) |
| Presence | Services\BuildingMapService.cs | 260 | vm.IsOnline = PresenceService.IsOnline(presence); |
| Presence | Services\BuildingMapService.cs | 261 | vm.IsRecent = PresenceService.IsRecent(presence); |
| Presence | Services\BuildingMapService.cs | 262 | vm.LastSeenText = PresenceService.TimeAgo(presence); |
| Apartments | Services\BuildingsService.cs | 24 | /// Business logic for buildings, floors and apartments (create, update, delete, settings). |
| Buildings | Services\BuildingsService.cs | 24 | /// Business logic for buildings, floors and apartments (create, update, delete, settings). |
| Floors | Services\BuildingsService.cs | 24 | /// Business logic for buildings, floors and apartments (create, update, delete, settings). |
| Apartments | Services\BuildingsService.cs | 80 | /// استعلام واحد. مفيدة للـ Views اللي بتستخدم <c>@b.Apartments.Count</c> |
| Floors | Services\BuildingsService.cs | 81 | /// و <c>@b.Floors.Count</c>. |
| AuditLog | Services\BuildingsService.cs | 172 | /// - AuditLog: Append-only (بيضيف بس) |
| Apartments | Services\BuildingsService.cs | 187 | public async Task<string> AddFloorAsync(string buildingId, string label, List<(string Phone, string Pin)> apartments) |
| Floors | Services\BuildingsService.cs | 192 | var maxOrder = building.Floors.Count == 0 ? -1 : building.Floors.Max(f => f.Order); |
| Floors | Services\BuildingsService.cs | 195 | building.Floors.Add(new Floor { Id = floorId, Label = label, Order = floorOrder }); |
| Apartments | Services\BuildingsService.cs | 197 | var number = building.Apartments.Count == 0 |
| Apartments | Services\BuildingsService.cs | 199 | : building.Apartments.Max(a => a.Number) + 1; |
| Apartments | Services\BuildingsService.cs | 201 | foreach (var (phoneRaw, pin) in apartments) |
| Apartments | Services\BuildingsService.cs | 204 | building.Apartments.Add(new Apartment |
| Floors | Services\BuildingsService.cs | 231 | var floor = building.Floors.FirstOrDefault(f => f.Id == floorId) |
| Apartments | Services\BuildingsService.cs | 234 | var nextNum = building.Apartments.Count == 0 |
| Apartments | Services\BuildingsService.cs | 236 | : building.Apartments.Max(a => a.Number) + 1; |
| Apartments | Services\BuildingsService.cs | 239 | building.Apartments.Add(new Apartment |
| Floors | Services\BuildingsService.cs | 286 | building.Floors = building.Floors.OrderBy(f => f.Order).ToList(); |
| Floors | Services\BuildingsService.cs | 287 | var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order); |
| Apartments | Services\BuildingsService.cs | 288 | building.Apartments = building.Apartments |
| Apartments | Services\BuildingsService.cs | 308 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId) |
| Buildings | Services\BuildingWriteLockFilter.cs | 7 | // ط§ظ„ط­ظ…ط§ظٹط© ط¹ط¨ط± ط£ظƒط«ط± ظ…ظ† application instance طھطھظ… ط¨ط´ظƒظ„ ظ…ط³طھظ‚ظ„ ط¹ظ† ط·ط±ظٹظ‚ Buildings.RowVersion |
| Buildings | Services\CategoriesService.cs | 17 | public CategoriesService(BuildingsService buildings, IAuditLogger audit) |
| Buildings | Services\CategoriesService.cs | 19 | _buildings = buildings; |
| Expenses | Services\CategoriesService.cs | 165 | usedCount += month.Expenses.Count(e => e.CategoryId == id); |
| Revenues | Services\CategoriesService.cs | 167 | usedCount += month.Revenues.Count(r => r.CategoryId == id); |
| Buildings | Services\CloudinaryService.cs | 55 | Folder = $"buildings/{buildingId}/receipts", |
| Cameras | Services\DvrService.cs | 9 | /// Business logic for DVR devices and cameras, including encryption of stored credentials. |
| Dvrs | Services\DvrService.cs | 36 | return await db.Dvrs |
| Cameras | Services\DvrService.cs | 37 | .Include(d => d.Cameras) |
| Dvrs | Services\DvrService.cs | 51 | return await db.Dvrs |
| Cameras | Services\DvrService.cs | 52 | .Include(d => d.Cameras) |
| Dvrs | Services\DvrService.cs | 76 | db.Dvrs.Add(new DvrEntity |
| Dvrs | Services\DvrService.cs | 109 | var entity = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == id) |
| Dvrs | Services\DvrService.cs | 132 | await db.Dvrs.Where(d => d.Id == id).ExecuteDeleteAsync(); |
| Dvrs | Services\DvrService.cs | 146 | var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId) |
| Cameras | Services\DvrService.cs | 150 | db.Cameras.Add(new CameraEntity |
| Cameras | Services\DvrService.cs | 171 | await db.Cameras.Where(c => c.Id == cameraId).ExecuteDeleteAsync(); |
| Dvrs | Services\DvrService.cs | 180 | var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId) |
| Cameras | Services\DvrService.cs | 200 | /// Result of synchronizing DVR devices and their cameras with the media server. |
| Dvrs | Services\DvrService.cs | 205 | /// Gets or sets the dvrs added. |
| Dvrs | Services\DvrService.cs | 209 | /// Gets or sets the dvrs updated. |
| Cameras | Services\DvrService.cs | 213 | /// Gets or sets the cameras added. |
| Cameras | Services\DvrService.cs | 217 | /// Gets or sets the cameras updated. |
| Dvrs | Services\DvrService.cs | 229 | /// Gets the dvrs total. |
| Cameras | Services\DvrService.cs | 233 | /// Gets the cameras total. |
| Apartments | Services\ExcelImportService.cs | 67 | var requiredSheets = new[] { "Building Info", "Floors & Apartments" }; |
| Floors | Services\ExcelImportService.cs | 67 | var requiredSheets = new[] { "Building Info", "Floors & Apartments" }; |
| Apartments | Services\ExcelImportService.cs | 79 | if (data.Apartments.Count == 0) |
| Apartments | Services\ExcelImportService.cs | 82 | if (data.Apartments.Any(a => !string.IsNullOrWhiteSpace(a.Pin) && |
| Apartments | Services\ExcelImportService.cs | 110 | // ============ Floors & Apartments ============ |
| Floors | Services\ExcelImportService.cs | 110 | // ============ Floors & Apartments ============ |
| Apartments | Services\ExcelImportService.cs | 111 | var aptsSheet = workbook.Worksheet("Floors & Apartments"); |
| Floors | Services\ExcelImportService.cs | 111 | var aptsSheet = workbook.Worksheet("Floors & Apartments"); |
| Apartments | Services\ExcelImportService.cs | 174 | data.Apartments.Add(apt); |
| Buildings | Services\ExcelImportService.cs | 221 | var existingNumbers = await db.Buildings.Select(b => b.BuildingNumber).ToListAsync(); |
| Apartments | Services\ExcelImportService.cs | 298 | /// Gets or sets the apartments. |
| Apartments | Services\ExcelImportService.cs | 300 | public List<ApartmentImportData> Apartments { get; set; } = new(); |
| Apartments | Services\ExcelTemplateService.cs | 97 | // ============ 3. Floors & Apartments ============ |
| Floors | Services\ExcelTemplateService.cs | 97 | // ============ 3. Floors & Apartments ============ |
| Apartments | Services\ExcelTemplateService.cs | 98 | var aptsSheet = workbook.Worksheets.Add("Floors & Apartments"); |
| Floors | Services\ExcelTemplateService.cs | 98 | var aptsSheet = workbook.Worksheets.Add("Floors & Apartments"); |
| Apartments | Services\ExcelTemplateService.cs | 238 | helpSheet.Cell(5, 1).Value = "2. املأ ورقة Floors & Apartments من الصف 6 (الصفوف 2-5 توضيحية)"; |
| Floors | Services\ExcelTemplateService.cs | 238 | helpSheet.Cell(5, 1).Value = "2. املأ ورقة Floors & Apartments من الصف 6 (الصفوف 2-5 توضيحية)"; |
| Expenses | Services\ExpensesService.cs | 6 | // طھط±ط¬ظ…ط© ط­ط±ظپظٹط© ظ„ظ€ js/core/expenses.js |
| Expenses | Services\ExpensesService.cs | 8 | /// Business logic for monthly expenses. |
| Expenses | Services\ExpensesService.cs | 37 | m.Expenses.Add(expense); |
| Expenses | Services\ExpensesService.cs | 47 | var before = m.Expenses.Count; |
| Expenses | Services\ExpensesService.cs | 48 | m.Expenses = m.Expenses.Where(e => e.Id != expenseId).ToList(); |
| Expenses | Services\ExpensesService.cs | 49 | return before != m.Expenses.Count; |
| Expenses | Services\ExpensesService.cs | 53 | /// Returns the total expenses of month. |
| Expenses | Services\ExpensesService.cs | 56 | building.Months.TryGetValue(monthKey, out var m) ? m.Expenses.Sum(e => e.Amount) : 0; |
| Deposits | Services\InvoiceService.cs | 8 | /// Builds invoice data (balances, shares, deposits) for an apartment and month. |
| Floors | Services\InvoiceService.cs | 23 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Apartments | Services\InvoiceService.cs | 32 | var openCount = building.Apartments.Count(a => !a.Closed); |
| Expenses | Services\InvoiceService.cs | 35 | var total = m.Expenses.Where(e => e.CategoryId == cat.Id).Sum(e => e.Amount); |
| Residents | Services\LegacyIdentityMappingService.cs | 51 | /// <param name="sourceTable">The exact legacy table name: Users or Residents.</param> |
| Users | Services\LegacyIdentityMappingService.cs | 51 | /// <param name="sourceTable">The exact legacy table name: Users or Residents.</param> |
| AuditLog | Services\LegacyIdentityMappingService.cs | 85 | db.AuditLog.Add(CreateAudit(nowUtc, reviewerId, "identity.mapping.candidate.registered", new |
| Residents | Services\LegacyIdentityMappingService.cs | 119 | if (map.SourceSystem != SourceSystem \|\| map.SourceTable is not ("Users" or "Residents")) |
| Users | Services\LegacyIdentityMappingService.cs | 119 | if (map.SourceSystem != SourceSystem \|\| map.SourceTable is not ("Users" or "Residents")) |
| AuditLog | Services\LegacyIdentityMappingService.cs | 135 | db.AuditLog.Add(CreateAudit(nowUtc, reviewerId, "identity.mapping.reviewed", new |
| Users | Services\LegacyIdentityMappingService.cs | 181 | if (sourceTable != "Users") |
| Residents | Services\LegacyIdentityMappingService.cs | 222 | if (sourceTable is not ("Users" or "Residents")) |
| Users | Services\LegacyIdentityMappingService.cs | 222 | if (sourceTable is not ("Users" or "Residents")) |
| Residents | Services\LegacyIdentityMappingService.cs | 223 | throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable)); |
| Users | Services\LegacyIdentityMappingService.cs | 223 | throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable)); |
| Residents | Services\LegacyIdentityMappingService.cs | 255 | if (sourceTable is not ("Users" or "Residents")) |
| Users | Services\LegacyIdentityMappingService.cs | 255 | if (sourceTable is not ("Users" or "Residents")) |
| Residents | Services\LegacyIdentityMappingService.cs | 256 | throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable)); |
| Users | Services\LegacyIdentityMappingService.cs | 256 | throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable)); |
| Users | Services\LegacyIdentityMappingService.cs | 262 | if (sourceTable == "Users" && request.AccountId is null) |
| Users | Services\LegacyIdentityMappingService.cs | 263 | throw new ArgumentException("A Users source row must resolve to both Person and UserAccount.", nameof(request)); |
| Residents | Services\LegacyIdentityMappingService.cs | 264 | if (sourceTable == "Residents" && request.AccountId is not null) |
| Residents | Services\LegacyIdentityMappingService.cs | 265 | throw new ArgumentException("A Residents source row resolves to Person only; it does not create a login account.", nameof(request)); |
| LoginThrottle | Services\LoginThrottle.cs | 9 | /// Persistence is intentionally disabled until LoginThrottle has an EF-managed |
| LoginThrottle | Services\LoginThrottle.cs | 12 | public sealed class LoginThrottle |
| LoginThrottle | Services\LoginThrottle.cs | 27 | public LoginThrottle(IMemoryCache cache) |
| Expenses | Services\MaintenanceService.cs | 89 | /// Adds the cost to expenses. |
| Expenses | Services\MaintenanceService.cs | 110 | m.Expenses.Add(expense); |
| Notifications | Services\NotificationsService.cs | 5 | // ترجمة حرفية لـ js/core/notifications.js (إشعارات داخل التطبيق) |
| Notifications | Services\NotificationsService.cs | 7 | /// Creates and reads in-app notifications. |
| Notifications | Services\NotificationsService.cs | 12 | /// Pushes the operation on the notifications. |
| Notifications | Services\NotificationsService.cs | 17 | building.Notifications ??= new List<Notification>(); |
| Notifications | Services\NotificationsService.cs | 32 | building.Notifications.Add(n); |
| Notifications | Services\NotificationsService.cs | 35 | if (building.Notifications.Count > 200) |
| Notifications | Services\NotificationsService.cs | 36 | building.Notifications = building.Notifications.Skip(building.Notifications.Count - 200).ToList(); |
| Notifications | Services\NotificationsService.cs | 45 | building.Notifications.Where(n => n.RecipientType == "admin") |
| Notifications | Services\NotificationsService.cs | 52 | building.Notifications.Where(n => n.RecipientType == "resident" && n.RecipientId == aptId) |
| Notifications | Services\NotificationsService.cs | 59 | building.Notifications.Count(n => n.RecipientType == "admin" && !n.Read); |
| Notifications | Services\NotificationsService.cs | 65 | building.Notifications.Count(n => n.RecipientType == "resident" && n.RecipientId == aptId && !n.Read); |
| Notifications | Services\NotificationsService.cs | 72 | var n = building.Notifications.FirstOrDefault(x => x.Id == notifId && |
| Notifications | Services\NotificationsService.cs | 86 | foreach (var n in building.Notifications.Where(n => n.RecipientType == "admin" && !n.Read)) |
| Notifications | Services\NotificationsService.cs | 98 | foreach (var n in building.Notifications.Where(n => n.RecipientType == "resident" && n.RecipientId == aptId && !n.Read)) |
| Residents | Services\NotificationsService.cs | 128 | /// Notifies the residents new expense. |
| Apartments | Services\NotificationsService.cs | 132 | foreach (var apt in building.Apartments.Where(a => !a.Closed)) |
| Polls | Services\PollsService.cs | 5 | // طھط±ط¬ظ…ط© ط­ط±ظپظٹط© ظ„ظ€ js/features/polls.js |
| Polls | Services\PollsService.cs | 7 | /// Business logic for polls and votes. |
| Polls | Services\PollsService.cs | 12 | /// Gets the polls. |
| Polls | Services\PollsService.cs | 15 | building.Polls.OrderByDescending(p => p.CreatedAt).ToList(); |
| Polls | Services\PollsService.cs | 64 | building.Polls.Add(poll); |
| Polls | Services\PollsService.cs | 70 | /// Records a vote on the operation on the polls. |
| Polls | Services\PollsService.cs | 74 | var poll = building.Polls.FirstOrDefault(p => p.Id == pollId); |
| Polls | Services\PollsService.cs | 88 | var poll = building.Polls.FirstOrDefault(p => p.Id == pollId); |
| Polls | Services\PollsService.cs | 95 | /// Deletes the operation on the polls. |
| Polls | Services\PollsService.cs | 99 | var before = building.Polls.Count; |
| Polls | Services\PollsService.cs | 100 | building.Polls = building.Polls.Where(p => p.Id != pollId).ToList(); |
| Polls | Services\PollsService.cs | 101 | return before != building.Polls.Count; |
| Users | Services\PresenceService.cs | 9 | /// Tracks and reports which users are online. |
| Presence | Services\PresenceService.cs | 52 | var existing = await db.Presence.FirstOrDefaultAsync(p => p.Uid == docId); |
| Presence | Services\PresenceService.cs | 56 | db.Presence.Add(new PresenceEntity |
| Presence | Services\PresenceService.cs | 75 | _logger.LogWarning(ex, "[Presence] Heartbeat failed for {DocId}", docId); |
| Presence | Services\PresenceService.cs | 88 | return await db.Presence |
| Presence | Services\PresenceService.cs | 93 | _logger.LogWarning(ex, "[Presence] GetOnlineCount failed"); |
| Presence | Services\PresenceService.cs | 101 | public async Task<Dictionary<string, Presence>> GetByBuildingAsync(string buildingId) |
| Presence | Services\PresenceService.cs | 103 | var result = new Dictionary<string, Presence>(); |
| Presence | Services\PresenceService.cs | 108 | var list = await db.Presence.AsNoTracking() |
| Presence | Services\PresenceService.cs | 117 | result[aptId] = new Presence |
| Presence | Services\PresenceService.cs | 129 | _logger.LogWarning(ex, "[Presence] GetByBuilding failed"); |
| Presence | Services\PresenceService.cs | 138 | public static bool IsOnline(Presence? p) |
| Presence | Services\PresenceService.cs | 148 | public static bool IsRecent(Presence? p) |
| Presence | Services\PresenceService.cs | 158 | public static string TimeAgo(Presence? p) |
| Presence | Services\PresenceService.cs | 180 | return await db.Presence |
| Presence | Services\PresenceService.cs | 186 | _logger.LogWarning(ex, "[Presence] ClearOld failed"); |
| Apartments | Services\QrCodePdfService.cs | 59 | col.Item().Text(Loc.T("Apartments_N", building.Apartments.Count)).FontSize(14).Bold(); |
| Floors | Services\QrCodePdfService.cs | 61 | foreach (var floor in building.Floors.OrderBy(f => f.Order)) |
| Apartments | Services\QrCodePdfService.cs | 63 | var apts = building.Apartments |
| QrUsages | Services\QrSecurityService.cs | 38 | /// <param name="sqlFactory">Factory لفتح DbContext (لاستخدام QrUsages).</param> |
| QrUsages | Services\QrSecurityService.cs | 151 | var usage = await db.QrUsages.AsNoTracking() |
| QrUsages | Services\QrSecurityService.cs | 192 | var updated = await db.QrUsages |
| QrUsages | Services\QrSecurityService.cs | 199 | if (await db.QrUsages.AnyAsync(x => x.TokenId == p.TokenId)) return false; |
| QrUsages | Services\QrSecurityService.cs | 203 | db.QrUsages.Add(new QrUsageEntity |
| QrUsages | Services\QrSecurityService.cs | 219 | updated = await db.QrUsages |
| QrUsages | Services\QrSecurityService.cs | 341 | /// <summary>معرّف فريد للتوكن (يُستخدم كـ PK في QrUsages).</summary> |
| QrTokens | Services\QrTokenStoreService.cs | 34 | db.QrTokens.Add(new QrTokenEntity |
| QrTokens | Services\QrTokenStoreService.cs | 65 | var token = await db.QrTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Uid == uid); |
| QrTokens | Services\QrTokenStoreService.cs | 71 | var affected = await db.QrTokens |
| QrTokens | Services\QrTokenStoreService.cs | 80 | var affected = await db.QrTokens.Where(t => t.Uid == uid && t.ExpiresAt >= DateTime.UtcNow) |
| QrTokens | Services\QrTokenStoreService.cs | 99 | await db.QrTokens.Where(t => t.Uid == uid).ExecuteDeleteAsync(); |
| Apartments | Services\ReportsService.cs | 82 | foreach (var a in building.Apartments) collected += _wallet.TotalConfirmedDeposits(building, a.Id, mk); |
| Expenses | Services\ReportsService.cs | 84 | decimal expenses = 0, revenues = 0; |
| Revenues | Services\ReportsService.cs | 84 | decimal expenses = 0, revenues = 0; |
| Expenses | Services\ReportsService.cs | 87 | expenses = m.Expenses.Sum(e => e.Amount); |
| Revenues | Services\ReportsService.cs | 88 | revenues = m.Revenues.Sum(r => r.Amount); |
| Expenses | Services\ReportsService.cs | 91 | totalCollected += collected; totalExpenses += expenses; totalRevenues += revenues; |
| Revenues | Services\ReportsService.cs | 91 | totalCollected += collected; totalExpenses += expenses; totalRevenues += revenues; |
| Expenses | Services\ReportsService.cs | 92 | monthly.Add(new MonthlyPoint { Month = mk, Label = MonthLabel(mk), Collected = collected, Revenues = revenues, Expenses = expenses }); |
| Revenues | Services\ReportsService.cs | 92 | monthly.Add(new MonthlyPoint { Month = mk, Label = MonthLabel(mk), Collected = collected, Revenues = revenues, Expenses = expenses }); |
| Expenses | Services\ReportsService.cs | 106 | foreach (var e in m.Expenses) |
| Apartments | Services\ReportsService.cs | 120 | var wallet = building.Apartments.Where(a => !a.Closed) |
| Apartments | Services\ReportsService.cs | 125 | var allBalances = building.Apartments |
| Residents | Services\ResidentsService.cs | 43 | var residents = await db.Residents |
| Residents | Services\ResidentsService.cs | 47 | if (residents.Count == 0) |
| Apartments | Services\ResidentsService.cs | 49 | var apt = await db.Apartments.AsNoTracking() |
| Residents | Services\ResidentsService.cs | 53 | db.Residents.Add(new BuildingManagementMvc.Data.Entities.ResidentEntity |
| Residents | Services\ResidentsService.cs | 73 | foreach (var res in residents) res.PinHash = hashed; |
| Residents | Services\ResidentsService.cs | 87 | var residents = await db.Residents |
| Residents | Services\ResidentsService.cs | 91 | if (residents.Any()) |
| Residents | Services\ResidentsService.cs | 94 | foreach (var res in residents) |
| Residents | Services\ResidentsService.cs | 112 | return await db.Residents |
| Residents | Services\ResidentsService.cs | 124 | return await db.Residents |
| Residents | Services\ResidentsService.cs | 135 | return await db.Residents.FirstOrDefaultAsync(r => r.Id == id); |
| Residents | Services\ResidentsService.cs | 139 | /// Creates the operation on the residents. |
| Residents | Services\ResidentsService.cs | 164 | db.Residents.Add(resident); |
| Residents | Services\ResidentsService.cs | 169 | /// Updates the operation on the residents. |
| Residents | Services\ResidentsService.cs | 176 | var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id); |
| Residents | Services\ResidentsService.cs | 197 | var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id); |
| Residents | Services\ResidentsService.cs | 213 | var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id); |
| Residents | Services\ResidentsService.cs | 225 | /// Deletes the operation on the residents. |
| Residents | Services\ResidentsService.cs | 230 | var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id); |
| Residents | Services\ResidentsService.cs | 233 | db.Residents.Remove(resident); |
| Residents | Services\ResidentsService.cs | 238 | /// Counts the operation on the residents. |
| Residents | Services\ResidentsService.cs | 243 | return await db.Residents.CountAsync(); |
| Apartments | Services\ResidentsService.cs | 247 | /// Migrates the from apartments. |
| Revenues | Services\RevenuesService.cs | 5 | // طھط±ط¬ظ…ط© ط­ط±ظپظٹط© ظ„ظ€ js/core/revenues.js |
| Revenues | Services\RevenuesService.cs | 7 | /// Business logic for monthly revenues. |
| Revenues | Services\RevenuesService.cs | 36 | m.Revenues.Add(revenue); |
| Revenues | Services\RevenuesService.cs | 46 | var before = m.Revenues.Count; |
| Revenues | Services\RevenuesService.cs | 47 | m.Revenues = m.Revenues.Where(r => r.Id != revenueId).ToList(); |
| Revenues | Services\RevenuesService.cs | 48 | return before != m.Revenues.Count; |
| Revenues | Services\RevenuesService.cs | 52 | /// Returns the total revenues of month. |
| Revenues | Services\RevenuesService.cs | 55 | building.Months.TryGetValue(monthKey, out var m) ? m.Revenues.Sum(r => r.Amount) : 0; |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 54 | public async Task<ScheduledBackupSettings> GetSettingsAsync() |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 57 | var entity = await db.ScheduledBackupSettings.AsNoTracking() |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 60 | if (entity == null) return new ScheduledBackupSettings(); |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 62 | return new ScheduledBackupSettings |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 82 | public async Task SaveSettingsAsync(ScheduledBackupSettings settings, string userId) |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 109 | var entity = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == SettingsId); |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 113 | db.ScheduledBackupSettings.Add(entity); |
| BackupHistory | Services\ScheduledBackupService.cs | 209 | db.BackupHistory.Add(new Data.Entities.BackupHistoryEntity |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 256 | private async Task SaveSettingsInternalAsync(ScheduledBackupSettings settings) |
| ScheduledBackupSettings | Services\ScheduledBackupService.cs | 259 | var entity = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == SettingsId); |
| BackupHistory | Services\ScheduledBackupService.cs | 274 | var list = await db.BackupHistory.AsNoTracking() |
| BackupHistory | Services\ScheduledBackupService.cs | 342 | await db.BackupHistory.Where(h => h.FileName == fileName).ExecuteDeleteAsync(); |
| Users | Services\SessionRevalidator.cs | 11 | /// Re-checks signed-in users against the database (disabled, removed, permissions changed) on every cookie validation, with a short cache. |
| Users | Services\SessionRevalidator.cs | 32 | UsersService users, |
| Users | Services\SessionRevalidator.cs | 37 | _users = users; |
| Users | Services\SessionRevalidator.cs | 65 | // Synthetic resident identities must never be treated as ordinary resident database users. |
| Users | Services\SessionRevalidator.cs | 104 | twoFactorEnabled = await securityDb.Users.AsNoTracking() |
| Users | Services\SessionRevalidator.cs | 163 | var account = await db.Users.AsNoTracking() |
| Users | Services\SessionRevalidator.cs | 249 | var actorSecurity = await db.Users.AsNoTracking() |
| Buildings | Services\SessionRevalidator.cs | 269 | var buildingExists = await db.Buildings.AsNoTracking() |
| Apartments | Services\SessionRevalidator.cs | 279 | await db.Apartments.AsNoTracking() |
| Residents | Services\SessionRevalidator.cs | 307 | var ok = await db.Residents.AsNoTracking() |
| Buildings | Services\SettingsService.cs | 25 | BuildingsService buildings, |
| Users | Services\SettingsService.cs | 26 | UsersService users, |
| Buildings | Services\SettingsService.cs | 31 | _buildings = buildings; |
| Users | Services\SettingsService.cs | 32 | _users = users; |
| GlobalSettings | Services\SettingsService.cs | 42 | public async Task<GlobalSettings> GetGlobalAsync() |
| GlobalSettings | Services\SettingsService.cs | 45 | var entity = await db.GlobalSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == GlobalId); |
| GlobalSettings | Services\SettingsService.cs | 50 | db.GlobalSettings.Add(entity); |
| GlobalSettings | Services\SettingsService.cs | 54 | return new GlobalSettings |
| GlobalSettings | Services\SettingsService.cs | 84 | public async Task SaveGlobalAsync(GlobalSettings settings, string userId) |
| GlobalSettings | Services\SettingsService.cs | 87 | var entity = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == GlobalId); |
| GlobalSettings | Services\SettingsService.cs | 91 | db.GlobalSettings.Add(entity); |
| BuildingSettings | Services\SettingsService.cs | 126 | public async Task<BuildingSettings> GetBuildingAsync(string buildingId) |
| BuildingSettings | Services\SettingsService.cs | 129 | var entity = await db.BuildingSettings.AsNoTracking() |
| BuildingSettings | Services\SettingsService.cs | 137 | return new BuildingSettings |
| BuildingSettings | Services\SettingsService.cs | 144 | return new BuildingSettings |
| BuildingSettings | Services\SettingsService.cs | 161 | public async Task SaveBuildingAsync(string buildingId, BuildingSettings settings, string userId, string userRole) |
| BuildingSettings | Services\SettingsService.cs | 164 | var entity = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == buildingId); |
| BuildingSettings | Services\SettingsService.cs | 168 | db.BuildingSettings.Add(entity); |
| Apartments | Services\SettingsService.cs | 205 | var apt = await db.Apartments.AsNoTracking() |
| Apartments | Services\SettingsService.cs | 247 | var apt = await db.Apartments |
| Apartments | Services\SettingsService.cs | 286 | var apt = await db.Apartments |
| Residents | Services\SettingsService.cs | 290 | var residents = await db.Residents |
| Residents | Services\SettingsService.cs | 294 | var primaryResident = residents.FirstOrDefault(r => r.IsPrimary) ?? residents.FirstOrDefault(); |
| Residents | Services\SettingsService.cs | 326 | if (residents.Any()) |
| Residents | Services\SettingsService.cs | 328 | foreach (var res in residents) |
| Residents | Services\SettingsService.cs | 338 | foreach (var resident in residents) resident.PinHash = hashedPin; |
| Residents | Services\SqlAuthService.cs | 11 | /// Authenticates residents, admins and super admins against the database and builds the list of accounts a phone/e-mail can enter. |
| Residents | Services\SqlAuthService.cs | 28 | ResidentSqlStore residents, |
| Users | Services\SqlAuthService.cs | 29 | UserSqlStore users, |
| Buildings | Services\SqlAuthService.cs | 30 | BuildingsService buildings, |
| Residents | Services\SqlAuthService.cs | 35 | _residents = residents; |
| Users | Services\SqlAuthService.cs | 36 | _users = users; |
| Buildings | Services\SqlAuthService.cs | 37 | _buildings = buildings; |
| Floors | Services\SqlAuthService.cs | 90 | var floor = building.Floors.FirstOrDefault(f => f.Id == floorId); |
| Apartments | Services\SqlAuthService.cs | 95 | var apt = building.Apartments.FirstOrDefault(a => |
| Residents | Services\SqlAuthService.cs | 102 | var residents = await _residents.GetByApartmentAsync(buildingId, apt.Id); |
| Residents | Services\SqlAuthService.cs | 104 | var resident = residents.FirstOrDefault(r => |
| Users | Services\SqlAuthService.cs | 170 | var candidates = await db.Users |
| BuildingAdmins | Services\SqlAuthService.cs | 172 | .Include(u => u.BuildingAdmins) |
| BuildingAdmins | Services\SqlAuthService.cs | 185 | u.BuildingAdmins.Any(ba => ba.BuildingId == buildingId)); |
| Users | Services\SqlAuthService.cs | 240 | var superAdmin = await db.Users |
| Users | Services\SqlAuthService.cs | 327 | var admins = await db.Users |
| BuildingAdmins | Services\SqlAuthService.cs | 328 | .Include(u => u.BuildingAdmins) |
| BuildingAdmins | Services\SqlAuthService.cs | 341 | var adminBuildingIds = admin.BuildingAdmins.Select(ba => ba.BuildingId).ToList(); |
| Buildings | Services\SqlAuthService.cs | 342 | var adminBuildings = await db.Buildings.AsNoTracking() |
| Residents | Services\SqlAuthService.cs | 367 | // 2. فحص السكان (Residents) — استعلام مباشر بالرقم بدل تحميل كل العمارات |
| Residents | Services\SqlAuthService.cs | 369 | var residentCandidates = await db.Residents.AsNoTracking() |
| Apartments | Services\SqlAuthService.cs | 378 | var apt = await db.Apartments.AsNoTracking() |
| Buildings | Services\SqlAuthService.cs | 384 | var building = await db.Buildings.AsNoTracking() |
| Users | Services\SqlAuthService.cs | 431 | var account = await db.Users |
| BuildingAdmins | Services\SqlAuthService.cs | 450 | \|\| !admin.BuildingAdmins.Any(ba => ba.BuildingId == context.BuildingId)) |
| Residents | Services\SqlAuthService.cs | 463 | var resident = await db.Residents.AsNoTracking() |
| Users | Services\TwoFactorService.cs | 49 | return await db.Users.AsNoTracking().AnyAsync( |
| Users | Services\TwoFactorService.cs | 67 | var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken); |
| Users | Services\TwoFactorService.cs | 105 | var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken); |
| Users | Services\TwoFactorService.cs | 124 | var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken); |
| Users | Services\UsersService.cs | 42 | var users = await _sql.GetAllAsync(); |
| Users | Services\UsersService.cs | 43 | return users.Select(UserSqlMapper.ToDoc).ToList(); |
| Users | Services\UsersService.cs | 68 | /// Sets the operation on the users. |
| Deposits | Services\WalletService.cs | 6 | // ترجمة حرفية لـ js/core/wallet.js + js/core/deposits.js |
| Deposits | Services\WalletService.cs | 8 | /// Core wallet logic: monthly expense/revenue distribution, deposits workflow, carry-over and apartment balances. |
| Apartments | Services\WalletService.cs | 41 | foreach (var apt in building.Apartments) |
| Apartments | Services\WalletService.cs | 59 | foreach (var apt in building.Apartments) |
| Deposits | Services\WalletService.cs | 72 | // -------- Deposits -------- |
| Deposits | Services\WalletService.cs | 80 | foreach (var d in m.Deposits) |
| Apartments | Services\WalletService.cs | 95 | if (!building.Apartments.Any(a => a.Id == aptId)) throw new ArgumentException("Apartment not found.", nameof(aptId)); |
| Deposits | Services\WalletService.cs | 109 | m.Deposits.Add(deposit); |
| Deposits | Services\WalletService.cs | 119 | var d = m.Deposits.FirstOrDefault(x => x.Id == depositId); |
| Deposits | Services\WalletService.cs | 133 | var d = m.Deposits.FirstOrDefault(x => x.Id == depositId); |
| Deposits | Services\WalletService.cs | 143 | /// Edits the amount and note of a pending deposit and records the change in its edit history. Throws <see cref="InvalidOperationException"/> for non-pending deposits or non-positive amounts. |
| Deposits | Services\WalletService.cs | 155 | var d = m.Deposits.FirstOrDefault(x => x.Id == depositId); |
| Deposits | Services\WalletService.cs | 188 | /// Sums the confirmed deposits of an apartment in a month. Pending and cancelled deposits are ignored. |
| Deposits | Services\WalletService.cs | 193 | return m.Deposits.Where(d => d.AptId == aptId && d.Status == "confirmed").Sum(d => d.Amount); |
| Deposits | Services\WalletService.cs | 197 | /// Gets the apartment deposits. |
| Deposits | Services\WalletService.cs | 202 | return m.Deposits.Where(d => d.AptId == aptId) |
| Deposits | Services\WalletService.cs | 207 | /// Gets all pending deposits. |
| Deposits | Services\WalletService.cs | 213 | foreach (var d in m.Deposits.Where(d => d.Status == "pending")) |
| Apartments | Services\WalletService.cs | 215 | var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId); |
| Apartments | Services\WalletService.cs | 223 | /// Splits the month's expenses equally between open apartments using deterministic largest-remainder cent allocation. |
| Expenses | Services\WalletService.cs | 223 | /// Splits the month's expenses equally between open apartments using deterministic largest-remainder cent allocation. |
| Apartments | Services\WalletService.cs | 228 | var openApts = building.Apartments.Where(a => !a.Closed).ToList(); |
| Expenses | Services\WalletService.cs | 229 | var totalExpenses = m.Expenses.Sum(e => e.Amount); |
| Apartments | Services\WalletService.cs | 245 | foreach (var a in building.Apartments) distribution[a.Id] = 0; |
| Apartments | Services\WalletService.cs | 253 | /// Splits the month's revenues equally between open apartments using deterministic largest-remainder cent allocation. |
| Revenues | Services\WalletService.cs | 253 | /// Splits the month's revenues equally between open apartments using deterministic largest-remainder cent allocation. |
| Apartments | Services\WalletService.cs | 258 | var openApts = building.Apartments.Where(a => !a.Closed).ToList(); |
| Revenues | Services\WalletService.cs | 263 | var totalRevenues = m.Revenues.Sum(r => r.Amount); |
| Apartments | Services\WalletService.cs | 275 | foreach (var a in building.Apartments) distribution[a.Id] = 0; |
| Deposits | Services\WalletService.cs | 284 | /// Computes an apartment's wallet balance for a month: carried-over balance, plus confirmed deposits, minus the monthly fee, minus the expense share, plus the revenue share, plus manual adjustments. |
| Apartments | Services\WalletService.cs | 288 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Deposits | Services\WalletService.cs | 298 | var totalPaid = m.Deposits |
| WalletAdjustments | Services\WalletService.cs | 313 | var adjustments = building.WalletAdjustments |
| Apartments | Services\WalletService.cs | 330 | if (string.IsNullOrWhiteSpace(aptId) \|\| !building.Apartments.Any(a => a.Id == aptId)) |
| WalletAdjustments | Services\WalletService.cs | 337 | building.WalletAdjustments ??= new List<WalletAdjustment>(); |
| WalletAdjustments | Services\WalletService.cs | 348 | building.WalletAdjustments.Add(adj); |
| WalletAdjustments | Services\WalletService.cs | 356 | building.WalletAdjustments.Where(a => a.AptId == aptId).OrderByDescending(a => a.CreatedAt).ToList(); |
| Apartments | Services\WalletService.cs | 369 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Deposits | Services\WalletService.cs | 430 | foreach (var d in m.Deposits.Where(d => d.AptId == aptId)) |
| WalletAdjustments | Services\WalletService.cs | 446 | foreach (var adj in building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey)) |
| Apartments | Services\WalletService.cs | 481 | var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId); |
| Deposits | Services\WalletService.cs | 532 | foreach (var d in m.Deposits.Where(d => d.AptId == aptId)) |
| Deposits | Services\WalletService.cs | 534 | bool typeMatch = isAll \|\| selectedTypes.Contains("deposits"); |
| Expenses | Services\WalletService.cs | 584 | if ((isAll \|\| selectedTypes.Contains("expenses")) && |
| Revenues | Services\WalletService.cs | 599 | if ((isAll \|\| selectedTypes.Contains("revenues")) && |
| WalletAdjustments | Services\WalletService.cs | 618 | foreach (var adj in building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey)) |
| Expenses | Services\WalletService.cs | 654 | public (decimal Collected, decimal Expenses, decimal Revenues, int OpenCount, int ClosedCount, decimal Balance, decimal TotalWallet) |
| Revenues | Services\WalletService.cs | 654 | public (decimal Collected, decimal Expenses, decimal Revenues, int OpenCount, int ClosedCount, decimal Balance, decimal TotalWallet) |
| Apartments | Services\WalletService.cs | 658 | var openApts = building.Apartments.Where(a => !a.Closed).ToList(); |
| Apartments | Services\WalletService.cs | 660 | var collected = building.Apartments.Sum(a => TotalConfirmedDeposits(building, a.Id, monthKey)); |
| Expenses | Services\WalletService.cs | 661 | var expenses = m.Expenses.Sum(e => e.Amount); |
| Revenues | Services\WalletService.cs | 662 | var revenues = m.Revenues.Sum(r => r.Amount); |
| Apartments | Services\WalletService.cs | 663 | var totalWallet = building.Apartments.Sum(a => ComputeWalletBalance(building, a.Id, monthKey)); |
| Expenses | Services\WalletService.cs | 665 | return (collected, expenses, revenues, openApts.Count, |
| Revenues | Services\WalletService.cs | 665 | return (collected, expenses, revenues, openApts.Count, |
| Apartments | Services\WalletService.cs | 666 | building.Apartments.Count - openApts.Count, collected + revenues - expenses, totalWallet); |
| Expenses | Services\WalletService.cs | 666 | building.Apartments.Count - openApts.Count, collected + revenues - expenses, totalWallet); |
| Revenues | Services\WalletService.cs | 666 | building.Apartments.Count - openApts.Count, collected + revenues - expenses, totalWallet); |
| Deposits | Services\WalletService.cs | 673 | /// Gets all pending deposits cross month. |
| Deposits | Services\WalletService.cs | 687 | foreach (var d in m.Deposits.Where(d => d.Status == "pending")) |
| Apartments | Services\WalletService.cs | 689 | var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId); |
| Apartments | Services\WalletService.cs | 734 | foreach (var apt in building.Apartments) |
| Apartments | Services\WalletService.cs | 739 | foreach (var apt in building.Apartments) |
| Apartments | Services\WalletService.cs | 774 | m.CarryOver = building.Apartments.ToDictionary(a => a.Id, a => 0m); |
| Apartments | Services\WalletService.cs | 782 | private static void DistributeExactCents(decimal total, IReadOnlyList<Apartment> apartments, |
| Apartments | Services\WalletService.cs | 785 | if (apartments.Count == 0) return; |
| Apartments | Services\WalletService.cs | 787 | var ordered = apartments |
| Floors | Services\WhatsAppTemplateService.cs | 66 | var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId); |
| Presence | ViewModels\BuildingMapViewModels.cs | 6 | // بتجمع: Building + Presence + Balance + Status |
| Floors | ViewModels\BuildingMapViewModels.cs | 18 | /// Gets or sets the floors. |
| Floors | ViewModels\BuildingMapViewModels.cs | 20 | public List<FloorMapVm> Floors { get; set; } = new(); |
| Apartments | ViewModels\BuildingMapViewModels.cs | 49 | /// Gets or sets the apartments. |
| Apartments | ViewModels\BuildingMapViewModels.cs | 51 | public List<ApartmentCardVm> Apartments { get; set; } = new(); |
| Apartments | ViewModels\BuildingMapViewModels.cs | 57 | public int OnlineCount => Apartments.Count(a => a.IsOnline); |
| Apartments | ViewModels\BuildingMapViewModels.cs | 61 | public int DebtorsCount => Apartments.Count(a => a.Balance < 0); |
| Deposits | ViewModels\BuildingMapViewModels.cs | 78 | /// Gets or sets the pending deposits count. |
| Presence | ViewModels\BuildingMapViewModels.cs | 82 | // Presence |
| Floors | ViewModels\BuildingMapViewModels.cs | 138 | /// Gets or sets the floors count. |
| Apartments | ViewModels\BuildingMapViewModels.cs | 142 | /// Gets or sets the apartments count. |
| Apartments | ViewModels\BuildingMapViewModels.cs | 146 | /// Gets or sets the open apartments count. |
| Apartments | ViewModels\BuildingMapViewModels.cs | 150 | /// Gets or sets the closed apartments count. |
| Deposits | ViewModels\BuildingMapViewModels.cs | 170 | /// Gets or sets the pending deposits count. |
| Deposits | ViewModels\BuildingMapViewModels.cs | 260 | /// Gets or sets the pending deposits count. |
| Deposits | ViewModels\BuildingMapViewModels.cs | 264 | /// Gets or sets the confirmed deposits count. |
| Presence | ViewModels\BuildingMapViewModels.cs | 272 | // Presence |
| Deposits | ViewModels\BuildingMapViewModels.cs | 300 | /// Gets or sets the recent deposits. |
| Deposits | ViewModels\InvoiceViewModels.cs | 38 | /// Gets or sets the month deposits. |
| Expenses | ViewModels\InvoiceViewModels.cs | 42 | /// Gets or sets the month expenses share. |
| Revenues | ViewModels\InvoiceViewModels.cs | 46 | /// Gets or sets the month revenues share. |
| Expenses | ViewModels\ReportsViewModels.cs | 23 | /// Gets or sets the total expenses. |
| Revenues | ViewModels\ReportsViewModels.cs | 27 | /// Gets or sets the total revenues. |
| Expenses | ViewModels\ReportsViewModels.cs | 62 | /// One point of the monthly collected-versus-expenses series. |
| Revenues | ViewModels\ReportsViewModels.cs | 79 | /// Gets or sets the revenues. |
| Revenues | ViewModels\ReportsViewModels.cs | 81 | public decimal Revenues { get; set; } |
| Expenses | ViewModels\ReportsViewModels.cs | 83 | /// Gets or sets the expenses. |
| Expenses | ViewModels\ReportsViewModels.cs | 85 | public decimal Expenses { get; set; } |
| Expenses | ViewModels\ReportsViewModels.cs | 89 | /// One slice of the expenses-by-category chart. |
| Buildings | ViewModels\ToolsViewModels.cs | 12 | /// Gets or sets the buildings scanned. |
| Residents | Views\Account\ChooseContext.cshtml | 14 | var residents = Model.Contexts.Where(c => c.Type == "resident").ToList(); |
| Residents | Views\Account\ChooseContext.cshtml | 17 | var residentsByBuilding = residents |
| Apartments | Views\Account\ChooseContext.cshtml | 24 | Apartments = g.OrderBy(r => r.AptNumber).ToList() |
| Residents | Views\Account\ChooseContext.cshtml | 109 | <h5 class="text-primary mb-3">@Loc.T("Residents_N_Apartments_In_N_Buildings", residents.Count, residentsByBuilding.Count)</h5> |
| Apartments | Views\Account\ChooseContext.cshtml | 122 | <div class="building-apartments"> |
| Apartments | Views\Account\ChooseContext.cshtml | 123 | @foreach (var apt in buildingGroup.Apartments) |
| Residents | Views\Account\ChooseContext.cshtml | 145 | @if (!superAdmins.Any() && !admins.Any() && !residents.Any()) |
| Apartments | Views\Account\ChooseContext.cshtml | 199 | .building-apartments { |
| Apartments | Views\Account\ChooseContext.cshtml | 231 | .building-apartments::-webkit-scrollbar { |
| Apartments | Views\Account\ChooseContext.cshtml | 235 | .building-apartments::-webkit-scrollbar-thumb { |
| Buildings | Views\Account\LoginAdmin.cshtml | 10 | var buildings = ViewBag.Buildings as List<Building> ?? new(); |
| Buildings | Views\Account\LoginAdmin.cshtml | 31 | @foreach (var b in buildings) |
| Residents | Views\Account\LoginChoice.cshtml | 13 | var residents = Model.Contexts.Where(c => c.Type == "resident").ToList(); |
| Residents | Views\Account\LoginChoice.cshtml | 15 | var residentsByBuilding = residents |
| Apartments | Views\Account\LoginChoice.cshtml | 22 | Apartments = g.OrderBy(r => r.AptNumber).ToList() |
| Residents | Views\Account\LoginChoice.cshtml | 97 | @* â•گâ•گâ•گâ•گâ•گâ•گâ•گ Residents â•گâ•گâ•گâ•گâ•گâ•گâ•گ *@ |
| Residents | Views\Account\LoginChoice.cshtml | 102 | ًںڈ  @Loc.T("Residents_N_Apartments_In_N_Buildings", residents.Count, residentsByBuilding.Count) |
| Apartments | Views\Account\LoginChoice.cshtml | 116 | <div class="building-apartments"> |
| Apartments | Views\Account\LoginChoice.cshtml | 117 | @foreach (var apt in buildingGroup.Apartments) |
| Residents | Views\Account\LoginChoice.cshtml | 139 | @if (!superAdmins.Any() && !admins.Any() && !residents.Any()) |
| Apartments | Views\Account\LoginChoice.cshtml | 191 | .building-apartments { |
| Apartments | Views\Account\LoginChoice.cshtml | 222 | .building-apartments::-webkit-scrollbar { |
| Apartments | Views\Account\LoginChoice.cshtml | 226 | .building-apartments::-webkit-scrollbar-thumb { |
| Buildings | Views\Account\LoginResident.cshtml | 10 | var buildings = ViewBag.Buildings as List<Building> ?? new(); |
| Buildings | Views\Account\LoginResident.cshtml | 34 | @foreach (var b in buildings) |
| Floors | Views\Account\LoginResident.cshtml | 167 | const floors = await res.json(); |
| Floors | Views\Account\LoginResident.cshtml | 169 | if (floors.length === 0) { |
| Floors | Views\Account\LoginResident.cshtml | 175 | floors.forEach(f => { |
| Floors | Views\AdminApts\Index.cshtml | 27 | @foreach (var floor in Model.Floors.OrderBy(f => f.Order)) |
| Apartments | Views\AdminApts\Index.cshtml | 29 | var apts = Model.Apartments.Where(a => a.FloorId == floor.Id).OrderBy(a => a.Number).ToList(); |
| Floors | Views\AdminHome\Index.cshtml | 63 | @foreach (var floor in Model.Floors) |
| Apartments | Views\AdminHome\Index.cshtml | 70 | <span class="badge bg-secondary ms-2">@Loc.T("N_Apartments", floor.Apartments.Count)</span> |
| Apartments | Views\AdminHome\Index.cshtml | 82 | <div class="apartments-grid"> |
| Apartments | Views\AdminHome\Index.cshtml | 83 | @foreach (var card in floor.Apartments) |
| Cameras | Views\AdminHome\Index.cshtml | 133 | @if (User.HasClaim("perm", "cameras.view") \|\| User.HasClaim("perm", "dvrs.manage")) |
| Dvrs | Views\AdminHome\Index.cshtml | 133 | @if (User.HasClaim("perm", "cameras.view") \|\| User.HasClaim("perm", "dvrs.manage")) |
| Cameras | Views\AdminHome\Index.cshtml | 136 | <a asp-controller="Cameras" asp-action="Live" |
| Buildings | Views\AdminHome\Index.cshtml | 305 | var res = await fetch('/Buildings/GenerateQuickQr', { |
| Deposits | Views\AdminHome\_ApartmentDetailsModal.cshtml | 89 | @* ===== Recent Deposits ===== *@ |
| Apartments | Views\AdminResidents\ChooseBuilding.cshtml | 38 | @b.ApartmentsCount @Loc.T("Apartments") |
| Residents | Views\AdminResidents\Index.cshtml | 88 | @* Residents Table                                               *@ |
| Apartments | Views\AdminResidents\Index.cshtml | 117 | var apt = building?.Apartments.FirstOrDefault(a => a.Id == r.ApartmentId); |
| Buildings | Views\AdminResidents\Migrate.cshtml | 9 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\AdminResidents\Migrate.cshtml | 42 | @foreach (var b in buildings) |
| Apartments | Views\AdminResidents\Migrate.cshtml | 45 | @b.Name (@b.BuildingNumber) — @b.ApartmentsCount @Loc.T("Apartments") |
| Buildings | Views\AdminResidents\MigrationResult.cshtml | 11 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\Admins\Create.cshtml | 9 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\Admins\Create.cshtml | 33 | @foreach (var b in buildings) |
| Buildings | Views\Admins\Index.cshtml | 10 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\Admins\Index.cshtml | 26 | @foreach (var b in buildings) |
| AuditLog | Views\AuditLog\Index.cshtml | 2 | View:        AuditLog/Index |
| Buildings | Views\AuditLog\Index.cshtml | 11 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\AuditLog\Index.cshtml | 63 | @if (canFilterBuilding && buildings.Count > 0) |
| Buildings | Views\AuditLog\Index.cshtml | 69 | @foreach (var b in buildings) |
| AuditLog | Views\AuditLog\Index.cshtml | 142 | ViewData["ControllerName"] = "AuditLog"; |
| AuditLog | Views\AuditLog\Index.cshtml | 157 | ControllerName = "AuditLog", |
| AuditLog | Views\AuditLog\Index.cshtml | 243 | ViewData["ControllerName"] = "AuditLog"; |
| AuditLog | Views\AuditLog\Index.cshtml | 258 | ControllerName = "AuditLog", |
| Apartments | Views\Backup\ConfirmImport.cshtml | 66 | @* ===== Apartments ===== *@ |
| Apartments | Views\Backup\ConfirmImport.cshtml | 69 | <strong>@Loc.T("Apartments_N", Model.Apartments.Count)</strong> |
| Apartments | Views\Backup\ConfirmImport.cshtml | 89 | @foreach (var apt in Model.Apartments) |
| Buildings | Views\Backup\Index.cshtml | 9 | var buildings = (List<Building>)ViewBag.Buildings; |
| ScheduledBackupSettings | Views\Backup\Index.cshtml | 12 | var settings = ViewBag.Settings as BuildingManagementMvc.Models.ScheduledBackupSettings ?? new(); |
| Buildings | Views\Backup\Index.cshtml | 272 | @if (buildings.Count > 0) |
| Apartments | Views\Backup\Index.cshtml | 284 | <th>@Loc.T("Apartments")</th> |
| Floors | Views\Backup\Index.cshtml | 285 | <th>@Loc.T("Floors")</th> |
| Buildings | Views\Backup\Index.cshtml | 290 | @foreach (var b in buildings) |
| Buildings | Views\Backup\Index.cshtml | 300 | <a asp-controller="Buildings" asp-action="Details" asp-route-id="@b.Id" class="btn btn-sm btn-outline-secondary">@Loc.T("View_2")</a> |
| ScheduledBackupSettings | Views\Backup\Scheduled.cshtml | 4 | Model:       BuildingManagementMvc.Models.ScheduledBackupSettings |
| ScheduledBackupSettings | Views\Backup\Scheduled.cshtml | 7 | @model BuildingManagementMvc.Models.ScheduledBackupSettings |
| Buildings | Views\Buildings\Create.cshtml | 2 | View:        Buildings/Create |
| Buildings | Views\Buildings\Details.cshtml | 2 | View:        Buildings/Details |
| Floors | Views\Buildings\Details.cshtml | 43 | @foreach (var floor in Model.Floors.OrderBy(f => f.Order)) |
| Apartments | Views\Buildings\Details.cshtml | 45 | var apts = Model.Apartments.Where(a => a.FloorId == floor.Id).OrderBy(a => a.Number).ToList(); |
| Buildings | Views\Buildings\Details.cshtml | 274 | const res = await fetch('@Url.Action("GenerateQuickQr", "Buildings")', { |
| Buildings | Views\Buildings\Index.cshtml | 2 | View:        Buildings/Index |
| Buildings | Views\Buildings\Index.cshtml | 9 | ViewData["Title"] = Loc.T("Buildings"); |
| Buildings | Views\Buildings\Index.cshtml | 29 | ViewData["ControllerName"] = "Buildings"; |
| Buildings | Views\Buildings\QrCodes.cshtml | 2 | View:        Buildings/QrCodes |
| Floors | Views\Buildings\QrCodes.cshtml | 65 | @foreach (var floor in Model.Floors.OrderBy(f => f.Order)) |
| Apartments | Views\Buildings\QrCodes.cshtml | 67 | var apts = Model.Apartments |
| Cameras | Views\Cameras\Live.cshtml | 2 | View:        Cameras/Live |
| Dvrs | Views\Cameras\Live.cshtml | 17 | "superadmin" or "admin" => Url.Action("Index", "Dvrs", new { buildingId }), |
| Cameras | Views\Cameras\Live.cshtml | 92 | const cameras = @Html.Raw(System.Text.Json.JsonSerializer.Serialize( |
| Cameras | Views\Cameras\Live.cshtml | 95 | cameras.forEach(async function (cam) { |
| Cameras | Views\Cameras\Live.cshtml | 97 | const res = await fetch(`/Cameras/GetHlsUrl?cameraId=${cam.id}`); |
| Dvrs | Views\Dvrs\ChooseBuilding.cshtml | 2 | View:        Dvrs/ChooseBuilding |
| Buildings | Views\Dvrs\ChooseBuilding.cshtml | 24 | <a asp-controller="Buildings" asp-action="Create" class="btn btn-primary mt-3"> |
| Dvrs | Views\Dvrs\Create.cshtml | 2 | View:        Dvrs/Create |
| Dvrs | Views\Dvrs\Details.cshtml | 2 | View:        Dvrs/Details |
| Cameras | Views\Dvrs\Details.cshtml | 93 | <span class="badge bg-info">@Model.Cameras.Count</span> |
| Cameras | Views\Dvrs\Details.cshtml | 159 | @* ===== Cameras List ===== *@ |
| Cameras | Views\Dvrs\Details.cshtml | 164 | @Loc.T("Cameras") (@Model.Cameras.Count) |
| Cameras | Views\Dvrs\Details.cshtml | 166 | @if (Model.Cameras.Count > 0) |
| Cameras | Views\Dvrs\Details.cshtml | 168 | <a asp-controller="Cameras" asp-action="Live" |
| Cameras | Views\Dvrs\Details.cshtml | 177 | @if (Model.Cameras.Count == 0) |
| Cameras | Views\Dvrs\Details.cshtml | 199 | @foreach (var cam in Model.Cameras.OrderBy(c => c.Channel)) |
| Dvrs | Views\Dvrs\Edit.cshtml | 2 | View:        Dvrs/Edit |
| Dvrs | Views\Dvrs\Index.cshtml | 2 | View:        Dvrs/Index |
| Dvrs | Views\Dvrs\Index.cshtml | 9 | ViewData["Title"] = Loc.T("DVRs"); |
| Dvrs | Views\Dvrs\Index.cshtml | 20 | @Loc.T("DVRs") |
| Cameras | Views\Dvrs\Index.cshtml | 32 | <a asp-controller="Cameras" asp-action="Live" |
| Cameras | Views\Dvrs\Index.cshtml | 96 | <span class="badge bg-info">@dvr.Cameras.Count</span> |
| Expenses | Views\Expenses\Index.cshtml | 2 | View:        Expenses/Index |
| Expenses | Views\Expenses\Index.cshtml | 9 | ViewData["Title"] = Loc.T("Expenses"); |
| Expenses | Views\Expenses\Index.cshtml | 76 | ViewData["ControllerName"] = "Expenses"; |
| Notifications | Views\Help\Index.cshtml | 218 | <a href="#resident-notifications" class="sub">@Loc.T("Notifications")</a> |
| Polls | Views\Help\Index.cshtml | 219 | <a href="#resident-polls" class="sub">@Loc.T("Polls")</a> |
| Deposits | Views\Help\Index.cshtml | 222 | <a href="#admin-deposits" class="sub">@Loc.T("Payments")</a> |
| Expenses | Views\Help\Index.cshtml | 223 | <a href="#admin-expenses" class="sub">@Loc.T("Expenses_2")</a> |
| Buildings | Views\Help\Index.cshtml | 227 | <a href="#superadmin-buildings" class="sub">@Loc.T("Buildings_2")</a> |
| Notifications | Views\Help\Index.cshtml | 294 | <h3 id="resident-notifications">@Loc.T("Notifications_2")</h3> |
| Polls | Views\Help\Index.cshtml | 303 | <h3 id="resident-polls">@Loc.T("Polls_2")</h3> |
| Deposits | Views\Help\Index.cshtml | 322 | <h3 id="admin-deposits">@Loc.T("Managing_Payments")</h3> |
| Expenses | Views\Help\Index.cshtml | 350 | <h3 id="admin-expenses">@Loc.T("Expenses_And_Revenues")</h3> |
| Buildings | Views\Help\Index.cshtml | 386 | <h3 id="superadmin-buildings">@Loc.T("Managing_Buildings")</h3> |
| Notifications | Views\Notifications\Index.cshtml | 2 | View:        Notifications/Index |
| Notifications | Views\Notifications\Index.cshtml | 34 | ViewData["ControllerName"] = "Notifications"; |
| Buildings | Views\Permissions\Index.cshtml | 13 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\Permissions\Index.cshtml | 50 | @foreach (var b in buildings) |
| Buildings | Views\Permissions\Index.cshtml | 81 | buildings.Where(b => admin.BuildingIds != null && admin.BuildingIds.Contains(b.Id)) |
| Buildings | Views\Permissions\Index.cshtml | 88 | data-buildings="@string.Join(",", admin.BuildingIds ?? new List<string>())"> |
| Buildings | Views\Permissions\Index.cshtml | 378 | const buildings = row.dataset.buildings \|\| ''; |
| Buildings | Views\Permissions\Index.cshtml | 381 | const matchBuilding = !buildingId \|\| buildings.includes(buildingId); |
| Polls | Views\Polls\Index.cshtml | 2 | View:        Polls/Index |
| Apartments | Views\Polls\Index.cshtml | 11 | var openApts = building.Apartments.Count(a => !a.Closed); |
| Polls | Views\Polls\Index.cshtml | 54 | ViewData["ControllerName"] = "Polls"; |
| Floors | Views\Qr\Index.cshtml | 13 | @foreach (var floor in Model.Floors.OrderBy(f => f.Order)) |
| Apartments | Views\Qr\Index.cshtml | 15 | var apts = Model.Apartments.Where(a => a.FloorId == floor.Id).OrderBy(a => a.Number).ToList(); |
| Revenues | Views\Reports\Index.cshtml | 122 | { label: '@Html.Raw(Loc.Js("Revenues"))', data: monthly.map(m => m.revenues), backgroundColor: '#2E7D5B' }, |
| Expenses | Views\Reports\Index.cshtml | 123 | { label: '@Html.Raw(Loc.Js("Expenses_3"))', data: monthly.map(m => m.expenses), backgroundColor: '#A0432A' } |
| Notifications | Views\ResidentHome\Index.cshtml | 26 | var notifications = ViewBag.Notifications as List<Notification> ?? new(); |
| Notifications | Views\ResidentHome\Index.cshtml | 135 | <a asp-controller="Notifications" asp-action="Index" class="rh-header-btn" title="@Loc.T("Notifications_3")"> |
| Notifications | Views\ResidentHome\Index.cshtml | 177 | <a asp-controller="Notifications" asp-action="Index" class="rh-action-item rh-action-info"> |
| Cameras | Views\ResidentHome\Index.cshtml | 357 | <a asp-controller="Cameras" asp-action="Live" |
| Polls | Views\ResidentHome\Index.cshtml | 502 | @* 9. ACTIVE POLLS                                               *@ |
| Polls | Views\ResidentHome\Index.cshtml | 517 | <div class="rh-polls-list"> |
| Apartments | Views\ResidentHome\Index.cshtml | 522 | var openApts = building.Apartments.Count(a => !a.Closed); |
| Notifications | Views\ResidentHome\Index.cshtml | 560 | @* 10. NOTIFICATIONS                                             *@ |
| Notifications | Views\ResidentHome\Index.cshtml | 562 | @if (notifications.Any()) |
| Notifications | Views\ResidentHome\Index.cshtml | 574 | <a asp-controller="Notifications" asp-action="Index" class="rh-link-more"> |
| Notifications | Views\ResidentHome\Index.cshtml | 580 | @foreach (var n in notifications) |
| Apartments | Views\ResidentPolls\Index.cshtml | 12 | var openApts = building.Apartments.Count(a => !a.Closed); |
| Deposits | Views\ResidentWallet\Index.cshtml | 160 | value="deposits" id="tf_deposits" |
| Deposits | Views\ResidentWallet\Index.cshtml | 161 | @(selectedTypes.Contains("deposits") ? "checked" : "") /> |
| Expenses | Views\ResidentWallet\Index.cshtml | 170 | value="expenses" id="tf_expenses" |
| Expenses | Views\ResidentWallet\Index.cshtml | 171 | @(selectedTypes.Contains("expenses") ? "checked" : "") /> |
| Revenues | Views\ResidentWallet\Index.cshtml | 180 | value="revenues" id="tf_revenues" |
| Revenues | Views\ResidentWallet\Index.cshtml | 181 | @(selectedTypes.Contains("revenues") ? "checked" : "") /> |
| Revenues | Views\Revenues\Index.cshtml | 2 | View:        Revenues/Index |
| Revenues | Views\Revenues\Index.cshtml | 76 | ViewData["ControllerName"] = "Revenues"; |
| BuildingSettings | Views\Settings\Building.cshtml | 4 | Model:       BuildingManagementMvc.Models.BuildingSettings |
| BuildingSettings | Views\Settings\Building.cshtml | 7 | @model BuildingManagementMvc.Models.BuildingSettings |
| Buildings | Views\Settings\ChooseBuilding.cshtml | 9 | var buildings = ViewBag.Buildings as List<BuildingManagementMvc.Models.Building> ?? new(); |
| Buildings | Views\Settings\ChooseBuilding.cshtml | 15 | @if (buildings.Count == 0) |
| Buildings | Views\Settings\ChooseBuilding.cshtml | 22 | @foreach (var b in buildings) |
| GlobalSettings | Views\Settings\Global.cshtml | 4 | Model:       BuildingManagementMvc.Models.GlobalSettings |
| GlobalSettings | Views\Settings\Global.cshtml | 7 | @model BuildingManagementMvc.Models.GlobalSettings |
| Presence | Views\Shared\_Layout.cshtml | 114 | @* âœ… Presence Heartbeat â€” ظٹط³ط¬ظ‘ظ„ ط§ظ„ظ…ط³طھط®ط¯ظ… ظƒظ€ "ظ†ط´ط·" *@ |
| Presence | Views\Shared\_Layout.cshtml | 115 | <script src="~/js/presence-heartbeat.js" asp-append-version="true"></script> |
| Notifications | Views\Shared\_Sidebar.cshtml | 94 | <li><a asp-controller="Notifications" asp-action="Index" class="nav-link @Active("Notifications")"><i class="bi bi-bell"></i><span>@Loc.T("Nav_Notifications")</span></a></li> |
| AuditLog | Views\Shared\_Sidebar.cshtml | 95 | <li><a asp-controller="AuditLog" asp-action="Index" class="nav-link @Active("AuditLog")"><i class="bi bi-journal-text"></i><span>@Loc.T("Nav_MyLog")</span></a></li> |
| Cameras | Views\Shared\_Sidebar.cshtml | 97 | <a asp-controller="Cameras" asp-action="Live" |
| Cameras | Views\Shared\_Sidebar.cshtml | 99 | class="nav-link @Active("Cameras")"> |
| Residents | Views\Shared\_Sidebar.cshtml | 118 | @if (Has("residents.manage")) |
| Cameras | Views\Shared\_Sidebar.cshtml | 126 | @if (Has("dvrs.manage") \|\| Has("cameras.view")) |
| Dvrs | Views\Shared\_Sidebar.cshtml | 126 | @if (Has("dvrs.manage") \|\| Has("cameras.view")) |
| Cameras | Views\Shared\_Sidebar.cshtml | 132 | <a asp-controller="Cameras" asp-action="Live" |
| Cameras | Views\Shared\_Sidebar.cshtml | 134 | class="nav-link @Active("Cameras")"> |
| Dvrs | Views\Shared\_Sidebar.cshtml | 139 | @if (Has("dvrs.manage")) |
| Dvrs | Views\Shared\_Sidebar.cshtml | 142 | <a asp-controller="Dvrs" asp-action="Index" |
| Dvrs | Views\Shared\_Sidebar.cshtml | 144 | class="nav-link @Active("Dvrs")"> |
| Expenses | Views\Shared\_Sidebar.cshtml | 161 | @if (Has("expenses.manage")) |
| Expenses | Views\Shared\_Sidebar.cshtml | 163 | <li><a asp-controller="Expenses" asp-action="Index" class="nav-link @Active("Expenses")"><i class="bi bi-receipt"></i><span>@Loc.T("Nav_Expenses")</span></a></li> |
| Revenues | Views\Shared\_Sidebar.cshtml | 165 | @if (Has("revenues.manage")) |
| Revenues | Views\Shared\_Sidebar.cshtml | 167 | <li><a asp-controller="Revenues" asp-action="Index" class="nav-link @Active("Revenues")"><i class="bi bi-cash-coin"></i><span>@Loc.T("Nav_Revenues")</span></a></li> |
| Polls | Views\Shared\_Sidebar.cshtml | 183 | @if (Has("polls.manage")) |
| Polls | Views\Shared\_Sidebar.cshtml | 185 | <li><a asp-controller="Polls" asp-action="Index" class="nav-link @Active("Polls")"><i class="bi bi-bar-chart"></i><span>@Loc.T("Nav_Polls")</span></a></li> |
| Notifications | Views\Shared\_Sidebar.cshtml | 191 | <li><a asp-controller="Notifications" asp-action="Index" class="nav-link @Active("Notifications")"><i class="bi bi-bell"></i><span>@Loc.T("Nav_Notifications")</span></a></li> |
| AuditLog | Views\Shared\_Sidebar.cshtml | 202 | <li><a asp-controller="AuditLog" asp-action="Index" class="nav-link @Active("AuditLog")"><i class="bi bi-journal-text"></i><span>@Loc.T("Nav_AuditLog")</span></a></li> |
| Buildings | Views\Shared\_Sidebar.cshtml | 221 | <li><a asp-controller="Buildings" asp-action="Index" class="nav-link @Active("Buildings")"><i class="bi bi-buildings"></i><span>@Loc.T("Nav_Buildings")</span></a></li> |
| Dvrs | Views\Shared\_Sidebar.cshtml | 233 | <a asp-controller="Dvrs" asp-action="Index" |
| Dvrs | Views\Shared\_Sidebar.cshtml | 234 | class="nav-link @Active("Dvrs")"> |
| Cameras | Views\Shared\_Sidebar.cshtml | 240 | <a asp-controller="Cameras" asp-action="Live" |
| Cameras | Views\Shared\_Sidebar.cshtml | 241 | class="nav-link @Active("Cameras")"> |
| AuditLog | Views\Shared\_Sidebar.cshtml | 252 | <li><a asp-controller="AuditLog" asp-action="Index" class="nav-link @Active("AuditLog")"><i class="bi bi-journal-text"></i><span>@Loc.T("Nav_AuditLog")</span></a></li> |
| Notifications | Views\Shared\_Topbar.cshtml | 35 | @* ===== Notifications — للأدمن والساكن بس ===== *@ |
| Notifications | Views\Shared\_Topbar.cshtml | 38 | <a asp-controller="Notifications" asp-action="Index" |
| Buildings | Views\SuperAdminHome\Index.cshtml | 12 | <a asp-controller="Buildings" asp-action="Create" class="btn btn-brand">@Loc.T("New_Building_2")</a> |
| Apartments | Views\SuperAdminHome\Index.cshtml | 23 | <thead><tr><th>@Loc.T("No")</th><th>@Loc.T("Name")</th><th>@Loc.T("Floors")</th><th>@Loc.T("Apartments")</th><th></th></tr></thead> |
| Floors | Views\SuperAdminHome\Index.cshtml | 23 | <thead><tr><th>@Loc.T("No")</th><th>@Loc.T("Name")</th><th>@Loc.T("Floors")</th><th>@Loc.T("Apartments")</th><th></th></tr></thead> |
| Buildings | Views\SuperAdminHome\Index.cshtml | 32 | <td><a asp-controller="Buildings" asp-action="Details" asp-route-id="@b.Id" class="btn btn-sm btn-outline-secondary">@Loc.T("Details")</a></td> |
| Expenses | Views\Wallet\Index.cshtml | 13 | var totals = ((decimal Collected, decimal Expenses, decimal Revenues, int OpenCount, int ClosedCount, decimal Balance, decimal TotalWallet))ViewBag.Totals; |
| Revenues | Views\Wallet\Index.cshtml | 13 | var totals = ((decimal Collected, decimal Expenses, decimal Revenues, int OpenCount, int ClosedCount, decimal Balance, decimal TotalWallet))ViewBag.Totals; |
| Expenses | Views\Wallet\Index.cshtml | 32 | <div class="col"><div class="bg-white rounded shadow-sm p-3"><div class="text-muted small">@Loc.T("Expenses_3")</div><div class="fw-bold">@totals.Expenses.ToString("0.##")</div></div></div> |
| Revenues | Views\Wallet\Index.cshtml | 33 | <div class="col"><div class="bg-white rounded shadow-sm p-3"><div class="text-muted small">@Loc.T("Revenues")</div><div class="fw-bold">@totals.Revenues.ToString("0.##")</div></div></div> |
| Floors | Views\Wallet\Index.cshtml | 144 | var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order); |
| Floors | Views\Wallet\Index.cshtml | 152 | var floor = building.Floors.FirstOrDefault(f => f.Id == floorGroup.Key); |
| Apartments | Views\WhatsApp\Index.cshtml | 21 | @foreach (var apt in building.Apartments.OrderBy(a => a.Number)) |
| Buildings | wwwroot\lib\bootstrap-icons\font\bootstrap-icons.json | 1874 | "buildings-fill": 63612, |
| Buildings | wwwroot\lib\bootstrap-icons\font\bootstrap-icons.json | 1875 | "buildings": 63613, |

## Test reference locations

| Table | File | Line | Reference |
|---|---|---:|---|
| Presence | Tests\BuildingManagement.Tests\ApplicationLogQueryServiceTests.cs | 139 | "UPDATE [Presence] SET [LastSeen] = @p0", |
| Presence | Tests\BuildingManagement.Tests\ApplicationLogQueryServiceTests.cs | 155 | Assert.Contains("UPDATE [Presence]", item.Details, StringComparison.Ordinal); |
| Presence | Tests\BuildingManagement.Tests\ApplicationLogQueryServiceTests.cs | 164 | Search = "UPDATE [Presence]" |
| Buildings | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 25 | db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test Building", BuildingNumber = "BLD-901" }); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 26 | db.AuditLog.AddRange( |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 55 | var entry = await db.AuditLog.SingleAsync(); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 68 | seed.AuditLog.Add(new AuditLogEntity { Action = "original", Ts = DateTime.UtcNow }); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 73 | var entry = await db.AuditLog.SingleAsync(); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 87 | seed.AuditLog.Add(new AuditLogEntity { Action = "keep", Ts = DateTime.UtcNow }); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 92 | var entry = await db.AuditLog.SingleAsync(); |
| AuditLog | Tests\BuildingManagement.Tests\AuditLoggerTests.cs | 93 | db.AuditLog.Remove(entry); |
| Expenses | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 39 | [InlineData("expenses.manage")] |
| Revenues | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 40 | [InlineData("revenues.manage")] |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 86 | "buildings.manage", |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 122 | "buildings.manage", |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 153 | ("buildings-role", AuthorizationEffect.Allow)); |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 158 | "buildings.manage", |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 171 | ("buildings-role", AuthorizationEffect.Allow)); |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 176 | "buildings.manage", |
| Buildings | Tests\BuildingManagement.Tests\AuthorizationEvaluatorTests.cs | 216 | var permission = grant.RoleKey == "audit-role" ? "audit.view.all" : "buildings.manage"; |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 54 | db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Nile Tower", BuildingNumber = "BLD-001" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 58 | var path = await WriteBackupAsync("buildings"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 62 | db.Buildings.RemoveRange(db.Buildings); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 66 | var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: false, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 72 | var restored = await check.Buildings.SingleAsync(); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 81 | db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Old", BuildingNumber = "BLD-001" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 85 | var path = await WriteBackupAsync("buildings"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 89 | (await db.Buildings.SingleAsync()).Name = "Changed"; |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 93 | var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: false, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 97 | Assert.Equal("Changed", (await check.Buildings.SingleAsync()).Name); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 105 | db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Old", BuildingNumber = "BLD-001" }); |
| Floors | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 106 | db.Floors.Add(new FloorEntity { BuildingId = "b1", Id = "f1", Label = "Ground" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 110 | var path = await WriteBackupAsync("buildings");        // الأدوار مش جوه الباك أب |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 114 | (await db.Buildings.SingleAsync()).Name = "Changed"; |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 118 | var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 124 | Assert.Equal("Old", (await check.Buildings.SingleAsync()).Name); |
| Floors | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 125 | Assert.Equal(1, await check.Floors.CountAsync());      // الدور لسه موجود |
| AuditLog | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 133 | db.AuditLog.Add(new AuditLogEntity { Action = "test", Label = "x", Ts = DateTime.UtcNow }); |
| AuditLog | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 137 | var path = await WriteBackupAsync("auditLog"); |
| AuditLog | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 139 | var result = await _service.RestoreAsync(path, new() { "auditLog" }, overwrite: false, "admin"); |
| AuditLog | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 143 | Assert.Equal(2, await check.AuditLog.CountAsync());     // الأصلي + المستعاد (append-only) |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 151 | db.Buildings.Add(new BuildingEntity { Id = "rollback-b1", Name = "Original", BuildingNumber = "BLD-992" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 155 | var (backupBytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin"); |
| Apartments | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 168 | using var malformed = outputZip.CreateEntry("apartments.json").Open(); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 175 | db.Buildings.RemoveRange(db.Buildings); |
| Apartments | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 181 | var result = await _service.RestoreAsync(path, new() { "buildings", "apartments" }, overwrite: true, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 181 | var result = await _service.RestoreAsync(path, new() { "buildings", "apartments" }, overwrite: true, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 185 | Assert.Empty(await check.Buildings.ToListAsync()); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 196 | var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupRestorePersistenceTests.cs | 215 | var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 46 | Assert.Contains(stats, s => s.Collection == "buildings"); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 47 | Assert.Contains(stats, s => s.Collection == "users"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 55 | _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test", BuildingNumber = "BLD-001" }); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 56 | _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 63 | var buildingsStat = stats.FirstOrDefault(s => s.Collection == "buildings"); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 67 | var usersStat = stats.FirstOrDefault(s => s.Collection == "users"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 79 | var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 90 | Assert.Contains(zip.Entries, e => e.Name == "buildings.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 98 | _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T1", BuildingNumber = "BLD-001" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 99 | _db.Buildings.Add(new BuildingEntity { Id = "b2", Name = "T2", BuildingNumber = "BLD-002" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 103 | var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 112 | var entry = zip.GetEntry("buildings.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 124 | _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" }); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 125 | _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 128 | var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings", "users" }, "admin"); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 128 | var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings", "users" }, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 136 | Assert.Contains(zip.Entries, e => e.Name == "buildings.json"); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 137 | Assert.Contains(zip.Entries, e => e.Name == "users.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 157 | _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 160 | var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 169 | Assert.Equal("buildings", preview.Collections[0].Collection); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 181 | _db.Buildings.Add(new BuildingEntity { Id = "session-b1", Name = "Session Tower", BuildingNumber = "BLD-991" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 183 | var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin-a"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 189 | var denied = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-b"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 193 | var restored = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-a"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 197 | var replay = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-a"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 220 | _db.Buildings.Add(new BuildingManagementMvc.Data.Entities.BuildingEntity |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 228 | var (bytes, backupResult) = await _service.CreateBackupAsync(new() { "buildings" }, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 238 | var result = await _service.RestoreAsync(tempPath, new() { "buildings" }, overwrite: false, "admin"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 243 | Assert.Contains(result.Details, d => d.Contains("buildings")); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 262 | var entry = zip.CreateEntry("buildings.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 282 | var entry = zip.CreateEntry("../buildings.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 300 | var entry = zip.CreateEntry("buildings.json"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 315 | _db.Buildings.Add(new BuildingEntity { Id = "phase10-original", Name = "Original", BuildingNumber = "BLD-910" }); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 318 | var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "phase10-tamper"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 327 | preview.RestoreSessionId!, new() { "buildings" }, overwrite: true, "phase10-tamper"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 332 | Assert.True(await _db.Buildings.AnyAsync(x => x.Id == "phase10-original")); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 338 | var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "phase10-session-cap"); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 352 | var firstRestore = await _service.RestoreSessionAsync(first.RestoreSessionId!, new() { "buildings" }, false, userId); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 353 | var secondRestore = await _service.RestoreSessionAsync(second.RestoreSessionId!, new() { "buildings" }, false, userId); |
| Buildings | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 367 | Assert.Contains("buildings", collections.Keys); |
| Users | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 368 | Assert.Contains("users", collections.Keys); |
| Residents | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 369 | Assert.Contains("residents", collections.Keys); |
| AuditLog | Tests\BuildingManagement.Tests\BackupServiceTests.cs | 370 | Assert.Contains("auditLog", collections.Keys); |
| Buildings | Tests\BuildingManagement.Tests\BuildingSqlMapperConcurrencyTests.cs | 53 | db.Buildings.Attach(building); |
| Apartments | Tests\BuildingManagement.Tests\BuildingsServiceTests.cs | 81 | Apartments = new List<Apartment> { new(), new(), new() }, |
| Floors | Tests\BuildingManagement.Tests\BuildingsServiceTests.cs | 82 | Floors = new List<Floor> { new(), new() } |
| Apartments | Tests\BuildingManagement.Tests\BuildingsServiceTests.cs | 89 | Assert.Equal(3, result[0].Apartments.Count); |
| Floors | Tests\BuildingManagement.Tests\BuildingsServiceTests.cs | 90 | Assert.Equal(2, result[0].Floors.Count); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 37 | Assert.Single(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 77 | Assert.Equal(2, b.Months["2025-01"].Expenses.Count); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 87 | Assert.Single(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 88 | Assert.Single(b.Months["2025-02"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 104 | Assert.Empty(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 116 | Assert.Single(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 135 | Assert.Single(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 136 | Assert.Equal(e2.Id, b.Months["2025-01"].Expenses[0].Id); |
| Revenues | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 202 | Assert.Single(b.Months["2025-01"].Revenues); |
| Revenues | Tests\BuildingManagement.Tests\ExpensesRevenuesServiceTests.cs | 237 | Assert.Empty(b.Months["2025-01"].Revenues); |
| Apartments | Tests\BuildingManagement.Tests\FinanceCommandValidationTests.cs | 12 | Apartments = [new Apartment { Id = "apt-1" }], |
| WalletAdjustments | Tests\BuildingManagement.Tests\FinanceCommandValidationTests.cs | 82 | Assert.Empty(building.WalletAdjustments); |
| WalletAdjustments | Tests\BuildingManagement.Tests\FinanceCommandValidationTests.cs | 94 | Assert.Empty(building.WalletAdjustments); |
| Buildings | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 58 | db.Buildings.Add(new BuildingEntity { Id = BuildingId, Name = "Test", BuildingNumber = "T-1" }); |
| Floors | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 59 | db.Floors.Add(new FloorEntity { BuildingId = BuildingId, Id = "floor-1", Label = "Floor 1" }); |
| Apartments | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 61 | db.Apartments.Add(new ApartmentEntity { BuildingId = BuildingId, Id = ApartmentId, FloorId = "floor-1" }); |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 70 | BuildingAdmins = new List<BuildingAdminEntity> |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 76 | db.Users.Add(new UserEntity |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 88 | var users = new UsersService(store, NullLogger<UsersService>.Instance); |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 90 | users, |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 130 | db.Users.Add(new UserEntity |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 155 | var super = await db.Users.SingleAsync(u => u.Uid == "super-1"); |
| Buildings | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 166 | seed.Buildings.Add(new BuildingEntity { Id = BuildingId, Name = "Test", BuildingNumber = "T-1" }); |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 167 | seed.Users.Add(new UserEntity |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 197 | seed.Users.Add(new UserEntity |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 247 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 269 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 295 | BuildingAdmins = new List<BuildingAdminEntity> |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 300 | var users = new UsersService( |
| Users | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 304 | users, |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 324 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 346 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 369 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 399 | BuildingAdmins = new List<BuildingAdminEntity> |
| BuildingAdmins | Tests\BuildingManagement.Tests\ImpersonatedSessionValidationTests.cs | 462 | BuildingAdmins = new List<BuildingAdminEntity>() |
| Floors | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 25 | b.Floors.Add(new Floor { Id = "f1", Label = "Floor 1", Order = 0 }); |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 26 | b.Apartments.Add(new Apartment |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 46 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 63 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 81 | var apt = b.Apartments[0]; |
| Deposits | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 83 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 96 | var apt = b.Apartments[0]; |
| Expenses | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 98 | m.Expenses.Add(new Expense { Id = "e1", CategoryId = "water", Amount = 300 }); |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 113 | b.Apartments.Add(new Apartment { Id = "a2", FloorId = "f1", Number = 2 }); |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 115 | var apt = b.Apartments[0]; |
| Expenses | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 117 | m.Expenses.Add(new Expense { Id = "e1", CategoryId = "water", Amount = 300 }); |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 132 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 145 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 158 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 174 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 188 | var apt = b.Apartments[0]; |
| Apartments | Tests\BuildingManagement.Tests\InvoiceServiceTests.cs | 201 | var apt = b.Apartments[0]; |
| Users | Tests\BuildingManagement.Tests\LegacyIdentityMappingServiceTests.cs | 40 | var mappingId = await service.RegisterPendingAsync(actor, "Users", "legacy-user-42"); |
| Users | Tests\BuildingManagement.Tests\LegacyIdentityMappingServiceTests.cs | 86 | var mappingId = await service.RegisterPendingAsync(actor, "Users", "legacy-user-43"); |
| AuditLog | Tests\BuildingManagement.Tests\LegacyIdentityMappingServiceTests.cs | 105 | Assert.Equal(2, await verify.AuditLog.CountAsync()); |
| Apartments | Tests\BuildingManagement.Tests\LegacyIdentityMappingValidatorTests.cs | 11 | Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateSource("Apartments", "apt-1")); |
| Users | Tests\BuildingManagement.Tests\LegacyIdentityMappingValidatorTests.cs | 12 | Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateSource("Users", "  ")); |
| Users | Tests\BuildingManagement.Tests\LegacyIdentityMappingValidatorTests.cs | 34 | LegacyIdentityMappingValidator.ValidateSourceSpecificFields("Users", request)); |
| Residents | Tests\BuildingManagement.Tests\LegacyIdentityMappingValidatorTests.cs | 43 | LegacyIdentityMappingValidator.ValidateSourceSpecificFields("Residents", request)); |
| Apartments | Tests\BuildingManagement.Tests\MaintenanceServiceTests.cs | 20 | b.Apartments.Add(new Apartment { Id = "a1", Number = 1, MonthlyFee = 100 }); |
| Apartments | Tests\BuildingManagement.Tests\MaintenanceServiceTests.cs | 21 | b.Apartments.Add(new Apartment { Id = "a2", Number = 2, MonthlyFee = 100 }); |
| Expenses | Tests\BuildingManagement.Tests\MaintenanceServiceTests.cs | 129 | Assert.Single(b.Months["2025-01"].Expenses); |
| Expenses | Tests\BuildingManagement.Tests\MaintenanceServiceTests.cs | 143 | Assert.Single(b.Months["2025-01"].Expenses); // مفيش تكرار |
| Apartments | Tests\BuildingManagement.Tests\NotificationsServiceTests.cs | 14 | b.Apartments.Add(new Apartment { Id = "a1", Number = 1 }); |
| Apartments | Tests\BuildingManagement.Tests\NotificationsServiceTests.cs | 15 | b.Apartments.Add(new Apartment { Id = "a2", Number = 2 }); |
| Notifications | Tests\BuildingManagement.Tests\NotificationsServiceTests.cs | 33 | Assert.Single(b.Notifications); |
| Notifications | Tests\BuildingManagement.Tests\NotificationsServiceTests.cs | 43 | Assert.Equal(200, b.Notifications.Count); |
| Apartments | Tests\BuildingManagement.Tests\PollsServiceTests.cs | 14 | b.Apartments.Add(new Apartment { Id = "a1", Number = 1 }); |
| Apartments | Tests\BuildingManagement.Tests\PollsServiceTests.cs | 15 | b.Apartments.Add(new Apartment { Id = "a2", Number = 2 }); |
| Polls | Tests\BuildingManagement.Tests\PollsServiceTests.cs | 34 | Assert.Single(b.Polls); |
| Polls | Tests\BuildingManagement.Tests\PollsServiceTests.cs | 150 | Assert.Empty(b.Polls); |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 21 | var p = new Presence { Ts = 0 }; |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 29 | var p = new Presence { Ts = now }; |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 37 | var p = new Presence { Ts = old }; |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 45 | var p = new Presence { Ts = now }; |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 53 | var p = new Presence { Ts = old }; |
| Presence | Tests\BuildingManagement.Tests\PresenceQrMediaMtxTests.cs | 69 | var p = new Presence { Ts = now }; |
| Apartments | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 20 | b.Apartments.Add(new Apartment { Id = "a1", Number = 1, MonthlyFee = 100 }); |
| Apartments | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 21 | b.Apartments.Add(new Apartment { Id = "a2", Number = 2, MonthlyFee = 100 }); |
| Deposits | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 136 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 137 | m.Deposits.Add(new Deposit { Id = "d2", AptId = "a2", Amount = 300, Status = "confirmed" }); |
| Expenses | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 150 | m.Expenses.Add(new Expense { Id = "e1", Amount = 200 }); |
| Expenses | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 151 | m.Expenses.Add(new Expense { Id = "e2", Amount = 150 }); |
| Revenues | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 163 | m.Revenues.Add(new Revenue { Id = "r1", Amount = 100 }); |
| Deposits | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 175 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Expenses | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 176 | m.Expenses.Add(new Expense { Id = "e1", Amount = 200 }); |
| Revenues | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 177 | m.Revenues.Add(new Revenue { Id = "r1", Amount = 100 }); |
| Deposits | Tests\BuildingManagement.Tests\ReportsServiceTests.cs | 204 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Apartments | Tests\BuildingManagement.Tests\ResidentsServiceSetPinTests.cs | 51 | _db.Apartments.Add(new ApartmentEntity |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceSetPinTests.cs | 60 | var resident = await _db.Residents.SingleAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceSetPinTests.cs | 70 | _db.Residents.AddRange( |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceSetPinTests.cs | 80 | var all = await _db.Residents.ToDictionaryAsync(r => r.Id); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 51 | var resident = await _db.Residents.FirstOrDefaultAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 69 | var resident = await _db.Residents.FirstOrDefaultAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 86 | var resident = await _db.Residents.FirstAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 92 | var updated = await _db.Residents.FindAsync(residentId); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 108 | Assert.Empty(_db.Residents); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 121 | var resident = await _db.Residents.FirstAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 127 | var updated = await _db.Residents.FindAsync(residentId); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 144 | var resident = await _db.Residents.FirstAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 150 | var updated = await _db.Residents.FindAsync(residentId); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 163 | var resident = await _db.Residents.FirstAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 170 | var updated = await _db.Residents.FindAsync(residentId); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 187 | var resident = await _db.Residents.FirstAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 193 | Assert.Empty(_db.Residents); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 207 | var residents = await _service.GetByBuildingAsync("b1"); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 209 | Assert.Equal(2, residents.Count); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 210 | Assert.All(residents, r => Assert.Equal("b1", r.BuildingId)); |
| Buildings | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 225 | _db.Buildings.Add(building); |
| Floors | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 226 | _db.Floors.Add(floor); |
| Apartments | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 227 | _db.Apartments.Add(apt); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 235 | var resident = await _db.Residents.FirstOrDefaultAsync(); |
| Residents | Tests\BuildingManagement.Tests\ResidentsServiceTests.cs | 253 | var resident = await _db.Residents.FirstAsync(); |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 38 | var settings = new ScheduledBackupSettings { MaxBackupsToKeep = keep }; |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 49 | var settings = new ScheduledBackupSettings { Hour = hour }; |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 58 | var settings = new ScheduledBackupSettings { Frequency = "hourly" }; |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 72 | var settings = new ScheduledBackupSettings |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 86 | var settings = new ScheduledBackupSettings |
| Buildings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 89 | Collections = new List<string> { "buildings", "not-a-collection" } |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 99 | var settings = new ScheduledBackupSettings |
| Buildings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 106 | Collections = new List<string> { "buildings", "buildings" } |
| ScheduledBackupSettings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 112 | var saved = await db.ScheduledBackupSettings.SingleAsync(); |
| Buildings | Tests\BuildingManagement.Tests\ScheduledBackupServiceTests.cs | 116 | Assert.Equal(new[] { "buildings" }, |
| LoginThrottle | Tests\BuildingManagement.Tests\SecurityHardeningTests.cs | 78 | private static LoginThrottle Create() => new(new MemoryCache(new MemoryCacheOptions())); |
| LoginThrottle | Tests\BuildingManagement.Tests\SecurityHardeningTests.cs | 119 | Assert.Equal(LoginThrottle.K("  User@X.com "), LoginThrottle.K("user@x.com")); |
| LoginThrottle | Tests\BuildingManagement.Tests\SecurityHardeningTests.cs | 120 | Assert.DoesNotContain("user", LoginThrottle.K("user@x.com")); |
| LoginThrottle | Tests\BuildingManagement.Tests\SecurityHardeningTests.cs | 121 | Assert.True(LoginThrottle.K("user@x.com").Length <= 80); |
| Users | Tests\BuildingManagement.Tests\SessionRevalidatorCacheInvalidationTests.cs | 16 | users: null!, |
| Users | Tests\BuildingManagement.Tests\SessionRevalidatorCacheInvalidationTests.cs | 41 | users: null!, |
| Users | Tests\BuildingManagement.Tests\TwoFactorServiceTests.cs | 64 | db.Users.Add(new UserEntity { Uid = "admin-1", Role = "admin", Email = "admin@example.test" }); |
| Users | Tests\BuildingManagement.Tests\TwoFactorServiceTests.cs | 83 | var user = await db.Users.SingleAsync(x => x.Uid == "admin-1"); |
| Users | Tests\BuildingManagement.Tests\TwoFactorServiceTests.cs | 122 | var user = await db.Users.SingleAsync(x => x.Uid == "admin-1"); |
| Users | Tests\BuildingManagement.Tests\TwoFactorServiceTests.cs | 142 | var user = await db.Users.SingleAsync(x => x.Uid == "admin-1"); |
| Buildings | Tests\BuildingManagement.Tests\UsersServiceTests.cs | 144 | ["permissions"] = new List<string> { "buildings.manage", "wallet.manage" } |
| Buildings | Tests\BuildingManagement.Tests\UsersServiceTests.cs | 154 | d.Permissions.Contains("buildings.manage") |
| Apartments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 20 | b.Apartments.Add(new Apartment |
| Apartments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 25 | Number = b.Apartments.Count + 1 |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 70 | prev.Deposits.Add(new Deposit |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 116 | prev.Deposits.Add(new Deposit |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 145 | m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = "D001", Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 155 | m.Deposits.Add(new Deposit { Id = "x1", AptId = "a1", Number = "D005", Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 156 | m.Deposits.Add(new Deposit { Id = "x2", AptId = "a1", Number = "D012", Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 157 | m.Deposits.Add(new Deposit { Id = "x3", AptId = "a1", Number = "D003", Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 172 | m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = invalidNumber, Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 182 | m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = null!, Amount = 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 216 | Assert.Single(m.Deposits); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 375 | month.Expenses.Add(new Expense { Id = "e1", Amount = 100m }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 385 | month.Expenses.Add(new Expense { Id = "e1", Amount = 100m }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 400 | Svc.EnsureMonth(first, Month).Expenses.Add(new Expense { Id = "e1", Amount = 100m }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 401 | Svc.EnsureMonth(second, Month).Expenses.Add(new Expense { Id = "e1", Amount = 100m }); |
| Apartments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 412 | var apartments = Enumerable.Range(1, 57) |
| Apartments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 415 | var b = BuildingWithApartments(apartments); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 417 | month.Expenses.Add(new Expense { Id = "e1", Amount = 4700m }); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 418 | month.Revenues.Add(new Revenue { Id = "r1", Amount = 27173m }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 420 | var expenses = Svc.RecalculateDistribution(b, Month); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 421 | var revenues = Svc.RecalculateRevenueDistribution(b, Month); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 423 | Assert.Equal(4700m, expenses.Values.Sum()); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 424 | Assert.Equal(35, expenses.Values.Count(x => x == 82.46m)); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 425 | Assert.Equal(22, expenses.Values.Count(x => x == 82.45m)); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 426 | Assert.Equal(27173m, revenues.Values.Sum()); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 427 | Assert.Equal(53, revenues.Values.Count(x => x == 476.72m)); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 428 | Assert.Equal(4, revenues.Values.Count(x => x == 476.71m)); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 436 | month.Revenues.Add(new Revenue { Id = "r1", Amount = 100m }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 551 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 564 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 577 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "pending" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 632 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" }); |
| WalletAdjustments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 663 | Assert.Single(b.WalletAdjustments); |
| WalletAdjustments | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 672 | Assert.Empty(b.WalletAdjustments); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 684 | m.Expenses.Add(new Expense { Id = "e1", Amount = 300 }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 697 | m.Expenses.Add(new Expense { Id = "e1", Amount = 100 }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 710 | m.Expenses.Add(new Expense { Id = "e1", Amount = 100 }); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 739 | m.Revenues.Add(new Revenue { Id = "r1", Amount = 200 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 768 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 785 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 300, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 786 | m.Deposits.Add(new Deposit { Id = "d2", AptId = "a2", Amount = 200, Status = "confirmed" }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 787 | m.Expenses.Add(new Expense { Id = "e1", Amount = 100 }); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 788 | m.Revenues.Add(new Revenue { Id = "r1", Amount = 50 }); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 790 | var (collected, expenses, revenues, openCount, closedCount, _, _) = |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 790 | var (collected, expenses, revenues, openCount, closedCount, _, _) = |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 794 | Assert.Equal(100, expenses); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 795 | Assert.Equal(50, revenues); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 849 | m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "pending" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTests.cs | 851 | m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "pending" }); |
| Apartments | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 19 | b.Apartments.Add(new Apartment { Id = id, MonthlyFee = 100, Number = b.Apartments.Count + 1 }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 94 | // GetAllTransactions - deposits |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 103 | m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed", CreatedAt = "2024-11-01T10:00:00Z" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 106 | m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed", CreatedAt = "2024-12-01T10:00:00Z" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 109 | var deposits = result.Where(t => t.Type == "deposit").ToList(); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 111 | Assert.Equal(2, deposits.Count); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 112 | Assert.Contains(deposits, d => d.Id == "d1"); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 113 | Assert.Contains(deposits, d => d.Id == "d2"); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 121 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 124 | var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits"); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 134 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 135 | m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "pending" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 137 | // نوع الفلتر "deposits,pending" |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 138 | var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits,pending"); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 149 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 150 | m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "cancelled" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 152 | var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits,success"); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 187 | var expenses = result.Where(t => t.Type == "expense").ToList(); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 189 | Assert.Single(expenses); |
| Expenses | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 190 | Assert.Equal(-75, expenses[0].Amount); // من الشهر الأحدث فقط |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 204 | var revenues = result.Where(t => t.Type == "revenue").ToList(); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 206 | Assert.Single(revenues); |
| Revenues | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 207 | Assert.Equal(200, revenues[0].Amount); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 220 | m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 223 | m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 226 | var deposits = result.Where(t => t.Type == "deposit").ToList(); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 228 | Assert.Single(deposits); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 229 | Assert.Equal("d2", deposits[0].Id); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 238 | m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 241 | m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 244 | var deposits = result.Where(t => t.Type == "deposit").ToList(); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 246 | Assert.Single(deposits); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 247 | Assert.Equal("d1", deposits[0].Id); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 259 | m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed", CreatedAt = "2024-11-01T10:00:00Z" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 260 | m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed", CreatedAt = "2024-11-05T10:00:00Z" }); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 263 | var deposits = result.Where(t => t.Type == "deposit").ToList(); |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 265 | Assert.Equal("d2", deposits[0].Id); // الأحدث الأول |
| Deposits | Tests\BuildingManagement.Tests\WalletServiceTransactionsTests.cs | 266 | Assert.Equal("d1", deposits[1].Id); |
| Floors | Tests\BuildingManagement.Tests\WhatsAppTemplateServiceTests.cs | 152 | b.Floors = new List<Floor>(); // مفيش أدوار |

## Documentation/schema reference locations

| Table | File | Line | Reference |
|---|---|---:|---|
| MonthlyApartmentShares | Database\Diagnostics\Run-Financial-Reconciliation-ReadOnly-Validation.ps1 | 23 | if ($sql -notmatch 'MonthlyApartmentShares' -or $sql -notmatch 'WalletAdjustments') { throw "Safety stop: expected financial reconciliation sources missing." } |
| WalletAdjustments | Database\Diagnostics\Run-Financial-Reconciliation-ReadOnly-Validation.ps1 | 23 | if ($sql -notmatch 'MonthlyApartmentShares' -or $sql -notmatch 'WalletAdjustments') { throw "Safety stop: expected financial reconciliation sources missing." } |
| Deposits | Database\Diagnostics\Run-Financial-Reconciliation-ReadOnly-Validation.ps1 | 95 | - Export per-apartment stored shares, carry-over, confirmed deposits, adjustments, and CURRENT apartment fee/closed state. |
| Apartments | Database\Diagnostics\Run-GateA-Preflight.ps1 | 109 | - Verify Buildings/Apartments key and index metadata. |
| Buildings | Database\Diagnostics\Run-GateA-Preflight.ps1 | 109 | - Verify Buildings/Apartments key and index metadata. |
| Buildings | Database\Diagnostics\Run-Redesign-Schema-Parity.ps1 | 291 | WHERE (t.name = N'Buildings' AND c.name = N'Id') |
| Apartments | Database\Diagnostics\Run-Redesign-Schema-Parity.ps1 | 292 | OR (t.name = N'Apartments' AND c.name IN (N'BuildingId', N'Id')) |
| __EFMigrationsHistory | Database\Diagnostics\Run-Redesign-Schema-Parity.ps1 | 297 | IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL |
| __EFMigrationsHistory | Database\Diagnostics\Run-Redesign-Schema-Parity.ps1 | 298 | SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId; |
| __EFMigrationsHistory | Database\Diagnostics\Run-Redesign-Schema-Parity.ps1 | 300 | SELECT CAST(N'__EFMigrationsHistory not found' AS nvarchar(200)) AS Status; |
| Presence | Database\Diagnostics\Run-Wallet-and-Credential-ReadOnly-Validation.ps1 | 82 | - Inspect the presence and definitions of wallet-named SQL views. |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 129 | principalTable: "Apartments", |
| Buildings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 134 | principalTable: "Buildings", |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 180 | principalTable: "Apartments", |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 224 | principalTable: "Apartments", |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 263 | principalTable: "Apartments", |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 304 | table.CheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 304 | table.CheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 306 | table.CheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIâ€¦ |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 306 | table.CheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIâ€¦ |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 406 | principalTable: "Apartments", |
| Buildings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.cs | 411 | principalTable: "Buildings", |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 283 | b.ToTable("Apartments", (string)null); |
| AuditLog | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 521 | b.ToTable("AuditLog", (string)null); |
| BackupHistory | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 563 | b.ToTable("BackupHistory", (string)null); |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 580 | b.ToTable("BuildingAdmins", (string)null); |
| Buildings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 643 | b.ToTable("Buildings", (string)null); |
| BuildingSettings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 686 | b.ToTable("BuildingSettings", (string)null); |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 733 | b.ToTable("Cameras", (string)null); |
| DepositEditHistory | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 785 | b.ToTable("DepositEditHistory", (string)null); |
| Deposits | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 877 | b.ToTable("Deposits", (string)null); |
| Dvrs | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 927 | b.ToTable("Dvrs", (string)null); |
| Expenses | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 972 | b.ToTable("Expenses", (string)null); |
| FinancialCategories | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1005 | b.ToTable("FinancialCategories", (string)null); |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1027 | b.ToTable("Floors", (string)null); |
| GlobalSettings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1093 | b.ToTable("GlobalSettings", (string)null); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1184 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1184 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1188 | t.HasCheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIdâ€¦ |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1188 | t.HasCheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIdâ€¦ |
| MaintenanceRecords | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1240 | b.ToTable("MaintenanceRecords", (string)null); |
| MonthlyApartmentShares | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1273 | b.ToTable("MonthlyApartmentShares", (string)null); |
| Notifications | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1323 | b.ToTable("Notifications", (string)null); |
| Polls | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1425 | b.ToTable("Polls", (string)null); |
| PollOptions | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1451 | b.ToTable("PollOptions", (string)null); |
| PollVotes | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1482 | b.ToTable("PollVotes", (string)null); |
| Presence | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1504 | b.ToTable("Presence", (string)null); |
| QrTokens | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1553 | b.ToTable("QrTokens", (string)null); |
| QrUsages | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1590 | b.ToTable("QrUsages", (string)null); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1683 | b.ToTable("Residents", (string)null); |
| Revenues | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1725 | b.ToTable("Revenues", (string)null); |
| ScheduledBackupSettings | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1818 | b.ToTable("ScheduledBackupSettings", (string)null); |
| SyncPendingChanges | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 1865 | b.ToTable("SyncPendingChanges", (string)null); |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2068 | b.ToTable("Users", (string)null); |
| UserPermissions | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2083 | b.ToTable("UserPermissions", (string)null); |
| WalletAdjustments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2280 | b.ToTable("WalletAdjustments", (string)null); |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2341 | .WithMany("Apartments") |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2347 | .WithMany("Apartments") |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2408 | .WithMany("BuildingAdmins") |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2438 | .WithMany("Cameras") |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2513 | .WithMany("Floors") |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2782 | b.Navigation("Apartments"); |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2786 | b.Navigation("Floors"); |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2798 | b.Navigation("Cameras"); |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2803 | b.Navigation("Apartments"); |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\20261118172211_Phase06_IdentityAuthorizationFoundation.Designer.cs | 2818 | b.Navigation("BuildingAdmins"); |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 280 | b.ToTable("Apartments", (string)null); |
| AuditLog | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 518 | b.ToTable("AuditLog", (string)null); |
| BackupHistory | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 560 | b.ToTable("BackupHistory", (string)null); |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 577 | b.ToTable("BuildingAdmins", (string)null); |
| Buildings | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 640 | b.ToTable("Buildings", (string)null); |
| BuildingSettings | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 683 | b.ToTable("BuildingSettings", (string)null); |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 730 | b.ToTable("Cameras", (string)null); |
| DepositEditHistory | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 782 | b.ToTable("DepositEditHistory", (string)null); |
| Deposits | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 874 | b.ToTable("Deposits", (string)null); |
| Dvrs | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 924 | b.ToTable("Dvrs", (string)null); |
| Expenses | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 969 | b.ToTable("Expenses", (string)null); |
| FinancialCategories | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1002 | b.ToTable("FinancialCategories", (string)null); |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1024 | b.ToTable("Floors", (string)null); |
| GlobalSettings | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1090 | b.ToTable("GlobalSettings", (string)null); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1181 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1181 | t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')"); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1185 | t.HasCheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIdâ€¦ |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1185 | t.HasCheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountIdâ€¦ |
| MaintenanceRecords | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1237 | b.ToTable("MaintenanceRecords", (string)null); |
| MonthlyApartmentShares | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1270 | b.ToTable("MonthlyApartmentShares", (string)null); |
| Notifications | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1320 | b.ToTable("Notifications", (string)null); |
| Polls | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1422 | b.ToTable("Polls", (string)null); |
| PollOptions | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1448 | b.ToTable("PollOptions", (string)null); |
| PollVotes | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1479 | b.ToTable("PollVotes", (string)null); |
| Presence | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1501 | b.ToTable("Presence", (string)null); |
| QrTokens | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1550 | b.ToTable("QrTokens", (string)null); |
| QrUsages | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1587 | b.ToTable("QrUsages", (string)null); |
| Residents | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1680 | b.ToTable("Residents", (string)null); |
| Revenues | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1722 | b.ToTable("Revenues", (string)null); |
| ScheduledBackupSettings | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1815 | b.ToTable("ScheduledBackupSettings", (string)null); |
| SyncPendingChanges | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 1862 | b.ToTable("SyncPendingChanges", (string)null); |
| Users | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2065 | b.ToTable("Users", (string)null); |
| UserPermissions | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2080 | b.ToTable("UserPermissions", (string)null); |
| WalletAdjustments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2277 | b.ToTable("WalletAdjustments", (string)null); |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2338 | .WithMany("Apartments") |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2344 | .WithMany("Apartments") |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2405 | .WithMany("BuildingAdmins") |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2435 | .WithMany("Cameras") |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2510 | .WithMany("Floors") |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2779 | b.Navigation("Apartments"); |
| Floors | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2783 | b.Navigation("Floors"); |
| Cameras | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2795 | b.Navigation("Cameras"); |
| Apartments | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2800 | b.Navigation("Apartments"); |
| BuildingAdmins | Database\Diagnostics\Phase06-Review-20261118-193249\AppDbContextModelSnapshot.cs | 2815 | b.Navigation("BuildingAdmins"); |

## Decision rules

- Runtime references must be resolved by an owning-module cutover; zero text references alone does not authorize DROP.
- Test-only references indicate tests still encode legacy contracts and must be migrated or intentionally retired.
- Documentation/schema references are not runtime blockers, but schema snapshots and migration history remain controlled artifacts.
- Buildings, Floors and Apartments are foundational and excluded from retirement regardless of counts.
- __EFMigrationsHistory is infrastructure metadata and is never part of legacy-domain retirement.
- This audit does not authorize DDL. Retirement still requires target data parity, FK review, EF mapping review, module cutover and rollback acceptance.
