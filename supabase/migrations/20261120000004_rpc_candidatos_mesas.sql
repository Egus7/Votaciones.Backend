-- =============================================================================
-- RPC: Candidatos (con alianzas electorales) y Mesas electorales.
-- Replica CandidatoService y MesaElectoralService del backend .NET.
-- =============================================================================

-- ¿Existe un acta de la dignidad que use la lista en el ámbito? (ActaRepository.ExistePorListaYAmbitoAsync)
create or replace function privado.acta_existe_lista_ambito(
  p_eleccion_id uuid, p_lista_id uuid, p_tipo public.tipo_candidato,
  p_provincia_id uuid, p_canton_id uuid, p_parroquia_id uuid)
returns boolean language sql stable set search_path = ''
as $$
  select exists (
    select 1
    from public.acta_eleccion a
    join public.mesa_electoral m on m.id_mesa_electoral = a.mesa_electoral_id
    join public.zona z on z.id_zona = m.zona_id
    join public.parroquia p on p.id_parroquia = z.parroquia_id
    join public.canton c on c.id_canton = p.canton_id
    where a.tipo_candidato = p_tipo
      and m.eleccion_id = p_eleccion_id
      and exists (select 1 from public.acta_detalle d where d.acta_id = a.id_acta and d.lista_electoral_id = p_lista_id)
      and (p_provincia_id is null or c.provincia_id = p_provincia_id)
      and (p_canton_id is null or p.canton_id = p_canton_id)
      and (p_parroquia_id is null or z.parroquia_id = p_parroquia_id));
$$;

-- -----------------------------------------------------------------------------
-- Candidatos: serialización
-- -----------------------------------------------------------------------------
create or replace function privado.candidato_dto(p_id uuid)
returns jsonb language sql stable set search_path = ''
as $$
  select jsonb_build_object(
    'idCandidato', c.id_candidato,
    'eleccion', e.nombre_eleccion,
    'descripcionEleccion', coalesce(e.descripcion, ''),
    'nombreCandidato', c.nombre_candidato,
    'tipoCandidato', c.tipo_candidato,
    'orden', c.orden,
    'alianzaElectoral', a.nombre_alianza,
    'provincia', pr.nombre_provincia,
    'canton', ca.nombre_canton,
    'parroquia', pa.nombre_parroquia,
    'activo', c.activo,
    'listasCandidato', coalesce((
      select jsonb_agg(jsonb_build_object(
        'listaElectoralId', l.id_lista_electoral,
        'numeroLista', l.numero_lista,
        'nombreLista', l.nombre_lista,
        'esPrincipal', lc.lista_principal
      ) order by lc.lista_principal desc, l.numero_lista)
      from public.lista_candidato lc
      join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
      where lc.candidato_id = c.id_candidato), '[]'::jsonb))
  from public.candidato c
  join public.eleccion e on e.id_eleccion = c.eleccion_id
  left join public.alianza_electoral a on a.id_alianza_electoral = c.alianza_electoral_id
  left join public.provincia pr on pr.id_provincia = c.provincia_id
  left join public.canton ca on ca.id_canton = c.canton_id
  left join public.parroquia pa on pa.id_parroquia = c.parroquia_id
  where c.id_candidato = p_id;
$$;

-- Entidad Candidato tal como la devolvía el POST.
create or replace function privado.candidato_entidad_json(p_id uuid)
returns jsonb language sql stable set search_path = ''
as $$
  select jsonb_build_object(
    'idCandidato', c.id_candidato,
    'eleccionId', c.eleccion_id,
    'nombreCandidato', c.nombre_candidato,
    'tipoCandidato', c.tipo_candidato,
    'orden', c.orden,
    'provinciaId', c.provincia_id,
    'cantonId', c.canton_id,
    'parroquiaId', c.parroquia_id,
    'alianzaElectoralId', c.alianza_electoral_id,
    'activo', c.activo,
    'listaCandidatos', coalesce((
      select jsonb_agg(jsonb_build_object(
        'idListaCandidato', lc.id_lista_candidato,
        'candidatoId', lc.candidato_id,
        'listaElectoralId', lc.lista_electoral_id,
        'listaPrincipal', lc.lista_principal))
      from public.lista_candidato lc where lc.candidato_id = c.id_candidato), '[]'::jsonb))
  from public.candidato c
  where c.id_candidato = p_id;
$$;

-- Valores de bitácora (CandidatoService.ObtenerValoresAuditoria).
create or replace function privado.candidato_auditoria(p_id uuid)
returns json language sql stable set search_path = ''
as $$
  select json_object_agg(t.k, t.v order by t.o)
  from public.candidato c
  join public.eleccion e on e.id_eleccion = c.eleccion_id
  left join public.alianza_electoral a on a.id_alianza_electoral = c.alianza_electoral_id
  left join public.provincia pr on pr.id_provincia = c.provincia_id
  left join public.canton ca on ca.id_canton = c.canton_id
  left join public.parroquia pa on pa.id_parroquia = c.parroquia_id
  cross join lateral (values
    (1, 'NombreCandidato', to_json(c.nombre_candidato), true),
    (2, 'TipoCandidato', to_json(c.tipo_candidato), true),
    (3, 'Orden', to_json(c.orden), true),
    (4, 'Activo', to_json(c.activo), true),
    (5, 'Eleccion', to_json(e.nombre_eleccion), true),
    (6, 'AlianzaElectoral', to_json(a.nombre_alianza), a.id_alianza_electoral is not null),
    (7, 'Provincia', to_json(pr.nombre_provincia), c.provincia_id is not null),
    (8, 'Canton', to_json(ca.nombre_canton), c.canton_id is not null),
    (9, 'Parroquia', to_json(pa.nombre_parroquia), c.parroquia_id is not null),
    (10, 'Listas Candidatos', coalesce((
      select json_agg(json_build_object('Lista', l.nombre_lista, 'NumeroLista', l.numero_lista, 'EsPrincipal', lc.lista_principal)
        order by lc.lista_principal desc, l.numero_lista)
      from public.lista_candidato lc
      join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
      where lc.candidato_id = c.id_candidato), '[]'::json), true)
  ) as t(o, k, v, incluir)
  where c.id_candidato = p_id and t.incluir;
$$;

-- -----------------------------------------------------------------------------
-- Candidatos: validaciones
-- -----------------------------------------------------------------------------
create or replace function privado.es_concejal(p_tipo public.tipo_candidato)
returns boolean language sql immutable set search_path = ''
as $$ select p_tipo in ('ConcejalUrbano', 'ConcejalRural'); $$;

