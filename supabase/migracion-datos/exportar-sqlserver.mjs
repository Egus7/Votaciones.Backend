// Exporta las tablas de SQL Server a JSON (una archivo por tabla) usando sqlcmd.
//
// Uso:
//   node exportar-sqlserver.mjs "<servidor>" "<base>" "<usuario>" "<contraseña>" [carpetaSalida]
// Ejemplo (RDS):
//   node exportar-sqlserver.mjs "tcp:srv-axis.ctc4g6omw8bt.us-east-2.rds.amazonaws.com,1433" votacionesDB administrator "***"
import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

export const TABLAS = [
  "Provincia", "Canton", "Parroquia", "Zona",
  "Eleccion", "ListaElectoral", "AlianzaElectoral", "AlianzaDetalle",
  "Candidato", "ListaCandidato", "MesaElectoral",
  "AdmRol", "AdmUsuario",
  "ActaEleccion", "ActaDetalle", "AdmBitacora",
];

const [servidor, base, usuario, password, salida = join(import.meta.dirname, "exportado")] = process.argv.slice(2);
if (!servidor || !base || !usuario || !password) {
  console.error('Uso: node exportar-sqlserver.mjs "<servidor>" "<base>" "<usuario>" "<contraseña>" [carpetaSalida]');
  process.exit(1);
}

mkdirSync(salida, { recursive: true });

for (const tabla of TABLAS) {
  // El JSON se guarda en una variable para obtenerlo en una sola celda (-y 0 = sin truncar).
  // Se escribe en UTF-16 (-u) a un archivo: la conversión de código de página de sqlcmd
  // corrompe algunos caracteres acentuados (ej. "SUCUMBÍOS").
  const archivoTemporal = join(salida, `${tabla}.utf16.tmp`);
  execFileSync("sqlcmd", [
    "-S", servidor, "-d", base, "-U", usuario, "-P", password, "-C",
    "-y", "0", "-u", "-o", archivoTemporal,
    "-Q", `SET NOCOUNT ON; DECLARE @j NVARCHAR(MAX) = (SELECT * FROM [${tabla}] FOR JSON PATH, INCLUDE_NULL_VALUES); SELECT ISNULL(@j, '[]');`,
  ], { maxBuffer: 1024 * 1024 * 512 });
  const texto = readFileSync(archivoTemporal, "utf16le").replace(/^﻿/, "");
  rmSync(archivoTemporal);

  // La salida incluye una línea de encabezado; el JSON va del primer "[" al último "]".
  const filas = JSON.parse(texto.slice(texto.indexOf("["), texto.lastIndexOf("]") + 1));
  writeFileSync(join(salida, `${tabla}.json`), JSON.stringify(filas, null, 2), "utf8");
  console.log(`${tabla}: ${filas.length} filas`);
}

console.log(`Exportado en ${salida}`);
