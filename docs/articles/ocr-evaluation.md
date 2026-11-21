# Receipt OCR evaluation

## Goal and safety boundary

Receipt OCR extracts candidate text and transaction fields. It is not a payment rail and must never confirm a deposit by itself. A human reviewer or trusted payment-provider reconciliation remains the authority for settlement.

## Public visual references reviewed

The following publicly accessible examples were used as layout references only. They are not included in the repository or test assemblies. They may be copyrighted, contain account-like identifiers, or be promotional mockups; do not redistribute them or treat them as real customer transactions.

| Provider / pattern | Reference | What it helps evaluate |
|---|---|---|
| InstaPay to a wallet | [BankyGate public transfer image](https://bankygate.com/UserFiles/News/2022/06/24/31461.jpg) | Success indicator, amount, sender/beneficiary block and transfer details |
| InstaPay service-suspension context | [Egyptke report](https://www.egyptke.com/63857) | Background context only; this is a service-suspension article, not a failed transaction receipt |
| Service-unavailable screen | [Al-Masry Al-Youm public image](https://mediaaws-live.almasryalyoum.com/AMAYLivePictures/portalimages/news/original/2025/08/28/2754988_0.jpg) | A service error must not be mistaken for proof of either a successful or failed transfer |
| Bank transfer confirmation | [BankyGate CIB transfer image](https://www.bankygate.com/UserFiles/NewsInnerImages/2025/04/23/60232/WhatsApp_Image_2025-04-23_at_14_28_10_27ce8139_20250423145846.jpg) | Source/destination account blocks, transfer amount, date and reference fields |
| Vodafone Cash confirmation-flow UX study | [Behance design study](https://mir-s3-cdn-cf.behance.net/project_modules/1400_webp/9e5bfa239576011.692ca6a02b7de.jpg) | Amount and From/To placement in a design study; this is not an official receipt or transaction record |
| Success/failure UI concept | [Transfer result mockup](https://mir-s3-cdn-cf.behance.net/project_modules/1400/8980aa236378031.68eab8d20243f.png) | Positive and negative state separation; this is a UI concept, not a provider screenshot |

A genuine Orange Cash receipt screenshot suitable for redistribution was not identified. The public InstaPay-to-wallet example above is a wallet-recipient layout, not an Orange Cash app screenshot. Do not label it as an Orange Cash native receipt.

## Current OCR pipeline

1. Validate and read the upload stream from position zero.
2. Decode the image through SkiaSharp, enforce a 30-megapixel ceiling, and normalize it to PNG. This also handles WebP uploads that the bundled Leptonica build cannot decode directly.
3. Run English + Arabic Tesseract LSTM using the fast models for an initial Auto/SingleBlock pass.
4. Return early only when confidence and receipt signals are sufficiently strong.
5. For difficult captures, use the accuracy models and selected grayscale, deskew, upscaling, adaptive Sauvola/Otsu and (when text is extremely sparse) orientation-aware passes.
6. Score candidates using Tesseract mean confidence plus receipt-specific amount/reference/status signals.
7. Parse the selected text. Unknown or pending states remain unconfirmed; failure signals take precedence over generic success wording.

Tesseract mean confidence is not field-level accuracy. A high-confidence OCR result can still contain a wrong amount or reference.

## What the public-image evaluation showed

The public examples were downloaded only to a temporary evaluation folder on the development machine and were not retained in the repository. The first evaluation exposed two concrete issues:
- Some websites served WebP bytes despite a `.jpg` filename; direct Leptonica decoding failed.
- The accuracy-only multi-pass run was too slow on difficult public images.

The implementation was updated to normalize WebP/other supported formats through SkiaSharp, keep model data outside `wwwroot`, use a fast first pass, and reserve expensive accuracy/preprocessing passes for weak captures.

The public examples are heterogeneous and several are low-resolution/promotional layouts. They are useful smoke references, not a statistically representative benchmark. The OCR pipeline must still be validated against a permissioned provider corpus before claiming production accuracy.

## Evaluation results

### Synthetic image regression

The committed synthetic corpus contains five original PNG layouts. The image-level suite checks success, failure and pending wording; English and Arabic; amount/reference extraction; WebP normalization; and invalid-image handling.

Latest focused result: **8 passed, 0 failed**. The suite also rotates a receipt image by 90 degrees to exercise orientation detection. This is a repeatable regression baseline for these synthetic layouts, not a real-provider accuracy score.

### Public-image smoke checks

The temporary public-reference run was executed on the development device after WebP normalization and the fast/accuracy model split. It deliberately emitted only field-detection flags and timing, not OCR text or names.

| Public reference | Amount detected | State detected | Reference detected | Approx. OCR time |
|---|---:|---:|---:|---:|
| Bank transfer sample | Yes | No | No | 3.9 s |
| InstaPay-to-wallet image | No | No | No | 2.7 s |
| Service interruption image | No | No | No | 1.3 s |
| Wallet UI concept showing two states | Yes | Both success and failure | No | 4.2 s |

The last image contains both success and failure panels, so it is not a valid single-transaction receipt. The other results show that the public samples are not yet consistently yielding the fields needed for automatic parsing. They must remain unconfirmed and require manual review. Do not interpret these smoke checks as evidence of production-grade accuracy.

## Permanent regression corpus

The committed test corpus must contain only original synthetic examples or images for which explicit redistribution permission exists. Use fictional amounts, account identifiers and references. Cover at least:

- English and Arabic
- InstaPay-like transfer layout
- Mobile-wallet layout
- Bank transfer layout
- Success, failed/rejected and pending states
- Arabic-Indic and Western digits
- Decimal/thousands separators and EGP labels
- Cropped/low-contrast variants and 90-degree rotation/orientation detection
- PNG, JPEG and WebP decoding

Do not commit public third-party receipt screenshots, unredacted customer receipts, or any test image containing real account identifiers.
