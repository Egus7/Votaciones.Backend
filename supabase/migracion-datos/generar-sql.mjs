// Convierte los JSON exportados de SQL Server en un script SQL para Supabase.
// - Renombra tablas/columnas a snake_case y convierte los enums numéricos a su nombre.
// - Crea cada usuario en Supabase Auth (auth.users + auth.identities) con el mismo id
//   y una contraseña temporal aleatoria (los hashes de ASP.NET Identity no son compatibles).
//   Las contraseñas temporales se escriben en un archivo aparte, ignorado por git; el SQL
//   solo lleva el hash bcrypt (el mismo formato que usa Supabase Auth).
//
// Uso (requiere `npm install` en esta carpeta):
//   node generar-sql.mjs [carpetaExportado] [archivoSql] [archivoCredenciales]
import bcrypt from "bcryptjs";
import { randomInt } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const dir = import.meta.dirname;
const [
  entrada = join(dir, "exportado"),
  archivoSql = join(dir, "datos.sql"),
  archivoCredenciales = join(dir, "..", ".credenciales-temporales.txt"),
] = process.argv.slice(2);

const ENUMS = {
  TipoEleccion: { tipo: "tipo_eleccion", valores: { 0: "General", 1: "General", 2: "Seccional" } },
  EstadoEleccion: { tipo: "estado_eleccion", valores: { 1: "Planificada", 2: "Activa", 3: "Cerrada" } },
  MetodoEscanos: { tipo: "metodo_escanos", valores: { 0: "DHondt", 1: "DHondt", 2: "Webster" } },
  Jurisdiccion: { tipo: "jurisdiccion", valores: { 1: "Nacional", 2: "Provincial", 3: "Cantonal", 4: "Circunscripcion" } },
  TipoCandidato: {
    tipo: "tipo_candidato",
    valores: { 1: "Presidente", 2: "Prefecto", 3: "Alcalde", 4: "ConcejalUrbano", 5: "ConcejalRural" },
  },
  TipoMesa: { tipo: "tipo_mesa", valores: { 1: "Femenina", 2: "Masculina" } },
  EstadoActa: {
    tipo: "estado_acta",
    valores: { 1: "Registrada", 2: "EnRevision", 3: "ConInconsistencia", 4: "Validada", 5: "Anulada" },
  },
};

// Tabla origen -> tabla destino y columnas con tratamiento especial.
const TABLAS = [
  ["Provincia", "provincia"],
  ["Canton", "canton"],
  ["Parroquia", "parroquia"],
  ["Zona", "zona"],
  ["Eleccion", "eleccion", { TipoEleccion: "TipoEleccion", Estado: "EstadoEleccion", MetodoEscanos: "MetodoEscanos" }],
  ["ListaElectoral", "lista_electoral", { Jurisdiccion: "Jurisdiccion" }],
  ["AlianzaElectoral", "alianza_electoral"],
  ["AlianzaDetalle", "alianza_detalle"],
  ["Candidato", "candidato", { TipoCandidato: "TipoCandidato" }],
  ["ListaCandidato", "lista_candidato"],
  ["MesaElectoral", "mesa_electoral", { TipoMesa: "TipoMesa" }],
  ["AdmRol", "adm_rol", { PermisosRol: "jsonb" }],
  ["AdmUsuario", "adm_usuario", { Password: "omitir", EmailUsuario: "minusculas" }],
  ["ActaEleccion", "acta_eleccion", { TipoCandidato: "TipoCandidato", Estado: "EstadoActa" }],
  ["ActaDetalle", "acta_detalle"],
  ["AdmBitacora", "adm_bitacora", { ValoresAnteriores: "json", ValoresNuevos: "json" }],
];

const snake = (nombre) => nombre.replace(/([a-z0-9])([A-Z])/g, "$1_$2").toLowerCase();

function literal(valor) {
  if (valor === null || valor === undefined) return "null";
  if (typeof valor === "boolean") return valor ? "true" : "false";
  if (typeof valor === "number") return String(valor);
  return `'${String(valor).replace(/'/g, "''")}'`;
}

