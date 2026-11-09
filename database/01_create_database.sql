/*
   Script de creación de la base de datos TaskManagerDb.

   Se puede abrir en SSMS y ejecutar completo con F5. Funciona en SQL Server 2019 o superior.

   Se puede correr varias veces: si la base ya existe no la vuelve a crear, pero sí borra y
   recrea las tablas, el trigger, los procedimientos y los datos de ejemplo. Ojo con eso si
   ya hay información que quiera conservar.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Base de datos
IF DB_ID(N'TaskManagerDb') IS NULL
BEGIN
    CREATE DATABASE TaskManagerDb;
END
GO

USE TaskManagerDb;
GO

-- Las funciones JSON (OPENJSON sobre todo) necesitan nivel de compatibilidad 130 o más.
IF (SELECT compatibility_level FROM sys.databases WHERE name = N'TaskManagerDb') < 130
BEGIN
    ALTER DATABASE TaskManagerDb SET COMPATIBILITY_LEVEL = 150;
END
GO

-- SQL Server exige estas opciones para poder indexar la columna calculada Priority.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Se borra todo antes de crearlo para poder ejecutar el script más de una vez.
-- El orden importa por las llaves foráneas.
DROP PROCEDURE IF EXISTS dbo.usp_Tasks_Search;
DROP PROCEDURE IF EXISTS dbo.usp_Tasks_SetAdditionalInfoValue;
DROP TABLE IF EXISTS dbo.Tasks;                  -- al borrar la tabla se borra también su trigger
DROP TABLE IF EXISTS dbo.TaskStatusTransitions;
DROP TABLE IF EXISTS dbo.TaskStatuses;
DROP TABLE IF EXISTS dbo.Users;
GO

-- Estados de una tarea. Los Id son los mismos del enum TaskItemStatus del backend,
-- así que si se cambian aquí hay que cambiarlos allá también.
CREATE TABLE dbo.TaskStatuses
(
    Id          TINYINT       NOT NULL,
    Code        NVARCHAR(20)  NOT NULL,
    Description NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_TaskStatuses      PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_TaskStatuses_Code UNIQUE (Code)
);
GO

INSERT INTO dbo.TaskStatuses (Id, Code, Description)
VALUES (1, N'Pending',    N'Tarea registrada, sin iniciar'),
       (2, N'InProgress', N'Tarea en ejecución'),
       (3, N'Done',       N'Tarea finalizada');
GO

-- Cambios de estado permitidos. Lo que no esté en esta tabla no se puede hacer.
-- Por eso no hay una fila de Pending a Done: es la regla de negocio del enunciado.
CREATE TABLE dbo.TaskStatusTransitions
(
    FromStatusId TINYINT NOT NULL,
    ToStatusId   TINYINT NOT NULL,

    CONSTRAINT PK_TaskStatusTransitions PRIMARY KEY CLUSTERED (FromStatusId, ToStatusId),
    CONSTRAINT FK_TaskStatusTransitions_From FOREIGN KEY (FromStatusId) REFERENCES dbo.TaskStatuses (Id),
    CONSTRAINT FK_TaskStatusTransitions_To   FOREIGN KEY (ToStatusId)   REFERENCES dbo.TaskStatuses (Id),
    CONSTRAINT CK_TaskStatusTransitions_Distinct CHECK (FromStatusId <> ToStatusId)
);
GO

INSERT INTO dbo.TaskStatusTransitions (FromStatusId, ToStatusId)
VALUES (1, 2),   -- de Pending a InProgress
       (2, 1),   -- de InProgress a Pending, por si hay que devolverla
       (2, 3),   -- de InProgress a Done
       (3, 2);   -- de Done a InProgress, para reabrirla
GO

-- Usuarios
CREATE TABLE dbo.Users
(
    Id        INT IDENTITY(1, 1) NOT NULL,
    Name      NVARCHAR(100)      NOT NULL,
    Email     NVARCHAR(256)      NOT NULL,
    CreatedAt DATETIME2(3)       NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Users              PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Users_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_Users_Email_Format  CHECK (Email LIKE N'%_@_%._%' AND Email NOT LIKE N'% %')
);
GO

-- Tareas
CREATE TABLE dbo.Tasks
(
    Id             INT IDENTITY(1, 1) NOT NULL,
    Title          NVARCHAR(200)      NOT NULL,
    Description    NVARCHAR(1000)     NULL,
    StatusId       TINYINT            NOT NULL CONSTRAINT DF_Tasks_StatusId  DEFAULT (1),
    UserId         INT                NOT NULL,
    -- Información adicional en JSON, por ejemplo:
    -- {"priority":"High","dueDate":"2026-12-01","tags":["backend","api"],"metadata":{"sprint":"S-24"}}
    AdditionalInfo NVARCHAR(MAX)      NULL,
    CreatedAt      DATETIME2(3)       NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)       NULL,

    -- La prioridad se saca del JSON en una columna calculada para poder ponerle un índice.
    -- Si se filtrara con JSON_VALUE en el WHERE, SQL Server tendría que leer toda la tabla.
    -- El CASE evita que falle con un JSON mal formado (de todas formas el CHECK lo rechaza).
    Priority AS CONVERT(NVARCHAR(20), CASE WHEN ISJSON(AdditionalInfo) = 1
                                           THEN JSON_VALUE(AdditionalInfo, N'$.priority') END),

    CONSTRAINT PK_Tasks PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Tasks_Users        FOREIGN KEY (UserId)   REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Tasks_TaskStatuses FOREIGN KEY (StatusId) REFERENCES dbo.TaskStatuses (Id),

    -- El título es obligatorio y tampoco puede ser solo espacios
    CONSTRAINT CK_Tasks_Title_NotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),

    -- Solo se acepta JSON válido y que sea un objeto { ... }
    CONSTRAINT CK_Tasks_AdditionalInfo_IsJson
        CHECK (AdditionalInfo IS NULL OR (ISJSON(AdditionalInfo) = 1 AND LEFT(LTRIM(AdditionalInfo), 1) = N'{')),

    -- Valida la prioridad y la fecha dentro del JSON. Se usa CASE en vez de OR porque SQL Server
    -- no siempre evalúa el OR en orden, y JSON_VALUE falla si el texto no es JSON válido.
    CONSTRAINT CK_Tasks_AdditionalInfo_Priority
        CHECK (CASE WHEN ISJSON(AdditionalInfo) = 1
                    THEN CASE WHEN JSON_VALUE(AdditionalInfo, N'$.priority') IS NULL
                                OR JSON_VALUE(AdditionalInfo, N'$.priority') IN (N'Low', N'Medium', N'High')
                              THEN 1 ELSE 0 END
                    ELSE 1 END = 1),

    CONSTRAINT CK_Tasks_AdditionalInfo_DueDate
        CHECK (CASE WHEN ISJSON(AdditionalInfo) = 1
                    THEN CASE WHEN JSON_VALUE(AdditionalInfo, N'$.dueDate') IS NULL
                                OR TRY_CONVERT(DATE, JSON_VALUE(AdditionalInfo, N'$.dueDate'), 23) IS NOT NULL
                              THEN 1 ELSE 0 END
                    ELSE 1 END = 1)
);
GO

-- Índices

-- No puede haber dos usuarios con el mismo correo
CREATE UNIQUE NONCLUSTERED INDEX UX_Users_Email
    ON dbo.Users (Email);
GO

-- Para la consulta principal: tareas de un usuario, por estado y ordenadas por fecha.
-- También le sirve a la llave foránea UserId, que SQL Server no indexa solo.
CREATE NONCLUSTERED INDEX IX_Tasks_UserId_StatusId_CreatedAt
    ON dbo.Tasks (UserId, StatusId, CreatedAt DESC)
    INCLUDE (Title);
GO

-- Para el listado filtrado solo por estado
CREATE NONCLUSTERED INDEX IX_Tasks_StatusId_CreatedAt
    ON dbo.Tasks (StatusId, CreatedAt DESC);
GO

-- Para filtrar por la prioridad que viene del JSON
CREATE NONCLUSTERED INDEX IX_Tasks_Priority
    ON dbo.Tasks (Priority)
    INCLUDE (StatusId, UserId, CreatedAt);
GO

-- Trigger para los cambios de estado.
-- La API ya valida que no se pase de Pending a Done, pero si alguien actualiza la tabla
-- directamente desde SQL, este trigger lo frena. Revisa contra TaskStatusTransitions.
CREATE OR ALTER TRIGGER dbo.TR_Tasks_ValidateStatusTransition
ON dbo.Tasks
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(StatusId) RETURN;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS i
        INNER JOIN deleted AS d ON d.Id = i.Id
        WHERE i.StatusId <> d.StatusId
          AND NOT EXISTS (SELECT 1
                          FROM dbo.TaskStatusTransitions AS tr
                          WHERE tr.FromStatusId = d.StatusId
                            AND tr.ToStatusId   = i.StatusId)
    )
    BEGIN
        ;THROW 50001, N'Transición de estado no permitida. Consulte dbo.TaskStatusTransitions.', 1;
    END
END
GO

/*
   Búsqueda de tareas. Es la que usa la API para el listado y para consultar una tarea.

   Todos los parámetros son opcionales; si llegan en NULL no se filtra por ese campo.
   Permite filtrar por usuario, estado, prioridad y etiqueta (estos dos últimos salen del
   JSON), ordenar por fecha de creación y paginar. TotalCount trae el total de registros
   para que el front pueda armar la paginación.

   El OPTION (RECOMPILE) es porque con tantos filtros opcionales el plan que SQL Server
   guarda para una combinación puede ser muy malo para otra.
*/
CREATE OR ALTER PROCEDURE dbo.usp_Tasks_Search
    @TaskId        INT          = NULL,
    @UserId        INT          = NULL,
    @StatusId      TINYINT      = NULL,
    @Priority      NVARCHAR(20) = NULL,
    @Tag           NVARCHAR(50) = NULL,
    @SortAscending BIT          = 0,
    @PageNumber    INT          = 1,
    @PageSize      INT          = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize  < 1 OR @PageSize > 100 SET @PageSize = 20;

    SELECT
        t.Id,
        t.Title,
        t.Description,
        t.StatusId,
        s.Code                                      AS StatusCode,
        t.UserId,
        u.Name                                      AS UserName,
        u.Email                                     AS UserEmail,
        t.CreatedAt,
        t.UpdatedAt,
        t.Priority,
        TRY_CONVERT(DATE, JSON_VALUE(t.AdditionalInfo, N'$.dueDate'), 23) AS DueDate,
        JSON_QUERY(t.AdditionalInfo, N'$.tags')     AS TagsJson,      -- JSON_QUERY porque es un arreglo
        JSON_QUERY(t.AdditionalInfo, N'$.metadata') AS MetadataJson,  -- y este es un objeto
        COUNT(*) OVER ()                            AS TotalCount
    FROM dbo.Tasks AS t
    INNER JOIN dbo.Users        AS u ON u.Id = t.UserId
    INNER JOIN dbo.TaskStatuses AS s ON s.Id = t.StatusId
    WHERE (@TaskId   IS NULL OR t.Id       = @TaskId)
      AND (@UserId   IS NULL OR t.UserId   = @UserId)
      AND (@StatusId IS NULL OR t.StatusId = @StatusId)
      AND (@Priority IS NULL OR t.Priority = @Priority)
      AND (@Tag      IS NULL OR EXISTS (SELECT 1
                                        FROM OPENJSON(t.AdditionalInfo, N'$.tags') AS tag
                                        WHERE tag.[value] = @Tag))
    ORDER BY
        CASE WHEN @SortAscending = 1 THEN t.CreatedAt END ASC,
        CASE WHEN @SortAscending = 0 THEN t.CreatedAt END DESC,
        t.Id DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY
    OPTION (RECOMPILE);
