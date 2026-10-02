# تعديلات الأمان والترجمة — الإصدار 4 (تراكمي: بيشمل 1 و 2 و 3)

> الإصدار 4 = ترجمة كاملة للصفحات والرسائل. التفاصيل في `LOCALIZATION.md`. الأمان (1–3) زي ما هو.

## اللي اتغيّر
1. **Account/GoogleSignIn (الأخطر):** كان بيصدّق الإيميل اللي العميل يبعته من غير ما يتحقق من الـ ID Token، فأي حد يعرف إيميل السوبر أدمن كان يقدر يدخل كسوبر أدمن. دلوقتي السيرفر بيتحقق من التوكن عن طريق Firebase Admin ويقرا الإيميل منه، ويشترط `email_verified`.
2. **LoginThrottle (جديد):** 5 محاولات غلط لنفس الحساب أو 20 من نفس الـ IP = قفل 15 دقيقة. متركّب على Super Admin / Admin / Resident / الدخول الموحد / جوجل.
3. **الأسرار من الـ config بس:** اتشال المفتاح المكتوب في `QrAuthService` وقيم `CHANGE_ME`. التطبيق بيفشل عند التشغيل لو سر ناقص. توقيع `QrAuthService` بقى HMAC-SHA256 مع مقارنة ثابتة الزمن.
4. **`QrAuthService` اتسجّل في `Program.cs`:** كان ناقص تسجيل، فـ `/Qr` و `/QrAccess` كانوا هيرموا خطأ DI.
5. **Home/OcrTest اتشال.**
6. **Cookies:** `HttpOnly` + `Secure` (في الإنتاج) + `SameSite=Lax` للـ auth والـ session والـ antiforgery، و `UseHttpsRedirection`، و `UseForwardedHeaders` عشان الـ IP الحقيقي ورا Azure.
7. **بيانات الـ service account** تتقرا من `Firebase:ServiceAccountJson` (متغير بيئة) الأول، وبعدها من الملف.

## الإصدار 2 — الجديد
8. **Impersonation/Exit (ثغرة خطيرة):** كان `[AllowAnonymous]` وبيبني جلسة الأدمن من كوكي `bm_admin_backup` اللي كان JSON عادي من غير توقيع، فأي حد يقدر يزوّر الكوكي (دور + صلاحيات) ويدخل كسوبر أدمن. دلوقتي: الكوكي مشفّر ومتوقّع بـ Data Protection وبينتهي بعد ساعتين، والخروج متاح بس لجلسة impersonation حقيقية، والدور ثابت `admin`، وبيتحقق إن الكوكي بتاع نفس الأدمن اللي دخل.
9. **إيميلات السوبر أدمن** اتنقلت من الكود لـ `Auth:SuperAdminEmails` (مفصولة بفاصلة). **إعداد جديد إجباري.**
10. **`AutoValidateAntiforgeryToken` عام:** أي POST جديد بيتحمى تلقائياً. (`Presence/Heartbeat` عامل `[IgnoreAntiforgeryToken]` أصلاً فمفيش حاجة اتكسرت.)
11. **الـ Session اتشالت:** مكانتش مستخدمة في أي مكان.
12. بانر الـ Impersonation في الـ layout بقى يعتمد على الـ claim مش على وجود الكوكي.

### إعداد جديد في الإصدار 2
- Azure ومحلياً: `Auth__SuperAdminEmails` = الإيميلين اللي كانوا مكتوبين في `AuthService.cs` مفصولين بفاصلة (محلياً: `"Auth": { "SuperAdminEmails": "a@x.com,b@y.com" }` في `appsettings.Development.json`).
- أي أدمن كان في وضع impersonation وقت النشر هيتعمله logout (الكوكي القديم مش صالح) — طبيعي.
- لو بتشغّل أكتر من instance لازم Data Protection keys تتشارك بينهم (على App Service instance واحدة مفيش مشكلة).

## الإصدار 3 — الجديد
13. **الجلسات كانت بتفضل صالحة 7 أيام حتى بعد تعطيل الأدمن:** التعطيل (`disabled` / `isActive`) كان بيتفحص وقت تسجيل الدخول بس. دلوقتي `SessionRevalidator` بيراجع الجلسة كل دقيقتين تقريباً: أدمن اتعطّل أو اتحذف ← الجلسة تبطل، وسوبر أدمن اتشال من `Auth:SuperAdminEmails` ← الجلسة تبطل. وصلاحيات الأدمن بتتحدّث من غير login جديد. لو Firestore وقع مؤقتاً الجلسة بتفضل شغالة (مش بيطرد الكل).
14. **الصلاحيات الدقيقة ماكانتش بتتطبّق:** `HasPermission` كان على Categories بس، فأي أدمن (حتى اللي صلاحياته "صيانة" بس) كان يقدر يأكّد دفعات ويشوف التقارير ويعمل impersonation. ضفت `[AdminPermission]` على Wallet / Expenses / Revenues / Reports / Polls / Maintenance / WhatsApp / AdminApts / Qr / Impersonation.Enter.
    - **مش مفعّل افتراضياً.** الأدمنز القدام (من نسخة الـ JS) غالباً مالهمش `permissions` في Firestore، ولو فعّلته على طول هيتقفل عليهم النظام.
    - الخطوات: (أ) افتح `/Permissions` وطبّق قالب (كامل / مالي / صيانة) على **كل** أدمن. (ب) سجّل دخول بأدمن تجريبي وتأكد إنه شايف الشاشات. (ج) ضيف `Auth__EnforceAdminPermissions` = `true`.
    - لو حصلت مشكلة: شيل المتغير أو خليه `false` ويرجع زي الأول.
    - ملاحظة: `Notifications` و `AdminHome` و `Invoice` مالهمش صلاحية مقابلة فاتسابوا.

