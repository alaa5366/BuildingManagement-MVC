using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using System.Text.RegularExpressions;

namespace BuildingManagementMvc.Services;

public class BuildingsService
{
    private readonly BuildingSqlStore _sql;
    private readonly ILogger<BuildingsService>? _log;

    public BuildingsService(
        BuildingSqlStore sql,
        ILogger<BuildingsService>? log = null)
    {
        _sql = sql;
        _log = log;
    }

    // ============================================================
    // ✅ القراءة
    // ============================================================
    public async Task<List<Building>> GetAllAsync()
    {
        var list = await _sql.GetAllAsync();
        foreach (var b in list) NormalizeOrder(b);
        return list;
    }

    public async Task<Building?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var building = await _sql.GetByIdAsync(id);
        if (building != null) NormalizeOrder(building);
        return building;
    }

    // ============================================================
    // ✅ الكتابة
    // ============================================================
    public async Task<(string Id, string BuildingNumber)> CreateAsync(
        string name, string adminPin, string adminWhatsapp = "",
        string? logoUrl = null, string? buildingNumber = null,
        bool addDefaultExpenseCategories = true,
        bool addDefaultRevenueCategories = true)
    {
        string nextNumber;
        if (!string.IsNullOrWhiteSpace(buildingNumber))
        {
            var all = await GetAllAsync();
            if (all.Any(b => b.BuildingNumber == buildingNumber))
                throw new InvalidOperationException(
                    Loc.T("Building_Number_N_Is_Already_In", buildingNumber));
            nextNumber = buildingNumber.Trim();
        }
        else
        {
            var all = await GetAllAsync();
            var maxNum = 0;
            foreach (var b in all)
            {
                var m = Regex.Match(b.BuildingNumber ?? "", @"^BLD-(\d+)$");
                if (m.Success) maxNum = Math.Max(maxNum, int.Parse(m.Groups[1].Value));
            }
            nextNumber = "BLD-" + (maxNum + 1).ToString("D3");
        }

        var building = new Building
        {
            Id = Guid.NewGuid().ToString("N"),
            BuildingNumber = nextNumber,
            Name = name,
            AdminPin = adminPin,
            AdminWhatsapp = AuthHelpers.NormalizePhone(adminWhatsapp),
            LogoUrl = logoUrl ?? "",
            DataVersion = "3.2",
            ExpenseCategories = addDefaultExpenseCategories ? DefaultExpenseCategories() : new(),
            RevenueCategories = addDefaultRevenueCategories ? DefaultRevenueCategories() : new(),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        await _sql.SaveFullAsync(building);
        return (building.Id, nextNumber);
    }

    public async Task DeleteAsync(string id)
    {
        await _sql.DeleteAsync(id);
    }

    public async Task SaveFullAsync(Building building)
    {
        await _sql.SaveFullAsync(building);
    }

    // ============================================================
    // ✅ إضافة أدوار وشقق
    // ============================================================
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

            number++;
        }

        await SaveFullAsync(building);
        return floorId;
    }

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

    private static void NormalizeOrder(Building building)
    {
        building.Floors = building.Floors.OrderBy(f => f.Order).ToList();
        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);
        building.Apartments = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .ToList();
    }

    public async Task UpdateApartmentAsync(
        string buildingId, string aptId, string owner, string label,
        string phone, string pin, double monthlyFee, string notes, string? email = null)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId)
            ?? throw new InvalidOperationException("apt-not-found");

        apt.Owner = owner ?? "";
        apt.Label = label ?? "";
        apt.Phone = AuthHelpers.NormalizePhone(phone);
        apt.Pin = pin;
        apt.MonthlyFee = monthlyFee;
        apt.Notes = notes ?? "";
        if (!string.IsNullOrWhiteSpace(email))
            apt.Email = email;

        await SaveFullAsync(building);
    }
}