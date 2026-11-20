// Crear y actualizar usuarios (reemplaza UsuariosController POST/PUT).
// Las validaciones, permisos y bitácora se hacen en las RPC usuarios_validar / usuarios_crear /
// usuarios_actualizar, llamadas con el JWT del usuario que hace la petición.
// Aquí solo se crea o actualiza la cuenta en Supabase Auth (requiere la service role key).
import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "jsr:@supabase/supabase-js@2";
import { corsHeaders, error, json } from "../_shared/http.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const ANON_KEY = Deno.env.get("SUPABASE_ANON_KEY")!;

type Usuario = {
  nombreUsuario?: string;
  emailUsuario?: string;
  password?: string;
  rolId?: string;
};

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
    const { accion, id, usuario } = (await req.json()) as { accion: string; id?: string; usuario: Usuario };
    const body = {
      nombreUsuario: usuario?.nombreUsuario?.trim() ?? "",
      emailUsuario: usuario?.emailUsuario?.trim().toLowerCase() ?? "",
      rolId: usuario?.rolId ?? null,
    };

    // Cliente con el JWT del usuario: las RPC validan sus permisos.
    const comoUsuario = createClient(SUPABASE_URL, ANON_KEY, {
      auth: { persistSession: false },
      global: { headers: { Authorization: authHeader } },
    });
    const admin = createClient(SUPABASE_URL, SERVICE_ROLE_KEY, { auth: { persistSession: false } });

    if (accion === "crear") {
      const { error: errorValidar } = await comoUsuario.rpc("usuarios_validar", { p_body: body });
      if (errorValidar) return error(errorValidar.message);

      if (!usuario?.password || !usuario.password.trim()) {
        return error("La contraseña es obligatoria.");
      }

      const { data: creado, error: errorAuth } = await admin.auth.admin.createUser({
        email: body.emailUsuario,
        password: usuario.password,
        email_confirm: true,
        user_metadata: { nombre_usuario: body.nombreUsuario },
      });
      if (errorAuth || !creado.user) {
        return error(errorAuth?.message ?? "No se pudo crear el usuario.");
      }

      const { data, error: errorCrear } = await comoUsuario.rpc("usuarios_crear", {
        p_id: creado.user.id,
        p_body: body,
      });
      if (errorCrear) {
        await admin.auth.admin.deleteUser(creado.user.id);
        return error(errorCrear.message);
      }
      return json(data, 201);
    }

    if (accion === "actualizar") {
      if (!id) return error("El usuario no existe.", 404);

      const { error: errorValidar } = await comoUsuario.rpc("usuarios_validar", { p_body: body, p_id: id });
      if (errorValidar) return error(errorValidar.message);

      const { data: actual, error: errorActual } = await admin.auth.admin.getUserById(id);
      if (errorActual || !actual.user) return error("El usuario no existe.", 404);

      const emailAnterior = actual.user.email ?? "";
      const cambiaEmail = emailAnterior.toLowerCase() !== body.emailUsuario;
      if (cambiaEmail) {
        const { error: errorEmail } = await admin.auth.admin.updateUserById(id, {
          email: body.emailUsuario,
          email_confirm: true,
        });
        if (errorEmail) return error(errorEmail.message);
      }

      const { data, error: errorActualizar } = await comoUsuario.rpc("usuarios_actualizar", {
        p_id: id,
        p_body: body,
      });
      if (errorActualizar) {
        if (cambiaEmail) {
          await admin.auth.admin.updateUserById(id, { email: emailAnterior, email_confirm: true });
        }
        return error(errorActualizar.message);
      }
      return json(data?.mensaje ?? "Usuario actualizado correctamente");
    }

    return error("Acción no válida.");
  } catch (e) {
    console.error(e);
    return error("Ocurrió un error interno en el servidor.", 500);
  }
});
