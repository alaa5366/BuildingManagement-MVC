using QRCoder;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/qr-generator.js (توليد صور QR)
public class QrGeneratorService
{
    // بيرجع صورة QR بصيغة PNG كـ byte[]
    public byte[] GeneratePng(string text, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule, new byte[] { 0x12, 0x43, 0x3C }, new byte[] { 0xFF, 0xFF, 0xFF });
    }

    // رابط ثابت للشقة (بيتحط على QR يتلصق على باب الشقة) — بيودّي مباشرة
    // لصفحة دخول الساكن مع تعبئة العمارة/الدور/رقم الشقة تلقائيًا
    public string GetApartmentLoginUrl(string baseUrl, string buildingId, int floorOrder, int aptNumber) =>
        $"{baseUrl.TrimEnd('/')}/Account/LoginResident?buildingId={buildingId}&floorOrder={floorOrder}&aptNumber={aptNumber}";
}
