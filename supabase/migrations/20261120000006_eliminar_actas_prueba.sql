-- =============================================================================
-- Eliminar actas de prueba (equivalente a POST api/Actas/eliminar-pruebas del backend .NET).
-- Solo la llama la Edge Function "actas-eliminar-pruebas" (service_role) después de validar
-- la contraseña del usuario; aquí se valida que el usuario tenga el rol Administrador.
-- =============================================================================
create or replace function public.actas_eliminar_pruebas(p_usuario_id uuid)
returns text language plpgsql security definer set search_path = ''
as $$
declare
  v_ids uuid[];
  v_mesas uuid[];
  v_actas integer;
  v_detalles integer;
  v_mesas_reabiertas integer;
  v_bitacoras integer;
  v_mensaje text;
  v_notificar uuid[];
begin
  if not exists (
    select 1 from public.adm_usuario u
    join public.adm_rol r on r.id_rol = u.rol_id
    where u.id_usuario = p_usuario_id and u.estado and r.activo and lower(r.nombre_rol) = 'administrador'
  ) then
    perform privado.error('Solo un usuario con rol Administrador puede eliminar las actas de prueba.');
  end if;

  select array_agg(id_acta), array_agg(distinct mesa_electoral_id) into v_ids, v_mesas from public.acta_eleccion;
  if v_ids is null then
    perform privado.error('No existen actas registradas para eliminar.');
  end if;

  -- Una notificación por elección y dignidad para que las pantallas de resultados se actualicen
  select array_agg(x.id_acta) into v_notificar
  from (select distinct on (eleccion_id, tipo_candidato) id_acta from public.acta_eleccion) x;
  perform privado.notificar_resultados(n) from unnest(v_notificar) n;

  update public.mesa_electoral set activa = true where id_mesa_electoral = any (v_mesas) and not activa;
  get diagnostics v_mesas_reabiertas = row_count;

  -- Bitácora de las actas eliminadas (son datos de prueba)
  delete from public.adm_bitacora
  where tabla = 'ActaEleccion' and id_registro = any (select lower(i::text) from unnest(v_ids) i);
  get diagnostics v_bitacoras = row_count;

  delete from public.acta_detalle where acta_id = any (v_ids);
  get diagnostics v_detalles = row_count;
  delete from public.acta_eleccion where id_acta = any (v_ids);
  get diagnostics v_actas = row_count;

  v_mensaje := format('Se eliminaron %s actas de prueba y se reabrieron %s mesas.', v_actas, v_mesas_reabiertas);

  -- Un único registro de auditoría de la eliminación
  insert into public.adm_bitacora (usuario_id, fecha_registro, accion, tabla, id_registro, descripcion, valores_nuevos)
  values (p_usuario_id, privado.ahora(), 'DELETE', 'ActaEleccion', null, v_mensaje,
    json_build_object('ActasEliminadas', v_actas, 'DetallesEliminados', v_detalles,
      'MesasReabiertas', v_mesas_reabiertas, 'RegistrosBitacoraEliminados', v_bitacoras));

  return v_mensaje;
end;
$$;

-- No se expone a los usuarios: solo la Edge Function (service_role) puede ejecutarla.
revoke execute on function public.actas_eliminar_pruebas(uuid) from public, anon, authenticated;
grant execute on function public.actas_eliminar_pruebas(uuid) to service_role;
