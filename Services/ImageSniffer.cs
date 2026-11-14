using System.Text;

namespace BuildingManagementMvc.Services;

/// <summary>
/// فحص نوع الصورة من أول بايتات الملف (مش من الامتداد/الـ Content-Type).
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠️ ليه ده مهم؟</b> لأن المستخدم ممكن يبعت ملف اسمه <c>image.jpg</c>
/// ويكون فعليًا HTML أو SVG أو PDF. الدالة دي بتقرأ أول بايتات عشان تتأكد
/// إن الملف صورة حقيقية.
/// </para>
/// <para>
/// <b>الصيغ المدعومة:</b> JPEG, PNG, GIF, WebP, HEIC/HEIF.
/// </para>
/// </remarks>
public static class ImageSniffer
{
    /// <summary>
    /// يتحقق لو الـ Stream فيه صورة مسموحة.
    /// </summary>
    /// <param name="stream">الـ stream (لازم يكون seekable عشان يرجّع الـ position).</param>
    /// <returns><c>true</c> لو الصورة مسموحة.</returns>
    public static bool IsAllowedImage(Stream stream)
    {
        var start = stream.CanSeek ? stream.Position : 0;
        var buf = new byte[16];
        var n = stream.Read(buf, 0, buf.Length);
        if (stream.CanSeek) stream.Position = start;
        return IsAllowedImage(buf.AsSpan(0, n));
    }

    /// <summary>
    /// يتحقق لو أول بايتات دي صورة مسموحة.
    /// </summary>
    /// <param name="b">أول 12+ بايت من الملف.</param>
    /// <returns><c>true</c> لو الصورة مسموحة.</returns>
    public static bool IsAllowedImage(ReadOnlySpan<byte> b)
    {
        if (b.Length < 12) return false;

        // JPEG
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;
        // PNG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;
        // GIF
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return true;
        // WebP
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return true;
        // HEIC/HEIF
        if (b[4] == 0x66 && b[5] == 0x74 && b[6] == 0x79 && b[7] == 0x70)
        {
            var brand = Encoding.ASCII.GetString(b.Slice(8, 4));
            return brand is "heic" or "heix" or "heim" or "heis" or "hevc" or "hevx" or "mif1" or "msf1";
        }

        return false;
    }
}