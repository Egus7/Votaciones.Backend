-- =============================================================================
-- Esquema base de Votaciones (migrado desde SQL Server / EF Core)
-- Los enums se guardan con el mismo nombre que en C# para que el frontend
-- reciba exactamente los mismos valores que antes ("Planificada", "Femenina"...).
-- Las fechas son "timestamp without time zone" en hora de Ecuador, igual que antes.
-- =============================================================================

create type public.tipo_eleccion as enum ('General', 'Seccional');
create type public.estado_eleccion as enum ('Planificada', 'Activa', 'Cerrada');
create type public.jurisdiccion as enum ('Nacional', 'Provincial', 'Cantonal', 'Circunscripcion');
create type public.tipo_candidato as enum ('Presidente', 'Prefecto', 'Alcalde', 'ConcejalUrbano', 'ConcejalRural');
create type public.tipo_mesa as enum ('Femenina', 'Masculina');
create type public.estado_acta as enum ('Registrada', 'EnRevision', 'ConInconsistencia', 'Validada', 'Anulada');
create type public.tipo_resultado as enum ('Oficial', 'Preliminar');
create type public.metodo_escanos as enum ('DHondt', 'Webster');

-- Zonas -----------------------------------------------------------------------
create table public.provincia (
  id_provincia uuid primary key default gen_random_uuid(),
  codigo_provincia varchar(2) not null,
  nombre_provincia varchar(100) not null,
  activo boolean not null default true
);

create table public.canton (
  id_canton uuid primary key default gen_random_uuid(),
  provincia_id uuid not null references public.provincia (id_provincia),
  nombre_canton varchar(100) not null,
  activo boolean not null default true
);
create index ix_canton_provincia on public.canton (provincia_id);

create table public.parroquia (
  id_parroquia uuid primary key default gen_random_uuid(),
  canton_id uuid not null references public.canton (id_canton),
  nombre_parroquia varchar(100) not null,
  parroquia_rural boolean not null default false,
  activo boolean not null default true
);
create index ix_parroquia_canton on public.parroquia (canton_id);

create table public.zona (
  id_zona uuid primary key default gen_random_uuid(),
  parroquia_id uuid not null references public.parroquia (id_parroquia),
  nombre_zona varchar(150) not null,
  activo boolean not null default true
);
create index ix_zona_parroquia on public.zona (parroquia_id);

-- Elecciones ------------------------------------------------------------------
create table public.eleccion (
  id_eleccion uuid primary key default gen_random_uuid(),
  nombre_eleccion varchar(200) not null,
  tipo_eleccion public.tipo_eleccion not null,
  descripcion varchar(500),
  fecha_eleccion timestamp not null,
  estado public.estado_eleccion not null default 'Planificada',
  metodo_escanos public.metodo_escanos not null default 'DHondt',
  fecha_creacion timestamp not null,
  fecha_modificacion timestamp
);

create table public.lista_electoral (
  id_lista_electoral uuid primary key default gen_random_uuid(),
  numero_lista integer not null,
  nombre_lista text not null,
  siglas text,
  jurisdiccion public.jurisdiccion not null,
  activo boolean not null default true
);

create table public.alianza_electoral (
  id_alianza_electoral uuid primary key default gen_random_uuid(),
  nombre_alianza varchar(200) not null,
  eleccion_id uuid not null references public.eleccion (id_eleccion) on delete restrict,
  provincia_id uuid references public.provincia (id_provincia),
  canton_id uuid references public.canton (id_canton),
  parroquia_id uuid references public.parroquia (id_parroquia)
);
create index ix_alianza_eleccion on public.alianza_electoral (eleccion_id);
create index ix_alianza_provincia on public.alianza_electoral (provincia_id);
create index ix_alianza_canton on public.alianza_electoral (canton_id);
create index ix_alianza_parroquia on public.alianza_electoral (parroquia_id);

create table public.alianza_detalle (
  id_alianza_detalle uuid primary key default gen_random_uuid(),
  alianza_electoral_id uuid not null references public.alianza_electoral (id_alianza_electoral) on delete cascade,
  lista_electoral_id uuid not null references public.lista_electoral (id_lista_electoral) on delete restrict,
  lista_alianza_principal boolean not null default false
);
create index ix_alianza_detalle_alianza on public.alianza_detalle (alianza_electoral_id);
create index ix_alianza_detalle_lista on public.alianza_detalle (lista_electoral_id);

create table public.candidato (
  id_candidato uuid primary key default gen_random_uuid(),
  eleccion_id uuid not null references public.eleccion (id_eleccion) on delete restrict,
  nombre_candidato varchar(200) not null,
  tipo_candidato public.tipo_candidato not null,
  orden integer,
  provincia_id uuid references public.provincia (id_provincia),
  canton_id uuid references public.canton (id_canton),
  parroquia_id uuid references public.parroquia (id_parroquia),
  alianza_electoral_id uuid references public.alianza_electoral (id_alianza_electoral) on delete restrict,
  activo boolean not null default true
);
create index ix_candidato_eleccion on public.candidato (eleccion_id);
create index ix_candidato_provincia on public.candidato (provincia_id);
create index ix_candidato_canton on public.candidato (canton_id);
create index ix_candidato_parroquia on public.candidato (parroquia_id);
create index ix_candidato_alianza on public.candidato (alianza_electoral_id);

