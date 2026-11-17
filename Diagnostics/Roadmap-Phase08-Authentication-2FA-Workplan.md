# Roadmap Phase08 — Authentication & 2FA Workplan

## Acceptance criteria

- [x] Inventory resident/admin/super-admin and unified login paths.
- [x] Implement TOTP setup and confirmation for admin/super-admin accounts.
- [x] Encrypt TOTP secrets at rest.
- [x] Generate one-time recovery codes and store only hashes.
- [x] Reject TOTP replay by recording the last accepted time-step.
- [x] Require second factor before issuing admin/super-admin authentication cookies when enabled.
- [x] Preserve resident and QR resident login behavior.
- [x] Add settings, setup, challenge, and recovery-code views.
- [x] Add EF migration and update the model snapshot.
- [ ] Local build and full test suite.
- [ ] TEST-only migration and manual end-to-end validation.
- [ ] User acceptance.

## Operational boundary

2FA is opt-in until a mandatory enrollment policy and recovery process are explicitly approved. Production is excluded.