function valorColumna(columna, valor, especial) {
  if (!especial) return literal(valor);
  if (especial === "jsonb") return valor == null ? "'[]'::jsonb" : `${literal(valor)}::jsonb`;
  if (especial === "json") return valor == null ? "null" : `${literal(valor)}::json`;
  if (especial === "minusculas") return literal(valor == null ? null : String(valor).trim().toLowerCase());
  const enumDef = ENUMS[especial];
  if (enumDef) {
    if (valor == null) return "null";
    const nombre = enumDef.valores[valor];
    if (!nombre) throw new Error(`Valor ${valor} no válido para ${especial} (columna ${columna}).`);
    return `'${nombre}'::public.${enumDef.tipo}`;
  }
  throw new Error(`Tratamiento desconocido: ${especial}`);
}

function contrasenaTemporal() {
  const caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#$%*";
  return Array.from({ length: 16 }, () => caracteres[randomInt(caracteres.length)]).join("");
}

const leer = (tabla) => JSON.parse(readFileSync(join(entrada, `${tabla}.json`), "utf8"));

const sql = [
  "-- Script generado por supabase/migracion-datos/generar-sql.mjs",
  "-- Carga los datos de SQL Server en Supabase. Es idempotente (on conflict do nothing).",
  "begin;",
  "",
];
const credenciales = [
  "Contraseñas temporales de los usuarios migrados a Supabase Auth.",
  "NO subir este archivo a git. Pedir a cada usuario que cambie su contraseña.",
  "",
];

// 1) Usuarios de Supabase Auth (deben existir antes de adm_usuario por la FK).
for (const u of leer("AdmUsuario")) {
  const email = u.EmailUsuario ? String(u.EmailUsuario).trim().toLowerCase() : null;
  if (!email) {
    throw new Error(`El usuario ${u.NombreUsuario} no tiene correo; Supabase Auth necesita uno para iniciar sesión.`);
  }
  const password = contrasenaTemporal();
  credenciales.push(`${u.NombreUsuario}\t${email}\t${password}`);
  const id = literal(u.IdUsuario);
  sql.push(
    `insert into auth.users (instance_id, id, aud, role, email, encrypted_password, email_confirmed_at,`,
    `  raw_app_meta_data, raw_user_meta_data, created_at, updated_at,`,
    `  confirmation_token, recovery_token, email_change, email_change_token_new)`,
    `values ('00000000-0000-0000-0000-000000000000', ${id}, 'authenticated', 'authenticated', ${literal(email)},`,
    `  ${literal(bcrypt.hashSync(password, 10))}, now(),`,
    `  '{"provider":"email","providers":["email"]}'::jsonb, jsonb_build_object('nombre_usuario', ${literal(u.NombreUsuario)}),`,
    `  now(), now(), '', '', '', '')`,
    `on conflict (id) do nothing;`,
    `insert into auth.identities (provider_id, user_id, identity_data, provider, last_sign_in_at, created_at, updated_at)`,
    `values (${id}, ${id}, jsonb_build_object('sub', ${id}, 'email', ${literal(email)}, 'email_verified', true),`,
    `  'email', now(), now(), now())`,
    `on conflict do nothing;`,
    "",
  );
}

// 2) Tablas de negocio en orden de dependencias.
for (const [origen, destino, especiales = {}] of TABLAS) {
  const filas = leer(origen);
  sql.push(`-- ${origen} -> public.${destino} (${filas.length} filas)`);
  if (filas.length === 0) {
    sql.push("");
    continue;
  }
  const columnas = Object.keys(filas[0]).filter((c) => especiales[c] !== "omitir");
  const lista = columnas.map(snake).join(", ");
  // Un insert por cada 100 filas para mantener el script compacto
  for (let i = 0; i < filas.length; i += 100) {
    const valores = filas
      .slice(i, i + 100)
      .map((fila) => `(${columnas.map((c) => valorColumna(c, fila[c], especiales[c])).join(", ")})`)
      .join(",\n  ");
    sql.push(`insert into public.${destino} (${lista}) values\n  ${valores}\non conflict do nothing;`);
  }
  sql.push("");
}

sql.push("commit;", "");

writeFileSync(archivoSql, sql.join("\n"), "utf8");
writeFileSync(archivoCredenciales, credenciales.join("\n") + "\n", "utf8");
console.log(`SQL generado: ${archivoSql}`);
console.log(`Credenciales temporales: ${archivoCredenciales}`);
