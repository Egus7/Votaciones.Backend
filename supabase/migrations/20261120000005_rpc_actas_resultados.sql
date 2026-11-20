-- =============================================================================
-- RPC: Actas, Resultados y notificación en tiempo real.
-- Replica ActaService, ResultadoService y ResultadosHub (SignalR -> Supabase Realtime).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Realtime: reemplaza ResultadosNotificacion (SignalR).
-- Canal privado "resultados:{eleccionId sin guiones}:{TipoCandidato}", evento "ResultadosActualizados".
-- -----------------------------------------------------------------------------
create or replace function privado.notificar_resultados(p_acta_id uuid)
returns void language plpgsql security definer set search_path = ''
as $$
declare
  v record;
begin
  select a.eleccion_id, a.tipo_candidato, a.mesa_electoral_id, m.zona_id, z.parroquia_id, p.canton_id, c.provincia_id
  into v
  from public.acta_eleccion a
  join public.mesa_electoral m on m.id_mesa_electoral = a.mesa_electoral_id
  join public.zona z on z.id_zona = m.zona_id
  join public.parroquia p on p.id_parroquia = z.parroquia_id
  join public.canton c on c.id_canton = p.canton_id
  where a.id_acta = p_acta_id;

  if not found then
    return;
  end if;

  perform realtime.send(
    jsonb_build_object(
      'eleccionId', v.eleccion_id, 'tipoCandidato', v.tipo_candidato, 'mesaId', v.mesa_electoral_id,
      'zonaId', v.zona_id, 'parroquiaId', v.parroquia_id, 'cantonId', v.canton_id, 'provinciaId', v.provincia_id),
    'ResultadosActualizados',
    'resultados:' || replace(v.eleccion_id::text, '-', '') || ':' || v.tipo_candidato,
    true);
exception when others then
  -- Igual que antes: si la notificación falla no se revierte el registro del acta.
  raise warning 'No se pudo notificar la actualización de resultados: %', sqlerrm;
end;
$$;

-- Solo usuarios con permiso resultados.view pueden escuchar los canales de resultados.
drop policy if exists "resultados_escuchar" on realtime.messages;
create policy "resultados_escuchar" on realtime.messages
for select to authenticated
using (realtime.topic() like 'resultados:%' and privado.tiene_permiso('resultados.view'));

-- -----------------------------------------------------------------------------
-- Actas: serialización
-- -----------------------------------------------------------------------------

-- Candidatos/listas para registrar un acta (ActaService.ObtenerCandidatoListaPorTipoZonaAsync).
create or replace function privado.candidatos_lista_registro(
  p_eleccion_id uuid, p_tipo public.tipo_candidato, p_provincia_id uuid, p_canton_id uuid, p_parroquia_id uuid)
returns jsonb language sql stable set search_path = ''
as $$
  with base as (
    select c.id_candidato, c.nombre_candidato, l.id_lista_electoral, l.numero_lista, l.nombre_lista,
      a.nombre_alianza,
      (select jsonb_agg(l2.numero_lista order by l2.numero_lista)
       from public.alianza_detalle ad
       join public.lista_electoral l2 on l2.id_lista_electoral = ad.lista_electoral_id
       where ad.alianza_electoral_id = a.id_alianza_electoral) as numeros_alianza
    from public.candidato c
    join public.lista_candidato lc on lc.candidato_id = c.id_candidato and lc.lista_principal
    join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
    left join public.alianza_electoral a on a.id_alianza_electoral = c.alianza_electoral_id
    where c.eleccion_id = p_eleccion_id
      and c.tipo_candidato = p_tipo
      and (p_tipo = 'Presidente' or p_provincia_id is null or c.provincia_id = p_provincia_id)
      and (p_tipo in ('Presidente', 'Prefecto') or p_canton_id is null or c.canton_id = p_canton_id)
      and (p_tipo <> 'ConcejalRural' or p_parroquia_id is null or c.parroquia_id = p_parroquia_id)
  ),
  filas as (
    -- En concejales el acta registra la lista: una fila por lista.
    select distinct on (case when privado.es_concejal(p_tipo) then b.id_lista_electoral::text else b.id_candidato::text end)
      b.*
    from base b
    order by case when privado.es_concejal(p_tipo) then b.id_lista_electoral::text else b.id_candidato::text end,
      b.nombre_alianza nulls last, b.nombre_candidato
  )
  select coalesce(jsonb_agg(jsonb_build_object(
    'candidatoId', case when privado.es_concejal(p_tipo) then null else f.id_candidato end,
    'nombreCandidato', case when privado.es_concejal(p_tipo) then null else f.nombre_candidato end,
    'listaElectoralId', f.id_lista_electoral,
    'numeroLista', f.numero_lista,
    'nombreLista', f.nombre_lista,
    'nombreAlianza', f.nombre_alianza,
    'numerosListasAlianza', f.numeros_alianza,
    'votos', 0
  ) order by f.numero_lista, f.nombre_candidato), '[]'::jsonb)
  from filas f;
$$;

-- ActaDTO (AutoMapper ActaEleccion -> ActaDTO). p_completar_alianzas replica ObtenerPorIdAsync.
create or replace function privado.acta_dto(p_id uuid, p_completar_alianzas boolean default false)
returns jsonb language plpgsql stable set search_path = ''
as $$
declare
  v record;
  v_detalles jsonb;
  v_registro jsonb;
