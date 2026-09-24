using System.Text.RegularExpressions;
using Google.Cloud.Firestore;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/services/buildings-service.js
public class BuildingsService
{
    private readonly FirestoreDb _db;
    private readonly FirebaseAuthRestService _fbAuth;

    public BuildingsService(FirestoreContext ctx, FirebaseAuthRestService fbAuth)
    {
        _db = ctx.Db;
        _fbAuth = fbAuth;
    }

    private CollectionReference Col => _db.Collection("buildings");

    public async Task<List<Building>> GetAllAsync()
    {
        var snap = await Col.OrderBy("buildingNumber").GetSnapshotAsync();
        var buildings = snap.Documents.Select(d => d.ConvertTo<Building>()).ToList();

        foreach (var b in buildings) NormalizeOrder(b);   // ← ضيف السطر ده

        return buildings;
    }

    public async Task<Building?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var doc = await Col.Document(id).GetSnapshotAsync();
        if (!doc.Exists) return null;

        var building = doc.ConvertTo<Building>();
        NormalizeOrder(building);   // ← ضيف السطر ده

        return building;
    }

    // إنشاء عمارة جديدة برقم تلقائي BLD-001, BLD-002...
    public async Task<(string Id, string BuildingNumber)> CreateAsync(string name, string adminPin)
    {
        var all = await GetAllAsync();
        var maxNum = 0;
        foreach (var b in all)
        {
            var m = Regex.Match(b.BuildingNumber ?? "", @"^BLD-(\d+)$");
            if (m.Success) maxNum = Math.Max(maxNum, int.Parse(m.Groups[1].Value));
        }
        var nextNumber = "BLD-" + (maxNum + 1).ToString("D3");

        var building = new Building
        {
            BuildingNumber = nextNumber,
            Name = name,
            AdminPin = adminPin,
            DataVersion = "3.2",
            ExpenseCategories = DefaultExpenseCategories(),
            RevenueCategories = DefaultRevenueCategories(),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        var docRef = await Col.AddAsync(building);
        return (docRef.Id, nextNumber);
    }

    public async Task DeleteAsync(string id) => await Col.Document(id).DeleteAsync();

    // حفظ كامل (استبدال) — بديل saveBuilding
    public async Task SaveFullAsync(Building building) =>
        await Col.Document(building.Id).SetAsync(building, SetOptions.Overwrite);

    // حفظ جزئي (merge) — بديل updateBuilding
    public async Task UpdateAsync(string id, Dictionary<string, object> partialData)
    {
        partialData["dataVersion"] = "3.2";
        await Col.Document(id).SetAsync(partialData, SetOptions.MergeAll);
    }

    // إضافة دور جديد بعدد شقق معين (كل شقة معاها رقم واتساب + PIN)
    // بيعمل حساب Firebase Auth لكل شقة، زي ما بيحصل بالظبط في add-floor-modal.js
    public async Task<string> AddFloorAsync(string buildingId, string label, List<(string Phone, string Pin)> apartments)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var maxOrder = building.Floors.Count == 0 ? -1 : building.Floors.Max(f => f.Order);
        var floorOrder = maxOrder + 1;
        var floorId = Guid.NewGuid().ToString("N");
        building.Floors.Add(new Floor { Id = floorId, Label = label, Order = floorOrder });

        var number = building.Apartments.Count == 0
            ? 1
            : building.Apartments.Max(a => a.Number) + 1;
        foreach (var (phoneRaw, pin) in apartments)
        {
            var phone = AuthHelpers.NormalizePhone(phoneRaw);
            building.Apartments.Add(new Apartment
            {
                Id = Guid.NewGuid().ToString("N"),
                FloorId = floorId,
                Number = number,
                Phone = phone,
                MonthlyFee = 0,
                Pin = pin,
                Closed = false,
                OpenDate = DateTime.UtcNow.ToString("o")
            });

            var email = AuthHelpers.ResidentInternalEmail(building.Id, floorOrder, number);
            var password = AuthHelpers.ResidentPassword(building.Id, number, pin);
            await _fbAuth.CreateUserAsync(email, password);

            number++;
        }

        await SaveFullAsync(building);
        return floorId;
    }

    // إضافة شقة واحدة في دور موجود
    public async Task AddApartmentAsync(string buildingId, string floorId, string phoneRaw, string pin)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var floor = building.Floors.FirstOrDefault(f => f.Id == floorId)
            ?? throw new InvalidOperationException("floor-not-found");

        var nextNum = building.Apartments.Count == 0
            ? 1
            : building.Apartments.Max(a => a.Number) + 1;
        var phone = AuthHelpers.NormalizePhone(phoneRaw);

        building.Apartments.Add(new Apartment
        {
            Id = Guid.NewGuid().ToString("N"),
            FloorId = floorId,
            Number = nextNum,
            Phone = phone,
            MonthlyFee = 0,
            Pin = pin,
            Closed = false,
            OpenDate = DateTime.UtcNow.ToString("o")
        });

        var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, nextNum);
        var password = AuthHelpers.ResidentPassword(building.Id, nextNum, pin);
        await _fbAuth.CreateUserAsync(email, password);

        await SaveFullAsync(building);
    }

    public static List<FinancialCategory> DefaultExpenseCategories() => new()
    {
        new FinancialCategory{ Id="water",       Name="مياه",   Color="#4A90D9", Active=true, Order=0 },
        new FinancialCategory{ Id="electricity", Name="كهرباء", Color="#D9B34A", Active=true, Order=1 },
        new FinancialCategory{ Id="cleaning",    Name="نظافة",  Color="#4FB286", Active=true, Order=2 },
        new FinancialCategory{ Id="other",       Name="أخرى",   Color="#A0432A", Active=true, Order=3 },
    };

    public static List<FinancialCategory> DefaultRevenueCategories() => new()
    {
        new FinancialCategory{ Id="rent-roof",     Name="تأجير السطح", Color="#2E7D5B", Active=true, Order=0 },
        new FinancialCategory{ Id="rent-shop",     Name="تأجير محل",   Color="#4FB286", Active=true, Order=1 },
        new FinancialCategory{ Id="scrap",         Name="بيع خردة",    Color="#8E6BB2", Active=true, Order=2 },
        new FinancialCategory{ Id="other-revenue", Name="إيراد آخر",   Color="#B9853B", Active=true, Order=3 },
    };
    // ✅ Helper: ترتيب الأدوار والشقق بشكل موحّد
    private static void NormalizeOrder(Building building)
    {
        building.Floors = building.Floors.OrderBy(f => f.Order).ToList();

        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);

        building.Apartments = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .ToList();
    }

}