-- ValidarDatosCandidato: devuelve la elección.
create or replace function privado.candidato_validar_datos(p_body jsonb)
returns public.eleccion language plpgsql stable set search_path = ''
as $$
declare
  v_eleccion public.eleccion;
  v_tipo public.tipo_candidato;
  v_listas jsonb := coalesce(p_body -> 'listaCandidatos', '[]'::jsonb);
  v_principales integer;
begin
  if nullif(trim(p_body ->> 'nombreCandidato'), '') is null then
    perform privado.error('El nombre del candidato es obligatorio.');
  end if;
  if nullif(p_body ->> 'eleccionId', '') is null then
    perform privado.error('La elección es obligatoria.');
  end if;
  select * into v_eleccion from public.eleccion where id_eleccion = (p_body ->> 'eleccionId')::uuid;
  if not found then perform privado.error('La elección ingresada no existe.'); end if;
  if v_eleccion.estado = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;
  if nullif(p_body ->> 'tipoCandidato', '') is null
     or not (p_body ->> 'tipoCandidato') = any (enum_range(null::public.tipo_candidato)::text[]) then
    perform privado.error('El tipo de candidato es inválido.');
  end if;
  v_tipo := (p_body ->> 'tipoCandidato')::public.tipo_candidato;

  if jsonb_typeof(v_listas) <> 'array' or jsonb_array_length(v_listas) = 0 then
    perform privado.error('Debe seleccionar al menos una lista electoral.');
  end if;
  select count(*) into v_principales from jsonb_array_elements(v_listas) x where coalesce((x ->> 'listaPrincipal')::boolean, false);
  if v_principales = 0 then perform privado.error('Debe seleccionar una lista principal.'); end if;
  if v_principales > 1 then perform privado.error('Solo puede haber una lista principal para el candidato.'); end if;
  if (select count(*) from jsonb_array_elements(v_listas)) <> (select count(distinct x ->> 'listaElectoralId') from jsonb_array_elements(v_listas) x) then
    perform privado.error('No se puede asociar la misma lista electoral más de una vez.');
  end if;
  if privado.es_concejal(v_tipo) and coalesce(nullif(p_body ->> 'orden', '')::integer, 0) <= 0 then
    perform privado.error('El orden del candidato es obligatorio.');
  end if;
  return v_eleccion;
end;
$$;

-- ValidarListasAsync: todas las listas existen y están activas.
create or replace function privado.candidato_validar_listas(p_body jsonb)
returns void language plpgsql stable set search_path = ''
as $$
declare
  v_item jsonb;
  v_lista public.lista_electoral;
begin
  for v_item in select * from jsonb_array_elements(p_body -> 'listaCandidatos') loop
    if nullif(v_item ->> 'listaElectoralId', '') is null then
      perform privado.error('La lista electoral es obligatoria.');
    end if;
    select * into v_lista from public.lista_electoral where id_lista_electoral = (v_item ->> 'listaElectoralId')::uuid;
    if not found then perform privado.error('La lista electoral ingresada no existe.'); end if;
    if not v_lista.activo then
      perform privado.error(format('La lista electoral ''%s'' está suspendida.', v_lista.nombre_lista));
    end if;
  end loop;
end;
$$;

-- Verifica que no exista otro candidato del mismo tipo (u orden, para concejales) en cada lista.
create or replace function privado.candidato_validar_unicidad(p_body jsonb, p_excluir_id uuid)
returns void language plpgsql stable set search_path = ''
as $$
declare
  v_tipo public.tipo_candidato := (p_body ->> 'tipoCandidato')::public.tipo_candidato;
  v_orden integer := nullif(p_body ->> 'orden', '')::integer;
  v_eleccion uuid := (p_body ->> 'eleccionId')::uuid;
  v_provincia uuid := nullif(p_body ->> 'provinciaId', '')::uuid;
  v_canton uuid := nullif(p_body ->> 'cantonId', '')::uuid;
  v_parroquia uuid := nullif(p_body ->> 'parroquiaId', '')::uuid;
  v_lista record;
begin
  for v_lista in
    select l.id_lista_electoral, l.nombre_lista
    from jsonb_array_elements(p_body -> 'listaCandidatos') x
    join public.lista_electoral l on l.id_lista_electoral = (x ->> 'listaElectoralId')::uuid
  loop
    if exists (
      select 1 from public.candidato c
      where c.eleccion_id = v_eleccion and c.tipo_candidato = v_tipo and c.activo
        and (v_provincia is null or c.provincia_id = v_provincia)
        and (v_canton is null or c.canton_id = v_canton)
        and (v_parroquia is null or c.parroquia_id = v_parroquia)
        and (p_excluir_id is null or c.id_candidato <> p_excluir_id)
        and (not privado.es_concejal(v_tipo) or c.orden = v_orden)
        and exists (select 1 from public.lista_candidato lc
                    where lc.candidato_id = c.id_candidato and lc.lista_electoral_id = v_lista.id_lista_electoral)
    ) then
      if privado.es_concejal(v_tipo) then
        perform privado.error(format('El orden ''%s'' ya está asignado a otro candidato para la lista electoral ''%s'' en la zona seleccionada.',
          v_orden, v_lista.nombre_lista));
      else
        perform privado.error(format('Ya existe un candidato de tipo ''%s'' registrado para la lista electoral ''%s'' en la zona seleccionada.',
          v_tipo, v_lista.nombre_lista));
      end if;
    end if;
  end loop;
end;
$$;

-- Alianzas con la misma elección, ámbito y exactamente las mismas listas.
create or replace function privado.alianzas_disponibles(
  p_eleccion_id uuid, p_provincia_id uuid, p_canton_id uuid, p_parroquia_id uuid, p_listas uuid[])
returns table (id_alianza_electoral uuid, nombre_alianza text)
language sql stable set search_path = ''
as $$
  select a.id_alianza_electoral, a.nombre_alianza::text
  from public.alianza_electoral a
  where coalesce(cardinality(p_listas), 0) > 0
    and a.eleccion_id = p_eleccion_id
    and a.provincia_id is not distinct from p_provincia_id
    and a.canton_id is not distinct from p_canton_id
    and a.parroquia_id is not distinct from p_parroquia_id
    and (select count(*) from public.alianza_detalle d where d.alianza_electoral_id = a.id_alianza_electoral)
        = (select count(distinct x) from unnest(p_listas) x)
    and not exists (select 1 from public.alianza_detalle d
                    where d.alianza_electoral_id = a.id_alianza_electoral
                      and not d.lista_electoral_id = any (p_listas));
