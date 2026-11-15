# Documentation guide

## XML comments (C#)

* Write them in English; IntelliSense and the generated site then read the same text.
* Put the `///` block **above** all attributes of the declaration and never on the same line as code.
* The summary says *what* the member is or does. Put *why* in an ordinary `//` comment (Arabic is fine).
* Use `<remarks>` for rules and side effects, `<see cref="..."/>` to link types, `<c>...</c>` for code words.
* Do not repeat the member name ("Gets the name"). A property whose purpose is obvious from its name can stay short.

The build already produces `BuildingManagementMVC.xml` (`GenerateDocumentationFile`). `DocumentationCoverageTests`
fails when a public type or a controller/service method has no documentation.

## Razor views

Each view starts with a Razor comment (`@* ... *@`) that names the view, the controller action that renders it and its model.
Razor views have no XML documentation, so keep that header up to date when a view moves or changes model.

## Comment banners

A banner such as `// =====` must be on its own line. A banner that shares a line with an attribute hides the attribute;
`SourceIntegrityTests` catches this.

## Build the site

See the [home page](../index.md).
