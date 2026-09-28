using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// صورة/PDF الإيصال بعد الرفع على Cloudinary + نتيجة الـ OCR (لو حصل)
[FirestoreData]
public class Receipt
{
    [FirestoreProperty("url")] public string Url { get; set; } = "";
    [FirestoreProperty("publicId")] public string PublicId { get; set; } = "";
    [FirestoreProperty("fileName")] public string FileName { get; set; } = "";
    [FirestoreProperty("uploadedAt")] public string UploadedAt { get; set; } = "";
    // النص اللي قراه الـ OCR من الصورة (للمراجعة اليدوية، مش موثوق 100%)
    [FirestoreProperty("ocrText")] public string? OcrText { get; set; }
    // أفضل تخمين لمبلغ من النص (لو اتلاقى رقم يشبه مبلغ فلوس)
    [FirestoreProperty("ocrAmountGuess")] public double? OcrAmountGuess { get; set; }
}