$$;

-- ResolverAlianzaAsync: reutiliza la alianza existente o crea una nueva. Devuelve su id (null si hay una sola lista).
create or replace function privado.resolver_alianza(p_body jsonb, p_nombre_alianza text)
returns uuid language plpgsql set search_path = ''
as $$
declare
  v_eleccion uuid := (p_body ->> 'eleccionId')::uuid;
  v_provincia uuid := nullif(p_body ->> 'provinciaId', '')::uuid;
  v_canton uuid := nullif(p_body ->> 'cantonId', '')::uuid;
  v_parroquia uuid := nullif(p_body ->> 'parroquiaId', '')::uuid;
  v_listas uuid[];
  v_alianza uuid;
  v_nombre text;
begin
  select array_agg(distinct (x ->> 'listaElectoralId')::uuid) into v_listas
  from jsonb_array_elements(p_body -> 'listaCandidatos') x;

  if coalesce(cardinality(v_listas), 0) <= 1 then
    return null;
  end if;

  select d.id_alianza_electoral into v_alianza
  from privado.alianzas_disponibles(v_eleccion, v_provincia, v_canton, v_parroquia, v_listas) d
  limit 1;
  if v_alianza is not null then
    return v_alianza;
  end if;

  v_nombre := nullif(trim(p_nombre_alianza), '');
  if v_nombre is null then
    perform privado.error('No existe una alianza para las listas seleccionadas. Debe ingresar el nombre de la nueva alianza.');
  end if;

  insert into public.alianza_electoral (nombre_alianza, eleccion_id, provincia_id, canton_id, parroquia_id)
  values (v_nombre, v_eleccion, v_provincia, v_canton, v_parroquia)
  returning id_alianza_electoral into v_alianza;

  insert into public.alianza_detalle (alianza_electoral_id, lista_electoral_id, lista_alianza_principal)
  select v_alianza, (x ->> 'listaElectoralId')::uuid, coalesce((x ->> 'listaPrincipal')::boolean, false)
  from jsonb_array_elements(p_body -> 'listaCandidatos') x;

  perform privado.registrar_bitacora('INSERT', 'AlianzaElectoral', v_alianza::text,
    format('Alianza electoral creada ''%s''', v_nombre), v_eleccion, null,
    json_build_object('NombreAlianza', v_nombre));

  return v_alianza;
end;
$$;

create or replace function privado.candidato_insertar(p_body jsonb, p_alianza_id uuid)
returns uuid language plpgsql set search_path = ''
as $$
declare
  v_id uuid;
begin
  insert into public.candidato (eleccion_id, nombre_candidato, tipo_candidato, orden, provincia_id, canton_id,
    parroquia_id, alianza_electoral_id, activo)
  values (
    (p_body ->> 'eleccionId')::uuid,
    trim(p_body ->> 'nombreCandidato'),
    (p_body ->> 'tipoCandidato')::public.tipo_candidato,
    nullif(p_body ->> 'orden', '')::integer,
    nullif(p_body ->> 'provinciaId', '')::uuid,
    nullif(p_body ->> 'cantonId', '')::uuid,
    nullif(p_body ->> 'parroquiaId', '')::uuid,
    p_alianza_id,
    true)
  returning id_candidato into v_id;

  insert into public.lista_candidato (candidato_id, lista_electoral_id, lista_principal)
  select v_id, (x ->> 'listaElectoralId')::uuid, coalesce((x ->> 'listaPrincipal')::boolean, false)
  from jsonb_array_elements(p_body -> 'listaCandidatos') x;

  return v_id;
end;
$$;

-- -----------------------------------------------------------------------------
-- Candidatos: RPC públicas
-- -----------------------------------------------------------------------------
create or replace function public.candidatos_paginacion(
  p_eleccion_id uuid,
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_busqueda text default null,
  p_tipo_candidato public.tipo_candidato default null,
  p_lista_electoral_id uuid default null)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('candidatos.view');

  with f as (
    select c.*,
      coalesce((select l.numero_lista from public.lista_candidato lc
                join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
                where lc.candidato_id = c.id_candidato and lc.lista_principal limit 1), 0) as numero_principal
    from public.candidato c
    where c.eleccion_id = p_eleccion_id
      and (nullif(trim(p_busqueda), '') is null or c.nombre_candidato ilike '%' || trim(p_busqueda) || '%')
      and (p_tipo_candidato is null or c.tipo_candidato = p_tipo_candidato)
      and (p_lista_electoral_id is null or exists (
            select 1 from public.lista_candidato lc
            where lc.candidato_id = c.id_candidato and lc.lista_electoral_id = p_lista_electoral_id))
  ),
  pagina as (
    select f.id_candidato,
      row_number() over (order by f.numero_principal, f.tipo_candidato, f.orden, f.provincia_id, f.canton_id,
        f.parroquia_id, f.nombre_candidato, f.id_candidato) as pos
    from f
    order by pos
    offset privado.offset_pagina(p_pagina, p_page_size)
    limit greatest(coalesce(p_page_size, 7), 1)
  )
  select (select count(*) from f),
         (select jsonb_agg(privado.candidato_dto(p.id_candidato) order by p.pos) from pagina p)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.candidatos_obtener(p_id uuid)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_candidato jsonb;
begin
  perform privado.exigir_permiso('candidatos.view');
  v_candidato := privado.candidato_dto(p_id);
  if v_candidato is null then perform privado.error('Candidato no existe'); end if;
  return v_candidato;
end;
$$;

create or replace function public.candidatos_alianzas_disponibles(
  p_eleccion_id uuid,
  p_provincia_id uuid default null,
  p_canton_id uuid default null,
  p_parroquia_id uuid default null,
  p_listas uuid[] default null)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('candidatos.view');
  return coalesce((
    select jsonb_agg(jsonb_build_object('idAlianzaElectoral', d.id_alianza_electoral, 'nombreAlianza', d.nombre_alianza))
    from privado.alianzas_disponibles(p_eleccion_id, p_provincia_id, p_canton_id, p_parroquia_id, p_listas) d
  ), '[]'::jsonb);
end;
$$;

