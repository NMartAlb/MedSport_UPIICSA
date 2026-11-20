import { api } from "../api.js";
import { $, $$, esc, mostrar, aviso, fecha, intentar, insignia } from "../ui.js";
import { panelRevision } from "./comun.js";

async function cargarMias(sesion) {
  const todas = await api.get("/api/actividades");
  return todas.filter((a) => a.coachId === sesion.usuarioId);
}

const opcionesActividad = (lista) =>
  lista.map((a) => `<option value="${a.id}">${esc(a.nombre)}</option>`).join("");

async function misActividades(sesion) {
  const [mias, tipos] = await Promise.all([cargarMias(sesion), api.get("/api/documentos/tipos?contexto=Actividad")]);
  let editando = null;

  const filas = mias.map((a) => `<tr data-id="${a.id}">
      <td>${esc(a.nombre)}</td><td>${esc(a.tipo)}</td><td>${esc(a.horario)}</td>
      <td>${a.cupoMaximo - a.cupoDisponible} / ${a.cupoMaximo}</td>
      <td><div class="acciones">
        <button type="button" class="secundario" data-editar>Editar</button>
        <button type="button" class="peligro" data-eliminar>Eliminar</button></div></td></tr>`);

  mostrar(`<h2>Mis actividades</h2>
    ${filas.length ? `<div class="tabla-envoltura"><table><thead><tr><th>Actividad</th><th>Tipo</th><th>Horario</th><th>Inscritos</th><th></th></tr></thead><tbody>${filas.join("")}</tbody></table></div>` : '<p class="vacio">Aún no tienes actividades.</p>'}
    <form id="form-actividad" class="tarjeta">
      <h3 id="titulo-form">Nueva actividad</h3>
      <div class="fila-formulario">
        <label>Nombre <input name="nombre" minlength="3" maxlength="80" required></label>
        <label>Tipo <select name="tipo"><option value="Deportiva">Deportiva</option><option value="Cultural">Cultural</option></select></label>
        <label>Horario <input name="horario" maxlength="80" placeholder="Lun y Mié 14:00" required></label>
        <label>Cupo máximo <input type="number" name="cupo" min="1" max="500" required></label>
      </div>
      <fieldset><legend>Documentos requeridos</legend>
        ${tipos.map((t) => `<label class="opcion"><input type="checkbox" name="documentos" value="${t.id}"> ${esc(t.nombre)}</label>`).join("")}
      </fieldset>
      <div class="acciones"><button type="submit">Guardar</button>
        <button type="button" class="secundario" id="cancelar-edicion" hidden>Cancelar edición</button></div>
    </form>`);

  const formulario = $("#form-actividad");

  function rellenar(actividad) {
    editando = actividad ? actividad.id : null;
    $("#titulo-form").textContent = actividad ? "Editar actividad" : "Nueva actividad";
    $("#cancelar-edicion").hidden = !actividad;
    formulario.elements.nombre.value = actividad?.nombre ?? "";
    formulario.elements.tipo.value = actividad?.tipo ?? "Deportiva";
    formulario.elements.horario.value = actividad?.horario ?? "";
    formulario.elements.cupo.value = actividad?.cupoMaximo ?? "";
    $$("input[name=documentos]", formulario).forEach((c) => {
      c.checked = actividad ? actividad.tiposDocumentoRequeridos.includes(Number(c.value)) : false;
    });
  }

  $("#cancelar-edicion").addEventListener("click", () => rellenar(null));

  $("#contenido tbody")?.addEventListener("click", async (e) => {
    const fila = e.target.closest("tr");
    if (!fila) return;
    const actividad = mias.find((a) => a.id === Number(fila.dataset.id));
    if (e.target.closest("[data-editar]")) {
      rellenar(actividad);
      formulario.scrollIntoView();
    } else if (e.target.closest("[data-eliminar]")) {
      if (!confirm(`¿Eliminar la actividad «${actividad.nombre}»?`)) return;
      let listo = false;
      await intentar(async () => {
        await api.borrar(`/api/actividades/${actividad.id}`);
        listo = true;
      });
      if (listo) {
        aviso("Actividad eliminada.");
        await misActividades(sesion);
      }
    }
  });

  formulario.addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = formulario.elements;
    const cuerpo = {
      nombre: f.nombre.value,
      tipo: f.tipo.value,
      horario: f.horario.value,
      cupoMaximo: Number(f.cupo.value),
      tiposDocumentoRequeridos: $$("input[name=documentos]:checked", formulario).map((c) => Number(c.value)),
      coachId: null,
    };
    let listo = false;
    await intentar(async () => {
      if (editando) await api.put(`/api/actividades/${editando}`, cuerpo);
      else await api.post("/api/actividades", cuerpo);
      listo = true;
    });
    if (listo) {
      aviso("Actividad guardada.");
      await misActividades(sesion);
    }
  });
}

