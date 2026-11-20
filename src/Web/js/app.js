import { obtenerSesion, cerrarSesion } from "./sesion.js";
import { $, intentar, etiqueta, marcarActiva } from "./ui.js";

const sesion = obtenerSesion();

// Cada rol tiene su propio archivo con sus secciones.
const VISTAS = {
  Alumno: "./vistas/alumno.js",
  Coach: "./vistas/coach.js",
  PersonalMedico: "./vistas/medico.js",
  Directivo: "./vistas/directivo.js",
  Administrador: "./vistas/admin.js",
};

async function iniciar() {
  $("#usuario").textContent = `${sesion.nombreCompleto} · ${etiqueta(sesion.rol)}`;
  $("#salir").addEventListener("click", cerrarSesion);

  const archivo = VISTAS[sesion.rol];
  if (!archivo) {
    $("#contenido").textContent = "Tu rol no tiene vistas disponibles.";
    return;
  }

  const { secciones } = await import(archivo);
  const menu = $("#menu");
  menu.innerHTML = secciones.map((s) => `<button type="button" data-id="${s.id}">${s.titulo}</button>`).join("");

  async function abrir(id) {
    const seccion = secciones.find((s) => s.id === id);
    marcarActiva(menu, id);
    $("#contenido").textContent = "Cargando…";
    await intentar(() => seccion.mostrar(sesion));
  }

  menu.addEventListener("click", (e) => {
    const boton = e.target.closest("button[data-id]");
    if (boton) abrir(boton.dataset.id);
  });
  await abrir(secciones[0].id);
}

if (!sesion) window.location.replace("index.html");
else iniciar();
