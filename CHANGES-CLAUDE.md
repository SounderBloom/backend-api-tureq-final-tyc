# Cambios hechos por Claude

Resumen de todo lo agregado/corregido sobre el repo original, para que sea fácil
ubicarlo en el historial de git al revisar el diff.

> **No se pudo compilar ni correr `dotnet ef` en el entorno donde se escribieron
> estos cambios** (no había SDK de .NET disponible). Antes de desplegar: `dotnet
> build` y, si la migración escrita a mano no cuadra, regenérala con
> `dotnet ef migrations add`.

## Bugs corregidos

- `ProductosService.CrearProducto` nunca creaba la carpeta
  `wwwroot/Uploads/Productos` antes de escribir las fotos del producto en disco
  (`new FileStream(...)`). Si esa carpeta no existía físicamente (instalación
  nueva, carpetas vacías no versionadas en git), lanzaba
  `DirectoryNotFoundException`, atrapada en silencio por el `catch` genérico, y
  el endpoint devolvía `400 { message: "Error al crear el producto" }` sin
  ninguna pista de la causa real. Se agregó `Directory.CreateDirectory(...)`
  antes de escribir cada archivo (como ya hacía correctamente
  `ChatsService.EnviarMensaje`) y se cambió el `catch` para loguear la
  excepción real en consola.
- **Bug más serio, mismo síntoma (400 al publicar con fotos), causa distinta**:
  `FotosProducto.Producto` y `Producto.Categoria` estaban declaradas como
  `= new Producto()` / `= new Categoria()` (inicializador por defecto). Al
  construir un `new FotosProducto {...}` o `new Producto {...}` sin tocar esa
  propiedad de navegación explícitamente, quedaba apuntando a una instancia
  "fantasma" vacía (`Id` en su valor CLR por defecto: `Guid.Empty` / `0`).
  Como `Producto.Id` está configurado `ValueGeneratedOnAdd()` y `Categoria.Id`
  es `IDENTITY`, EF Core interpretaba esas instancias vacías como entidades
  **nuevas** que había que insertar — con `Titulo`/`Descripcion` en `null`,
  violando las columnas `NOT NULL` y tirando un `SqlException` (Error 515) en
  el segundo `SaveChangesAsync()`, justo al publicar un producto **con al
  menos una foto**. Se quitaron esos inicializadores por defecto (las
  navegaciones ahora son `Producto?`/`Categoria?`, sin valor salvo que EF las
  llene vía `.Include(...)`). No requiere migración nueva: es un bug de
  construcción de objetos en memoria, no del esquema de base de datos.
- **Mismo patrón, dos entidades más**: `MensajeChat.Chat = new()` y
  `ArchivosMensaje.Mensaje = new()` tenían el mismo problema. No provocaban una
  excepción (los campos de `Chat` tienen defaults seguros), pero insertaban en
  silencio una fila `Chat` fantasma vacía cada vez que se enviaba un mensaje —
  corrupción de datos silenciosa. Se quitaron los inicializadores (ahora
  `Chat?`/`MensajeChat?`).
