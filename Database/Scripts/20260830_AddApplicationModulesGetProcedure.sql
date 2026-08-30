CREATE OR ALTER PROCEDURE [dbo].[SpApplicationModulesGet]
    @PageNumber int = 1,
    @PageSize int = 10,
    @SearchTerm nvarchar(200) = NULL,
    @IncludeInactive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
        THROW 50010, 'PageNumber must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 100
        THROW 50011, 'PageSize must be between 1 and 100.', 1;

    SET @SearchTerm = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    DECLARE @SearchPattern nvarchar(402) = NULL;
    IF @SearchTerm IS NOT NULL
        SET @SearchPattern = N'%' +
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(@SearchTerm, N'\', N'\\'),
                        N'%', N'\%'),
                    N'_', N'\_'),
                N'[', N'\[') + N'%';

    SELECT COUNT_BIG(1) AS [TotalRecords]
    FROM [dbo].[ApplicationModule] AS module
    WHERE (@IncludeInactive = 1 OR module.[IsActive] = 1)
      AND (@SearchPattern IS NULL
        OR module.[Code] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Name] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Description] LIKE @SearchPattern ESCAPE N'\');

    SELECT
        module.[Id],
        module.[Code],
        module.[Name],
        module.[Description],
        module.[Icon],
        module.[DisplayOrder],
        module.[IsActive],
        module.[CreatedAtUtc]
    FROM [dbo].[ApplicationModule] AS module
    WHERE (@IncludeInactive = 1 OR module.[IsActive] = 1)
      AND (@SearchPattern IS NULL
        OR module.[Code] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Name] LIKE @SearchPattern ESCAPE N'\'
        OR module.[Description] LIKE @SearchPattern ESCAPE N'\')
    ORDER BY module.[DisplayOrder], module.[Name], module.[Id]
    OFFSET (CONVERT(bigint, @PageNumber) - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

