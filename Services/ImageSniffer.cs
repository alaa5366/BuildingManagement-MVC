using System.Text;

namespace BuildingManagementMvc.Services;

// فحص نوع الصورة من أول بايتات الملف (مش من الامتداد/الـ Content-Type اللي المستخدم بيبعته)
public static class ImageSniffer
{
    public static bool IsAllowedImage(Stream stream)
    {
        var start = stream.CanSeek ? stream.Position : 0;
        var buf = new byte[16];
        var n = stream.Read(buf, 0, buf.Length);
        if (stream.CanSeek) stream.Position = start;
        return IsAllowedImage(buf.AsSpan(0, n));
    }

    public static bool IsAllowedImage(ReadOnlySpan<byte> b)
    {
        if (b.Length < 12) return false;

        // JPEG
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;
        // PNG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;
        // GIF
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return true;
        // WebP: RIFF....WEBP
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return true;
        // HEIC/HEIF: ....ftyp<brand>
        if (b[4] == 0x66 && b[5] == 0x74 && b[6] == 0x79 && b[7] == 0x70)
        {
            var brand = Encoding.ASCII.GetString(b.Slice(8, 4));
            return brand is "heic" or "heix" or "heim" or "heis" or "hevc" or "hevx" or "mif1" or "msf1";
        }

        return false;
    }
}