create or replace function public.candidatos_crear(p_body jsonb, p_nombre_alianza text default null)
returns jsonb language plpgsql security definer set search_path = ''
as $$
declare
  v_eleccion public.eleccion;
  v_id uuid;
begin
  perform privado.exigir_permiso('candidatos.create');
  v_eleccion := privado.candidato_validar_datos(p_body);
  perform privado.candidato_validar_listas(p_body);
  perform privado.candidato_validar_unicidad(p_body, null);

  v_id := privado.candidato_insertar(p_body, privado.resolver_alianza(p_body, p_nombre_alianza));

  perform privado.registrar_bitacora('INSERT', 'Candidato', v_id::text,
    format('Candidato creado ''%s''', trim(p_body ->> 'nombreCandidato')), v_eleccion.id_eleccion,
    null, privado.candidato_auditoria(v_id));

  return privado.candidato_entidad_json(v_id);
end;
$$;

create or replace function public.candidatos_crear_varios(p_body jsonb, p_nombre_alianza text default null)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_primero jsonb;
  v_item jsonb;
  v_eleccion public.eleccion;
  v_cantidad integer;
  v_alianza uuid;
  v_ids uuid[] := '{}';
  v_lista_principal text;
  v_valores json;
begin
  perform privado.exigir_permiso('candidatos.create');

  if p_body is null or jsonb_typeof(p_body) <> 'array' or jsonb_array_length(p_body) = 0 then
    perform privado.error('Debe ingresar al menos un candidato.');
  end if;
  v_primero := p_body -> 0;
  v_cantidad := jsonb_array_length(p_body);

  if exists (select 1 from jsonb_array_elements(p_body) x
             where coalesce(x ->> 'tipoCandidato', '') not in ('ConcejalUrbano', 'ConcejalRural')) then
    perform privado.error('El registro múltiple solo está permitido para concejales.');
  end if;
  if exists (select 1 from jsonb_array_elements(p_body) x where x ->> 'eleccionId' is distinct from v_primero ->> 'eleccionId') then
    perform privado.error('Todos los candidatos deben pertenecer a la misma elección.');
  end if;
  if exists (select 1 from jsonb_array_elements(p_body) x where x ->> 'tipoCandidato' is distinct from v_primero ->> 'tipoCandidato') then
    perform privado.error('Todos los candidatos deben ser del mismo tipo.');
  end if;

  for v_item in select * from jsonb_array_elements(p_body) loop
    v_eleccion := privado.candidato_validar_datos(v_item);
    perform privado.candidato_validar_listas(v_item);
  end loop;

  if exists (select 1 from jsonb_array_elements(p_body) x
             where nullif(x ->> 'provinciaId', '') is distinct from nullif(v_primero ->> 'provinciaId', '')
                or nullif(x ->> 'cantonId', '') is distinct from nullif(v_primero ->> 'cantonId', '')
                or nullif(x ->> 'parroquiaId', '') is distinct from nullif(v_primero ->> 'parroquiaId', '')) then
    perform privado.error('Todos los candidatos deben pertenecer a la misma zona.');
  end if;
  if exists (
    select 1 from jsonb_array_elements(p_body) x
    where (select array_agg((l ->> 'listaElectoralId')::uuid order by (l ->> 'listaElectoralId')::uuid)
           from jsonb_array_elements(x -> 'listaCandidatos') l)
       is distinct from
          (select array_agg((l ->> 'listaElectoralId')::uuid order by (l ->> 'listaElectoralId')::uuid)
           from jsonb_array_elements(v_primero -> 'listaCandidatos') l)) then
    perform privado.error('Todos los candidatos deben pertenecer a las mismas listas electorales.');
  end if;
  if exists (
    select 1 from jsonb_array_elements(p_body) x
    where (select l ->> 'listaElectoralId' from jsonb_array_elements(x -> 'listaCandidatos') l
           where coalesce((l ->> 'listaPrincipal')::boolean, false) limit 1)
       is distinct from
          (select l ->> 'listaElectoralId' from jsonb_array_elements(v_primero -> 'listaCandidatos') l
           where coalesce((l ->> 'listaPrincipal')::boolean, false) limit 1)) then
    perform privado.error('Todos los candidatos deben tener la misma lista principal.');
  end if;
  if (select count(*) from jsonb_array_elements(p_body)) <> (select count(distinct x ->> 'orden') from jsonb_array_elements(p_body) x) then
    perform privado.error('No puede existir más de un candidato con el mismo orden.');
  end if;

  for v_item in select * from jsonb_array_elements(p_body) loop
    if exists (
      select 1
      from jsonb_array_elements(v_item -> 'listaCandidatos') l
      join public.candidato c on c.eleccion_id = (v_item ->> 'eleccionId')::uuid
      where c.tipo_candidato = (v_item ->> 'tipoCandidato')::public.tipo_candidato
        and c.orden = (v_item ->> 'orden')::integer and c.activo
        and (nullif(v_item ->> 'provinciaId', '') is null or c.provincia_id = (v_item ->> 'provinciaId')::uuid)
        and (nullif(v_item ->> 'cantonId', '') is null or c.canton_id = (v_item ->> 'cantonId')::uuid)
        and (nullif(v_item ->> 'parroquiaId', '') is null or c.parroquia_id = (v_item ->> 'parroquiaId')::uuid)
        and exists (select 1 from public.lista_candidato lc
                    where lc.candidato_id = c.id_candidato and lc.lista_electoral_id = (l ->> 'listaElectoralId')::uuid)
    ) then
      perform privado.error(format('El orden ''%s'' ya está asignado para la lista electoral seleccionada.', v_item ->> 'orden'));
    end if;
  end loop;

  v_alianza := privado.resolver_alianza(v_primero, p_nombre_alianza);
  for v_item in select * from jsonb_array_elements(p_body) loop
    v_ids := v_ids || privado.candidato_insertar(v_item, v_alianza);
  end loop;

  select l.nombre_lista into v_lista_principal
  from jsonb_array_elements(v_primero -> 'listaCandidatos') x
  join public.lista_electoral l on l.id_lista_electoral = (x ->> 'listaElectoralId')::uuid
  where coalesce((x ->> 'listaPrincipal')::boolean, false)
  limit 1;

  select json_object_agg(t.k, t.v order by t.o) into v_valores
  from (values
    (1, 'CantidadCandidatos', to_json(v_cantidad), true),
    (2, 'TipoCandidato', to_json(v_primero ->> 'tipoCandidato'), true),
    (3, 'Eleccion', to_json(v_eleccion.nombre_eleccion), true),
    (4, 'AlianzaElectoral', (select to_json(a.nombre_alianza) from public.alianza_electoral a where a.id_alianza_electoral = v_alianza), v_alianza is not null),
    (5, 'Provincia', (select to_json(p.nombre_provincia) from public.provincia p where p.id_provincia = nullif(v_primero ->> 'provinciaId', '')::uuid), nullif(v_primero ->> 'provinciaId', '') is not null),
    (6, 'Canton', (select to_json(c.nombre_canton) from public.canton c where c.id_canton = nullif(v_primero ->> 'cantonId', '')::uuid), nullif(v_primero ->> 'cantonId', '') is not null),
    (7, 'Parroquia', (select to_json(p.nombre_parroquia) from public.parroquia p where p.id_parroquia = nullif(v_primero ->> 'parroquiaId', '')::uuid), nullif(v_primero ->> 'parroquiaId', '') is not null),
    (8, 'Candidatos', (
      select json_agg(json_build_object(
        'NombreCandidato', c.nombre_candidato,
        'Orden', c.orden,
        'Listas', (select json_agg(json_build_object('NumeroLista', l.numero_lista, 'Lista', l.nombre_lista, 'EsPrincipal', lc.lista_principal)
                          order by lc.lista_principal desc, l.numero_lista)
                   from public.lista_candidato lc
                   join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
                   where lc.candidato_id = c.id_candidato)) order by c.orden)
      from public.candidato c where c.id_candidato = any (v_ids)), true)
  ) as t(o, k, v, incluir)
  where t.incluir;

  perform privado.registrar_bitacora('INSERT', 'Candidato', null,
    format('Se registraron %s candidatos de tipo ''%s'' para la lista electoral ''%s''.',
      v_cantidad, v_primero ->> 'tipoCandidato', coalesce(v_lista_principal, '')),
    v_eleccion.id_eleccion, null, v_valores);

  return format('%s candidatos creados correctamente', v_cantidad);