begin
  select a.*, e.nombre_eleccion, e.descripcion as descripcion_eleccion, m.codigo_mesa, m.descripcion as descripcion_mesa,
    z.nombre_zona, p.nombre_parroquia, ca.nombre_canton, pr.nombre_provincia,
    pr.id_provincia, ca.id_canton, p.id_parroquia
  into v
  from public.acta_eleccion a
  join public.eleccion e on e.id_eleccion = a.eleccion_id
  join public.mesa_electoral m on m.id_mesa_electoral = a.mesa_electoral_id
  join public.zona z on z.id_zona = m.zona_id
  join public.parroquia p on p.id_parroquia = z.parroquia_id
  join public.canton ca on ca.id_canton = p.canton_id
  join public.provincia pr on pr.id_provincia = ca.provincia_id
  where a.id_acta = p_id;

  if not found then
    return null;
  end if;

  if p_completar_alianzas and privado.es_concejal(v.tipo_candidato) then
    v_registro := privado.candidatos_lista_registro(v.eleccion_id, v.tipo_candidato, v.id_provincia, v.id_canton, v.id_parroquia);
  end if;

  select coalesce(jsonb_agg(jsonb_build_object(
      'candidatoId', d.candidato_id,
      'nombreCandidato', c.nombre_candidato,
      'listaElectoralId', d.lista_electoral_id,
      'numeroLista', l.numero_lista,
      'nombreLista', l.nombre_lista,
      'nombreAlianza', coalesce(r.value ->> 'nombreAlianza', al.nombre_alianza),
      'numerosListasAlianza', coalesce(r.value -> 'numerosListasAlianza',
        (select jsonb_agg(l2.numero_lista order by l2.numero_lista)
         from public.alianza_detalle ad
         join public.lista_electoral l2 on l2.id_lista_electoral = ad.lista_electoral_id
         where ad.alianza_electoral_id = al.id_alianza_electoral)),
      'votos', d.votos
    ) order by l.numero_lista, c.nombre_candidato), '[]'::jsonb)
  into v_detalles
  from public.acta_detalle d
  join public.lista_electoral l on l.id_lista_electoral = d.lista_electoral_id
  left join public.candidato c on c.id_candidato = d.candidato_id
  left join public.alianza_electoral al on al.id_alianza_electoral = c.alianza_electoral_id
  left join lateral (
    select x.value from jsonb_array_elements(coalesce(v_registro, '[]'::jsonb)) x
    where (x.value ->> 'listaElectoralId')::uuid = d.lista_electoral_id
    limit 1
  ) r on true
  where d.acta_id = p_id;

  return jsonb_build_object(
    'idActa', v.id_acta,
    'tipoCandidato', v.tipo_candidato,
    'fechaRegistro', v.fecha_registro,
    'eleccion', v.nombre_eleccion,
    'descripcionEleccion', coalesce(v.descripcion_eleccion, ''),
    'codigoMesa', v.codigo_mesa,
    'descripcionMesa', coalesce(v.descripcion_mesa, ''),
    'provincia', v.nombre_provincia,
    'canton', v.nombre_canton,
    'parroquia', v.nombre_parroquia,
    'zona', v.nombre_zona,
    'estado', v.estado,
    'votosBlancos', v.votos_blancos,
    'votosNulos', v.votos_nulos,
    'totalVotos', v.total_votos,
    'actasDetalle', v_detalles);
end;
$$;

-- Entidad ActaEleccion tal como la devolvía el POST.
create or replace function privado.acta_entidad_json(p_id uuid)
returns jsonb language sql stable set search_path = ''
as $$
  select jsonb_build_object(
    'idActa', a.id_acta, 'eleccionId', a.eleccion_id, 'mesaElectoralId', a.mesa_electoral_id,
    'tipoCandidato', a.tipo_candidato, 'fechaRegistro', a.fecha_registro, 'usuarioRegistroId', a.usuario_registro_id,
    'votosBlancos', a.votos_blancos, 'votosNulos', a.votos_nulos, 'totalVotos', a.total_votos, 'estado', a.estado,
    'fechaModificacion', a.fecha_modificacion, 'usuarioModificacionId', a.usuario_modificacion_id,
    'actaDetalles', coalesce((
      select jsonb_agg(jsonb_build_object('idActaDetalle', d.id_acta_detalle, 'actaId', d.acta_id,
        'listaElectoralId', d.lista_electoral_id, 'candidatoId', d.candidato_id, 'votos', d.votos))
      from public.acta_detalle d where d.acta_id = a.id_acta), '[]'::jsonb))
  from public.acta_eleccion a
  where a.id_acta = p_id;
$$;

-- Valores de bitácora del acta (ActaService.ObtenerValoresAuditoria).
create or replace function privado.acta_auditoria(p_id uuid)
returns json language sql stable set search_path = ''
as $$
  select json_build_object(
    'TipoCandidato', a.tipo_candidato,
    'FechaRegistro', a.fecha_registro,
    'VotosBlancos', a.votos_blancos,
    'VotosNulos', a.votos_nulos,
    'TotalVotos', a.total_votos,
    'Estado', a.estado,
    'Eleccion', e.nombre_eleccion,
    'Mesa', m.codigo_mesa,
    'Zona', z.nombre_zona,
    'Parroquia', p.nombre_parroquia,
    'Canton', ca.nombre_canton,
    'Provincia', pr.nombre_provincia,
    'Detalles', coalesce((
      select json_agg(case when privado.es_concejal(a.tipo_candidato)
          then json_build_object('Lista', l.nombre_lista, 'NumeroLista', l.numero_lista, 'Votos', d.votos)
          else json_build_object('Candidato', c.nombre_candidato, 'Lista', l.nombre_lista, 'NumeroLista', l.numero_lista, 'Votos', d.votos)
        end order by l.numero_lista, c.nombre_candidato)
      from public.acta_detalle d
      join public.lista_electoral l on l.id_lista_electoral = d.lista_electoral_id
      left join public.candidato c on c.id_candidato = d.candidato_id
      where d.acta_id = a.id_acta), '[]'::json))
  from public.acta_eleccion a
  join public.eleccion e on e.id_eleccion = a.eleccion_id
  join public.mesa_electoral m on m.id_mesa_electoral = a.mesa_electoral_id
  join public.zona z on z.id_zona = m.zona_id
  join public.parroquia p on p.id_parroquia = z.parroquia_id
  join public.canton ca on ca.id_canton = p.canton_id
  join public.provincia pr on pr.id_provincia = ca.provincia_id
  where a.id_acta = p_id;
$$;

