import { API_URL } from "./config.js";
import { obtenerSesion, cerrarSesion } from "./sesion.js";

async function solicitar(ruta, { metodo = "GET", cuerpo, blob = false } = {}) {
  const sesion = obtenerSesion();
  const encabezados = {};
  if (sesion?.token) encabezados["Authorization"] = `Bearer ${sesion.token}`;

  let contenido = cuerpo;
  if (cuerpo !== undefined && !(cuerpo instanceof FormData)) {
    encabezados["Content-Type"] = "application/json";
    contenido = JSON.stringify(cuerpo);
  }

  let respuesta;
  try {
    respuesta = await fetch(`${API_URL}${ruta}`, { method: metodo, headers: encabezados, body: contenido });
  } catch {
    throw new Error("No se pudo conectar con el servidor.");
  }

  if (respuesta.status === 401 && sesion && !ruta.startsWith("/api/auth/")) {
    cerrarSesion();
    throw new Error("Tu sesión venció. Inicia sesión de nuevo.");
  }
  if (respuesta.status === 204) return null;
  if (blob && respuesta.ok) return respuesta.blob();

  let datos = null;
  if ((respuesta.headers.get("content-type") || "").includes("json")) datos = await respuesta.json();

  if (!respuesta.ok) {
    if (respuesta.status === 429) throw new Error("Demasiados intentos. Espera un minuto.");
    if (respuesta.status === 413) throw new Error("El archivo es demasiado grande.");
    if (datos?.errors) throw new Error("Revisa los datos capturados.");
    throw new Error(datos?.error || "Ocurrió un error. Intenta de nuevo.");
  }
  return datos;
}

export const api = {
  get: (ruta) => solicitar(ruta),
  post: (ruta, cuerpo) => solicitar(ruta, { metodo: "POST", cuerpo: cuerpo ?? {} }),
  put: (ruta, cuerpo) => solicitar(ruta, { metodo: "PUT", cuerpo }),
  borrar: (ruta) => solicitar(ruta, { metodo: "DELETE" }),
  archivo: (ruta) => solicitar(ruta, { blob: true }),
};