END
GO

/*
   Cambia un solo campo del JSON sin tocar el resto, usando JSON_MODIFY.
   Ejemplo: EXEC dbo.usp_Tasks_SetAdditionalInfoValue 1, N'$.priority', N'Low';
   Si @Value llega en NULL, la propiedad se elimina del JSON.
*/
CREATE OR ALTER PROCEDURE dbo.usp_Tasks_SetAdditionalInfoValue
    @TaskId INT,
    @Path   NVARCHAR(200),
    @Value  NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Id = @TaskId)
    BEGIN
        ;THROW 50002, N'La tarea no existe.', 1;
    END

    UPDATE dbo.Tasks
    SET AdditionalInfo = JSON_MODIFY(COALESCE(AdditionalInfo, N'{}'), @Path, @Value),
        UpdatedAt      = SYSUTCDATETIME()
    WHERE Id = @TaskId;
END
GO

-- Datos de ejemplo
INSERT INTO dbo.Users (Name, Email)
VALUES (N'Ana Gómez',     N'ana.gomez@empresa.com'),
       (N'Carlos Pérez',  N'carlos.perez@empresa.com'),
       (N'Laura Restrepo', N'laura.restrepo@empresa.com');
GO

INSERT INTO dbo.Tasks (Title, Description, StatusId, UserId, AdditionalInfo, CreatedAt)
VALUES
(N'Configurar pipeline CI/CD', N'Pipeline de build y despliegue en Azure DevOps', 2, 1,
 N'{"priority":"High","dueDate":"2026-11-20","tags":["devops","azure"],"metadata":{"sprint":"S-24","storyPoints":"5"}}',
 DATEADD(DAY, -6, SYSUTCDATETIME())),

(N'Diseñar modelo de datos', N'Tablas Users y Tasks con sus relaciones', 3, 1,
 N'{"priority":"Medium","dueDate":"2026-11-10","tags":["database","sql-server"],"metadata":{"sprint":"S-23"}}',
 DATEADD(DAY, -10, SYSUTCDATETIME())),

(N'Crear endpoint de tareas', NULL, 1, 2,
 N'{"priority":"High","tags":["backend","api"],"metadata":{"sprint":"S-24","reviewer":"Ana"}}',
 DATEADD(DAY, -3, SYSUTCDATETIME())),

(N'Maquetar listado de tareas', N'Vista con filtro por estado', 1, 3,
 N'{"priority":"Low","dueDate":"2026-12-05","tags":["frontend","angular"]}',
 DATEADD(DAY, -2, SYSUTCDATETIME())),

(N'Pruebas unitarias del servicio', NULL, 2, 2,
 N'{"priority":"Medium","tags":["backend","testing"]}',
 DATEADD(DAY, -1, SYSUTCDATETIME())),

(N'Documentar API en README', NULL, 1, 3, NULL, SYSUTCDATETIME());
GO

PRINT N'Base de datos TaskManagerDb creada correctamente.';
GO