-- -----------------------------------------------------------------------------
-- Actas: RPC públicas
-- -----------------------------------------------------------------------------
create or replace function public.actas_paginacion(
  p_eleccion_id uuid,
  p_pagina integer default 1,
  p_page_size integer default 7,
  p_provincia_id uuid default null,
  p_canton_id uuid default null,
  p_parroquia_id uuid default null,
  p_zona_id uuid default null,
  p_mesa_id uuid default null,
  p_estado_acta public.estado_acta default null,
  p_tipo_candidato public.tipo_candidato default null)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_total integer;
  v_items jsonb;
begin
  perform privado.exigir_permiso('actas.view');

  with f as (
    select a.id_acta, z.nombre_zona, m.codigo_mesa, a.tipo_candidato
    from public.acta_eleccion a
    join public.mesa_electoral m on m.id_mesa_electoral = a.mesa_electoral_id
    join public.zona z on z.id_zona = m.zona_id
    join public.parroquia p on p.id_parroquia = z.parroquia_id
    join public.canton c on c.id_canton = p.canton_id
    where a.eleccion_id = p_eleccion_id
      and (p_provincia_id is null or c.provincia_id = p_provincia_id)
      and (p_canton_id is null or p.canton_id = p_canton_id)
      and (p_parroquia_id is null or z.parroquia_id = p_parroquia_id)
      and (p_zona_id is null or m.zona_id = p_zona_id)
      and (p_mesa_id is null or a.mesa_electoral_id = p_mesa_id)
      and (p_estado_acta is null or a.estado = p_estado_acta)
      and (p_tipo_candidato is null or a.tipo_candidato = p_tipo_candidato)
  ),
  pagina as (
    select f.id_acta, row_number() over (order by f.nombre_zona, f.codigo_mesa, f.tipo_candidato) as pos
    from f
    order by pos
    offset privado.offset_pagina(p_pagina, p_page_size)
    limit greatest(coalesce(p_page_size, 7), 1)
  )
  select (select count(*) from f),
         (select jsonb_agg(privado.acta_dto(p.id_acta) order by p.pos) from pagina p)
  into v_total, v_items;

  return privado.paginacion(v_items, p_pagina, p_page_size, v_total);
end;
$$;

create or replace function public.actas_obtener(p_id uuid)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_acta jsonb;
begin
  perform privado.exigir_permiso('actas.view');
  v_acta := privado.acta_dto(p_id, true);
  if v_acta is null then perform privado.error('El acta no existe.'); end if;
  return v_acta;
end;
$$;

create or replace function public.actas_por_mesa(p_mesa_id uuid)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  v_acta uuid;
begin
  perform privado.exigir_permiso('actas.view');
  select id_acta into v_acta from public.acta_eleccion where mesa_electoral_id = p_mesa_id limit 1;
  if v_acta is null then perform privado.error('La mesa no tiene un acta registrada.'); end if;
  return privado.acta_dto(v_acta);
end;
$$;

create or replace function public.actas_candidatos_listas_registro(
  p_eleccion_id uuid,
  p_tipo_candidato public.tipo_candidato,
  p_provincia_id uuid default null,
  p_canton_id uuid default null,
  p_parroquia_id uuid default null)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('actas.view');
  return privado.candidatos_lista_registro(p_eleccion_id, p_tipo_candidato, p_provincia_id, p_canton_id, p_parroquia_id);
end;
$$;

create or replace function public.actas_crear(p_body jsonb)
returns jsonb language plpgsql security definer set search_path = ''
as $$
declare
  v_usuario uuid;
  v_eleccion public.eleccion;
  v_mesa public.mesa_electoral;
  v_zona text;
  v_tipo public.tipo_candidato;
  v_concejal boolean;
  v_blancos integer := coalesce(nullif(p_body ->> 'votosBlancos', '')::integer, 0);
  v_nulos integer := coalesce(nullif(p_body ->> 'votosNulos', '')::integer, 0);
  v_detalles jsonb := coalesce(p_body -> 'actaDetalles', '[]'::jsonb);
  v_item jsonb;
  v_lista public.lista_electoral;
  v_candidato public.candidato;
  v_lista_principal uuid;
  v_filas jsonb := '[]'::jsonb;
  v_acta uuid;
