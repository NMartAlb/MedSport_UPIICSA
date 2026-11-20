/** Escapa el texto antes de ponerlo en HTML (evita que alguien inyecte código). */
export const esc = (texto) =>
  String(texto ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

export const $ = (selector, raiz = document) => raiz.querySelector(selector);
export const $$ = (selector, raiz = document) => [...raiz.querySelectorAll(selector)];

export function mostrar(html) {
  $("#contenido").innerHTML = html;
}

export function aviso(texto, tipo = "ok") {
  const el = $("#aviso");
  el.textContent = texto;
  el.className = `aviso ${tipo}`;
  el.hidden = false;
  clearTimeout(aviso.temporizador);
  aviso.temporizador = setTimeout(() => (el.hidden = true), 5000);
}

/** Ejecuta una acción; si falla, muestra el error en pantalla y devuelve null. */
export async function intentar(accion) {
  try {
    return await accion();
  } catch (error) {
    aviso(error.message, "error");
    return null;
  }
}

export const fecha = (iso) =>
  iso ? new Date(iso).toLocaleString("es-MX", { dateStyle: "medium", timeStyle: "short" }) : "";

const ETIQUETAS = {
  PendienteDocumentos: "Pendiente de documentos",
  Pendiente: "Pendiente",
  Recibido: "Recibido",
  EnRevision: "En revisión",
  Aprobado: "Aprobado",
  Rechazado: "Rechazado",
  Completado: "Completado",
  PersonalMedico: "Personal médico",
};
const CLASES = { Aprobado: "ok", Completado: "ok", Rechazado: "mal", Pendiente: "alerta", PendienteDocumentos: "alerta" };

export const etiqueta = (valor) => ETIQUETAS[valor] ?? valor;
export const insignia = (estado) => `<span class="insignia ${CLASES[estado] ?? ""}">${esc(etiqueta(estado))}</span>`;

/** Convierte una pestaña de menú en la sección activa. */
export function marcarActiva(menu, id) {
  $$("button", menu).forEach((b) => b.classList.toggle("activo", b.dataset.id === id));
}
