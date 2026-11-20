-- =============================================================================
-- RPC: Zonas, Elecciones, Listas electorales, Roles, Usuarios y Bitácora.
-- Cada función replica un endpoint del backend .NET y devuelve el mismo JSON
-- (camelCase) que devolvía el controlador correspondiente.
-- =============================================================================

-- Convierte la fecha que envía el frontend (ISO con o sin zona horaria) a hora de Ecuador.
create or replace function privado.a_timestamp(p_valor text)
returns timestamp
language sql
immutable
set search_path = ''
as $$
  select case
    when nullif(trim(p_valor), '') is null then null
    when p_valor ~ '(Z|[+-]\d{2}:?\d{2})$' then (p_valor::timestamptz at time zone 'America/Guayaquil')::timestamp
    else p_valor::timestamp
  end;
$$;

-- -----------------------------------------------------------------------------
-- Zonas (ZonasController: solo requiere usuario autenticado)
-- -----------------------------------------------------------------------------
create or replace function public.zonas_provincias()
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_usuario();
  return coalesce((
    select jsonb_agg(jsonb_build_object(
      'idProvincia', p.id_provincia,
      'codigoProvincia', p.codigo_provincia,
      'nombreProvincia', p.nombre_provincia,
      'activo', p.activo
    ) order by nullif(regexp_replace(p.codigo_provincia, '\D', '', 'g'), '')::integer nulls last)
    from public.provincia p
  ), '[]'::jsonb);
end;
$$;

