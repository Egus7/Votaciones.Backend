# Votaciones en Supabase

El backend .NET (`Votaciones.Api`) fue migrado a Supabase (proyecto **Votaciones-Axis**, ref `ljuaptdrpiiahiwbmkjj`).
El frontend Angular llama directamente a Supabase; ya no necesita la API .NET ni SignalR.

## Qué reemplaza a qué

| Antes (.NET) | Ahora (Supabase) |
| --- | --- |
| SQL Server + EF Core | Postgres (`migrations/`) |
| Controladores + servicios | Funciones RPC en `public` (`supabase.rpc('actas_crear', ...)`) |
| `[Authorize(Policy = ...)]` | `privado.exigir_permiso('actas.registrar')` dentro de cada RPC |
| JWT propio + `AuthController` | Supabase Auth + Edge Function `login` (usuario o correo) |
| `UsuariosController` POST/PUT | Edge Function `usuarios-admin` + RPC `usuarios_*` |
| `BitacoraService` | `privado.registrar_bitacora` (solo guarda los campos que cambiaron) |
| SignalR `ResultadosHub` | Realtime, canal privado `resultados:{eleccionId}:{TipoCandidato}` |
| Excel con ClosedXML | Se genera en el navegador con `exceljs` |

Las tablas tienen RLS activo sin políticas: no se pueden leer ni escribir directamente desde la API REST,
todo pasa por las RPC (`security definer`) que validan el permiso del rol del usuario.

## Estructura

- `migrations/` — esquema, funciones y permisos (ya aplicados en el proyecto).
- `functions/login`, `functions/usuarios-admin` — Edge Functions (ya desplegadas).
- `migracion-datos/` — scripts para copiar los datos desde SQL Server.

## Migrar los datos desde SQL Server (RDS)

Los hashes de contraseña de ASP.NET Identity no son compatibles con Supabase Auth, así que cada usuario
se crea con una contraseña temporal aleatoria que se guarda en `supabase/.credenciales-temporales.txt`
(ignorado por git). Hay que pedir a cada usuario que la cambie.

```bash
cd supabase/migracion-datos
node exportar-sqlserver.mjs "tcp:<servidor-rds>,1433" votacionesDB <usuario> "<contraseña>"
node generar-sql.mjs
```

Luego ejecutar `migracion-datos/datos.sql` en el SQL Editor de Supabase (o con `psql`). El script es
idempotente (`on conflict do nothing`). `datos.sql` contiene las contraseñas temporales: no subirlo a git.