end;
$$;

create or replace function public.candidatos_actualizar(p_id uuid, p_body jsonb, p_nombre_alianza text default null)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_existente public.candidato;
  v_eleccion public.eleccion;
  v_tipo_nuevo public.tipo_candidato;
  v_principal_existente uuid;
  v_principal_nueva uuid;
  v_tiene_actas boolean;
  v_alianza uuid;
  v_lista record;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('candidatos.edit');

  select * into v_existente from public.candidato where id_candidato = p_id for update;
  if not found then perform privado.error('El candidato no existe.'); end if;

  v_eleccion := privado.candidato_validar_datos(p_body);
  if v_existente.eleccion_id <> v_eleccion.id_eleccion then
    perform privado.error('No se puede cambiar la elección del candidato.');
  end if;
  perform privado.candidato_validar_listas(p_body);
  v_tipo_nuevo := (p_body ->> 'tipoCandidato')::public.tipo_candidato;

  select lista_electoral_id into v_principal_existente
  from public.lista_candidato where candidato_id = p_id and lista_principal limit 1;
  select (x ->> 'listaElectoralId')::uuid into v_principal_nueva
  from jsonb_array_elements(p_body -> 'listaCandidatos') x
  where coalesce((x ->> 'listaPrincipal')::boolean, false) limit 1;

  if privado.es_concejal(v_tipo_nuevo) then
    -- Para concejales el acta registra la lista, no el candidato
    v_tiene_actas := v_principal_existente is not null and privado.acta_existe_lista_ambito(
      v_existente.eleccion_id, v_principal_existente, v_existente.tipo_candidato,
      v_existente.provincia_id, v_existente.canton_id, v_existente.parroquia_id);
  else
    v_tiene_actas := exists (
      select 1 from public.acta_eleccion a
      join public.acta_detalle d on d.acta_id = a.id_acta
      where a.eleccion_id = v_existente.eleccion_id and d.candidato_id = p_id);
  end if;

  if v_tiene_actas then
    if v_existente.tipo_candidato <> v_tipo_nuevo then
      perform privado.error('No se puede modificar el tipo de candidato porque el candidato tiene actas registradas.');
    end if;
    if v_existente.provincia_id is distinct from nullif(p_body ->> 'provinciaId', '')::uuid
       or v_existente.canton_id is distinct from nullif(p_body ->> 'cantonId', '')::uuid
       or v_existente.parroquia_id is distinct from nullif(p_body ->> 'parroquiaId', '')::uuid then
      perform privado.error('No se puede modificar la zona del candidato porque ya tiene actas registradas.');
    end if;
    if v_principal_existente is not null and v_principal_nueva is not null and v_principal_existente <> v_principal_nueva then
      perform privado.error('No se puede cambiar la lista principal del candidato porque ya tiene actas registradas.');
    end if;
  end if;

  -- No permitir eliminar una lista que ya fue utilizada en un acta
  for v_lista in
    select lc.lista_electoral_id, l.nombre_lista
    from public.lista_candidato lc
    join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
    where lc.candidato_id = p_id
      and not exists (select 1 from jsonb_array_elements(p_body -> 'listaCandidatos') x
                      where (x ->> 'listaElectoralId')::uuid = lc.lista_electoral_id)
  loop
    if privado.acta_existe_lista_ambito(v_existente.eleccion_id, v_lista.lista_electoral_id, v_existente.tipo_candidato,
         v_existente.provincia_id, v_existente.canton_id, v_existente.parroquia_id) then
      perform privado.error(format('No se puede eliminar la lista electoral ''%s'' porque ya fue utilizada en un acta.', v_lista.nombre_lista));
    end if;
  end loop;

  perform privado.candidato_validar_unicidad(p_body, p_id);

  v_alianza := privado.resolver_alianza(p_body, p_nombre_alianza);
  v_anteriores := privado.candidato_auditoria(p_id);

  update public.candidato
  set nombre_candidato = trim(p_body ->> 'nombreCandidato'),
      tipo_candidato = v_tipo_nuevo,
      orden = nullif(p_body ->> 'orden', '')::integer,
      provincia_id = nullif(p_body ->> 'provinciaId', '')::uuid,
      canton_id = nullif(p_body ->> 'cantonId', '')::uuid,
      parroquia_id = nullif(p_body ->> 'parroquiaId', '')::uuid,
      activo = coalesce((p_body ->> 'activo')::boolean, activo),
      alianza_electoral_id = v_alianza
  where id_candidato = p_id;

  delete from public.lista_candidato where candidato_id = p_id;
  insert into public.lista_candidato (candidato_id, lista_electoral_id, lista_principal)
  select p_id, (x ->> 'listaElectoralId')::uuid, coalesce((x ->> 'listaPrincipal')::boolean, false)
  from jsonb_array_elements(p_body -> 'listaCandidatos') x;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.candidato_auditoria(p_id));
  if v_cambios.nuevos is not null then
    perform privado.registrar_bitacora('UPDATE', 'Candidato', p_id::text,
      format('Candidato modificado ''%s''', trim(p_body ->> 'nombreCandidato')), v_existente.eleccion_id,
      v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return 'Candidato actualizado correctamente';
end;
$$;

create or replace function public.candidatos_cambiar_estado(p_id uuid)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_candidato public.candidato;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('candidatos.edit');

  select * into v_candidato from public.candidato where id_candidato = p_id for update;
  if not found then perform privado.error('El candidato no existe.'); end if;

  v_anteriores := json_build_object('NombreCandidato', v_candidato.nombre_candidato, 'TipoCandidato', v_candidato.tipo_candidato,
    'Orden', v_candidato.orden, 'Activo', v_candidato.activo);
  v_candidato.activo := not v_candidato.activo;
  update public.candidato set activo = v_candidato.activo where id_candidato = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores,
    json_build_object('NombreCandidato', v_candidato.nombre_candidato, 'TipoCandidato', v_candidato.tipo_candidato,
      'Orden', v_candidato.orden, 'Activo', v_candidato.activo));
  perform privado.registrar_bitacora('UPDATE', 'Candidato', p_id::text,
    format('Candidato %s modificado estado a ''%s''', v_candidato.nombre_candidato,
      case when v_candidato.activo then 'Activo' else 'Inactivo' end),
    v_candidato.eleccion_id, v_cambios.anteriores, v_cambios.nuevos);

  return case when v_candidato.activo then 'Candidato activado correctamente.' else 'Candidato desactivado correctamente.' end;
