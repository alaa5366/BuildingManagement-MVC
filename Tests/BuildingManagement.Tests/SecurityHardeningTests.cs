using System.Security.Cryptography;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BuildingManagement.Tests;

public class BackupFileProtectorTests
{
    public BackupFileProtectorTests() => BackupFileProtector.Initialize("unit-test-backup-secret-key");

    [Fact]
    public void Protect_then_Unprotect_roundtrips()
    {
        var plain = new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4, 5 };   // شكل بداية ZIP
        var enc = BackupFileProtector.Protect(plain);

        Assert.True(BackupFileProtector.IsProtected(enc));
        Assert.NotEqual(plain, enc);
        Assert.Equal(plain, BackupFileProtector.Unprotect(enc));
    }

    [Fact]
    public void Old_unencrypted_files_are_returned_unchanged()
    {
        var plain = new byte[] { 0x50, 0x4B, 0x03, 0x04, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 };
        Assert.False(BackupFileProtector.IsProtected(plain));
        Assert.Equal(plain, BackupFileProtector.Unprotect(plain));
    }

    [Fact]
    public void Tampered_file_is_rejected()
    {
        var enc = BackupFileProtector.Protect(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        enc[^1] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(() => BackupFileProtector.Unprotect(enc));
    }
}

public class ImageSnifferTests
{
    private static byte[] Pad(params byte[] head) => head.Concat(new byte[16]).ToArray();

    [Fact] public void Jpeg_is_allowed() => Assert.True(ImageSniffer.IsAllowedImage(Pad(0xFF, 0xD8, 0xFF, 0xE0)));
    [Fact] public void Png_is_allowed() => Assert.True(ImageSniffer.IsAllowedImage(Pad(0x89, 0x50, 0x4E, 0x47)));
    [Fact] public void Gif_is_allowed() => Assert.True(ImageSniffer.IsAllowedImage(Pad(0x47, 0x49, 0x46, 0x38)));

    [Fact]
    public void Webp_is_allowed() =>
        Assert.True(ImageSniffer.IsAllowedImage(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50, 0, 0, 0, 0 }));

    [Fact]
    public void Heic_is_allowed() =>
        Assert.True(ImageSniffer.IsAllowedImage(new byte[] { 0, 0, 0, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63, 0, 0, 0, 0 }));

    [Fact]
    public void Html_svg_and_pdf_are_rejected()
    {
        Assert.False(ImageSniffer.IsAllowedImage(System.Text.Encoding.ASCII.GetBytes("<html><script>alert(1)</script>")));
        Assert.False(ImageSniffer.IsAllowedImage(System.Text.Encoding.ASCII.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'/>")));
        Assert.False(ImageSniffer.IsAllowedImage(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7 ......")));
    }

    [Fact]
    public void Too_short_input_is_rejected() => Assert.False(ImageSniffer.IsAllowedImage(new byte[] { 0xFF, 0xD8 }));

    [Fact]
    public void Stream_position_is_restored()
    {
        using var ms = new MemoryStream(Pad(0xFF, 0xD8, 0xFF, 0xE0));
        Assert.True(ImageSniffer.IsAllowedImage(ms));
        Assert.Equal(0, ms.Position);
    }
}

public class LoginThrottleTests
{
    private static LoginThrottle Create() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void Account_is_locked_after_five_failures()
    {
        var t = Create();
        for (var i = 0; i < 4; i++) t.RegisterFailure("0100", null);
        Assert.False(t.IsLocked("0100", null));
        t.RegisterFailure("0100", null);
        Assert.True(t.IsLocked("0100", null));
    }

    [Fact]
    public void Reset_clears_the_lock()
    {
        var t = Create();
        for (var i = 0; i < 5; i++) t.RegisterFailure("a@b.c", null);
        t.Reset("a@b.c");
        Assert.False(t.IsLocked("a@b.c", null));
    }

    [Fact]
    public void Ip_is_locked_after_twenty_failures_across_accounts()
    {
        var t = Create();
        for (var i = 0; i < 20; i++) t.RegisterFailure("user" + i, "1.2.3.4");
        Assert.True(t.IsLocked("someone-else", "1.2.3.4"));
        Assert.False(t.IsLocked("someone-else", "5.6.7.8"));
    }

    [Fact]
    public void Keys_are_case_and_whitespace_insensitive_and_hashed()
    {
        Assert.Equal(LoginThrottle.K("  User@X.com "), LoginThrottle.K("user@x.com"));
        Assert.DoesNotContain("user", LoginThrottle.K("user@x.com"));
        Assert.True(LoginThrottle.K("user@x.com").Length <= 80);
    }
}
