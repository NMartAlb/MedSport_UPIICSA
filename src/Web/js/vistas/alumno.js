import { api } from "../api.js";
import { $, $$, esc, mostrar, aviso, fecha, intentar, insignia } from "../ui.js";
import { requisitosHtml, enlazarCargas, vistaCampanas, vistaNotificaciones } from "./comun.js";

async function actividades() {
  const [lista, mias] = await Promise.all([api.get("/api/actividades"), api.get("/api/inscripciones/mias")]);
  const inscritas = new Set(mias.map((i) => i.actividadId));

  const filas = lista.map((a) => {
    let boton;
    if (inscritas.has(a.id)) boton = insignia("Completado").replace("Completado", "Inscrito");
    else if (a.cupoDisponible === 0) boton = '<span class="vacio">Sin cupo</span>';
    else boton = `<button type="button" data-inscribir="${a.id}">Inscribirme</button>`;
    return `<tr>
      <td>${esc(a.nombre)}</td><td>${esc(a.tipo === "Deportiva" ? "Deportiva" : "Cultural")}</td>
      <td>${esc(a.horario)}</td><td>${esc(a.coach)}</td>
      <td>${a.cupoDisponible} de ${a.cupoMaximo}</td><td>${boton}</td></tr>`;
  });

  mostrar(`<h2>Actividades deportivas y culturales</h2>` + (filas.length
    ? `<div class="tabla-envoltura"><table><thead><tr><th>Actividad</th><th>Tipo</th><th>Horario</th><th>Coach</th><th>Cupo disponible</th><th></th></tr></thead><tbody>${filas.join("")}</tbody></table></div>`
    : `<p class="vacio">Todavía no hay actividades publicadas.</p>`));

  $("#contenido tbody")?.addEventListener("click", async (e) => {
    const boton = e.target.closest("button[data-inscribir]");
    if (!boton) return;
    boton.disabled = true;
    let listo = false;
    await intentar(async () => {
      await api.post("/api/inscripciones", { actividadId: Number(boton.dataset.inscribir) });
      listo = true;
    });
    if (listo) {
      aviso("Inscripción realizada. Ahora sube tus documentos en «Mis trámites».");
      await actividades();
    } else {
      boton.disabled = false;
    }
  });
}

async function misTramites() {
  const inscripciones = await api.get("/api/inscripciones/mias");
  const tarjetas = inscripciones.map((i) => `<article class="tarjeta" data-id="${i.id}">
      <h3>${esc(i.actividad)} ${insignia(i.estadoTramite)}</h3>
      <p class="vacio">Inscrito el ${fecha(i.fecha)}</p>
      ${i.requisitos.length ? requisitosHtml(i.requisitos) : '<p class="vacio">Esta actividad no requiere documentos.</p>'}
      <div class="acciones"><button type="button" class="peligro" data-cancelar>Cancelar inscripción</button></div>
    </article>`);

  mostrar(`<h2>Mis trámites</h2>${tarjetas.join("") || '<p class="vacio">Aún no te has inscrito a ninguna actividad.</p>'}`);

  $$("#contenido article").forEach((articulo) => {
    const id = Number(articulo.dataset.id);
    enlazarCargas(articulo, "Actividad", id, misTramites);
    $("[data-cancelar]", articulo).addEventListener("click", async () => {
      if (!confirm("¿Seguro que quieres cancelar esta inscripción?")) return;
      let listo = false;
      await intentar(async () => {
        await api.borrar(`/api/inscripciones/${id}`);
        listo = true;
      });
      if (listo) {
        aviso("Inscripción cancelada.");
        await misTramites();
      }
    });
  });
}

async function servicioMedico() {
  const [tramite, valoraciones] = await Promise.all([
    api.get("/api/servicio-medico/nuevo-ingreso"),
    api.get("/api/servicio-medico/valoraciones/mias"),
  ]);

  const bloqueTramite = tramite
    ? `<article class="tarjeta" id="tramite" data-id="${tramite.id}">
         <h3>Nuevo ingreso al servicio médico ${insignia(tramite.estadoTramite)}</h3>
         <p>Sube tus documentos antes de acudir al servicio médico.</p>
         ${requisitosHtml(tramite.requisitos)}
       </article>`
    : `<article class="tarjeta">
         <h3>Nuevo ingreso al servicio médico</h3>
         <p>Inicia tu trámite y carga tus documentos desde aquí, antes de ir en persona.</p>
         <button type="button" id="iniciar-tramite">Iniciar trámite</button>
       </article>`;

  const bloqueValoraciones = valoraciones.length
    ? valoraciones.map((v) => `<article class="tarjeta"><h3>Valoración del ${fecha(v.fecha)}</h3>
        <p><strong>Resultado:</strong> ${esc(v.resultado)}</p>
        ${v.observaciones ? `<p><strong>Observaciones:</strong> ${esc(v.observaciones)}</p>` : ""}
        <small>Registrada por ${esc(v.medico)}</small></article>`).join("")
    : '<p class="vacio">Aún no tienes una valoración médica registrada.</p>';

  mostrar(`<h2>Servicio médico</h2>${bloqueTramite}<h2>Mi valoración médica</h2>${bloqueValoraciones}`);

  $("#iniciar-tramite")?.addEventListener("click", async () => {
    let listo = false;
    await intentar(async () => {
      await api.post("/api/servicio-medico/nuevo-ingreso");
      listo = true;
    });
    if (listo) await servicioMedico();
  });

  const caja = $("#tramite");
  if (caja) enlazarCargas(caja, "NuevoIngresoMedico", Number(caja.dataset.id), servicioMedico);
}

export const secciones = [
  { id: "actividades", titulo: "Actividades", mostrar: actividades },
  { id: "tramites", titulo: "Mis trámites", mostrar: misTramites },
  { id: "medico", titulo: "Servicio médico", mostrar: servicioMedico },
  { id: "campanas", titulo: "Campañas", mostrar: vistaCampanas },
  { id: "notificaciones", titulo: "Notificaciones", mostrar: vistaNotificaciones },
];
