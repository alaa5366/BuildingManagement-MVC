﻿namespace BuildingManagementMvc.Services;

// ترجمة حرفية لدوال js/utils/auth-helpers.js و js/utils/phone-utils.js
// عشان الحسابات المُشفَّرة (internal email/password) تطلع متطابقة 100%
// مع نسخة الـ JS، فتقدر تستخدم نفس مستخدمي Firebase Auth الموجودين.
/// <summary>
/// Shared authentication helpers such as phone-number normalization and internal e-mail generation.
/// </summary>
public static class AuthHelpers
{
    /// <summary>
    /// Returns the resident internal email.
    /// </summary>
    public static string ResidentInternalEmail(string buildingId, int floorOrder, int aptNumber)
        => $"res-{buildingId.ToLowerInvariant()}-{floorOrder}-{aptNumber}@building.local";

    /// <summary>
    /// Returns the resident password.
    /// </summary>
    public static string ResidentPassword(string buildingId, int aptNumber, string pin)
    {
        var last6 = Last(buildingId, 6).ToLowerInvariant();
        return $"res-{last6}-{aptNumber}-{pin}";
    }

    /// <summary>
    /// Returns the admin email for phone.
    /// </summary>
    public static string AdminEmailForPhone(string buildingId, string phone)
    {
        var last6 = Last(buildingId, 6).ToLowerInvariant();
        var digits = DigitsOnly(phone);
        return $"adm-{last6}-{digits}@building.local";
    }

    /// <summary>
    /// Returns the admin password for phone.
    /// </summary>
    public static string AdminPasswordForPhone(string phone, string pin)
    {
        var digits = DigitsOnly(phone);
        return $"adm-{digits}-{pin}";
    }

    // نفس منطق normalizePhone بالظبط (يفترض أرقام مصرية بصيغة +20)
    /// <summary>
    /// Normalizes the phone.
    /// </summary>
    public static string NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var d = DigitsOnly(raw);
        if (string.IsNullOrEmpty(d)) return "";
        if (d.StartsWith("00")) d = d[2..];
        if (d.StartsWith("20")) return "+" + d;
        if (d.StartsWith("0")) return "+20" + d[1..];
        if (d.Length >= 11) return "+" + d;
        return "+20" + d;
    }

    private static string DigitsOnly(string? s) =>
        new string((s ?? "")
            .Where(char.IsDigit)
            .Select(character => (char)('0' + (int)char.GetNumericValue(character)))
            .ToArray());

    private static string Last(string s, int n) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[^n..]);
}