- `ChatsService.EnviarMensaje` devolvía `false` (400 genérico "No se pudo
  enviar el mensaje") en tres puntos distintos sin loguear nada, así que no
  había forma de saber cuál de las tres condiciones falló. Se agregó
  `Console.Error.WriteLine` en cada rama de rechazo (chat inexistente/sin
  producto, o el emisor no es ni el comprador ni el vendedor de ese chat) y en
  el `catch` general.
- **La causa real de "No se pudo enviar el mensaje"**:
  `ChatsController.EnviarMensaje([FromForm] CrearMensajeDTO Mensaje)` — el
  parámetro se llamaba `Mensaje`, y `CrearMensajeDTO` **también tiene una
  propiedad `Mensaje`** (el texto del chat). ASP.NET Core, al encontrar un
  campo de formulario literalmente llamado `"Mensaje"`, lo tomaba como el
  *prefijo* obligatorio para bindear el resto de las propiedades del DTO
  (esperando `Mensaje.ChatId`, `Mensaje.EsSistema`, etc. en vez de `ChatId`,
  `EsSistema` sin prefijo). Como el frontend manda los campos sin prefijo,
  `ChatId` siempre llegaba como `Guid.Empty` — el chat "no existía" para el
  backend aunque el usuario mandara el Guid correcto. Se renombró el
  parámetro a `dto` para eliminar la colisión de nombres. Revisé el resto de
  los `[FromForm]`/`[FromBody]` del proyecto: es el único caso con este
  problema (los `[FromBody]` no lo sufren, deserializan el body completo como
  JSON en vez de usar el sistema de prefijos por propiedad).

- `ProductoDTO.Fotos` era `List<IFormFile>` (no servía como respuesta JSON) y
  `BuscarProductos`/`ObtenerProductosDeUsuario` nunca lo llenaban. Ahora es
  `List<string>` con las URLs reales de las fotos.
- `ProductosController.ObtenerMisProductos` devolvía `Data = true` en vez de la
  lista de productos del usuario.
- `[Authorize(nameof(Rol.Administrador))]` (usado en `CategoriasController` y ahora
  también en los endpoints nuevos de admin) referenciaba una política llamada
  `"Administrador"` que **nunca se registró** en `Program.cs` — `AddAuthorization()`
  se llamaba sin argumentos. Esto hacía que esos endpoints fallaran en tiempo de
  ejecución para cualquier usuario, incluidos administradores. Se registró la
  política con `RequireRole`.
- `ChatsService.CrearChat` no guardaba `ImagenProductoSnapshot` ni
  `PrecioProductoSnapshot` del producto (quedaban vacío/0). Ahora sí.

## Funcionalidad nueva

- `GET /api/Productos/Obtener/{id}` — detalle de un producto por Id (no existía).
- **Notificaciones**: se agregaron `Leida`, `Tipo` y `ReferenciaId` a la entidad
  `Notificaciones` (ya existía la tabla pero no había controlador). Nuevo
  `NotificacionesController`: `Obtener`, `ContarNoLeidas`, `MarcarLeida`,
  `MarcarTodasLeidas`. Se generan automáticamente al recibir un mensaje, una
  propuesta, una respuesta a propuesta o una calificación.
- **Propuestas de trueque**: nueva entidad `Propuesta` (chat, producto solicitado,
  producto ofrecido, proponente, vendedor, estado). Nuevo `PropuestasController`:
  `Crear` (solo el comprador del chat, valida que el producto ofrecido le
  pertenezca), `ObtenerPorChat`, `Responder` (solo el vendedor puede
  aceptar/rechazar — al aceptar, ambos productos se marcan `Disponible = false`),
  `PendientesDeCalificar`.
- **Calificaciones**: nueva entidad `Calificacion` (una por propuesta, índice
  único). Nuevo `CalificacionesController`: `Crear` (solo si la propuesta está
  Aceptada y quien califica es el proponente), `ObtenerDeUsuario/{id}` (promedio +
  recientes, público).
- **Mi perfil**: `GET /api/Usuarios/MiPerfil` — datos del usuario + trueques
  realizados + artículos activos + promedio/calificaciones.
- **Panel de administrador**: `GET /api/Usuarios/ObtenerTodos` y
  `POST /api/Usuarios/CambiarRol/{id}` (ambos `[Authorize(Administrador)]`; no se
  puede cambiar el propio rol). Las categorías se administran con los endpoints
  que ya existían en `CategoriasController`.

## Archivos nuevos relevantes

- `Data/Propuestas/`, `Data/Calificaciones/`, `Data/Notificacion/TipoNotificacion.cs`
- `Models/Propuestas/`, `Models/Calificaciones/`, `Models/Notificaciones/`,
  `Models/Usuarios/`
- `Service/NotificacionesService.cs`, `Service/PropuestasService.cs`,
  `Service/CalificacionesService.cs`, `Service/UsuariosService.cs`
- `Controllers/NotificacionesController.cs`, `Controllers/PropuestasController.cs`,
  `Controllers/CalificacionesController.cs`, `Controllers/UsuariosController.cs`
- `Migrations/20260807000000_AgregarPropuestasCalificacionesYNotificaciones.*`
  (escrita a mano, ver nota arriba)

## Funcionalidad nueva (sesión de ofertas extendidas)

- **Chats sin duplicar**: `ChatsService.CrearChat` ahora busca primero si ya
  existe un chat entre ese usuario y ese producto y lo reutiliza (y lo vuelve
  a mostrar si el usuario lo había ocultado), en vez de crear una fila nueva
  cada vez. El endpoint `POST /api/Chats/Crear/{ProductoId}` ahora devuelve el
  **Id del chat** en `Data` (antes devolvía solo `true`/`false`), para que el
  frontend pueda navegar directo sin tener que volver a listar todos los
  chats para encontrarlo.
- **Ofertas con tipo**: `Propuesta` ahora soporta tres tipos
  (`TipoOferta`): `Trueque` (producto por producto, como antes), `Compra`
  (solo efectivo, sin producto ofrecido) y `TruequeConDiferencia` (producto +
  una diferencia en efectivo, con `DireccionMonto` indicando si el proponente
  la pone o la pide). Nuevos campos: `TipoOferta`, `Monto` (nullable),
  `DireccionMonto` (nullable). `ProductoOfrecidoId` pasó a ser nullable
  (`Guid?`) porque una `Compra` no ofrece ningún producto. Migración nueva:
  `20260807010000_AgregarTipoOfertaAPropuesta`.

## Solicitud de donación (TipoOferta.SolicitudDonacion)

- Nuevo valor `TipoOferta.SolicitudDonacion = 3`: para artículos publicados
  como `TipoTransaccion.Donar`, no requiere producto ofrecido ni monto — es
  solo la solicitud de que el vendedor te done el artículo a ti, que él
  puede aceptar o rechazar como cualquier otra oferta.
- `PropuestasService.CrearPropuesta` ahora valida en el servidor (no solo en
  el frontend) que el tipo de oferta sea consistente con el producto: un
  artículo de donación solo admite `SolicitudDonacion`, y `SolicitudDonacion`
  no aplica a artículos que no son de donación.
- No requiere migración nueva (mismo patrón que `TipoOferta` ya existente,
  columna `int` sin restricción a nivel de base de datos).
