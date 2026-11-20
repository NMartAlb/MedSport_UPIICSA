import { api } from "../api.js";
import { $, $$, esc, mostrar, aviso, fecha, intentar, insignia, etiqueta } from "../ui.js";

const ROLES = ["Alumno", "Coach", "PersonalMedico", "Directivo", "Administrador"];
const opcionesRol = (seleccionado) =>
  ROLES.map((r) => `<option value="${r}" ${r === seleccionado ? "selected" : ""}>${esc(etiqueta(r))}</option>`).join("");

async function usuarios(sesion) {
  const lista = await api.get("/api/usuarios");
  mostrar(`<h2>Usuarios y roles</h2>
    <form id="form-usuario" class="tarjeta">
      <h3>Dar de alta a un usuario</h3>
      <p class="vacio">Los alumnos se registran solos con su correo @ipn.mx. Aquí se da de alta al personal (coaches, médicos, directivos) con su rol; después cada persona crea su contraseña con el código que llega a su correo.</p>
      <div class="fila-formulario">
        <label>Correo institucional <input type="email" name="correo" required></label>
        <label>Nombre completo <input name="nombre" maxlength="120" required></label>
        <label>Rol <select name="rol">${opcionesRol("Coach")}</select></label>
      </div>
      <button type="submit">Dar de alta</button>
    </form>
    <div class="tabla-envoltura"><table><thead><tr><th>Correo</th><th>Nombre</th><th>Rol</th><th>Estado</th><th></th></tr></thead><tbody>
    ${lista.map((u) => `<tr data-id="${u.id}">
        <td>${esc(u.correo)}</td><td>${esc(u.nombreCompleto)}</td>
        <td><select class="cambiar-rol" ${u.id === sesion.usuarioId ? "disabled" : ""}>${opcionesRol(u.rol)}</select></td>
        <td>${u.activo ? insignia("Aprobado").replace("Aprobado", "Activo") : insignia("Rechazado").replace("Rechazado", "Inactivo")}</td>
        <td>${u.id === sesion.usuarioId ? "" : `<button type="button" class="secundario" data-activo="${u.activo ? "0" : "1"}">${u.activo ? "Desactivar" : "Activar"}</button>`}</td>
      </tr>`).join("")}
    </tbody></table></div>`);

  $("#form-usuario").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target.elements;
    let listo = false;
    await intentar(async () => {
      await api.post("/api/usuarios", { correo: f.correo.value, nombreCompleto: f.nombre.value, rol: f.rol.value });
      listo = true;
    });
    if (listo) {
      aviso("Usuario dado de alta.");
      await usuarios(sesion);
    }
  });

  $("#contenido tbody").addEventListener("change", async (e) => {
    if (!e.target.classList.contains("cambiar-rol")) return;
    const id = Number(e.target.closest("tr").dataset.id);
    let listo = false;
    await intentar(async () => {
      await api.put(`/api/usuarios/${id}/rol`, { rol: e.target.value });
      listo = true;
    });
    aviso(listo ? "Rol actualizado." : "No se pudo cambiar el rol.", listo ? "ok" : "error");
    if (!listo) await usuarios(sesion);
  });

  $("#contenido tbody").addEventListener("click", async (e) => {
    const boton = e.target.closest("button[data-activo]");
    if (!boton) return;
    const id = Number(boton.closest("tr").dataset.id);
    let listo = false;
    await intentar(async () => {
      await api.put(`/api/usuarios/${id}/activo`, { activo: boton.dataset.activo === "1" });
      listo = true;
    });
    if (listo) await usuarios(sesion);
  });
}

async function bitacora() {
  const lista = await api.get("/api/auditoria");
  mostrar(`<h2>Bitácora de accesos</h2>
    <p class="vacio">Quién consultó o modificó información médica, y cuándo.</p>` + (lista.length
    ? `<div class="tabla-envoltura"><table><thead><tr><th>Fecha</th><th>Usuario</th><th>Acción</th><th>Recurso</th></tr></thead><tbody>
       ${lista.map((r) => `<tr><td>${fecha(r.fecha)}</td><td>${esc(r.usuario)}</td><td>${esc(r.accion)}</td><td>${esc(r.recurso)}</td></tr>`).join("")}
       </tbody></table></div>`
    : '<p class="vacio">Aún no hay registros.</p>'));
}

export const secciones = [
  { id: "usuarios", titulo: "Usuarios y roles", mostrar: usuarios },
  { id: "bitacora", titulo: "Bitácora de accesos", mostrar: bitacora },
];