begin
  v_usuario := privado.exigir_permiso('actas.registrar');

  if nullif(p_body ->> 'eleccionId', '') is null then perform privado.error('La elección es obligatoria.'); end if;
  select * into v_eleccion from public.eleccion where id_eleccion = (p_body ->> 'eleccionId')::uuid;
  if not found then perform privado.error('La elección ingresada no existe'); end if;
  if v_eleccion.estado = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;

  if nullif(p_body ->> 'mesaElectoralId', '') is null then perform privado.error('La mesa electoral es obligatorio.'); end if;
  select * into v_mesa from public.mesa_electoral where id_mesa_electoral = (p_body ->> 'mesaElectoralId')::uuid;
  if not found then perform privado.error('La mesa electoral ingresada no existe'); end if;
  if v_mesa.eleccion_id <> v_eleccion.id_eleccion then perform privado.error('La mesa electoral no pertenece a la elección.'); end if;
  if not v_mesa.activa then perform privado.error('La mesa ya está cerrada.'); end if;

  if nullif(p_body ->> 'tipoCandidato', '') is null
     or not (p_body ->> 'tipoCandidato') = any (enum_range(null::public.tipo_candidato)::text[]) then
    perform privado.error('El tipo de candidato no es válido.');
  end if;
  v_tipo := (p_body ->> 'tipoCandidato')::public.tipo_candidato;
  v_concejal := privado.es_concejal(v_tipo);

  -- Evita registrar dos actas de la misma dignidad para la mesa en paralelo.
  perform pg_advisory_xact_lock(hashtext(v_mesa.id_mesa_electoral::text || v_tipo::text));
  if exists (select 1 from public.acta_eleccion where mesa_electoral_id = v_mesa.id_mesa_electoral and tipo_candidato = v_tipo) then
    select nombre_zona into v_zona from public.zona where id_zona = v_mesa.zona_id;
    perform privado.error(format('La mesa electoral ''%s-%s'', ya tiene un acta registrada para %s.', v_mesa.codigo_mesa, v_zona, v_tipo));
  end if;

  if v_blancos < 0 then perform privado.error('Los votos blancos no pueden ser negativos.'); end if;
  if v_nulos < 0 then perform privado.error('Los votos nulos no pueden ser negativos.'); end if;
  if jsonb_typeof(v_detalles) <> 'array' or jsonb_array_length(v_detalles) = 0 then
    perform privado.error('El acta debe tener al menos un candidato o lista electoral.');
  end if;
  if v_concejal then
    if (select count(*) from jsonb_array_elements(v_detalles)) <> (select count(distinct x ->> 'listaElectoralId') from jsonb_array_elements(v_detalles) x) then
      perform privado.error('No puede existir la misma lista electoral más de una vez en la misma acta.');
    end if;
  else
    if (select count(*) from jsonb_array_elements(v_detalles)) <> (select count(distinct coalesce(x ->> 'candidatoId', '')) from jsonb_array_elements(v_detalles) x) then
      perform privado.error('No puede existir el mismo candidato más de una vez en la misma acta.');
    end if;
  end if;

  for v_item in select * from jsonb_array_elements(v_detalles) loop
    if coalesce(nullif(v_item ->> 'votos', '')::integer, 0) < 0 then
      perform privado.error('Los votos no pueden ser negativos.');
    end if;

    if v_concejal then
      if nullif(v_item ->> 'listaElectoralId', '') is null then perform privado.error('La lista electoral es obligatoria.'); end if;
      select * into v_lista from public.lista_electoral where id_lista_electoral = (v_item ->> 'listaElectoralId')::uuid;
      if not found then perform privado.error('La lista electoral no existe.'); end if;
      if not v_lista.activo then perform privado.error(format('La lista electoral ''%s'' está suspendida.', v_lista.nombre_lista)); end if;
      if not exists (
        select 1 from public.candidato c join public.lista_candidato lc on lc.candidato_id = c.id_candidato
        where c.eleccion_id = v_eleccion.id_eleccion and c.tipo_candidato = v_tipo and c.activo
          and lc.lista_electoral_id = v_lista.id_lista_electoral) then
        perform privado.error(format('La lista electoral ''%s'' no tiene candidatos registrados para %s en esta elección.', v_lista.nombre_lista, v_tipo));
      end if;
      if not exists (
        select 1 from public.candidato c join public.lista_candidato lc on lc.candidato_id = c.id_candidato
        where c.eleccion_id = v_eleccion.id_eleccion and c.tipo_candidato = v_tipo and c.activo
          and lc.lista_electoral_id = v_lista.id_lista_electoral and lc.lista_principal) then
        perform privado.error(format('La lista electoral ''%s'' no es la lista principal de una candidatura de %s en esta elección.', v_lista.nombre_lista, v_tipo));
      end if;
      v_filas := v_filas || jsonb_build_object('lista', v_lista.id_lista_electoral, 'candidato', null,
        'votos', coalesce(nullif(v_item ->> 'votos', '')::integer, 0));
    else
      if nullif(v_item ->> 'candidatoId', '') is null then perform privado.error('El candidato es obligatorio.'); end if;
      select * into v_candidato from public.candidato where id_candidato = (v_item ->> 'candidatoId')::uuid;
      if not found then perform privado.error('El candidato no existe.'); end if;
      if not v_candidato.activo then perform privado.error(format('El candidato ''%s'' está inactivo.', v_candidato.nombre_candidato)); end if;
      if v_candidato.eleccion_id <> v_eleccion.id_eleccion then
        perform privado.error(format('El candidato ''%s'' no pertenece a esta elección.', v_candidato.nombre_candidato));
      end if;
      if v_candidato.tipo_candidato <> v_tipo then
        perform privado.error(format('El candidato %s no corresponde al tipo de candidatura %s del acta.', v_candidato.nombre_candidato, v_tipo));
      end if;
      if (select count(*) from public.lista_candidato where candidato_id = v_candidato.id_candidato and lista_principal) <> 1 then
        perform privado.error(format('El candidato ''%s'' debe tener una lista principal', v_candidato.nombre_candidato));
      end if;
      select lista_electoral_id into v_lista_principal
      from public.lista_candidato where candidato_id = v_candidato.id_candidato and lista_principal;
      v_filas := v_filas || jsonb_build_object('lista', v_lista_principal, 'candidato', v_candidato.id_candidato,
        'votos', coalesce(nullif(v_item ->> 'votos', '')::integer, 0));
    end if;
  end loop;

  insert into public.acta_eleccion (eleccion_id, mesa_electoral_id, tipo_candidato, fecha_registro, usuario_registro_id,
    votos_blancos, votos_nulos, total_votos, estado)
  values (v_eleccion.id_eleccion, v_mesa.id_mesa_electoral, v_tipo, privado.ahora(), v_usuario,
    v_blancos, v_nulos,
    v_blancos + v_nulos + (select coalesce(sum((x ->> 'votos')::integer), 0) from jsonb_array_elements(v_filas) x),
    'Registrada')
  returning id_acta into v_acta;

  insert into public.acta_detalle (acta_id, lista_electoral_id, candidato_id, votos)
  select v_acta, (x ->> 'lista')::uuid, nullif(x ->> 'candidato', '')::uuid, (x ->> 'votos')::integer
  from jsonb_array_elements(v_filas) x;

  perform privado.registrar_bitacora('INSERT', 'ActaEleccion', v_acta::text,
    format('Acta registrada para la mesa ''%s-%s''.', v_mesa.codigo_mesa, v_tipo), v_eleccion.id_eleccion,
    null, privado.acta_auditoria(v_acta));

  perform privado.notificar_resultados(v_acta);

  return privado.acta_entidad_json(v_acta);
end;
$$;