create table public.lista_candidato (
  id_lista_candidato uuid primary key default gen_random_uuid(),
  candidato_id uuid not null references public.candidato (id_candidato) on delete cascade,
  lista_electoral_id uuid not null references public.lista_electoral (id_lista_electoral) on delete restrict,
  lista_principal boolean not null default false
);
create index ix_lista_candidato_candidato on public.lista_candidato (candidato_id);
create index ix_lista_candidato_lista on public.lista_candidato (lista_electoral_id);

create table public.mesa_electoral (
  id_mesa_electoral uuid primary key default gen_random_uuid(),
  eleccion_id uuid not null references public.eleccion (id_eleccion) on delete restrict,
  zona_id uuid not null references public.zona (id_zona),
  codigo_mesa varchar(20) not null,
  tipo_mesa public.tipo_mesa not null,
  descripcion varchar(200),
  activa boolean not null default true
);
create index ix_mesa_eleccion on public.mesa_electoral (eleccion_id);
create index ix_mesa_zona on public.mesa_electoral (zona_id);

-- Seguridad -------------------------------------------------------------------
create table public.adm_rol (
  id_rol uuid primary key default gen_random_uuid(),
  nombre_rol text not null,
  descripcion_rol text,
  permisos_rol jsonb not null default '[]'::jsonb check (jsonb_typeof(permisos_rol) = 'array'),
  activo boolean not null default true
);

-- El id del usuario es el mismo id de auth.users (Supabase Auth).
create table public.adm_usuario (
  id_usuario uuid primary key references auth.users (id) on delete cascade,
  nombre_usuario varchar(400) not null,
  email_usuario text,
  rol_id uuid not null references public.adm_rol (id_rol) on delete restrict,
  estado boolean not null default true,
  fecha_creacion timestamp not null
);
create index ix_usuario_rol on public.adm_usuario (rol_id);

-- Actas -----------------------------------------------------------------------
create table public.acta_eleccion (
  id_acta uuid primary key default gen_random_uuid(),
  eleccion_id uuid not null references public.eleccion (id_eleccion) on delete restrict,
  mesa_electoral_id uuid not null references public.mesa_electoral (id_mesa_electoral) on delete restrict,
  tipo_candidato public.tipo_candidato not null,
  fecha_registro timestamp not null,
  usuario_registro_id uuid not null,
  votos_blancos integer not null,
  votos_nulos integer not null,
  total_votos integer not null,
  estado public.estado_acta not null,
  fecha_modificacion timestamp,
  usuario_modificacion_id uuid
);
create index ix_acta_eleccion on public.acta_eleccion (eleccion_id);
create index ix_acta_mesa on public.acta_eleccion (mesa_electoral_id);

create table public.acta_detalle (
  id_acta_detalle uuid primary key default gen_random_uuid(),
  acta_id uuid not null references public.acta_eleccion (id_acta) on delete cascade,
  lista_electoral_id uuid not null references public.lista_electoral (id_lista_electoral),
  candidato_id uuid references public.candidato (id_candidato),
  votos integer not null
);
create index ix_acta_detalle_acta on public.acta_detalle (acta_id);
create index ix_acta_detalle_lista on public.acta_detalle (lista_electoral_id);
create index ix_acta_detalle_candidato on public.acta_detalle (candidato_id);

-- Bitácora: los valores se guardan como "json" (no jsonb) para conservar el orden
-- de las claves tal como se muestran en el detalle de la bitácora.
create table public.adm_bitacora (
  id_bitacora uuid primary key default gen_random_uuid(),
  eleccion_id uuid references public.eleccion (id_eleccion) on delete set null,
  usuario_id uuid references public.adm_usuario (id_usuario) on delete set null,
  fecha_registro timestamp not null,
  accion text not null,
  tabla text not null,
  id_registro text,
  descripcion text,
  valores_anteriores json,
  valores_nuevos json
);
create index ix_bitacora_eleccion on public.adm_bitacora (eleccion_id);
create index ix_bitacora_usuario on public.adm_bitacora (usuario_id);
create index ix_bitacora_fecha on public.adm_bitacora (fecha_registro desc);
create index ix_bitacora_registro on public.adm_bitacora (tabla, id_registro);

-- Todo el acceso pasa por funciones RPC (security definer) que validan permisos.
-- RLS activo y sin políticas = ningún acceso directo desde la API REST.
alter table public.provincia enable row level security;
alter table public.canton enable row level security;
alter table public.parroquia enable row level security;
alter table public.zona enable row level security;
alter table public.eleccion enable row level security;
alter table public.lista_electoral enable row level security;
alter table public.alianza_electoral enable row level security;
alter table public.alianza_detalle enable row level security;
alter table public.candidato enable row level security;
alter table public.lista_candidato enable row level security;
alter table public.mesa_electoral enable row level security;
alter table public.adm_rol enable row level security;
alter table public.adm_usuario enable row level security;
alter table public.acta_eleccion enable row level security;
alter table public.acta_detalle enable row level security;
alter table public.adm_bitacora enable row level security;