end;
$$;

-- -----------------------------------------------------------------------------
-- Mesas electorales
-- -----------------------------------------------------------------------------
create or replace function privado.mesa_dto(p_id uuid)
returns jsonb language sql stable set search_path = ''
as $$
  select jsonb_build_object(
    'idMesaElectoral', m.id_mesa_electoral,
    'eleccion', e.nombre_eleccion,
    'descripcionEleccion', coalesce(e.descripcion, ''),
    'codigoMesa', m.codigo_mesa,
    'tipoMesa', m.tipo_mesa,
    'descripcion', coalesce(m.descripcion, ''),
    'zona', z.nombre_zona,
    'parroquia', p.nombre_parroquia,
    'canton', c.nombre_canton,
    'provincia', pr.nombre_provincia,
    'activa', m.activa)
  from public.mesa_electoral m
  join public.eleccion e on e.id_eleccion = m.eleccion_id
  join public.zona z on z.id_zona = m.zona_id
  join public.parroquia p on p.id_parroquia = z.parroquia_id
  join public.canton c on c.id_canton = p.canton_id
  join public.provincia pr on pr.id_provincia = c.provincia_id
  where m.id_mesa_electoral = p_id;
$$;

create or replace function privado.mesa_entidad_json(m public.mesa_electoral)
returns jsonb language sql immutable set search_path = ''
as $$
  select jsonb_build_object(
    'idMesaElectoral', m.id_mesa_electoral,
    'eleccionId', m.eleccion_id,
    'zonaId', m.zona_id,
    'codigoMesa', m.codigo_mesa,
    'tipoMesa', m.tipo_mesa,
    'descripcion', m.descripcion,
    'activa', m.activa);
$$;

create or replace function privado.mesa_auditoria(m public.mesa_electoral, p_relaciones boolean default true)
returns json language sql stable set search_path = ''
as $$
  select json_object_agg(t.k, t.v order by t.o)
  from (values
    (1, 'CodigoMesa', to_json(m.codigo_mesa), true),
    (2, 'TipoMesa', to_json(m.tipo_mesa), true),
    (3, 'Descripcion', to_json(m.descripcion), true),
    (4, 'Activa', to_json(m.activa), true),
    (5, 'Eleccion', (select to_json(e.nombre_eleccion) from public.eleccion e where e.id_eleccion = m.eleccion_id), p_relaciones),
    (6, 'Zona', (select to_json(z.nombre_zona) from public.zona z where z.id_zona = m.zona_id), p_relaciones)
  ) as t(o, k, v, incluir)
  where t.incluir;
$$;

create or replace function privado.mesa_codigo(p_numero integer, p_tipo public.tipo_mesa)
returns text language sql immutable set search_path = ''
as $$
  select case when p_numero < 10 then '0' || p_numero else p_numero::text end
      || case p_tipo when 'Femenina' then 'F' else 'M' end;
$$;

create or replace function privado.mesa_descripcion(p_numero integer, p_tipo public.tipo_mesa)
returns text language sql immutable set search_path = ''
as $$ select 'Mesa ' || p_numero || ' ' || p_tipo; $$;

-- Siguiente número de mesa por elección + zona + tipo. Bloquea la combinación para evitar duplicados concurrentes.
create or replace function privado.mesa_siguiente_numero(p_eleccion_id uuid, p_zona_id uuid, p_tipo public.tipo_mesa)
returns integer language plpgsql volatile set search_path = ''
as $$
declare
  v_sufijo text := case p_tipo when 'Femenina' then 'F' else 'M' end;
begin
  perform pg_advisory_xact_lock(hashtext(p_eleccion_id::text || p_zona_id::text || p_tipo::text));
  return coalesce((
    select max(nullif(regexp_replace(left(m.codigo_mesa, length(m.codigo_mesa) - 1), '\D', '', 'g'), '')::integer)
    from public.mesa_electoral m
    where m.eleccion_id = p_eleccion_id and m.zona_id = p_zona_id and right(m.codigo_mesa, 1) = v_sufijo
  ), 0) + 1;
end;
$$;

-- Validación común: elección existente y no cerrada; zona existente.
create or replace function privado.mesa_validar_eleccion_zona(p_eleccion_id uuid, p_zona_id uuid)
returns void language plpgsql stable set search_path = ''
as $$
declare
  v_estado public.estado_eleccion;
