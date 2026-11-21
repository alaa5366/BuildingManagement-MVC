# Third-party notices

## Tesseract trained data

The English, Arabic and orientation trained-data files in `App_Data/tessdata` are sourced from:

- [tesseract-ocr/tessdata_best](https://github.com/tesseract-ocr/tessdata_best)
- [tesseract-ocr/tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast)

The trained-data repositories use the Apache License 2.0. A copy of the license is included at `App_Data/tessdata/LICENSE-2.0.txt`.

## SkiaSharp

The application references [SkiaSharp](https://github.com/mono/SkiaSharp) **4.153.1** to decode supported image formats (including WebP) and normalize uploads to PNG before Tesseract processing. SkiaSharp is distributed under the MIT license; see the package/repository license for the exact terms.

## Tesseract .NET wrapper

The application references the Tesseract .NET wrapper package. Consult the package metadata and upstream repository for the applicable license and notices before redistributing the application.

Third-party package versions are declared in `BuildingManagementMVC.csproj`. Keep this notice synchronized when adding or upgrading native image/OCR dependencies.