create or replace function public.actas_actualizar(p_id uuid, p_body jsonb)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_usuario uuid;
  v_acta public.acta_eleccion;
  v_estado_eleccion public.estado_eleccion;
  v_mesa public.mesa_electoral;
  v_concejal boolean;
  v_detalles jsonb := coalesce(p_body -> 'actaDetalles', '[]'::jsonb);
  v_blancos integer := coalesce(nullif(p_body ->> 'votosBlancos', '')::integer, 0);
  v_nulos integer := coalesce(nullif(p_body ->> 'votosNulos', '')::integer, 0);
  v_item jsonb;
  v_detalle uuid;
  v_anteriores json;
  v_cambios record;
begin
  v_usuario := privado.exigir_permiso('actas.editar');

  select * into v_acta from public.acta_eleccion where id_acta = p_id for update;
  if not found then perform privado.error('El acta no existe.'); end if;
  if v_acta.estado not in ('Registrada', 'EnRevision', 'ConInconsistencia') then
    perform privado.error(format('El acta no puede ser modificada en su estado actual ''%s''.', v_acta.estado));
  end if;

  if nullif(p_body ->> 'eleccionId', '') is null then perform privado.error('La elección es obligatoria.'); end if;
  if (p_body ->> 'eleccionId')::uuid <> v_acta.eleccion_id then perform privado.error('No se puede modificar la elección del acta.'); end if;
  select estado into v_estado_eleccion from public.eleccion where id_eleccion = v_acta.eleccion_id;
  if not found then perform privado.error('La elección del acta no existe.'); end if;
  if v_estado_eleccion = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;

  if nullif(p_body ->> 'mesaElectoralId', '') is null then perform privado.error('La mesa electoral es obligatorio.'); end if;
  if (p_body ->> 'mesaElectoralId')::uuid <> v_acta.mesa_electoral_id then
    perform privado.error('No se puede modificar la mesa electoral del acta.');
  end if;
  select * into v_mesa from public.mesa_electoral where id_mesa_electoral = v_acta.mesa_electoral_id;
  if not found then perform privado.error('La mesa electoral del acta no existe.'); end if;
  if not v_mesa.activa then perform privado.error('La mesa ya está cerrada.'); end if;

  if (p_body ->> 'tipoCandidato') is distinct from v_acta.tipo_candidato::text then
    perform privado.error('No se puede modificar la dignidad del acta.');
  end if;
  if v_blancos < 0 then perform privado.error('Los votos blancos no pueden ser negativos.'); end if;
  if v_nulos < 0 then perform privado.error('Los votos nulos no pueden ser negativos.'); end if;
  if jsonb_typeof(v_detalles) <> 'array' or jsonb_array_length(v_detalles) = 0 then
    perform privado.error('El acta debe tener al menos un candidato o lista.');
  end if;
  if jsonb_array_length(v_detalles) <> (select count(*) from public.acta_detalle where acta_id = p_id) then
    perform privado.error('No se pueden agregar ni eliminar candidatos o listas del acta.');
  end if;

  v_anteriores := privado.acta_auditoria(p_id);
  v_concejal := privado.es_concejal(v_acta.tipo_candidato);

  for v_item in select * from jsonb_array_elements(v_detalles) loop
    if coalesce(nullif(v_item ->> 'votos', '')::integer, 0) < 0 then
      perform privado.error('Los votos no pueden ser negativos.');
    end if;
    if v_concejal then
      if nullif(v_item ->> 'listaElectoralId', '') is null then perform privado.error('La lista electoral es obligatoria.'); end if;
      select id_acta_detalle into v_detalle from public.acta_detalle
      where acta_id = p_id and lista_electoral_id = (v_item ->> 'listaElectoralId')::uuid limit 1;
      if v_detalle is null then perform privado.error('La lista electoral no pertenece al acta.'); end if;
      update public.acta_detalle set votos = coalesce(nullif(v_item ->> 'votos', '')::integer, 0), candidato_id = null
      where id_acta_detalle = v_detalle;
    else
      if nullif(v_item ->> 'candidatoId', '') is null then perform privado.error('El candidato es obligatorio.'); end if;
      select id_acta_detalle into v_detalle from public.acta_detalle
      where acta_id = p_id and candidato_id = (v_item ->> 'candidatoId')::uuid limit 1;
      if v_detalle is null then perform privado.error('El candidato no pertenece al acta.'); end if;
      update public.acta_detalle set votos = coalesce(nullif(v_item ->> 'votos', '')::integer, 0)
      where id_acta_detalle = v_detalle;
    end if;
  end loop;

  update public.acta_eleccion
  set votos_blancos = v_blancos,
      votos_nulos = v_nulos,
      total_votos = v_blancos + v_nulos + (select coalesce(sum(votos), 0) from public.acta_detalle where acta_id = p_id)
  where id_acta = p_id;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.acta_auditoria(p_id));

  -- Si hubo cambios y el acta estaba Registrada o ConInconsistencia, pasa automáticamente a EnRevision.
  if v_cambios.nuevos is not null and v_acta.estado in ('Registrada', 'ConInconsistencia') then
    update public.acta_eleccion set estado = 'EnRevision' where id_acta = p_id;
    select * into v_cambios from privado.solo_cambios(v_anteriores, privado.acta_auditoria(p_id));
  end if;

  if v_cambios.nuevos is not null then
    update public.acta_eleccion
    set usuario_modificacion_id = v_usuario, fecha_modificacion = privado.ahora()
    where id_acta = p_id;

    perform privado.registrar_bitacora('UPDATE', 'ActaEleccion', p_id::text,
      format('Acta modificada para la mesa ''%s-%s''.', v_mesa.codigo_mesa, v_acta.tipo_candidato), v_acta.eleccion_id,
      v_cambios.anteriores, v_cambios.nuevos);

    perform privado.notificar_resultados(p_id);
  end if;

  return 'Acta actualizada correctamente.';
end;
$$;

create or replace function public.actas_cambiar_estado(p_id uuid, p_nuevo_estado public.estado_acta)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_usuario uuid;
  v_acta public.acta_eleccion;
  v_eleccion public.eleccion;
  v_mesa public.mesa_electoral;
  v_rural boolean;
  v_obligatorios public.tipo_candidato[];
  v_anteriores json;
  v_cambios record;
