# Phase 25 - Credential format profile

- Run: 2026-11-19T02:42:08.7171163+02:00
- Mode: READ ONLY

## Results
- Residents.PinHash: populated=79; BCrypt-shaped=0; other-shape=79
- Residents.PasswordHash: populated=0; BCrypt-shaped=0; other-shape=0
- Users rows: 5; legacy Users.Pin column present: 1; encrypted MFA column present: 1
- No credential content, prefixes, hashes, ciphertext, or plaintext values were returned.
- FORMAT PROFILE ONLY: BCrypt-shaped values are not proof of successful verification or policy compatibility.
- No credential conversion or target credential rows were created.

## Security decision
- This report contains aggregate format counts only, never credential values.
- Hash-shape recognition is not cryptographic verification. Any future migration requires known algorithm/version, successful test vectors, and compatible verification code.
- Legacy PIN/password/MFA values are not modified or copied.
- No source rows are updated or deleted.
