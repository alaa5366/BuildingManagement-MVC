# Building Management

ASP.NET Core 8 MVC application for managing residential buildings: apartments and residents,
monthly expenses and revenues, apartment wallets and payments, invoices, polls, maintenance,
cameras, QR login, backups and reports. The UI is available in Arabic, English, French and German.

* [Architecture](articles/architecture.md) – how the project is organized and how the main flows work.
* [Security](articles/security.md) – the security decisions and the deployment checklist.
* [Testing](articles/testing.md) – how to run the tests and what they cover.
* [Documentation guide](articles/documentation-guide.md) – how to write and build the documentation.
* [API reference](api/index.md) – generated from the XML comments in the code.

## Build the documentation site

```
dotnet tool install -g docfx
dotnet build BuildingManagementMVC.csproj
docfx docs/docfx.json --serve
```