begin
  if p_eleccion_id is null then perform privado.error('La elección es obligatoria.'); end if;
  if p_zona_id is null then perform privado.error('La zona es obligatoria.'); end if;
  select estado into v_estado from public.eleccion where id_eleccion = p_eleccion_id;
  if not found then perform privado.error('La elección ingresada no existe.'); end if;
  if v_estado = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;
  if not exists (select 1 from public.zona where id_zona = p_zona_id) then
    perform privado.error('La zona ingresada no existe.');
  end if;
end;
$$;

create or replace function public.mesas_paginacion(
  p_eleccion_id uuid,
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_busqueda text default null,
  p_tipo_mesa public.tipo_mesa default null)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('mesas.view');

  with f as (
    select m.id_mesa_electoral, z.nombre_zona, m.codigo_mesa, m.tipo_mesa
    from public.mesa_electoral m
    join public.zona z on z.id_zona = m.zona_id
    where m.eleccion_id = p_eleccion_id
      and (nullif(trim(p_busqueda), '') is null
           or m.codigo_mesa ilike '%' || trim(p_busqueda) || '%'
           or m.descripcion ilike '%' || trim(p_busqueda) || '%')
      and (p_tipo_mesa is null or m.tipo_mesa = p_tipo_mesa)
  ),
  pagina as (
    select f.id_mesa_electoral, row_number() over (order by f.nombre_zona, f.codigo_mesa, f.tipo_mesa) as pos
    from f
    order by pos
    offset privado.offset_pagina(p_pagina, p_page_size)
    limit greatest(coalesce(p_page_size, 7), 1)
  )
  select (select count(*) from f),
         (select jsonb_agg(privado.mesa_dto(p.id_mesa_electoral) order by p.pos) from pagina p)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.mesas_obtener(p_id uuid)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_mesa jsonb;
begin
  perform privado.exigir_permiso('mesas.view');
  v_mesa := privado.mesa_dto(p_id);
  if v_mesa is null then perform privado.error('Mesa electoral no existe'); end if;
  return v_mesa;
end;
$$;

create or replace function public.mesas_por_zona(p_eleccion_id uuid, p_zona_id uuid)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('mesas.view');
  return coalesce((
    select jsonb_agg(privado.mesa_dto(m.id_mesa_electoral) order by m.codigo_mesa)
    from public.mesa_electoral m
    where m.eleccion_id = p_eleccion_id and m.zona_id = p_zona_id
  ), '[]'::jsonb);
end;
$$;

create or replace function public.mesas_disponible_por_zona(p_eleccion_id uuid, p_zona_id uuid, p_tipo_candidato public.tipo_candidato)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('mesas.view');
  return coalesce((
    select jsonb_agg(privado.mesa_dto(m.id_mesa_electoral) order by m.codigo_mesa)
    from public.mesa_electoral m
    where m.eleccion_id = p_eleccion_id and m.zona_id = p_zona_id
      and not exists (select 1 from public.acta_eleccion a
                      where a.mesa_electoral_id = m.id_mesa_electoral and a.tipo_candidato = p_tipo_candidato)
  ), '[]'::jsonb);
end;
$$;

create or replace function public.mesas_codigo_mesa(p_eleccion_id uuid, p_zona_id uuid, p_tipo_mesa public.tipo_mesa)
returns jsonb language plpgsql volatile security definer set search_path = ''
as $$
declare
  v_numero integer;
begin
  perform privado.exigir_permiso('mesas.view');
  perform privado.mesa_validar_eleccion_zona(p_eleccion_id, p_zona_id);
  if p_tipo_mesa is null then perform privado.error('El tipo de mesa no es válido.'); end if;
  v_numero := privado.mesa_siguiente_numero(p_eleccion_id, p_zona_id, p_tipo_mesa);
  return jsonb_build_object(
    'codigoMesa', privado.mesa_codigo(v_numero, p_tipo_mesa),
    'descripcion', privado.mesa_descripcion(v_numero, p_tipo_mesa));
end;
$$;

create or replace function public.mesas_crear(p_body jsonb)
returns jsonb language plpgsql security definer set search_path = ''
as $$
declare
  v_eleccion_id uuid := nullif(p_body ->> 'eleccionId', '')::uuid;
  v_zona_id uuid := nullif(p_body ->> 'zonaId', '')::uuid;
  v_tipo public.tipo_mesa := nullif(p_body ->> 'tipoMesa', '')::public.tipo_mesa;
  v_numero integer;
  v_mesa public.mesa_electoral;
begin
  perform privado.exigir_permiso('mesas.create');
  perform privado.mesa_validar_eleccion_zona(v_eleccion_id, v_zona_id);
  if v_tipo is null then perform privado.error('El tipo de mesa no es válido.'); end if;

  v_numero := privado.mesa_siguiente_numero(v_eleccion_id, v_zona_id, v_tipo);
  insert into public.mesa_electoral (eleccion_id, zona_id, codigo_mesa, tipo_mesa, descripcion, activa)
  values (v_eleccion_id, v_zona_id, privado.mesa_codigo(v_numero, v_tipo), v_tipo, privado.mesa_descripcion(v_numero, v_tipo), true)
  returning * into v_mesa;

  perform privado.registrar_bitacora('INSERT', 'MesaElectoral', v_mesa.id_mesa_electoral::text,
    format('Mesa electoral creada ''%s'' - Zona: %s.', v_mesa.codigo_mesa,
      (select nombre_zona from public.zona where id_zona = v_zona_id)),
    v_eleccion_id, null, privado.mesa_auditoria(v_mesa));

  return privado.mesa_entidad_json(v_mesa);
end;
$$;

create or replace function public.mesas_crear_lote(
  p_eleccion_id uuid, p_zona_id uuid, p_cantidad_femeninas integer default 0, p_cantidad_masculinas integer default 0)
returns jsonb language plpgsql security definer set search_path = ''
as $$
declare
  v_zona text;
  v_tipo public.tipo_mesa;
  v_cantidad integer;
  v_inicio integer;
  v_resultado jsonb := '[]'::jsonb;
  v_mesas jsonb;
  v_codigo_inicial text;
  v_codigo_final text;