begin
  v_usuario := privado.exigir_permiso('actas.validar');

  select * into v_acta from public.acta_eleccion where id_acta = p_id for update;
  if not found then perform privado.error('El acta no existe.'); end if;
  if v_acta.estado = 'Validada' then perform privado.error('No se puede cambiar de estado porque el acta ya esta validada.'); end if;
  if v_acta.estado = 'Anulada' then perform privado.error('No se puede cambiar de estado porque el acta ya esta anulada.'); end if;
  if p_nuevo_estado is null then perform privado.error('El estado del acta no es válido.'); end if;
  if v_acta.estado = p_nuevo_estado then
    perform privado.error(format('El acta ya se encuentra en estado ''%s''.', p_nuevo_estado));
  end if;
  if not (
    (v_acta.estado = 'Registrada' and p_nuevo_estado in ('EnRevision', 'ConInconsistencia', 'Validada')) or
    (v_acta.estado = 'EnRevision' and p_nuevo_estado in ('ConInconsistencia', 'Validada')) or
    (v_acta.estado = 'ConInconsistencia' and p_nuevo_estado in ('EnRevision', 'Anulada'))
  ) then
    perform privado.error(format('No se permite cambiar el acta de ''%s'' a ''%s''.', v_acta.estado, p_nuevo_estado));
  end if;

  select * into v_eleccion from public.eleccion where id_eleccion = v_acta.eleccion_id;
  if not found then perform privado.error('La elección del acta no existe.'); end if;
  if v_eleccion.estado = 'Cerrada' then perform privado.error('La elección está cerrada.'); end if;

  select * into v_mesa from public.mesa_electoral where id_mesa_electoral = v_acta.mesa_electoral_id for update;
  if not found then perform privado.error('La mesa electoral del acta no existe.'); end if;
  if not v_mesa.activa then perform privado.error('La mesa electoral ya está cerrada.'); end if;

  v_anteriores := privado.acta_auditoria(p_id);

  update public.acta_eleccion
  set estado = p_nuevo_estado, usuario_modificacion_id = v_usuario, fecha_modificacion = privado.ahora()
  where id_acta = p_id;

  -- Si el acta se acaba de validar, cerrar la mesa cuando todas las actas obligatorias estén validadas.
  if p_nuevo_estado = 'Validada' then
    select p.parroquia_rural into v_rural
    from public.zona z join public.parroquia p on p.id_parroquia = z.parroquia_id
    where z.id_zona = v_mesa.zona_id;

    v_obligatorios := case v_eleccion.tipo_eleccion
      when 'Seccional' then
        case when v_rural then array['Alcalde', 'ConcejalUrbano', 'ConcejalRural']::public.tipo_candidato[]
             else array['Alcalde', 'ConcejalUrbano']::public.tipo_candidato[] end
      else array['Presidente']::public.tipo_candidato[]
    end;

    if not exists (
      select 1 from unnest(v_obligatorios) t(tipo)
      where not exists (
        select 1 from public.acta_eleccion a
        where a.mesa_electoral_id = v_mesa.id_mesa_electoral and a.tipo_candidato = t.tipo and a.estado = 'Validada')
    ) then
      update public.mesa_electoral set activa = false where id_mesa_electoral = v_mesa.id_mesa_electoral;
    end if;
  end if;

  select * into v_cambios from privado.solo_cambios(v_anteriores, privado.acta_auditoria(p_id));
  perform privado.registrar_bitacora('UPDATE', 'ActaEleccion', p_id::text,
    format('Estado del acta modificado a ''%s''.', p_nuevo_estado), v_acta.eleccion_id,
    v_cambios.anteriores, v_cambios.nuevos);

  perform privado.notificar_resultados(p_id);

  return 'Estado del acta actualizado correctamente.';
end;
$$;

-- -----------------------------------------------------------------------------
-- Resultados
-- -----------------------------------------------------------------------------
create or replace function privado.validar_ambito(p_tipo public.tipo_candidato, p_nivel text)
returns void language plpgsql immutable set search_path = ''
as $$
begin
  if p_tipo is null then
    perform privado.error('El tipo de candidatura ingresado no es válido.');
  end if;
  if not (case p_nivel
      when 'Nacional' then p_tipo = 'Presidente'
      when 'Provincia' then p_tipo in ('Presidente', 'Prefecto')
      else true
    end) then
    perform privado.error(format('La dignidad %s no permite consultar resultados a nivel %s.', p_tipo, p_nivel));
  end if;
end;
$$;

-- ResultadoService.CalcularResultadosAsync. Los filtros de ámbito en null no se aplican.
create or replace function privado.resultados_calcular(
  p_eleccion_id uuid, p_tipo public.tipo_candidato, p_tipo_resultado public.tipo_resultado,
  p_provincia_id uuid default null, p_canton_id uuid default null, p_parroquia_id uuid default null,
  p_zona_id uuid default null, p_mesa_id uuid default null)
returns jsonb language plpgsql stable set search_path = ''
as $$
declare
  v_resultado jsonb;
