// Eliminar actas de prueba (equivalente a POST api/Actas/eliminar-pruebas del backend .NET).
// Confirma la contraseña del usuario autenticado y llama a la RPC actas_eliminar_pruebas,
// que valida el rol Administrador y elimina las actas en una sola transacción.
import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "jsr:@supabase/supabase-js@2";
import { corsHeaders, error, json } from "../_shared/http.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY")!;

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }
  if (req.method !== "POST") {
    return error("Método no permitido.", 405);
  }

  const authHeader = req.headers.get("Authorization");
  if (!authHeader) {
    return error("Se requiere autenticación, por favor inicie sesión.", 401);
  }

  try {
    const { password } = await req.json();
    // Errores 400 (no 401) para que el frontend no cierre la sesión por una contraseña incorrecta
    if (!password || !String(password).trim()) {
      return error("Debe ingresar su contraseña para confirmar la eliminación.");
    }

    const comoUsuario = createClient(SUPABASE_URL, ANON_KEY, {
      auth: { persistSession: false },
      global: { headers: { Authorization: authHeader } },
    });
    const { data: actual, error: errorUsuario } = await comoUsuario.auth.getUser();
    if (errorUsuario || !actual.user?.email) {
      return error("Su sesión ha expirado, por favor inicie sesión nuevamente.", 401);
    }

    // Verificar la contraseña iniciando sesión aparte y cerrando solo esa sesión
    const verificador = createClient(SUPABASE_URL, ANON_KEY, { auth: { persistSession: false } });
    const { error: errorPassword } = await verificador.auth.signInWithPassword({
      email: actual.user.email,
      password: String(password),
    });
    if (errorPassword) {
      return error("La contraseña es incorrecta.");
    }
    await verificador.auth.signOut({ scope: "local" });

    const admin = createClient(SUPABASE_URL, SERVICE_ROLE_KEY, { auth: { persistSession: false } });
    const { data, error: errorEliminar } = await admin.rpc("actas_eliminar_pruebas", {
      p_usuario_id: actual.user.id,
    });
    if (errorEliminar) {
      return error(errorEliminar.message);
    }

    return json(data);
  } catch (e) {
    console.error(e);
    return error("Ocurrió un error interno en el servidor.", 500);
  }
});