create or replace function public.zonas_cantones(p_provincia_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_usuario();
  return coalesce((
    select jsonb_agg(jsonb_build_object(
      'idCanton', c.id_canton,
      'provinciaId', c.provincia_id,
      'nombreCanton', c.nombre_canton,
      'activo', c.activo
    ) order by c.nombre_canton)
    from public.canton c
    where c.provincia_id = p_provincia_id
  ), '[]'::jsonb);
end;
$$;

create or replace function public.zonas_parroquias(p_canton_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_usuario();
  return coalesce((
    select jsonb_agg(jsonb_build_object(
      'idParroquia', p.id_parroquia,
      'cantonId', p.canton_id,
      'nombreParroquia', p.nombre_parroquia,
      'parroquiaRural', p.parroquia_rural,
      'activo', p.activo
    ) order by p.nombre_parroquia)
    from public.parroquia p
    where p.canton_id = p_canton_id
  ), '[]'::jsonb);
end;
$$;

create or replace function public.zonas_zonas(p_parroquia_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_usuario();
  return coalesce((
    select jsonb_agg(jsonb_build_object(
      'idZona', z.id_zona,
      'parroquiaId', z.parroquia_id,
      'nombreZona', z.nombre_zona,
      'activo', z.activo
    ) order by z.nombre_zona)
    from public.zona z
    where z.parroquia_id = p_parroquia_id
  ), '[]'::jsonb);
end;
$$;

-- -----------------------------------------------------------------------------
-- Elecciones
-- -----------------------------------------------------------------------------
create or replace function privado.eleccion_json(e public.eleccion)
returns jsonb
language sql
immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'idEleccion', e.id_eleccion,
    'nombreEleccion', e.nombre_eleccion,
    'tipoEleccion', e.tipo_eleccion,
    'descripcion', e.descripcion,
    'fechaEleccion', e.fecha_eleccion,
    'estado', e.estado,
    'metodoEscanos', e.metodo_escanos,
    'fechaCreacion', e.fecha_creacion,
    'fechaModificacion', e.fecha_modificacion
  );
$$;

create or replace function privado.eleccion_auditoria(e public.eleccion)
returns json
language sql
immutable
set search_path = ''
as $$
  select json_build_object(
    'NombreEleccion', e.nombre_eleccion,
    'Descripcion', e.descripcion,
    'FechaEleccion', e.fecha_eleccion,
    'Estado', e.estado,
    'TipoEleccion', e.tipo_eleccion,
    'MetodoEscanos', e.metodo_escanos,
    'FechaCreacion', e.fecha_creacion
  );
$$;

create or replace function public.elecciones_paginacion(
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_busqueda text default null,
  p_fecha_inicio timestamp default null,
  p_fecha_fin timestamp default null,
  p_tipo_eleccion public.tipo_eleccion default null
)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('elecciones.view');

  with f as (
    select e.*
    from public.eleccion e
    where (nullif(trim(p_busqueda), '') is null
           or e.nombre_eleccion ilike '%' || trim(p_busqueda) || '%'
           or e.descripcion ilike '%' || trim(p_busqueda) || '%')
      and (p_fecha_inicio is null or e.fecha_eleccion >= p_fecha_inicio)
      and (p_fecha_fin is null or e.fecha_eleccion <= p_fecha_fin)
      and (p_tipo_eleccion is null or e.tipo_eleccion = p_tipo_eleccion)
  )
  select
    (select count(*) from f),
    (select jsonb_agg(privado.eleccion_json(x::public.eleccion) order by x.fecha_eleccion)
     from (
       select * from f
       order by f.fecha_eleccion
       offset privado.offset_pagina(p_pagina, p_page_size)
       limit greatest(coalesce(p_page_size, 7), 1)
     ) x)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.elecciones_listar()
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_permiso('elecciones.view');
  return coalesce((
    select jsonb_agg(privado.eleccion_json(e) order by e.fecha_eleccion desc)
    from public.eleccion e
  ), '[]'::jsonb);
end;
$$;

create or replace function public.elecciones_obtener(p_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_eleccion public.eleccion;
begin
  perform privado.exigir_permiso('elecciones.view');
  select * into v_eleccion from public.eleccion where id_eleccion = p_id;
  if not found then
    perform privado.error('Eleccion no existe');
  end if;
  return privado.eleccion_json(v_eleccion);
end;
$$;

create or replace function public.elecciones_crear(p_body jsonb)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_eleccion public.eleccion;
begin
  perform privado.exigir_permiso('elecciones.create');

  if nullif(trim(p_body ->> 'nombreEleccion'), '') is null then
    perform privado.error('El nombre de la elección es obligatorio.');
  end if;
  if nullif(p_body ->> 'tipoEleccion', '') is null then
    perform privado.error('El tipo de elección es obligatorio.');
  end if;
  if privado.a_timestamp(p_body ->> 'fechaEleccion') is null then
    perform privado.error('La fecha de la elección es obligatoria.');
  end if;

  insert into public.eleccion (
    nombre_eleccion, tipo_eleccion, descripcion, fecha_eleccion, estado,
    metodo_escanos, fecha_creacion
  )
  values (
    p_body ->> 'nombreEleccion',
    (p_body ->> 'tipoEleccion')::public.tipo_eleccion,
    p_body ->> 'descripcion',
    privado.a_timestamp(p_body ->> 'fechaEleccion'),
    'Planificada',
    coalesce(nullif(p_body ->> 'metodoEscanos', '')::public.metodo_escanos, 'DHondt'),
    privado.ahora()
  )
  returning * into v_eleccion;

  perform privado.registrar_bitacora('INSERT', 'Eleccion', v_eleccion.id_eleccion::text,
    format('Elección creada ''%s''', v_eleccion.nombre_eleccion), v_eleccion.id_eleccion,
    null, privado.eleccion_auditoria(v_eleccion));

  return privado.eleccion_json(v_eleccion);
end;
$$;

create or replace function public.elecciones_actualizar(p_id uuid, p_body jsonb)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_eleccion public.eleccion;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('elecciones.edit');

  select * into v_eleccion from public.eleccion where id_eleccion = p_id for update;
  if not found then
    perform privado.error('La elección no existe.');
  end if;
  if nullif(trim(p_body ->> 'nombreEleccion'), '') is null then
    perform privado.error('El nombre de la elección es obligatorio.');
  end if;

  v_anteriores := privado.eleccion_auditoria(v_eleccion);

  v_eleccion.nombre_eleccion := p_body ->> 'nombreEleccion';
  v_eleccion.descripcion := p_body ->> 'descripcion';
  v_eleccion.fecha_eleccion := coalesce(privado.a_timestamp(p_body ->> 'fechaEleccion'), v_eleccion.fecha_eleccion);
  v_eleccion.estado := coalesce(nullif(p_body ->> 'estado', '')::public.estado_eleccion, v_eleccion.estado);
  v_eleccion.tipo_eleccion := coalesce(nullif(p_body ->> 'tipoEleccion', '')::public.tipo_eleccion, v_eleccion.tipo_eleccion);
  v_eleccion.metodo_escanos := coalesce(nullif(p_body ->> 'metodoEscanos', '')::public.metodo_escanos, v_eleccion.metodo_escanos);

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.eleccion_auditoria(v_eleccion));

  if v_cambios.nuevos is not null then
    update public.eleccion
    set nombre_eleccion = v_eleccion.nombre_eleccion,
        descripcion = v_eleccion.descripcion,
        fecha_eleccion = v_eleccion.fecha_eleccion,
        estado = v_eleccion.estado,
        tipo_eleccion = v_eleccion.tipo_eleccion,
        metodo_escanos = v_eleccion.metodo_escanos,
        fecha_modificacion = privado.ahora()
    where id_eleccion = p_id;

    perform privado.registrar_bitacora('UPDATE', 'Eleccion', p_id::text,
      format('Elección modificada ''%s''', v_eleccion.nombre_eleccion), p_id,
      v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return 'Elección actualizada correctamente';
end;
$$;

-- -----------------------------------------------------------------------------
-- Listas electorales
-- -----------------------------------------------------------------------------
create or replace function privado.lista_json(l public.lista_electoral)
returns jsonb
language sql
immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'idListaElectoral', l.id_lista_electoral,
    'numeroLista', l.numero_lista,
    'nombreLista', l.nombre_lista,
    'siglas', l.siglas,
    'jurisdiccion', l.jurisdiccion,
    'activo', l.activo
  );
$$;

create or replace function privado.lista_auditoria(l public.lista_electoral)
returns json
language sql
immutable
set search_path = ''
as $$
  select json_build_object(
    'NombreLista', l.nombre_lista,
    'NumeroLista', l.numero_lista,
    'Siglas', l.siglas,
    'Jurisdiccion', l.jurisdiccion,
    'Activo', l.activo
  );
$$;

create or replace function privado.lista_validar(p_body jsonb, p_id_excluir uuid)
returns void
language plpgsql
stable
set search_path = ''
as $$
declare
  v_numero integer := nullif(p_body ->> 'numeroLista', '')::integer;
begin
  if nullif(trim(p_body ->> 'nombreLista'), '') is null then
    perform privado.error('El nombre de la lista electoral es obligatorio.');
  end if;
  if coalesce(v_numero, 0) <= 0 then
    perform privado.error('El número de la lista debe ser mayor a 0.');
  end if;
  if nullif(p_body ->> 'jurisdiccion', '') is null then
    perform privado.error('La jurisdicción es obligatoria.');
  end if;
  if exists (
    select 1 from public.lista_electoral
    where numero_lista = v_numero
      and (p_id_excluir is null or id_lista_electoral <> p_id_excluir)
  ) then
    perform privado.error('El número de lista electoral ya existe');
  end if;
end;
$$;

create or replace function public.listas_paginacion(
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_buscar text default null,
  p_jurisdiccion public.jurisdiccion default null
)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('listas.view');

  with f as (
    select l.*
    from public.lista_electoral l
    where (nullif(trim(p_buscar), '') is null
           or l.nombre_lista ilike '%' || trim(p_buscar) || '%'
           or l.siglas ilike '%' || trim(p_buscar) || '%')
      and (p_jurisdiccion is null or l.jurisdiccion = p_jurisdiccion)
  )
  select
    (select count(*) from f),
    (select jsonb_agg(privado.lista_json(x::public.lista_electoral) order by x.numero_lista)
     from (
       select * from f
       order by f.numero_lista
       offset privado.offset_pagina(p_pagina, p_page_size)
       limit greatest(coalesce(p_page_size, 7), 1)
     ) x)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.listas_obtener(p_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_lista public.lista_electoral;
begin
  perform privado.exigir_permiso('listas.view');
  select * into v_lista from public.lista_electoral where id_lista_electoral = p_id;
  if not found then
    perform privado.error('No existe la lista electoral.');
  end if;
  return privado.lista_json(v_lista);
end;
$$;

create or replace function public.listas_crear(p_body jsonb)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_lista public.lista_electoral;
begin
  perform privado.exigir_permiso('listas.create');
  perform privado.lista_validar(p_body, null);

  insert into public.lista_electoral (numero_lista, nombre_lista, siglas, jurisdiccion, activo)
  values (
    (p_body ->> 'numeroLista')::integer,
    p_body ->> 'nombreLista',
    p_body ->> 'siglas',
    (p_body ->> 'jurisdiccion')::public.jurisdiccion,
    true
  )
  returning * into v_lista;

  perform privado.registrar_bitacora('INSERT', 'ListaElectoral', v_lista.id_lista_electoral::text,
    format('Lista electoral creada ''%s - %s''.', v_lista.nombre_lista, v_lista.numero_lista), null,
    null, privado.lista_auditoria(v_lista));

  return privado.lista_json(v_lista);
end;
$$;

create or replace function public.listas_actualizar(p_id uuid, p_body jsonb)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_lista public.lista_electoral;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('listas.edit');

  select * into v_lista from public.lista_electoral where id_lista_electoral = p_id for update;
  if not found then
    perform privado.error('La lista electoral no existe.');
  end if;
  perform privado.lista_validar(p_body, p_id);

  v_anteriores := privado.lista_auditoria(v_lista);
  v_lista.nombre_lista := p_body ->> 'nombreLista';
  v_lista.numero_lista := (p_body ->> 'numeroLista')::integer;
  v_lista.siglas := p_body ->> 'siglas';
  v_lista.jurisdiccion := (p_body ->> 'jurisdiccion')::public.jurisdiccion;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.lista_auditoria(v_lista));

  if v_cambios.nuevos is not null then
    update public.lista_electoral
    set nombre_lista = v_lista.nombre_lista,
        numero_lista = v_lista.numero_lista,
        siglas = v_lista.siglas,
        jurisdiccion = v_lista.jurisdiccion
    where id_lista_electoral = p_id;

    perform privado.registrar_bitacora('UPDATE', 'ListaElectoral', p_id::text,
      format('Lista electoral modificada ''%s - %s''.', v_lista.nombre_lista, v_lista.numero_lista), null,
      v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return 'Lista electoral actualizada correctamente';
end;
$$;

create or replace function public.listas_cambiar_estado(p_id uuid)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_lista public.lista_electoral;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('listas.edit');

  select * into v_lista from public.lista_electoral where id_lista_electoral = p_id for update;
  if not found then
    perform privado.error('La lista electoral no existe.');
  end if;

  v_anteriores := privado.lista_auditoria(v_lista);
  v_lista.activo := not v_lista.activo;
  update public.lista_electoral set activo = v_lista.activo where id_lista_electoral = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.lista_auditoria(v_lista));
  perform privado.registrar_bitacora('UPDATE', 'ListaElectoral', p_id::text,
    format('Lista electoral %s - %s modificado estado a ''%s''', v_lista.nombre_lista, v_lista.numero_lista,
      case when v_lista.activo then 'Activo' else 'Inactivo' end), null,
    v_cambios.anteriores, v_cambios.nuevos);

  return case when v_lista.activo
    then 'Lista electoral activada correctamente.'
    else 'Lista electoral suspendida correctamente.' end;
end;
$$;

-- -----------------------------------------------------------------------------
-- Roles
-- -----------------------------------------------------------------------------
create or replace function privado.permisos_validos()
returns text[]
language sql
immutable
set search_path = ''
as $$
  select array[
    'elecciones.view', 'elecciones.create', 'elecciones.edit',
    'listas.view', 'listas.create', 'listas.edit',
    'candidatos.view', 'candidatos.create', 'candidatos.edit',
    'mesas.view', 'mesas.create', 'mesas.edit',
    'actas.view', 'actas.registrar', 'actas.editar', 'actas.validar',
    'resultados.view',
    'usuarios.view', 'usuarios.create', 'usuarios.edit',
    'roles.view', 'roles.create', 'roles.edit',
    'bitacora.view'
  ];
$$;

-- Texto JSON compacto, igual al que guardaba System.Text.Json en PermisosRol.
create or replace function privado.permisos_texto(p_permisos jsonb)
returns text
language sql
immutable
set search_path = ''
as $$
  select '[' || coalesce(string_agg(to_json(p.valor)::text, ',' order by p.ord), '') || ']'
  from jsonb_array_elements_text(coalesce(p_permisos, '[]'::jsonb)) with ordinality as p(valor, ord);
$$;

create or replace function privado.rol_json(r public.adm_rol)
returns jsonb
language sql
immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'idRol', r.id_rol,
    'nombreRol', r.nombre_rol,
    'descripcionRol', r.descripcion_rol,
    'permisos', r.permisos_rol,
    'activo', r.activo
  );
$$;

create or replace function privado.rol_auditoria(r public.adm_rol)
returns json
language sql
immutable
set search_path = ''
as $$
  select json_build_object(
    'NombreRol', r.nombre_rol,
    'DescripcionRol', r.descripcion_rol,
    'PermisosRol', privado.permisos_texto(r.permisos_rol),
    'Activo', r.activo
  );
$$;

create or replace function privado.rol_validar(p_body jsonb, p_id_excluir uuid)
returns jsonb
language plpgsql
stable
set search_path = ''
as $$
declare
  v_permisos jsonb := coalesce(p_body -> 'permisos', '[]'::jsonb);
  v_invalidos text;
begin
  if nullif(trim(p_body ->> 'nombreRol'), '') is null then
    perform privado.error('El nombre del rol es obligatorio.');
  end if;
  if exists (
    select 1 from public.adm_rol
    where lower(nombre_rol) = lower(trim(p_body ->> 'nombreRol'))
      and (p_id_excluir is null or id_rol <> p_id_excluir)
  ) then
    perform privado.error('El nombre del rol ya existe.');
  end if;
  if jsonb_typeof(v_permisos) <> 'array' then
    perform privado.error('Los permisos del rol no son válidos.');
  end if;

  select string_agg(p, ', ')
  into v_invalidos
  from jsonb_array_elements_text(v_permisos) p
  where lower(p) <> all (select lower(x) from unnest(privado.permisos_validos()) x);

  if v_invalidos is not null then
    perform privado.error('Los siguientes permisos no son válidos: ' || v_invalidos);
  end if;

  return v_permisos;
end;
$$;

create or replace function public.roles_paginacion(
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_buscar text default null
)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('roles.view');

  with f as (
    select r.*
    from public.adm_rol r
    where nullif(trim(p_buscar), '') is null or r.nombre_rol ilike '%' || trim(p_buscar) || '%'
  )
  select
    (select count(*) from f),
    (select jsonb_agg(privado.rol_json(x::public.adm_rol) order by x.nombre_rol)
     from (
       select * from f
       order by f.nombre_rol
       offset privado.offset_pagina(p_pagina, p_page_size)
       limit greatest(coalesce(p_page_size, 7), 1)
     ) x)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.roles_obtener(p_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_rol public.adm_rol;
begin
  perform privado.exigir_permiso('roles.view');
  select * into v_rol from public.adm_rol where id_rol = p_id;
  if not found then
    perform privado.error('El rol no existe.');
  end if;
  return privado.rol_json(v_rol);
end;
$$;

create or replace function public.roles_crear(p_body jsonb)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_rol public.adm_rol;
  v_permisos jsonb;
begin
  perform privado.exigir_permiso('roles.create');
  v_permisos := privado.rol_validar(p_body, null);

  insert into public.adm_rol (nombre_rol, descripcion_rol, permisos_rol, activo)
  values (trim(p_body ->> 'nombreRol'), nullif(trim(p_body ->> 'descripcionRol'), ''), v_permisos, true)
  returning * into v_rol;

  perform privado.registrar_bitacora('INSERT', 'AdmRol', v_rol.id_rol::text,
    format('Rol creado ''%s''.', v_rol.nombre_rol), null, null, privado.rol_auditoria(v_rol));

  return privado.rol_json(v_rol);
end;
$$;

create or replace function public.roles_actualizar(p_id uuid, p_body jsonb)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_rol public.adm_rol;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('roles.edit');

  select * into v_rol from public.adm_rol where id_rol = p_id for update;
  if not found then
    perform privado.error('El rol no existe.');
  end if;

  v_anteriores := privado.rol_auditoria(v_rol);
  v_rol.permisos_rol := privado.rol_validar(p_body, p_id);
  v_rol.nombre_rol := trim(p_body ->> 'nombreRol');
  v_rol.descripcion_rol := nullif(trim(p_body ->> 'descripcionRol'), '');

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.rol_auditoria(v_rol));

  if v_cambios.nuevos is not null then
    update public.adm_rol
    set nombre_rol = v_rol.nombre_rol,
        descripcion_rol = v_rol.descripcion_rol,
        permisos_rol = v_rol.permisos_rol
    where id_rol = p_id;

    perform privado.registrar_bitacora('UPDATE', 'AdmRol', p_id::text,
      format('Rol modificado ''%s''', v_rol.nombre_rol), null, v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return 'Rol actualizado correctamente';
end;
$$;

create or replace function public.roles_cambiar_estado(p_id uuid)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_rol public.adm_rol;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('roles.edit');

  select * into v_rol from public.adm_rol where id_rol = p_id for update;
  if not found then
    perform privado.error('El rol no existe.');
  end if;

  v_anteriores := privado.rol_auditoria(v_rol);
  v_rol.activo := not v_rol.activo;
  update public.adm_rol set activo = v_rol.activo where id_rol = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.rol_auditoria(v_rol));
  perform privado.registrar_bitacora('UPDATE', 'AdmRol', p_id::text,
    format('Rol modificado %s el estado a ''%s''', v_rol.nombre_rol,
      case when v_rol.activo then 'Activo' else 'Inactivo' end), null,
    v_cambios.anteriores, v_cambios.nuevos);

  return case when v_rol.activo then 'Rol activado correctamente.' else 'Rol desactivado correctamente.' end;
end;
$$;

-- -----------------------------------------------------------------------------
-- Usuarios
-- La creación y el cambio de correo pasan por la Edge Function "usuarios-admin",
-- que crea/actualiza el usuario en Supabase Auth y luego llama a estas RPC.
-- -----------------------------------------------------------------------------
create or replace function privado.usuario_json(p_id uuid)
returns jsonb
language sql
stable
set search_path = ''
as $$
  select jsonb_build_object(
    'idUsuario', u.id_usuario,
    'nombreUsuario', u.nombre_usuario,
    'emailUsuario', u.email_usuario,
    'rolId', u.rol_id,
    'nombreRol', r.nombre_rol,
    'estado', u.estado,
    'fechaCreacion', u.fecha_creacion
  )
  from public.adm_usuario u
  join public.adm_rol r on r.id_rol = u.rol_id
  where u.id_usuario = p_id;
$$;

create or replace function privado.usuario_auditoria(u public.adm_usuario, p_nombre_rol text)
returns json
language sql
immutable
set search_path = ''
as $$
  select json_build_object(
    'NombreUsuario', u.nombre_usuario,
    'EmailUsuario', u.email_usuario,
    'Estado', u.estado,
    'FechaCreacion', u.fecha_creacion,
    'Rol', p_nombre_rol
  );
$$;

-- Validaciones de UsuarioService.CrearAsync / ActualizarAsync. Devuelve el nombre del rol.
create or replace function privado.usuario_validar(p_body jsonb, p_id_excluir uuid)
returns text
language plpgsql
stable
set search_path = ''
as $$
declare
  v_rol public.adm_rol;
begin
  if nullif(trim(p_body ->> 'nombreUsuario'), '') is null then
    perform privado.error('El nombre de usuario es obligatorio.');
  end if;
  if nullif(trim(p_body ->> 'emailUsuario'), '') is null then
    perform privado.error('El email es obligatorio.');
  end if;
  if exists (
    select 1 from public.adm_usuario
    where lower(nombre_usuario) = lower(trim(p_body ->> 'nombreUsuario'))
      and (p_id_excluir is null or id_usuario <> p_id_excluir)
  ) then
    perform privado.error('El nombre de usuario ingresado ya existe.');
  end if;
  if exists (
    select 1 from public.adm_usuario
    where lower(email_usuario) = lower(trim(p_body ->> 'emailUsuario'))
      and (p_id_excluir is null or id_usuario <> p_id_excluir)
  ) then
    perform privado.error('El email ingresado ya existe.');
  end if;

  select * into v_rol from public.adm_rol where id_rol = nullif(p_body ->> 'rolId', '')::uuid;
  if not found then
    perform privado.error('El rol especificado no existe.');
  end if;
  if not v_rol.activo then
    perform privado.error('El rol seleccionado está inactivo.');
  end if;

  return v_rol.nombre_rol;
end;
$$;

create or replace function public.usuarios_paginacion(
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_buscar text default null
)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('usuarios.view');

  with f as (
    select u.id_usuario, u.nombre_usuario
    from public.adm_usuario u
    where nullif(trim(p_buscar), '') is null
       or u.nombre_usuario ilike '%' || trim(p_buscar) || '%'
       or u.email_usuario ilike '%' || trim(p_buscar) || '%'
  )
  select
    (select count(*) from f),
    (select jsonb_agg(privado.usuario_json(x.id_usuario) order by x.nombre_usuario)
     from (
       select * from f
       order by f.nombre_usuario
       offset privado.offset_pagina(p_pagina, p_page_size)
       limit greatest(coalesce(p_page_size, 7), 1)
     ) x)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.usuarios_obtener(p_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_usuario jsonb;
begin
  perform privado.exigir_permiso('usuarios.view');
  v_usuario := privado.usuario_json(p_id);
  if v_usuario is null then
    perform privado.error('El usuario no existe.');
  end if;
  return v_usuario;
end;
$$;

-- Permisos del usuario autenticado (UsuariosController.ObtenerPermisos).
create or replace function public.usuarios_permisos()
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception using message = 'Se requiere autenticación, por favor inicie sesión.', errcode = '28000';
  end if;
  return coalesce(privado.permisos_usuario_actual(), '[]'::jsonb);
end;
$$;

-- Usado por la Edge Function antes de crear el usuario en Supabase Auth.
create or replace function public.usuarios_validar(p_body jsonb, p_id uuid default null)
returns void
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_permiso(case when p_id is null then 'usuarios.create' else 'usuarios.edit' end);
  if p_id is not null and not exists (select 1 from public.adm_usuario where id_usuario = p_id) then
    perform privado.error('El usuario no existe.');
  end if;
  perform privado.usuario_validar(p_body, p_id);
end;
$$;

-- p_id es el id ya creado en auth.users por la Edge Function.
create or replace function public.usuarios_crear(p_id uuid, p_body jsonb)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_usuario public.adm_usuario;
  v_nombre_rol text;
begin
  perform privado.exigir_permiso('usuarios.create');
  v_nombre_rol := privado.usuario_validar(p_body, null);

  insert into public.adm_usuario (id_usuario, nombre_usuario, email_usuario, rol_id, estado, fecha_creacion)
  values (
    p_id,
    trim(p_body ->> 'nombreUsuario'),
    lower(trim(p_body ->> 'emailUsuario')),
    (p_body ->> 'rolId')::uuid,
    true,
    privado.ahora()
  )
  returning * into v_usuario;

  perform privado.registrar_bitacora('INSERT', 'AdmUsuario', p_id::text,
    format('Usuario creado ''%s''.', v_usuario.nombre_usuario), null,
    null, privado.usuario_auditoria(v_usuario, v_nombre_rol));

  return privado.usuario_json(p_id);
end;
$$;

-- Devuelve el correo anterior para que la Edge Function sincronice Supabase Auth.
create or replace function public.usuarios_actualizar(p_id uuid, p_body jsonb)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_usuario public.adm_usuario;
  v_email_anterior text;
  v_rol_anterior text;
  v_rol_nuevo text;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('usuarios.edit');

  select * into v_usuario from public.adm_usuario where id_usuario = p_id for update;
  if not found then
    perform privado.error('El usuario no existe.');
  end if;
  v_rol_nuevo := privado.usuario_validar(p_body, p_id);

  select nombre_rol into v_rol_anterior from public.adm_rol where id_rol = v_usuario.rol_id;
  v_anteriores := privado.usuario_auditoria(v_usuario, v_rol_anterior);
  v_email_anterior := v_usuario.email_usuario;

  v_usuario.nombre_usuario := trim(p_body ->> 'nombreUsuario');
  v_usuario.email_usuario := lower(trim(p_body ->> 'emailUsuario'));
  v_usuario.rol_id := (p_body ->> 'rolId')::uuid;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.usuario_auditoria(v_usuario, v_rol_nuevo));

  if v_cambios.nuevos is not null then
    update public.adm_usuario
    set nombre_usuario = v_usuario.nombre_usuario,
        email_usuario = v_usuario.email_usuario,
        rol_id = v_usuario.rol_id
    where id_usuario = p_id;

    perform privado.registrar_bitacora('UPDATE', 'AdmUsuario', p_id::text,
      format('Usuario modificado ''%s''', v_usuario.nombre_usuario), null,
      v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return jsonb_build_object(
    'emailAnterior', v_email_anterior,
    'emailNuevo', v_usuario.email_usuario,
    'mensaje', 'Usuario actualizado correctamente'
  );
end;
$$;

create or replace function public.usuarios_cambiar_estado(p_id uuid)
returns text
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_usuario public.adm_usuario;
  v_nombre_rol text;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('usuarios.edit');

  select * into v_usuario from public.adm_usuario where id_usuario = p_id for update;
  if not found then
    perform privado.error('El usuario no existe.');
  end if;

  select nombre_rol into v_nombre_rol from public.adm_rol where id_rol = v_usuario.rol_id;
  v_anteriores := privado.usuario_auditoria(v_usuario, v_nombre_rol);
  v_usuario.estado := not v_usuario.estado;
  update public.adm_usuario set estado = v_usuario.estado where id_usuario = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.usuario_auditoria(v_usuario, v_nombre_rol));
  perform privado.registrar_bitacora('UPDATE', 'AdmUsuario', p_id::text,
    format('Usuario modificado %s el estado a ''%s''', v_usuario.nombre_usuario,
      case when v_usuario.estado then 'Activo' else 'Inactivo' end), null,
    v_cambios.anteriores, v_cambios.nuevos);

  return case when v_usuario.estado
    then 'Usuario activado correctamente.'
    else 'Usuario desactivado correctamente.' end;
end;
$$;

-- -----------------------------------------------------------------------------
-- Bitácora
-- -----------------------------------------------------------------------------
create or replace function privado.bitacora_json(b public.adm_bitacora)
returns jsonb
language sql
stable
set search_path = ''
as $$
  select jsonb_build_object(
    'idBitacora', b.id_bitacora,
    'fechaRegistro', b.fecha_registro,
    'usuario', coalesce(u.nombre_usuario, ''),
    'correoUsuario', coalesce(u.email_usuario, ''),
    'accion', b.accion,
    'tabla', b.tabla,
    'descripcion', coalesce(b.descripcion, ''),
    'valoresAnteriores', b.valores_anteriores::text,
    'valoresNuevos', b.valores_nuevos::text
  )
  from (select 1) d
  left join public.adm_usuario u on u.id_usuario = b.usuario_id;
$$;

create or replace function public.bitacora_paginacion(
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_fecha_desde date default null,
  p_fecha_hasta date default null,
  p_usuario_id uuid default null,
  p_accion text default null,
  p_tabla text default null
)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('bitacora.view');

  with f as (
    select b.*
    from public.adm_bitacora b
    where (p_fecha_desde is null or b.fecha_registro >= p_fecha_desde)
      and (p_fecha_hasta is null or b.fecha_registro < p_fecha_hasta + 1)
      and (p_usuario_id is null or b.usuario_id = p_usuario_id)
      and (nullif(trim(p_accion), '') is null or b.accion = p_accion)
      and (nullif(trim(p_tabla), '') is null or b.tabla = p_tabla)
  )
  select
    (select count(*) from f),
    (select jsonb_agg(privado.bitacora_json(x::public.adm_bitacora) order by x.fecha_registro desc)
     from (
       select * from f
       order by f.fecha_registro desc
       offset privado.offset_pagina(p_pagina, p_page_size)
       limit greatest(coalesce(p_page_size, 7), 1)
     ) x)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.bitacora_obtener(p_bitacora_id uuid)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_permiso('bitacora.view');
  return (select privado.bitacora_json(b) from public.adm_bitacora b where b.id_bitacora = p_bitacora_id);
end;
$$;

create or replace function public.bitacora_por_registro(p_tabla text, p_id_registro text)
returns jsonb
language plpgsql
stable
security definer
set search_path = ''
as $$
begin
  perform privado.exigir_permiso('bitacora.view');
  return coalesce((
    select jsonb_agg(privado.bitacora_json(b) order by b.fecha_registro desc)
    from public.adm_bitacora b
    where b.tabla = p_tabla and b.id_registro = p_id_registro
  ), '[]'::jsonb);
end;
$$;
