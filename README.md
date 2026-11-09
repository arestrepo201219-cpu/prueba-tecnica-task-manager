# Gestor de tareas

Prueba técnica Fullstack. Es una aplicación para registrar usuarios, crearles tareas y hacer seguimiento al estado de cada una.

Tecnologías usadas:

- Backend: .NET 10 Web API con Entity Framework Core
- Frontend: Angular 22
- Base de datos: SQL Server

## Pasos para ejecutar el proyecto

Antes de empezar se necesita tener instalado:

- .NET SDK 10
- Node.js 22 o superior
- SQL Server 2019 o superior y SQL Server Management Studio (SSMS)

### 1. Base de datos

En la carpeta `database` hay dos scripts:

1. `01_create_database.sql`: crea la base de datos TaskManagerDb con sus tablas, índices, el procedimiento almacenado y algunos datos de ejemplo.
2. `02_queries_and_json_examples.sql`: no es obligatorio. Tiene las consultas de ejemplo, incluidas las de JSON.

Para crear la base de datos:

1. Abrir SSMS y conectarse al servidor. En "Nombre del servidor" escribir `localhost` (o `localhost\SQLEXPRESS` si instaló SQL Server Express) y en "Autenticación" elegir "Autenticación de Windows".
2. Ir a Archivo > Abrir > Archivo y seleccionar `database/01_create_database.sql`.
3. Ejecutarlo con F5. Al terminar, en la pestaña de mensajes debe aparecer "Base de datos TaskManagerDb creada correctamente."

No hace falta crear ningún usuario. La API se conecta con el mismo usuario de Windows con el que usted entra a SSMS.

### 2. Cadena de conexión

La cadena de conexión está en `backend/src/TaskManager.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "TaskManagerDb": "Server=localhost;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
}
```

`Trusted_Connection=True` significa que la conexión se hace con el usuario de Windows que ejecuta la API, por eso no lleva usuario ni contraseña.

Lo único que puede tener que cambiar es el servidor: debe ser el mismo nombre que usa para conectarse en SSMS. Si usa SQL Server Express, quedaría así:

```
Server=localhost\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True
```

### 3. Backend

```bash
cd backend
dotnet run --project src/TaskManager.Api
```

La API queda en http://localhost:5080 y la documentación de Swagger en http://localhost:5080/swagger.

Para confirmar que la API se conecta a la base de datos, abra http://localhost:5080/health. Debe responder `Healthy`. Si responde `Unhealthy`, revise la cadena de conexión y que el servicio de SQL Server esté encendido.

### 4. Frontend

En otra terminal:

```bash
cd frontend/task-manager-web
npm install
npm start
```

La aplicación queda en http://localhost:4200. El backend debe estar corriendo para que cargue la información.

### 5. Pruebas

```bash
cd backend
dotnet test
```

```bash
cd frontend/task-manager-web
npm test
```

## Decisiones técnicas

El backend está organizado en cuatro proyectos siguiendo Clean Architecture. Domain tiene las entidades y las reglas de negocio, Application los servicios, DTOs y validaciones, Infrastructure el acceso a datos con EF Core, y Api los controladores. Las capas se comunican por medio de interfaces, lo que permite probarlas por separado.

Las reglas de negocio están en la entidad de tarea y no en el controlador. Ahí se valida que el título y el usuario sean obligatorios y que una tarea no pueda pasar de Pending a Done sin pasar antes por InProgress. Toda tarea nueva empieza en Pending.

Las validaciones están en tres niveles: FluentValidation revisa los datos que llegan a la API, la entidad protege sus reglas y la base de datos tiene restricciones propias (CHECK, llaves foráneas y un trigger para la regla de estados). Así, aunque alguien modifique los datos directamente en SQL, las reglas se siguen cumpliendo.

Los errores se manejan en un solo lugar. La API responde con un formato estándar (Problem Details) y con el código HTTP que corresponde: 400 cuando los datos son inválidos, 404 cuando no existe el registro, 409 cuando el correo ya está registrado y 422 cuando se incumple una regla de negocio.

Para guardar se usa EF Core, y para consultar el listado de tareas se usa el procedimiento almacenado `usp_Tasks_Search`. El procedimiento filtra por usuario, estado y valores del JSON, ordena por fecha de creación y pagina directamente en SQL Server.

La información adicional de cada tarea (prioridad, fecha estimada, etiquetas y metadatos) se guarda en formato JSON en la columna `AdditionalInfo`. Se valida con ISJSON y se consulta con JSON_VALUE, JSON_QUERY, OPENJSON y JSON_MODIFY. La prioridad tiene una columna calculada con índice para que filtrar por ella sea rápido.

En el frontend se usaron componentes standalone, signals y formularios reactivos. Los errores de la API se manejan en un interceptor HTTP, y la pantalla solo muestra los cambios de estado permitidos para cada tarea, porque la API los envía junto con la tarea.

Hay pruebas unitarias del dominio y de los servicios, pruebas de integración de la API y pruebas del frontend con Vitest.

## Funcionalidades pendientes

- Inicio de sesión y permisos por usuario.
- Editar y eliminar tareas y usuarios.
- Editar desde la pantalla la información adicional (JSON) de una tarea ya creada.
- Historial de cambios de estado de cada tarea.
- Pruebas de integración contra una base de datos SQL Server real.
- Un usuario de base de datos propio para la aplicación, con permisos limitados, para ambientes productivos.
- Docker para levantar la base de datos, la API y el frontend con un solo comando, y un pipeline de despliegue.
