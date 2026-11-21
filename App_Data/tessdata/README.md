# Tesseract language data

The application uses Tesseract LSTM language data for English and Arabic.

| Purpose | Path | Source |
|---|---|---|
| Accuracy model | `eng.traineddata` | [tessdata_best](https://github.com/tesseract-ocr/tessdata_best) |
| Accuracy model | `ara.traineddata` | [tessdata_best](https://github.com/tesseract-ocr/tessdata_best) |
| Orientation model | `osd.traineddata` | [tessdata_best](https://github.com/tesseract-ocr/tessdata_best) |
| Fast-pass orientation model | `fast/osd.traineddata` | Same orientation model copied beside the fast language models |
| Fast first-pass model | `fast/eng.traineddata` | [tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast) |
| Fast first-pass model | `fast/ara.traineddata` | [tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast) |

Both model repositories distribute their trained data under Apache License 2.0. The full license text is stored beside these files as `LICENSE-2.0.txt`.

The fast models are used for the initial low-latency OCR pass. If the text is too weak or lacks receipt indicators, the service retries with the larger accuracy models and additional preprocessing. The orientation model is used only for very short/low-confidence captures.

Do not remove these files from deployment packages. The OCR service loads them from `App_Data/tessdata`, outside the public web root. Do not expose raw receipt images or extracted personal data in logs.
