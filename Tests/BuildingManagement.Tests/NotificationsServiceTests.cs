using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class NotificationsServiceTests
{
    private readonly NotificationsService _service = new();

    private static Building CreateBuilding()
    {
        var b = new Building { Name = "Test" };
        b.Apartments.Add(new Apartment { Id = "a1", Number = 1 });
        b.Apartments.Add(new Apartment { Id = "a2", Number = 2 });
        return b;
    }

    // ================================================================
    // Push
    // ================================================================

    [Fact]
    public void Push_ValidData_AddsNotification()
    {
        var b = CreateBuilding();
        var n = _service.Push(b, "admin", null, "info", "🔔", "Title", "Body");

        Assert.NotNull(n);
        Assert.Equal("admin", n.RecipientType);
        Assert.Equal("Title", n.Title);
        Assert.False(n.Read);
        Assert.Single(b.Notifications);
    }

    [Fact]
    public void Push_MoreThan200_TrimsTo200()
    {
        var b = CreateBuilding();
        for (int i = 0; i < 250; i++)
            _service.Push(b, "admin", null, "info", "🔔", $"T{i}", "B");

        Assert.Equal(200, b.Notifications.Count);
    }

    // ================================================================
    // GetForAdmin / GetForResident
    // ================================================================

    [Fact]
    public void GetForAdmin_FiltersByRecipientType()
    {
        var b = CreateBuilding();
        _service.Push(b, "admin", null, "info", "🔔", "Admin1", "B");
        _service.Push(b, "resident", "a1", "info", "🔔", "Resident1", "B");
        _service.Push(b, "admin", null, "info", "🔔", "Admin2", "B");

        var adminNotifs = _service.GetForAdmin(b);

        Assert.Equal(2, adminNotifs.Count);
        Assert.All(adminNotifs, n => Assert.Equal("admin", n.RecipientType));
    }

    [Fact]
    public void GetForResident_FiltersByApartment()
    {
        var b = CreateBuilding();
        _service.Push(b, "resident", "a1", "info", "🔔", "For A1", "B");
        _service.Push(b, "resident", "a2", "info", "🔔", "For A2", "B");

        var a1Notifs = _service.GetForResident(b, "a1");

        Assert.Single(a1Notifs);
        Assert.Equal("For A1", a1Notifs[0].Title);
    }

    // ================================================================
    // UnreadCount
    // ================================================================

    [Fact]
    public void UnreadCountForAdmin_CountsUnreadOnly()
    {
        var b = CreateBuilding();
        _service.Push(b, "admin", null, "info", "🔔", "T1", "B");
        var n2 = _service.Push(b, "admin", null, "info", "🔔", "T2", "B");
        _service.MarkRead(b, n2.Id,"admin",null);

        Assert.Equal(1, _service.UnreadCountForAdmin(b));
    }

    // ================================================================
    // MarkRead
    // ================================================================

    [Fact]
    public void MarkRead_ExistingNotification_SetsReadAndReadAt()
    {
        var b = CreateBuilding();
        var n = _service.Push(b, "admin", null, "info", "🔔", "T", "B");

        var result = _service.MarkRead(b, n.Id, "admin", null);

        Assert.True(result);
        Assert.True(n.Read);
        Assert.NotNull(n.ReadAt);
    }

    [Fact]
    public void MarkRead_NonExisting_ReturnsFalse()
    {
        var b = CreateBuilding();
        Assert.False(_service.MarkRead(b, "ghost","owner-1", "owner"));
    }

    // ================================================================
    // MarkAllRead
    // ================================================================

    [Fact]
    public void MarkAllReadForAdmin_MarksAllAdminNotifications()
    {
        var b = CreateBuilding();
        _service.Push(b, "admin", null, "info", "🔔", "T1", "B");
        _service.Push(b, "admin", null, "info", "🔔", "T2", "B");
        _service.Push(b, "resident", "a1", "info", "🔔", "R1", "B");

        _service.MarkAllReadForAdmin(b);

        Assert.Equal(0, _service.UnreadCountForAdmin(b));
        // إشعار الساكن لسه غير مقروء
        Assert.Equal(1, _service.UnreadCountForResident(b, "a1"));
    }

    [Fact]
    public void MarkAllReadForResident_MarksOnlySpecificApartment()
    {
        var b = CreateBuilding();
        _service.Push(b, "resident", "a1", "info", "🔔", "A1-1", "B");
        _service.Push(b, "resident", "a1", "info", "🔔", "A1-2", "B");
        _service.Push(b, "resident", "a2", "info", "🔔", "A2-1", "B");

        _service.MarkAllReadForResident(b, "a1");

        Assert.Equal(0, _service.UnreadCountForResident(b, "a1"));
        Assert.Equal(1, _service.UnreadCountForResident(b, "a2"));
    }
}