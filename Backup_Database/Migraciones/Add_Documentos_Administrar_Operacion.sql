/* Operacion adicional requerida por el CRUD persistente de Documentos. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Modulo', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Operacion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Roll_Operacion', N'U') IS NULL
    THROW 50101, 'No se encontro el esquema de seguridad esperado.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ModuloId int = (SELECT MIN(id) FROM dbo.Modulo WHERE nombre = 'Documentos');
    IF @ModuloId IS NULL
        THROW 50102, 'Ejecute primero Create_Module_Documentos.sql.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Operacion WITH (UPDLOCK, HOLDLOCK)
        WHERE nombre = 'Administrar_Documentos' AND id_Modulo = @ModuloId
    )
        INSERT INTO dbo.Operacion (nombre, id_Modulo)
        VALUES ('Administrar_Documentos', @ModuloId);

    DECLARE @OperacionId int =
    (
        SELECT MIN(id) FROM dbo.Operacion
        WHERE nombre = 'Administrar_Documentos' AND id_Modulo = @ModuloId
    );

    /* Permiso inicial sólo para roles que ya administran seguridad. */
    INSERT INTO dbo.Roll_Operacion (id_Roll, id_Operacion)
    SELECT DISTINCT ro.id_Roll, @OperacionId
    FROM dbo.Roll_Operacion AS ro
    INNER JOIN dbo.Operacion AS op ON op.id = ro.id_Operacion
    WHERE op.nombre IN ('Mostrar_Rol_Y_Priv', 'Agregar_Privilegios')
      AND ro.id_Roll IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Roll_Operacion AS existente
          WHERE existente.id_Roll = ro.id_Roll
            AND existente.id_Operacion = @OperacionId
      );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT m.nombre AS Modulo, o.id, o.nombre AS Operacion
FROM dbo.Modulo AS m
INNER JOIN dbo.Operacion AS o ON o.id_Modulo = m.id
WHERE m.id = @ModuloId AND o.id = @OperacionId;
