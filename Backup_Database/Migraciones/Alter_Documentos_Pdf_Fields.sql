/*
    Campos persistentes y soporte para generación PDF del módulo Documentos.
    Script incremental e idempotente. No elimina datos existentes.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Documento', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DocumentoFirma', N'U') IS NULL
    THROW 50201, 'Ejecute primero Create_Module_Documentos.sql.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Documento', N'Observaciones') IS NULL
        ALTER TABLE dbo.Documento ADD Observaciones nvarchar(max) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'Notas') IS NULL
        ALTER TABLE dbo.Documento ADD Notas nvarchar(max) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'ClienteNombre') IS NULL
        ALTER TABLE dbo.Documento ADD ClienteNombre nvarchar(250) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'ResponsableNombre') IS NULL
        ALTER TABLE dbo.Documento ADD ResponsableNombre nvarchar(200) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'ResponsablePuesto') IS NULL
        ALTER TABLE dbo.Documento ADD ResponsablePuesto nvarchar(150) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'LugarEmision') IS NULL
        ALTER TABLE dbo.Documento ADD LugarEmision nvarchar(250) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'SubTotal') IS NULL
        ALTER TABLE dbo.Documento ADD SubTotal decimal(18,4) NOT NULL
            CONSTRAINT DF_Documento_SubTotal DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.Documento', N'IVA') IS NULL
        ALTER TABLE dbo.Documento ADD IVA decimal(18,4) NOT NULL
            CONSTRAINT DF_Documento_IVA DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.Documento', N'Total') IS NULL
        ALTER TABLE dbo.Documento ADD Total decimal(18,4) NOT NULL
            CONSTRAINT DF_Documento_Total DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.Documento', N'RutaUltimoPdf') IS NULL
        ALTER TABLE dbo.Documento ADD RutaUltimoPdf nvarchar(1000) NULL;

    IF COL_LENGTH(N'dbo.Documento', N'PdfGeneratedAt') IS NULL
        ALTER TABLE dbo.Documento ADD PdfGeneratedAt datetime2(0) NULL;

    IF COL_LENGTH(N'dbo.DocumentoFirma', N'Orden') IS NULL
        ALTER TABLE dbo.DocumentoFirma ADD Orden int NOT NULL
            CONSTRAINT DF_DocumentoFirma_Orden DEFAULT (0) WITH VALUES;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Documento')
          AND name = N'CK_Documento_Totales'
    )
        ALTER TABLE dbo.Documento WITH CHECK ADD CONSTRAINT CK_Documento_Totales
            CHECK (SubTotal >= 0 AND IVA >= 0 AND Total >= 0);

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.DocumentoFirma')
          AND name = N'CK_DocumentoFirma_Orden'
    )
        ALTER TABLE dbo.DocumentoFirma WITH CHECK ADD CONSTRAINT CK_DocumentoFirma_Orden
            CHECK (Orden >= 0);

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.DocumentoFirma')
          AND name = N'IX_DocumentoFirma_Documento_Activo_Orden'
    )
        CREATE NONCLUSTERED INDEX IX_DocumentoFirma_Documento_Activo_Orden
            ON dbo.DocumentoFirma(DocumentoId, Activo, Orden)
            INCLUDE (TipoFirma, NombreFirmante, FechaFirma);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
