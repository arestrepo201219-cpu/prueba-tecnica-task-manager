/*
   Consultas de ejemplo. Se ejecutan después de 01_create_database.sql.
   Cada bloque se puede seleccionar y ejecutar por separado en SSMS.

   Parte A: la consulta que pide el enunciado (tareas por usuario, por estado y por fecha).
   Parte B: ejemplos de las funciones JSON.
   Parte C: pruebas de que la base de datos rechaza datos inválidos.
*/
USE TaskManagerDb;
GO

-- ===== Parte A: consulta de tareas por usuario =====

-- A.1 Tareas de un usuario, filtradas por estado y ordenadas por fecha de creación
DECLARE @UserId   INT     = 1;
DECLARE @StatusId TINYINT = 2;          -- 1 Pending, 2 InProgress, 3 Done. En NULL trae todas

SELECT  t.Id,
        t.Title,
        s.Code      AS Status,
        u.Name      AS AssignedTo,
        t.CreatedAt
FROM dbo.Tasks AS t
INNER JOIN dbo.Users        AS u ON u.Id = t.UserId
INNER JOIN dbo.TaskStatuses AS s ON s.Id = t.StatusId
WHERE t.UserId = @UserId
  AND (@StatusId IS NULL OR t.StatusId = @StatusId)
ORDER BY t.CreatedAt DESC;
GO

-- A.2 Lo mismo, pero con el procedimiento que usa la API
EXEC dbo.usp_Tasks_Search @UserId = 1;                       -- todas las tareas del usuario 1
EXEC dbo.usp_Tasks_Search @UserId = 1, @StatusId = 3;        -- usuario 1, solo Done
EXEC dbo.usp_Tasks_Search @StatusId = 1, @SortAscending = 1; -- Pending, de la más antigua a la más nueva
GO

-- ===== Parte B: funciones JSON =====

-- B.1 ISJSON dice si un texto es JSON válido. Es la que usa el CHECK de la tabla.
SELECT  Id,
        ISJSON(AdditionalInfo) AS EsJsonValido      -- 1 válido, 0 inválido, NULL si no hay datos
FROM dbo.Tasks;

SELECT ISJSON(N'{"priority":"High"}') AS Valido,  -- 1
       ISJSON(N'{priority:High}')     AS Invalido; -- 0
GO

-- B.2 JSON_VALUE lee un valor puntual
SELECT  Id,
        Title,
        JSON_VALUE(AdditionalInfo, N'$.priority')          AS Prioridad,
        JSON_VALUE(AdditionalInfo, N'$.dueDate')           AS FechaEstimada,
        JSON_VALUE(AdditionalInfo, N'$.metadata.sprint')   AS Sprint,      -- se puede entrar a objetos anidados
        JSON_VALUE(AdditionalInfo, N'$.tags[0]')           AS PrimeraEtiqueta
FROM dbo.Tasks
WHERE AdditionalInfo IS NOT NULL;
GO

-- B.3 JSON_QUERY trae un arreglo u objeto completo. JSON_VALUE aquí devolvería NULL
SELECT  Id,
        Title,
        JSON_QUERY(AdditionalInfo, N'$.tags')     AS Etiquetas,   -- ["devops","azure"]
        JSON_QUERY(AdditionalInfo, N'$.metadata') AS Metadatos    -- {"sprint":"S-24",...}
FROM dbo.Tasks
WHERE AdditionalInfo IS NOT NULL;
GO

-- B.4 Filtrar por un valor del JSON
-- Directamente con JSON_VALUE
SELECT Id, Title, JSON_VALUE(AdditionalInfo, N'$.priority') AS Prioridad
FROM dbo.Tasks
WHERE JSON_VALUE(AdditionalInfo, N'$.priority') = N'High';

-- Con la columna calculada. Da lo mismo, pero aprovecha el índice IX_Tasks_Priority
SELECT Id, Title, Priority
FROM dbo.Tasks
WHERE Priority = N'High';

-- Tareas con fecha estimada antes del 1 de diciembre
SELECT Id, Title, TRY_CONVERT(DATE, JSON_VALUE(AdditionalInfo, N'$.dueDate'), 23) AS FechaEstimada
FROM dbo.Tasks
WHERE TRY_CONVERT(DATE, JSON_VALUE(AdditionalInfo, N'$.dueDate'), 23) < '2026-12-01'
ORDER BY FechaEstimada;
GO