begin
  perform privado.exigir_permiso('mesas.create');
  if p_eleccion_id is null then perform privado.error('La elección es obligatoria.'); end if;
  if p_zona_id is null then perform privado.error('La zona es obligatoria.'); end if;
  if coalesce(p_cantidad_femeninas, 0) < 0 then perform privado.error('La cantidad de mesas femeninas no puede ser negativa.'); end if;
  if coalesce(p_cantidad_masculinas, 0) < 0 then perform privado.error('La cantidad de mesas masculinas no puede ser negativa.'); end if;
  if coalesce(p_cantidad_femeninas, 0) = 0 and coalesce(p_cantidad_masculinas, 0) = 0 then
    perform privado.error('Debe ingresar al menos una mesa.');
  end if;
  perform privado.mesa_validar_eleccion_zona(p_eleccion_id, p_zona_id);
  select nombre_zona into v_zona from public.zona where id_zona = p_zona_id;

  for v_tipo, v_cantidad in
    select * from (values ('Femenina'::public.tipo_mesa, coalesce(p_cantidad_femeninas, 0)),
                          ('Masculina'::public.tipo_mesa, coalesce(p_cantidad_masculinas, 0))) t(tipo, cantidad)
    where t.cantidad > 0
  loop
    v_inicio := privado.mesa_siguiente_numero(p_eleccion_id, p_zona_id, v_tipo);

    with nuevas as (
      insert into public.mesa_electoral (eleccion_id, zona_id, codigo_mesa, tipo_mesa, descripcion, activa)
      select p_eleccion_id, p_zona_id, privado.mesa_codigo(n, v_tipo), v_tipo, privado.mesa_descripcion(n, v_tipo), true
      from generate_series(v_inicio, v_inicio + v_cantidad - 1) n
      returning *
    )
    select jsonb_agg(privado.mesa_entidad_json(nuevas::public.mesa_electoral) order by nuevas.codigo_mesa),
           min(nuevas.codigo_mesa), max(nuevas.codigo_mesa)
    into v_mesas, v_codigo_inicial, v_codigo_final
    from nuevas;

    v_resultado := v_resultado || v_mesas;

    perform privado.registrar_bitacora('INSERT', 'MesaElectoral', null,
      format('Se crearon %s mesas %s para la zona ''%s''. Rango de códigos: %s - %s.',
        v_cantidad, case v_tipo when 'Femenina' then 'femeninas' else 'masculinas' end, v_zona, v_codigo_inicial, v_codigo_final),
      p_eleccion_id, null,
      json_build_object('Zona', v_zona, 'TipoMesa', v_tipo, 'Cantidad', v_cantidad,
        'CodigoInicial', v_codigo_inicial, 'CodigoFinal', v_codigo_final));
  end loop;

  return v_resultado;
end;
$$;

create or replace function public.mesas_actualizar(p_id uuid, p_body jsonb)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_mesa public.mesa_electoral;
  v_zona_nueva uuid := nullif(p_body ->> 'zonaId', '')::uuid;
  v_tipo_nuevo public.tipo_mesa := nullif(p_body ->> 'tipoMesa', '')::public.tipo_mesa;
  v_estado public.estado_eleccion;
  v_numero integer;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('mesas.edit');
  if p_id is null then perform privado.error('El id de la mesa es obligatorio.'); end if;
  if v_zona_nueva is null then perform privado.error('La zona es obligatoria.'); end if;
  if v_tipo_nuevo is null then perform privado.error('El tipo de mesa no es válido.'); end if;

  select * into v_mesa from public.mesa_electoral where id_mesa_electoral = p_id for update;
  if not found then perform privado.error('La mesa electoral a actualizar no existe.'); end if;
  if not v_mesa.activa then perform privado.error('La mesa electoral está cerrada y no puede ser modificada.'); end if;

  select estado into v_estado from public.eleccion where id_eleccion = v_mesa.eleccion_id;
  if not found then perform privado.error('La elección de la mesa no existe.'); end if;
  if v_estado = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;
  if not exists (select 1 from public.zona where id_zona = v_zona_nueva) then
    perform privado.error('La zona ingresada no existe.');
  end if;

  v_anteriores := privado.mesa_auditoria(v_mesa);

  if v_mesa.zona_id <> v_zona_nueva or v_mesa.tipo_mesa <> v_tipo_nuevo then
    v_numero := privado.mesa_siguiente_numero(v_mesa.eleccion_id, v_zona_nueva, v_tipo_nuevo);
    v_mesa.codigo_mesa := privado.mesa_codigo(v_numero, v_tipo_nuevo);
    v_mesa.descripcion := privado.mesa_descripcion(v_numero, v_tipo_nuevo);
  end if;
  v_mesa.zona_id := v_zona_nueva;
  v_mesa.tipo_mesa := v_tipo_nuevo;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.mesa_auditoria(v_mesa));
  if v_cambios.nuevos is not null then
    update public.mesa_electoral
    set zona_id = v_mesa.zona_id, tipo_mesa = v_mesa.tipo_mesa,
        codigo_mesa = v_mesa.codigo_mesa, descripcion = v_mesa.descripcion
    where id_mesa_electoral = p_id;

    perform privado.registrar_bitacora('UPDATE', 'MesaElectoral', p_id::text,
      format('Mesa electoral modificada ''%s''.', v_mesa.codigo_mesa), v_mesa.eleccion_id,
      v_cambios.anteriores, v_cambios.nuevos);
  end if;

  return 'Mesa electoral actualizada correctamente';
end;
$$;

create or replace function public.mesas_cambiar_estado(p_id uuid)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_mesa public.mesa_electoral;
  v_anteriores json;
  v_cambios record;
begin
  perform privado.exigir_permiso('mesas.edit');

  select * into v_mesa from public.mesa_electoral where id_mesa_electoral = p_id for update;
  if not found then perform privado.error('La mesa electoral no existe.'); end if;

  v_anteriores := privado.mesa_auditoria(v_mesa, false);
  v_mesa.activa := not v_mesa.activa;
  update public.mesa_electoral set activa = v_mesa.activa where id_mesa_electoral = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.mesa_auditoria(v_mesa, false));
  perform privado.registrar_bitacora('UPDATE', 'MesaElectoral', p_id::text,
    format('Mesa electoral %s modificado estado a ''%s''', v_mesa.codigo_mesa,
      case when v_mesa.activa then 'Activo' else 'Inactivo' end),
    v_mesa.eleccion_id, v_cambios.anteriores, v_cambios.nuevos);

  return case when v_mesa.activa then 'Mesa electoral activada correctamente.' else 'Mesa electoral desactivada correctamente.' end;
end;
$$;
