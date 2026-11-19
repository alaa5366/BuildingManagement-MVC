/*
    Resident phone collision review
    Read-only report. Includes archived Persons because an archived record can
    still make a phone identifier ambiguous and must not be silently reassigned.

    The normalization mirrors the Egyptian phone rules used by AuthHelpers:
      00... -> international prefix
      20... -> +20...
      0...  -> +20...
      short national digits -> +20...
    The application remains authoritative and strips every non-digit character.
*/
SET NOCOUNT ON;

;WITH SourcePhones AS
(
    SELECT
        p.PersonId,
        p.DisplayName,
        p.IsArchived,
        p.PrimaryPhone,
        TranslatedPhone = TRANSLATE(
            COALESCE(p.PrimaryPhone, N''),
            N'٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹',
            N'01234567890123456789')
    FROM dbo.Persons AS p
    WHERE NULLIF(LTRIM(RTRIM(p.PrimaryPhone)), N'') IS NOT NULL
),
Digits AS
(
    SELECT
        PersonId,
        DisplayName,
        IsArchived,
        PrimaryPhone,
        DigitsOnly = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            TranslatedPhone, N'+', N''), N' ', N''), N'-', N''), N'(', N''),
            N')', N''), N'.', N''), N'/', N'')
    FROM SourcePhones
),
Normalized AS
(
    SELECT
        PersonId,
        DisplayName,
        IsArchived,
        PrimaryPhone,
        NormalizedPhone = CASE
            WHEN DigitsOnly LIKE N'00%' THEN N'+' + SUBSTRING(DigitsOnly, 3, 32)
            WHEN DigitsOnly LIKE N'20%' THEN N'+' + DigitsOnly
            WHEN DigitsOnly LIKE N'0%' THEN N'+20' + SUBSTRING(DigitsOnly, 2, 32)
            WHEN LEN(DigitsOnly) >= 11 THEN N'+' + DigitsOnly
            WHEN LEN(DigitsOnly) > 0 THEN N'+20' + DigitsOnly
            ELSE NULL
        END
    FROM Digits
),
Collisions AS
(
    SELECT NormalizedPhone, MatchCount = COUNT_BIG(*)
    FROM Normalized
    WHERE NormalizedPhone IS NOT NULL
    GROUP BY NormalizedPhone
    HAVING COUNT_BIG(*) > 1
)
SELECT
    c.NormalizedPhone,
    c.MatchCount,
    n.PersonId,
    n.DisplayName,
    n.IsArchived,
    n.PrimaryPhone
FROM Collisions AS c
JOIN Normalized AS n ON n.NormalizedPhone = c.NormalizedPhone
ORDER BY c.NormalizedPhone, n.IsArchived, n.DisplayName, n.PersonId;

-- Records that cannot use phone-based resident login until their primary phone is supplied.
SELECT
    p.PersonId,
    p.DisplayName,
    p.IsArchived,
    p.PrimaryPhone,
    p.Whatsapp
FROM dbo.Persons AS p
WHERE NULLIF(LTRIM(RTRIM(p.PrimaryPhone)), N'') IS NULL
ORDER BY p.IsArchived, p.DisplayName, p.PersonId;
