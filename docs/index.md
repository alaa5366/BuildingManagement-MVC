# Building Management

ASP.NET Core 8 MVC application for residential building operations: identity and role governance, apartments and occupancy, wallet and ledger workflows, invoices, polls, maintenance, notifications, QR access, backups, reports and operational logs.

The user interface supports Arabic, English, French and German.

## Documentation

- [User handbook](articles/user-handbook.md) — resident, building administrator and SuperAdmin workflows.
- [Technical reference](articles/technical-reference.md) — identity, authorization, financial workflows, OCR, invoices, backups, audit and localization contracts.
- [OCR evaluation](articles/ocr-evaluation.md) — public visual references, limitations and test-corpus rules.
- [Architecture](articles/architecture.md) — application layers and cross-cutting flows.
- [Security and deployment](articles/security.md) — security decisions and deployment checklist.
- [Testing](articles/testing.md) — test commands, coverage areas and validation boundaries.
- [Documentation guide](articles/documentation-guide.md) — XML comments, Razor headers and DocFX.
- [Third-party notices](third-party-notices.md) — OCR models and image dependencies.
- [Generated API reference](api/index.md) — generated from XML documentation comments.

## Build the documentation site

Run from the repository root:

```powershell
dotnet tool install -g docfx
dotnet build .\BuildingManagementMVC.csproj
docfx docs/docfx.json --serve
```

DocFX metadata generation reads the application project and writes generated API YAML files under `docs/api`. Keep generated API output separate from hand-maintained articles.