begin
  if p_tipo_resultado is null then
    perform privado.error('El tipo de resultado ingresado no es válido.');
  end if;

  with mesas as (
    select m.id_mesa_electoral
    from public.mesa_electoral m
    join public.zona z on z.id_zona = m.zona_id
    join public.parroquia p on p.id_parroquia = z.parroquia_id
    join public.canton c on c.id_canton = p.canton_id
    where m.eleccion_id = p_eleccion_id
      and (p_provincia_id is null or c.provincia_id = p_provincia_id)
      and (p_canton_id is null or p.canton_id = p_canton_id)
      and (p_parroquia_id is null or z.parroquia_id = p_parroquia_id)
      and (p_zona_id is null or m.zona_id = p_zona_id)
      and (p_mesa_id is null or m.id_mesa_electoral = p_mesa_id)
  ),
  actas as (
    select a.*
    from public.acta_eleccion a
    join mesas on mesas.id_mesa_electoral = a.mesa_electoral_id
    where a.eleccion_id = p_eleccion_id and a.tipo_candidato = p_tipo
  ),
  validas as (
    select * from actas
    where (p_tipo_resultado = 'Oficial' and estado = 'Validada')
       or (p_tipo_resultado = 'Preliminar' and estado in ('Registrada', 'EnRevision', 'Validada'))
  ),
  detalles as (
    select d.* from public.acta_detalle d join validas v on v.id_acta = d.acta_id
  ),
  total_validos as (
    select coalesce(sum(votos), 0)::integer as total from detalles
  ),
  -- Concejales: votos por lista, mostrando la alianza a la que pertenece la lista.
  por_lista as (
    select d.lista_electoral_id, l.nombre_lista, l.numero_lista, sum(d.votos)::integer as votos
    from detalles d
    join public.lista_electoral l on l.id_lista_electoral = d.lista_electoral_id
    group by d.lista_electoral_id, l.nombre_lista, l.numero_lista
  ),
  por_lista_alianza as (
    select pl.*, al.nombre_alianza,
      (select string_agg(x.numero_lista::text, ' - ' order by x.numero_lista)
       from (select distinct l2.numero_lista
             from public.alianza_detalle ad
             join public.lista_electoral l2 on l2.id_lista_electoral = ad.lista_electoral_id
             where ad.alianza_electoral_id = al.id_alianza_electoral) x) as numeros_alianza
    from por_lista pl
    left join lateral (
      select a.id_alianza_electoral, a.nombre_alianza
      from public.lista_candidato lc
      join public.candidato c on c.id_candidato = lc.candidato_id
      left join public.alianza_electoral a on a.id_alianza_electoral = c.alianza_electoral_id
      where lc.lista_electoral_id = pl.lista_electoral_id and c.eleccion_id = p_eleccion_id
      order by (c.tipo_candidato = p_tipo) desc, (a.id_alianza_electoral is null), lc.lista_principal desc
      limit 1
    ) al on true
  ),
  -- Otras dignidades: votos por candidato con su lista principal o alianza.
  por_candidato as (
    select d.candidato_id, c.nombre_candidato, c.alianza_electoral_id, sum(d.votos)::integer as votos
    from detalles d
    join public.candidato c on c.id_candidato = d.candidato_id
    where d.candidato_id is not null
    group by d.candidato_id, c.nombre_candidato, c.alianza_electoral_id
  ),
  por_candidato_lista as (
    select pc.*, lp.id_lista_electoral, lp.numero_lista, lp.nombre_lista, a.nombre_alianza,
      (select string_agg(x.numero_lista::text, ' - ' order by x.numero_lista)
       from (select distinct l2.numero_lista
             from public.alianza_detalle ad
             join public.lista_electoral l2 on l2.id_lista_electoral = ad.lista_electoral_id
             where ad.alianza_electoral_id = a.id_alianza_electoral) x) as numeros_alianza
    from por_candidato pc
    left join public.alianza_electoral a on a.id_alianza_electoral = pc.alianza_electoral_id
    left join lateral (
      select l.id_lista_electoral, l.numero_lista, l.nombre_lista
      from public.lista_candidato lc
      join public.lista_electoral l on l.id_lista_electoral = lc.lista_electoral_id
      where lc.candidato_id = pc.candidato_id
      order by lc.lista_principal desc, l.numero_lista
      limit 1
    ) lp on true
  ),
  resultados as (
    select jsonb_build_object(
        'candidatoId', null,
        'candidato', null,
        'listaElectoralId', x.lista_electoral_id,
        'lista', coalesce(x.nombre_alianza, x.nombre_lista),
        'numeroLista', x.numero_lista,
        'numerosLista', case when x.nombre_alianza is not null then x.numeros_alianza else x.numero_lista::text end,
        'votos', x.votos,
        'porcentaje', case when t.total > 0 then round(x.votos * 100.0 / t.total, 2) else 0 end) as fila,
      x.votos
    from por_lista_alianza x cross join total_validos t
    where privado.es_concejal(p_tipo)
    union all
    select jsonb_build_object(
        'candidatoId', x.candidato_id,
        'candidato', x.nombre_candidato,
        'listaElectoralId', coalesce(x.id_lista_electoral, '00000000-0000-0000-0000-000000000000'::uuid),
        'lista', coalesce(x.nombre_alianza, x.nombre_lista, ''),
        'numeroLista', coalesce(x.numero_lista, 0),
        'numerosLista', case when x.nombre_alianza is not null then coalesce(x.numeros_alianza, '')
                             else coalesce(x.numero_lista::text, '') end,
        'votos', x.votos,
        'porcentaje', case when t.total > 0 then round(x.votos * 100.0 / t.total, 2) else 0 end),
      x.votos
    from por_candidato_lista x cross join total_validos t
    where not privado.es_concejal(p_tipo)
  )
  select jsonb_build_object(
    'mesasRegistradas', (select count(*) from actas where estado = 'Registrada'),
    'mesasEnRevision', (select count(*) from actas where estado = 'EnRevision'),
    'mesasConInconsistencia', (select count(*) from actas where estado = 'ConInconsistencia'),
    'mesasValidadas', (select count(*) from actas where estado = 'Validada'),
    'totalMesas', (select count(*) from mesas),
    'totalVotosBlancos', (select coalesce(sum(votos_blancos), 0) from validas),
    'totalVotosNulos', (select coalesce(sum(votos_nulos), 0) from validas),
    'totalVotos', (select coalesce(sum(total_votos), 0) from validas),
    'totalVotosValidos', (select total from total_validos),
    'resultados', coalesce((select jsonb_agg(r.fila order by r.votos desc) from resultados r), '[]'::jsonb))
  into v_resultado;

  return v_resultado;
end;
$$;

create or replace function public.resultados_eleccion(
  p_eleccion_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  perform privado.validar_ambito(p_tipo_candidato, 'Nacional');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado);
