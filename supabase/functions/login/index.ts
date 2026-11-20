// Login por nombre de usuario o correo (reemplaza AuthController / AuthService).
// Supabase Auth solo inicia sesión por correo, así que aquí se resuelve el usuario,
// se validan estado y rol (igual que antes) y se devuelve la sesión de Supabase.
import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "jsr:@supabase/supabase-js@2";
import { corsHeaders, error, json, textoExacto } from "../_shared/http.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY")!;

type UsuarioRol = {
  id_usuario: string;
  nombre_usuario: string;
  email_usuario: string | null;
  estado: boolean;
  rol: { id_rol: string; nombre_rol: string; activo: boolean } | null;
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }
  if (req.method !== "POST") {
    return error("Método no permitido.", 405);
  }

  try {
    const { usuario, password } = await req.json();

    if (!usuario || !String(usuario).trim()) {
      return error("El usuario o correo es obligatorio.");
    }
    if (!password || !String(password).trim()) {
      return error("La contraseña es obligatoria.");
    }

    const identificador = String(usuario).trim();
    const admin = createClient(SUPABASE_URL, SERVICE_ROLE_KEY, { auth: { persistSession: false } });
    const columnas = "id_usuario, nombre_usuario, email_usuario, estado, rol:adm_rol(id_rol, nombre_rol, activo)";

    let { data: encontrado } = await admin
      .from("adm_usuario")
      .select(columnas)
      .ilike("nombre_usuario", textoExacto(identificador))
      .limit(1)
      .maybeSingle<UsuarioRol>();

    if (!encontrado) {
      ({ data: encontrado } = await admin
        .from("adm_usuario")
        .select(columnas)
        .ilike("email_usuario", textoExacto(identificador))
        .limit(1)
        .maybeSingle<UsuarioRol>());
    }

    if (!encontrado) {
      return error(`El usuario ${identificador} no existe.`, 401);
    }
    if (!encontrado.estado) {
      return error("El usuario se encuentra inactivo.", 401);
    }
    if (!encontrado.rol) {
      return error("El usuario no tiene un rol asignado.", 401);
    }
    if (!encontrado.rol.activo) {
      return error("El rol del usuario se encuentra inactivo.", 401);
    }
    if (!encontrado.email_usuario) {
      return error("El usuario no tiene un correo registrado.", 401);
    }

    const cliente = createClient(SUPABASE_URL, ANON_KEY, { auth: { persistSession: false } });
    const { data: sesion, error: errorLogin } = await cliente.auth.signInWithPassword({
      email: encontrado.email_usuario,
      password: String(password),
    });

    if (errorLogin || !sesion.session) {
      return error("La contraseña es incorrecta.", 401);
    }

    const s = sesion.session;
    return json({
      session: {
        access_token: s.access_token,
        refresh_token: s.refresh_token,
        expires_at: s.expires_at,
        expires_in: s.expires_in,
      },
      token: s.access_token,
      expiracionToken: new Date((s.expires_at ?? 0) * 1000).toISOString(),
      idUsuario: encontrado.id_usuario,
      nombreUsuario: identificador,
      rolId: encontrado.rol.id_rol,
      nombreRol: encontrado.rol.nombre_rol,
    });
  } catch (e) {
    console.error(e);
    return error("Ocurrió un error interno en el servidor.", 500);
  }
});