async function inscritos(sesion) {
  const mias = await cargarMias(sesion);
  mostrar(`<h2>Alumnos inscritos</h2>` + (mias.length
    ? `<label>Actividad <select id="actividad">${opcionesActividad(mias)}</select></label><div id="lista"></div>`
    : '<p class="vacio">Aún no tienes actividades.</p>'));
  if (!mias.length) return;

  async function cargar() {
    const lista = await intentar(() => api.get(`/api/actividades/${$("#actividad").value}/inscritos`));
    if (!lista) return;
    $("#lista").innerHTML = lista.length
      ? `<div class="tabla-envoltura"><table><thead><tr><th>Alumno</th><th>Correo</th><th>Inscrito</th><th>Trámite</th></tr></thead><tbody>${lista.map((i) =>
          `<tr><td>${esc(i.nombre)}</td><td>${esc(i.correo)}</td><td>${fecha(i.fecha)}</td><td>${insignia(i.estadoTramite)}</td></tr>`).join("")}</tbody></table></div>`
      : '<p class="vacio">Nadie se ha inscrito todavía.</p>';
  }
  $("#actividad").addEventListener("change", cargar);
  await cargar();
}

async function paseLista(sesion) {
  const mias = await cargarMias(sesion);
  const hoy = new Date().toISOString().slice(0, 10);
  mostrar(`<h2>Pase de lista</h2>` + (mias.length
    ? `<form id="form-lista" class="tarjeta"><div class="fila-formulario">
         <label>Actividad <select name="actividad">${opcionesActividad(mias)}</select></label>
         <label>Fecha <input type="date" name="fecha" value="${hoy}" max="${hoy}" required></label>
       </div><button type="submit">Cargar lista</button></form><div id="lista"></div>`
    : '<p class="vacio">Aún no tienes actividades.</p>'));
  if (!mias.length) return;

  $("#form-lista").addEventListener("submit", async (e) => {
    e.preventDefault();
    const actividadId = e.target.elements.actividad.value;
    const dia = e.target.elements.fecha.value;
    const alumnos = await intentar(() => api.get(`/api/actividades/${actividadId}/asistencia?fecha=${dia}`));
    if (!alumnos) return;

    $("#lista").innerHTML = alumnos.length
      ? `<form id="form-asistencia" class="tarjeta"><table><thead><tr><th>Alumno</th><th>Presente</th></tr></thead><tbody>${alumnos.map((a) =>
          `<tr data-alumno="${a.alumnoId}"><td>${esc(a.nombre)}<br><small>${esc(a.correo)}</small></td>
           <td><input type="checkbox" ${a.presente ? "checked" : ""} aria-label="Presente"></td></tr>`).join("")}</tbody></table>
         <div class="acciones"><button type="submit">Guardar asistencia</button></div></form>`
      : '<p class="vacio">No hay alumnos inscritos.</p>';

    $("#form-asistencia")?.addEventListener("submit", async (ev) => {
      ev.preventDefault();
      const asistencias = $$("#form-asistencia tbody tr").map((fila) => ({
        alumnoId: Number(fila.dataset.alumno),
        presente: $("input", fila).checked,
      }));
      let listo = false;
      await intentar(async () => {
        await api.post(`/api/actividades/${actividadId}/asistencia`, { fecha: dia, asistencias });
        listo = true;
      });
      if (listo) aviso("Asistencia guardada.");
    });
  });
}

export const secciones = [
  { id: "actividades", titulo: "Mis actividades", mostrar: misActividades },
  { id: "inscritos", titulo: "Inscritos", mostrar: inscritos },
  { id: "revision", titulo: "Revisión de documentos", mostrar: () => panelRevision("Revisión de documentos") },
  { id: "lista", titulo: "Pase de lista", mostrar: paseLista },
];
