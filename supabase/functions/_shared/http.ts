// Utilidades HTTP compartidas por las Edge Functions.

export const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

// Misma forma de error que el backend .NET: { message: "..." }
export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

export function error(message: string, status = 400): Response {
  return json({ message }, status);
}

// Escapa los comodines de LIKE para comparar el texto tal cual (sin distinguir mayúsculas).
export function textoExacto(valor: string): string {
  return valor.replace(/[\\%_]/g, (c) => `\\${c}`);
}
