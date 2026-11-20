const CLAVE = "sesion";

export function guardarSesion(sesion) {
  sessionStorage.setItem(CLAVE, JSON.stringify(sesion));
}

export function obtenerSesion() {
  try {
    return JSON.parse(sessionStorage.getItem(CLAVE));
  } catch {
    return null;
  }
}

export function cerrarSesion() {
  sessionStorage.removeItem(CLAVE);
  window.location.href = "index.html";
}
