-- =============================================================================
-- Esquema "privado": funciones internas (no expuestas por la API REST).
-- Reemplazan a CurrentUserService, PermissionAuthorizationHandler, BitacoraHelper
-- y BitacoraService del backend .NET.
-- =============================================================================

create schema if not exists privado;
revoke all on schema privado from public;
-- authenticated necesita "usage" para la política RLS de Realtime (tiene_permiso).
grant usage on schema privado to authenticated;

-- Hora actual de Ecuador (antes: Fecha.DevolverDatetime con "SA Pacific Standard Time").
create or replace function privado.ahora()
returns timestamp
language sql
stable
set search_path = ''
as $$
  select (now() at time zone 'America/Guayaquil')::timestamp;
$$;

-- Lanza un error de negocio (equivalente a ArgumentException / InvalidOperationException).
create or replace function privado.error(p_mensaje text)
returns void
language plpgsql
set search_path = ''
as $$
begin
  raise exception using message = p_mensaje, errcode = 'P0001';
end;
$$;

-- Devuelve los permisos del usuario autenticado, solo si el usuario y su rol están activos.
create or replace function privado.permisos_usuario_actual()
returns jsonb
language sql
stable
security definer
set search_path = ''
as $$
  select r.permisos_rol
  from public.adm_usuario u
  join public.adm_rol r on r.id_rol = u.rol_id
  where u.id_usuario = auth.uid()
    and u.estado
    and r.activo;
$$;

create or replace function privado.tiene_permiso(p_permiso text)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
  select coalesce(
    exists (
      select 1
      from jsonb_array_elements_text(privado.permisos_usuario_actual()) p
      where lower(p) = lower(p_permiso)
    ),
    false
  );
$$;

-- Exige un usuario autenticado y activo (equivalente a [Authorize]).
create or replace function privado.exigir_usuario()
returns uuid
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception using message = 'Se requiere autenticación, por favor inicie sesión.', errcode = '28000';
  end if;
  if privado.permisos_usuario_actual() is null then
    raise exception using message = 'El usuario o su rol se encuentran inactivos.', errcode = '42501';
  end if;
  return auth.uid();
end;
$$;

-- Exige un permiso (equivalente a [Authorize(Policy = RolPermisos.X)]).
create or replace function privado.exigir_permiso(p_permiso text)
returns uuid
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_usuario uuid := privado.exigir_usuario();
begin
  if not privado.tiene_permiso(p_permiso) then
    raise exception using message = 'No tiene permisos para realizar esta acción.', errcode = '42501';
  end if;
  return v_usuario;
end;
$$;

-- Devuelve solo las claves cuyo valor cambió, conservando el orden original
-- (equivalente a BitacoraHelper.ObtenerSoloCambios). Si no hay cambios, ambos son null.
create or replace function privado.solo_cambios(
  p_anteriores json,
  p_nuevos json,
  out anteriores json,
  out nuevos json
)
language sql
immutable
set search_path = ''
as $$
  with n as (
    select key, value, ord
    from json_each(coalesce(p_nuevos, '{}'::json)) with ordinality as t(key, value, ord)
  ),
  a as (
    select key, value from json_each(coalesce(p_anteriores, '{}'::json))
  ),
  c as (
    select n.key, n.ord, a.value as valor_anterior, n.value as valor_nuevo
    from n
    left join a on a.key = n.key
    where a.value::jsonb is distinct from n.value::jsonb
  )
  select
    json_object_agg(c.key, c.valor_anterior order by c.ord),
    json_object_agg(c.key, c.valor_nuevo order by c.ord)
  from c;
$$;

-- Inserta un registro de auditoría (equivalente a BitacoraService.RegistrarBitacoraAsync).
create or replace function privado.registrar_bitacora(
  p_accion text,
  p_tabla text,
  p_id_registro text,
  p_descripcion text,
  p_eleccion_id uuid default null,
  p_valores_anteriores json default null,
  p_valores_nuevos json default null
)
returns void
language sql
security definer
set search_path = ''
as $$
  insert into public.adm_bitacora (
    eleccion_id, usuario_id, fecha_registro, accion, tabla, id_registro,
    descripcion, valores_anteriores, valores_nuevos
  )
  values (
    p_eleccion_id, auth.uid(), privado.ahora(), p_accion, p_tabla, p_id_registro,
    p_descripcion, p_valores_anteriores, p_valores_nuevos
  );
$$;

-- Total de páginas como en .NET: Math.Ceiling(total / pageSize).
create or replace function privado.paginacion(
  p_items jsonb,
  p_pagina integer,
  p_page_size integer,
  p_total integer
)
returns jsonb
language sql
immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'items', coalesce(p_items, '[]'::jsonb),
    'pageActual', p_pagina,
    'pageSize', p_page_size,
    'totalRegistros', p_total,
    'totalPages', case when p_page_size > 0 then ceil(p_total::numeric / p_page_size)::integer else 0 end
  );
$$;

-- Valida página y tamaño (en .NET llegaban con valores por defecto 1 y 7).
create or replace function privado.offset_pagina(p_pagina integer, p_page_size integer)
returns integer
language sql
immutable
set search_path = ''
as $$
  select greatest(coalesce(p_pagina, 1) - 1, 0) * greatest(coalesce(p_page_size, 7), 1);
$$;

revoke all on all functions in schema privado from public, anon, authenticated;
grant execute on function privado.tiene_permiso(text) to authenticated;
grant execute on function privado.permisos_usuario_actual() to authenticated;