### مراجعات ما احتاجتش تعديل
- `Admins` و `Permissions` و `Backup` و `Tools` و `Sync` و `Settings` و `Categories`: كلها مقيّدة بالدور وبعمارة المستخدم.
- تحميل النسخ الاحتياطي (`DownloadBackup`) محمي من path traversal أصلاً.
- `AuditLog`: الساكن مقيّد بشقته والأدمن بعمارته.

### ملاحظات من غير تعديل (قرارك)
- **الـ PIN متخزّن نص عادي في Firestore** (`users.pin`) ومقارنته نص عادي، وكمان كلمة سر Firebase مشتقة منه. ده من تصميم نسخة الـ JS الأصلية. وأي نسخة احتياطية (`Backups/*.zip`) فيها الـ PINs دي، فاعتبرها بيانات حساسة.
- الـ PIN قصير: حماية التخمين اللي ضفتها في الإصدار 1 بتقلل الخطر بس مش بتشيله.

## الدمج
انسخ الملفات دي فوق مشروعك بنفس المسارات. `changes.diff` فيه كل التغييرات للمراجعة.
**ماتستبدلش `appsettings.json` بتاعك** — الملف ده مش جوه الباتش.

## لازم تعمله بعد الدمج
1. **غيّر (rotate) كل الأسرار اللي كانت في الـ zip:**
   - Firebase Console ← Project settings ← Service accounts ← Generate new private key، وبعدين امسح المفتاح القديم.
   - Cloudinary ← API Keys ← اعمل API secret جديد.
   - `Qr:SecretKey` و `Excel:SecretKey`: قيم جديدة عشوائية 32 حرف أو أكتر (الحالية 15 حرف بس).
     - PowerShell 7: `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))`
     - Linux/macOS: `openssl rand -base64 32`
   - الـ Firebase Web API Key مش سر بالمعنى الحرفي، بس قيّده بالدومينات في Google Cloud Console.
2. **Azure App Service ← Environment variables** (الـ `.gitignore` بيستثني `appsettings.json` فالـ CI مش هيبعتها):
   - `Firebase__ProjectId`
   - `Firebase__WebApiKey`
   - `Firebase__ServiceAccountJson` ← محتوى ملف الـ service account كله في سطر واحد
   - `Cloudinary__CloudName` / `Cloudinary__ApiKey` / `Cloudinary__ApiSecret`
   - `Excel__SecretKey`
   - `Qr__SecretKey` (اختياري: `Qr__TokenSecretKey` لو عايز مفتاح منفصل لتوكنات الدخول)
3. **محلياً:** حط نفس القيم في `appsettings.Development.json` (متجاهَل في git) أو `dotnet user-secrets`. القيم ماينفعش تحتوي على `CHANGE_ME`.
4. امسح من الـ zip: `bin/` و `obj/` و `Backups/` و `run.log` و `firebase-service-account.json`.

## تنبيهات
- **تغيير `Qr:SecretKey` بيبطّل أكواد الـ QR الثابتة المطبوعة على الأبواب** (بتتوقّع بالمفتاح ده) — هتحتاج تعيد طباعتها من شاشة `/Qr`. الروابط المؤقتة وقوالب الإكسل القديمة بتبطل برضه.
- القفل على مستوى الحساب معناه إن حد ممكن يقفل حساب حد تاني بمحاولات غلط متعمدة (15 دقيقة). ده المقابل المعتاد.
- العدّاد في الذاكرة: بيتصفّر مع restart، ومش مشترك بين أكتر من instance.

## اختبار سريع
- 6 محاولات PIN غلط ← رسالة القفل.
- POST على `/Account/GoogleSignIn` بتوكن مزوّر ← `success:false`.
- `/Home/OcrTest` ← 404.
- على الإنتاج: الـ cookies `bm_auth` و `bm_session` عليها Secure و HttpOnly.

## لسه ماتعالجش
- مراجعة الصلاحيات على الصفوف (IDOR): اتعملت للـ controllers كلها بقراءة الكود (مش اختبار فعلي).
- لو أدمن عنده أكتر من عمارة، الكود بياخد أول `buildingId` claim بس.
- العدّاد في `LoginThrottle` في الذاكرة.
- **الكود اتكتب من غير build**: شغّل `dotnet build` قبل أي نشر.
