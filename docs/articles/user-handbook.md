# Building Management — User Handbook

This handbook describes the current application workflows for residents, building administrators and SuperAdmins. Screen names may differ slightly by language, but the security and financial rules are the same.

## 1. Sign-in and account contexts

### Resident
- Sign in with the registered mobile number and personal PIN.
- Access is available only when the account is active, the resident role is valid and the apartment occupancy is active.
- If the system asks for a PIN change, complete it before continuing.
- A resident can access only the apartment scope granted to the account.

### Building administrator
- Sign in with the registered mobile number and PIN.
- Access is limited to assigned building scope and permissions.
- An administrator assigned to more than one building may need to choose the building context before working.

### SuperAdmin
- Sign in with an allow-listed email address and password.
- Complete the authenticator challenge when two-factor authentication is enabled or required for the account.
- SuperAdmin privileges are global and must not be confused with a building administrator role.

### One person with multiple roles
A single person/account can have more than one role, for example Resident for a particular apartment and BuildingAdmin for a building. Choose the requested role and scope on the context-selection screen. The scopes remain separate: resident actions use the apartment context, while building-administration actions use the building context. Do not create a duplicate person or account just to add another role.

## 2. Account activation and PINs

- Resident accounts are tied to an active apartment occupancy and an explicit role assignment.
- Accounts that are awaiting identity/phone review remain unavailable for normal sign-in.
- A non-SuperAdmin phone number must identify one person uniquely. Resolve duplicates through the account-governance workflow rather than merging people automatically.
- Initial or recovery PINs must be changed when the application requests it.
- If you forget your PIN, contact an authorized account manager. Identity must be verified before a replacement PIN is issued through the approved Account Management workflow; the existing PIN cannot be viewed.
- Never share a PIN, authenticator secret or recovery code. Support staff should never ask you to send them.

## 3. Wallets, deposits and receipts

### Submit a deposit
1. Open the wallet for the correct apartment and financial period.
2. Enter the amount and any required note.
3. Upload the original receipt screenshot or image when available.
4. Submit the deposit and keep its reference for follow-up.

### Receipt OCR
The application decodes supported image uploads through SkiaSharp and normalizes them to PNG, including WebP files. It then runs Tesseract LSTM with English and Arabic language data: a fast pass runs first, and low-confidence captures can trigger the larger accuracy model plus selected grayscale, deskew, upscaling, Sauvola/Otsu thresholding and orientation passes. Image decoding is limited to 30 megapixels, and the trained-data files are deployed under App_Data/tessdata outside the public web root. It attempts to extract candidate values such as amount, date, reference, sender/recipient text and transaction status.

OCR is advisory, not payment authorization. It can misread small text, compressed screenshots, Arabic/Latin mixed text, low contrast, glare, rotation or cropped details. A green tick or a recognized “successful” word is not proof that the money reached the correct account. The authorized administrator must verify the payment using trusted transaction evidence before confirming the deposit.

### Capture a readable image
- Prefer the original screenshot downloaded or shared by the payment application.
- Keep the complete transaction screen, amount, currency, date, reference and status visible.
- Avoid blur, glare, reflections, filters, heavy recompression and tight crops.
- For camera photos, keep the phone parallel to the screen/document and use even lighting.
- Do not alter the amount, recipient, sender, reference or status.
- Do not upload another person's unredacted financial data for testing or support.

### Deposit states and history
Pending deposits await authorized review. Confirmed, cancelled and reversed deposits are different states and must not be treated as interchangeable. Historical records and available receipt links can remain visible in wallet history or the resident statement. Uploading a receipt does not itself post money to the ledger.

## 4. Financial periods and invoices

- Financial entries belong to a building and a defined period.
- Closed or missing periods block protected financial changes. Do not edit historical dates to bypass a period lock.
- Use the authorized period-management and correction process for a legitimate correction.
- Issued invoice PDFs include a signed QR verification link.
- The public verification page displays limited invoice facts (building, apartment number, period and issue date). It does not expose resident contact information or account balances.
- Invalid signatures and unknown invoice numbers are rejected.

## 5. Impersonation / support access

Authorized administrators may temporarily enter a permitted resident or building context to investigate a problem. The application displays an impersonation banner. Use **Back to my account** to restore the previous identity. If impersonation is nested, the button returns one level at a time. Actions remain subject to the target scope and are audit logged. Do not leave a support session open when the investigation is complete.

## 6. Two-factor authentication

Where TOTP is enabled or required for an administrator account:
1. Enroll using an authenticator application.
2. Verify the current code to complete enrollment.
3. Store one-time recovery codes securely offline.
4. Complete the challenge during sign-in.

A recovery code is single-use. Never send an authenticator secret or recovery code by email, chat or support ticket.

## 7. Audit and application logs

- Audit logs record important account, administrative and financial actions.
- Application logs help diagnose errors and performance issues.
- Results are restricted to the viewer's permitted building/apartment scope; sensitive values are redacted.
- Include the correlation ID when reporting a technical issue.
- Logs support investigation but do not replace bank statements, payment-provider evidence or other source financial documents.

## 8. Backup and restore

- Create and retain backups before significant operational changes.
- Review the archive preview and selected collections before confirming a restore.
- A restore session is bound to the account that created the preview, expires, is single-use and verifies the reviewed archive digest before applying changes.
- Restore validation rejects unsafe archive entries and unexpected collections. Database writes are performed transactionally so a failed restore can roll back.
- Perform restore rehearsals only against a disposable/test database. Never experiment on production.

## 9. Help and troubleshooting

- If a resident cannot sign in, verify account status, phone uniqueness, active occupancy, active role assignment and PIN-change requirements.
- If an OCR result is uncertain, re-upload a clearer original image and manually verify all extracted fields.
- If a payment remains pending, contact the authorized building administrator with the deposit reference and a redacted receipt.
- If a page displays stale permissions, sign out and sign in again after the account's role or permission change has been completed.
- When contacting technical support, provide the page/action, approximate UTC time and correlation ID. Never include passwords, PINs, TOTP codes or unredacted financial identifiers.

## Related references

- [Technical reference](technical-reference.md)
- [Security and deployment](security.md)
- [Architecture](architecture.md)
- [Testing](testing.md)