end;
$$;

create or replace function public.resultados_provincia(
  p_eleccion_id uuid, p_provincia_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  perform privado.validar_ambito(p_tipo_candidato, 'Provincia');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_provincia_id => p_provincia_id);
end;
$$;

create or replace function public.resultados_canton(
  p_eleccion_id uuid, p_canton_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  perform privado.validar_ambito(p_tipo_candidato, 'Canton');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_canton_id => p_canton_id);
end;
$$;

create or replace function public.resultados_parroquia(
  p_eleccion_id uuid, p_parroquia_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  perform privado.validar_ambito(p_tipo_candidato, 'Parroquia');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_parroquia_id => p_parroquia_id);
end;
$$;

create or replace function public.resultados_zona(
  p_eleccion_id uuid, p_zona_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  perform privado.validar_ambito(p_tipo_candidato, 'Zona');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_zona_id => p_zona_id);
end;
$$;

create or replace function public.resultados_mesa(
  p_eleccion_id uuid, p_mesa_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
begin
  perform privado.exigir_permiso('resultados.view');
  return privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_mesa_id => p_mesa_id);
end;
$$;

-- Asignación de escaños para concejales (ResultadoService.ObtenerAsignacionConcejalesAsync).
-- Incluye "metodoEscanos" para que el frontend genere el Excel con los mismos cocientes.
create or replace function public.resultados_asignacion_escanos(
  p_eleccion_id uuid, p_canton_id uuid, p_tipo_candidato public.tipo_candidato, p_tipo_resultado public.tipo_resultado)
returns jsonb language plpgsql stable security definer set search_path = ''
as $$
declare
  c_total_escanos constant integer := 5;
  v_eleccion public.eleccion;
  v_resultado jsonb;
  v_listas jsonb;
begin
  perform privado.exigir_permiso('resultados.view');

  select * into v_eleccion from public.eleccion where id_eleccion = p_eleccion_id;
  if not found then perform privado.error('La elección no existe.'); end if;
  if not privado.es_concejal(p_tipo_candidato) then
    perform privado.error('La asignación de escaños está habilitada solo para Concejales.');
  end if;

  v_resultado := privado.resultados_calcular(p_eleccion_id, p_tipo_candidato, p_tipo_resultado, p_canton_id => p_canton_id);

  with listas as (
    select (r.value ->> 'listaElectoralId')::uuid as lista_id,
      (r.value ->> 'numeroLista')::integer as numero_lista,
      r.value ->> 'numerosLista' as numeros_lista,
      coalesce(r.value ->> 'lista', '') as nombre_lista,
      (r.value ->> 'votos')::integer as votos,
      r.ordinality as pos
    from jsonb_array_elements(v_resultado -> 'resultados') with ordinality r
    where (r.value ->> 'listaElectoralId')::uuid <> '00000000-0000-0000-0000-000000000000'::uuid
  ),
  cocientes as (
    select l.lista_id, l.numero_lista,
      l.votos::numeric / case v_eleccion.metodo_escanos when 'Webster' then (2 * i) - 1 else i end as cociente
    from listas l
    cross join generate_series(1, c_total_escanos) i
    where l.votos > 0
  ),
  ganadores as (
    select c.lista_id from cocientes c
    order by c.cociente desc, c.numero_lista
    limit c_total_escanos
  ),
  escanos as (
    select g.lista_id, count(*)::integer as escanos from ganadores g group by g.lista_id
  ),
  candidatos as (
    select distinct on (c.id_candidato) c.id_candidato, c.nombre_candidato, c.orden, lc.lista_electoral_id
    from public.candidato c
    join public.lista_candidato lc on lc.candidato_id = c.id_candidato and lc.lista_principal
    where c.eleccion_id = p_eleccion_id and c.tipo_candidato = p_tipo_candidato
      and c.canton_id = p_canton_id and c.activo
    order by c.id_candidato
  )
  select coalesce(jsonb_agg(jsonb_build_object(
      'listaElectoralId', l.lista_id,
      'numeroLista', l.numero_lista,
      'numerosLista', l.numeros_lista,
      'nombreLista', l.nombre_lista,
      'votos', l.votos,
      'escanosAsignados', coalesce(e.escanos, 0),
      'candidatos', coalesce((
        select jsonb_agg(jsonb_build_object(
            'candidatoId', x.id_candidato,
            'nombreCandidato', x.nombre_candidato,
            'orden', coalesce(x.orden, x.idx),
            'electo', x.idx <= coalesce(e.escanos, 0)) order by x.idx)
        from (
          select cd.*, row_number() over (order by cd.orden nulls last, cd.nombre_candidato)::integer as idx
          from candidatos cd where cd.lista_electoral_id = l.lista_id
        ) x), '[]'::jsonb)
    ) order by l.pos), '[]'::jsonb)
  into v_listas
  from listas l
  left join escanos e on e.lista_id = l.lista_id;

  return jsonb_build_object(
    'eleccionId', v_eleccion.id_eleccion,
    'nombreEleccion', v_eleccion.nombre_eleccion,
    'dignidad', p_tipo_candidato,
    'estadoResultado', p_tipo_resultado,
    'metodoEscanos', v_eleccion.metodo_escanos,
    'totalEscanos', c_total_escanos,
    'totalListas', jsonb_array_length(v_listas),
    'listas', v_listas);
end;
$$;

-- -----------------------------------------------------------------------------
-- Permisos de ejecución: solo usuarios autenticados pueden llamar a las RPC.
-- (Cada RPC valida además el permiso del rol con privado.exigir_permiso.)
-- -----------------------------------------------------------------------------
revoke execute on all functions in schema public from public, anon;
grant execute on all functions in schema public to authenticated, service_role;
revoke all on all functions in schema privado from public, anon, authenticated;
grant execute on function privado.tiene_permiso(text) to authenticated;
grant execute on function privado.permisos_usuario_actual() to authenticated;
alter default privileges for role postgres in schema public revoke execute on functions from public, anon;