-- B.5 OPENJSON convierte el JSON en filas
-- Tareas que tienen una etiqueta
DECLARE @Tag NVARCHAR(50) = N'backend';

SELECT t.Id, t.Title
FROM dbo.Tasks AS t
WHERE EXISTS (SELECT 1
              FROM OPENJSON(t.AdditionalInfo, N'$.tags') AS tag
              WHERE tag.[value] = @Tag);

-- Cuántas tareas hay por etiqueta
SELECT tag.[value] AS Etiqueta,
       COUNT(*)    AS CantidadTareas
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalInfo, N'$.tags') AS tag
GROUP BY tag.[value]
ORDER BY CantidadTareas DESC, Etiqueta;

-- Con WITH se pueden sacar los campos del JSON como columnas normales
SELECT t.Id,
       t.Title,
       info.Priority,
       info.DueDate,
       info.Sprint,
       info.Tags
FROM dbo.Tasks AS t
CROSS APPLY OPENJSON(t.AdditionalInfo)
WITH
(
    Priority NVARCHAR(20)  N'$.priority',
    DueDate  DATE          N'$.dueDate',
    Sprint   NVARCHAR(50)  N'$.metadata.sprint',
    Tags     NVARCHAR(MAX) N'$.tags' AS JSON
) AS info;
GO

-- B.6 JSON_MODIFY cambia un campo sin reescribir el JSON completo
BEGIN TRANSACTION;

    UPDATE dbo.Tasks
    SET AdditionalInfo = JSON_MODIFY(AdditionalInfo, N'$.priority', N'Low'),
        UpdatedAt      = SYSUTCDATETIME()
    WHERE Id = 1;

    -- Con append se agrega un elemento al arreglo
    UPDATE dbo.Tasks
    SET AdditionalInfo = JSON_MODIFY(AdditionalInfo, N'append $.tags', N'urgent')
    WHERE Id = 1;

    SELECT Id, AdditionalInfo, Priority FROM dbo.Tasks WHERE Id = 1;

ROLLBACK TRANSACTION;   -- se deshace para no dañar los datos de ejemplo

-- Lo mismo con el procedimiento
BEGIN TRANSACTION;
    EXEC dbo.usp_Tasks_SetAdditionalInfoValue @TaskId = 3, @Path = N'$.metadata.reviewer', @Value = N'Laura';
    SELECT Id, AdditionalInfo FROM dbo.Tasks WHERE Id = 3;
ROLLBACK TRANSACTION;
GO

-- B.7 El procedimiento de búsqueda filtrando por datos del JSON
EXEC dbo.usp_Tasks_Search @Priority = N'High';
EXEC dbo.usp_Tasks_Search @Tag = N'backend', @StatusId = 1;
GO

-- ===== Parte C: validaciones de la base de datos =====
-- Todos estos casos deben fallar. Si funcionan, algo está mal.

-- C.1 JSON mal formado
BEGIN TRY
    INSERT INTO dbo.Tasks (Title, UserId, AdditionalInfo) VALUES (N'Prueba', 1, N'{esto no es json');
END TRY
BEGIN CATCH
    SELECT N'C.1 OK (rechazado)' AS Resultado, ERROR_MESSAGE() AS Mensaje;
END CATCH;

-- C.2 Título vacío
BEGIN TRY
    INSERT INTO dbo.Tasks (Title, UserId) VALUES (N'   ', 1);
END TRY
BEGIN CATCH
    SELECT N'C.2 OK (rechazado)' AS Resultado, ERROR_MESSAGE() AS Mensaje;
END CATCH;

-- C.3 Usuario que no existe
BEGIN TRY
    INSERT INTO dbo.Tasks (Title, UserId) VALUES (N'Sin usuario válido', 9999);
END TRY
BEGIN CATCH
    SELECT N'C.3 OK (rechazado)' AS Resultado, ERROR_MESSAGE() AS Mensaje;
END CATCH;

-- C.4 Pasar de Pending a Done, lo frena el trigger
BEGIN TRY
    UPDATE dbo.Tasks SET StatusId = 3 WHERE Id = 3 AND StatusId = 1;  -- la tarea 3 está en Pending
END TRY
BEGIN CATCH
    SELECT N'C.4 OK (rechazado)' AS Resultado, ERROR_MESSAGE() AS Mensaje;
END CATCH;
GO
