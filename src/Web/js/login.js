import { api } from "./api.js";
import { guardarSesion, obtenerSesion } from "./sesion.js";
import { $ } from "./ui.js";

if (obtenerSesion()) window.location.replace("app.html");

const pasos = ["acceso", "correo", "codigo", "contrasena"];
let correoRegistro = "";
let tokenRegistro = "";

function ir(paso) {
  for (const p of pasos) $(`#paso-${p}`).hidden = p !== paso;
}

function mensaje(texto, tipo = "error") {
  const el = $("#mensaje");
  el.textContent = texto;
  el.className = tipo === "ok" ? "ok" : "";
  el.hidden = !texto;
}

async function enviar(formulario, accion) {
  const boton = formulario.querySelector("button[type=submit]");
  boton.disabled = true;
  mensaje("");
  try {
    await accion();
  } catch (error) {
    mensaje(error.message);
  } finally {
    boton.disabled = false;
  }
}

$("#form-login").addEventListener("submit", (e) => {
  e.preventDefault();
  const f = e.target;
  enviar(f, async () => {
    const sesion = await api.post("/api/auth/login", { correo: f.correo.value, contrasena: f.contrasena.value });
    guardarSesion(sesion);
    window.location.href = "app.html";
  });
});

$("#ir-registro").addEventListener("click", () => {
  mensaje("");
  ir("correo");
});

document.querySelectorAll("[data-volver]").forEach((b) =>
  b.addEventListener("click", () => {
    mensaje("");
    ir("acceso");
  })
);

$("#form-correo").addEventListener("submit", (e) => {
  e.preventDefault();
  const f = e.target;
  enviar(f, async () => {
    correoRegistro = f.correo.value.trim();
    const respuesta = await api.post("/api/auth/solicitar-codigo", { correo: correoRegistro });
    mensaje(respuesta.mensaje, "ok");
    ir("codigo");
  });
});

$("#form-codigo").addEventListener("submit", (e) => {
  e.preventDefault();
  const f = e.target;
  enviar(f, async () => {
    const respuesta = await api.post("/api/auth/verificar-codigo", { correo: correoRegistro, codigo: f.codigo.value.trim() });
    tokenRegistro = respuesta.tokenRegistro;
    f.reset();
    ir("contrasena");
  });
});

$("#form-contrasena").addEventListener("submit", (e) => {
  e.preventDefault();
  const f = e.target;
  if (f.contrasena.value !== f.repetir.value) {
    mensaje("Las contraseñas no coinciden.");
    return;
  }
  enviar(f, async () => {
    const respuesta = await api.post("/api/auth/crear-contrasena", {
      correo: correoRegistro,
      tokenRegistro,
      contrasena: f.contrasena.value,
    });
    f.reset();
    tokenRegistro = "";
    ir("acceso");
    mensaje(respuesta.mensaje, "ok");
  });
});
