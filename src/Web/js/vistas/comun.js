// Piezas que usan varias vistas: carga de documentos, revisión, buscador de alumnos, campañas, notificaciones y reportes.
import { api } from "../api.js";
import { $, $$, esc, mostrar, aviso, fecha, intentar, insignia } from "../ui.js";

// ---------- Archivos ----------
export function abrirArchivo(blob) {
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.target = "_blank";
  enlace.rel = "noopener";
  enlace.click();
  setTimeout(() => URL.revokeObjectURL(url), 60000);
}

export function descargarArchivo(blob, nombre) {
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.download = nombre;
  enlace.click();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}

// ---------- Requisitos y carga de documentos (alumno) ----------
export function requisitosHtml(requisitos) {
  const filas = requisitos.map((r) => {
    const puedeSubir = r.estado === "Pendiente" || r.estado === "Rechazado";
    return `<li>
      <div><strong>${esc(r.nombre)}</strong> ${insignia(r.estado)}</div>
      ${r.motivoRechazo ? `<p class="motivo">Motivo del rechazo: ${esc(r.motivoRechazo)}</p>` : ""}
      ${puedeSubir ? `<form class="carga" data-tipo="${r.tipoDocumentoId}">
          <label>Archivo (PDF, JPG o PNG, máximo 5 MB)
            <input type="file" name="archivo" accept=".pdf,.jpg,.jpeg,.png" required>
          </label>
          <button type="submit">${r.estado === "Rechazado" ? "Subir de nuevo" : "Subir"}</button>
        </form>` : ""}
    </li>`;
  });
  return `<ul class="requisitos">${filas.join("")}</ul>`;
}

export function enlazarCargas(raiz, contexto, referenciaId, alTerminar) {
  $$("form.carga", raiz).forEach((formulario) =>
    formulario.addEventListener("submit", async (e) => {
      e.preventDefault();
      const archivo = formulario.elements.archivo.files[0];
      if (!archivo) return;
      const datos = new FormData();
      datos.append("contexto", contexto);
      datos.append("referenciaId", referenciaId);
      datos.append("tipoDocumentoId", formulario.dataset.tipo);
      datos.append("archivo", archivo);

      let subido = false;
      await intentar(async () => {
        await api.post("/api/documentos", datos);
        subido = true;
      });
      if (subido) {
        aviso("Documento enviado.");
        await alTerminar();
      }
    })
  );
}

// ---------- Revisión de documentos (coach y personal médico) ----------
export async function panelRevision(titulo) {
  const pendientes = await api.get("/api/documentos/pendientes");
  const filas = pendientes.map((d) => `<tr data-id="${d.id}">
      <td>${esc(d.alumnoNombre)}<br><small>${esc(d.alumnoCorreo)}</small></td>
      <td>${esc(d.tipoDocumento)}</td>
      <td>${esc(d.referencia)}</td>
      <td>${insignia(d.estado)}<br><small>${fecha(d.fechaCarga)}</small></td>
      <td><div class="acciones">
        <button type="button" class="secundario" data-accion="ver">Ver archivo</button>
        <input type="text" class="motivo-rechazo" maxlength="300" placeholder="Motivo si rechazas">
        <button type="button" data-accion="aprobar">Aprobar</button>
        <button type="button" class="peligro" data-accion="rechazar">Rechazar</button>
      </div></td>
    </tr>`);

  mostrar(`<h2>${esc(titulo)}</h2>` + (filas.length
    ? `<div class="tabla-envoltura"><table><thead><tr><th>Alumno</th><th>Documento</th><th>Trámite</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>${filas.join("")}</tbody></table></div>`
    : `<p class="vacio">No hay documentos pendientes de revisión.</p>`));

  const cuerpo = $("#contenido tbody");
  if (!cuerpo) return;
  cuerpo.addEventListener("click", async (e) => {
    const boton = e.target.closest("button[data-accion]");
    if (!boton) return;
    const fila = boton.closest("tr");
    const id = Number(fila.dataset.id);
    const accion = boton.dataset.accion;

    if (accion === "ver") {
      const archivo = await intentar(() => api.archivo(`/api/documentos/${id}/archivo`));
      if (archivo) abrirArchivo(archivo);
      return;
    }

    const motivo = $(".motivo-rechazo", fila).value.trim();
    if (accion === "rechazar" && motivo.length < 5) {
      aviso("Escribe el motivo del rechazo (mínimo 5 caracteres).", "error");
      return;
    }
    let listo = false;
    await intentar(async () => {
      await api.post(`/api/documentos/${id}/revision`, { aprobar: accion === "aprobar", motivo });
      listo = true;
    });
    if (listo) {
      aviso(accion === "aprobar" ? "Documento aprobado." : "Documento rechazado.");
      await panelRevision(titulo);
    }
  });
}

// ---------- Buscador de alumnos ----------
/** Dibuja un buscador dentro de `contenedor`. `obtener()` devuelve el alumno elegido ({id, nombre}) o null. */
export function selectorAlumno(contenedor, { alElegir } = {}) {
  contenedor.innerHTML = `
    <label>Buscar alumno (nombre o correo)
      <input type="search" class="buscar-alumno" autocomplete="off" placeholder="Escribe al menos 3 letras">
    </label>
    <ul class="resultados-alumno"></ul>
    <p class="alumno-elegido" hidden></p>`;

  let elegido = null;
  let espera;
  const entrada = $(".buscar-alumno", contenedor);
  const lista = $(".resultados-alumno", contenedor);
  const texto = $(".alumno-elegido", contenedor);

  entrada.addEventListener("input", () => {
    clearTimeout(espera);
    const consulta = entrada.value.trim();
    if (consulta.length < 3) {
      lista.innerHTML = "";
      return;
    }
    espera = setTimeout(async () => {
      const alumnos = await intentar(() => api.get(`/api/alumnos?buscar=${encodeURIComponent(consulta)}`));
      if (!alumnos) return;
      lista.innerHTML = alumnos.length
        ? alumnos.map((a) => `<li><button type="button" class="enlace" data-id="${a.id}" data-nombre="${esc(a.nombreCompleto)}">${esc(a.nombreCompleto)} — ${esc(a.correo)}</button></li>`).join("")
        : "<li class=\"vacio\">Sin resultados.</li>";
    }, 300);
  });

  lista.addEventListener("click", (e) => {
    const boton = e.target.closest("button[data-id]");
    if (!boton) return;
    elegido = { id: Number(boton.dataset.id), nombre: boton.dataset.nombre };
    lista.innerHTML = "";
    entrada.value = "";
    texto.hidden = false;
    texto.textContent = `Alumno seleccionado: ${elegido.nombre}`;
    if (alElegir) alElegir(elegido);
  });

  return { obtener: () => elegido };
}

// ---------- Campañas ----------
export function campanasHtml(campanas) {
  return campanas.length
    ? campanas.map((c) => `<article class="tarjeta"><h3>${esc(c.titulo)}</h3><p>${esc(c.descripcion)}</p>
        <p class="vacio">Del ${esc(c.fechaInicio)} al ${esc(c.fechaFin)}</p></article>`).join("")
    : `<p class="vacio">No hay campañas vigentes.</p>`;
}

export async function vistaCampanas() {
  mostrar(`<h2>Campañas de salud</h2>${campanasHtml(await api.get("/api/campanas"))}`);
}

// ---------- Notificaciones ----------
export async function vistaNotificaciones() {
  const lista = await api.get("/api/notificaciones");
  const filas = lista.map((n) => `<article class="tarjeta" data-id="${n.id}">
      <p>${esc(n.mensaje)} ${n.leida ? "" : '<span class="insignia">Nueva</span>'}</p>
      <small>${fecha(n.fecha)}</small>
      ${n.leida ? "" : '<div class="acciones"><button type="button" class="secundario" data-leida>Marcar como leída</button></div>'}
    </article>`);
  mostrar(`<h2>Notificaciones</h2>${filas.join("") || '<p class="vacio">No tienes notificaciones.</p>'}`);

  $$("#contenido button[data-leida]").forEach((boton) =>
    boton.addEventListener("click", async () => {
      const id = Number(boton.closest("article").dataset.id);
      let listo = false;
      await intentar(async () => {
        await api.post(`/api/notificaciones/${id}/leida`);
        listo = true;
      });
      if (listo) await vistaNotificaciones();
    })
  );
}

// ---------- Reportes ----------
export async function panelReportes() {
  const hoy = new Date();
  mostrar(`<h2>Reportes</h2>
    <form id="form-reporte" class="tarjeta">
      <div class="fila-formulario">
        <label>Periodo
          <select name="periodo"><option value="Mensual">Mensual</option><option value="Semestral">Semestral</option></select>
        </label>
        <label>Año <input type="number" name="anio" min="2020" max="2100" value="${hoy.getFullYear()}" required></label>
        <label><span id="etiqueta-numero">Mes (1-12)</span>
          <input type="number" name="numero" min="1" max="12" value="${hoy.getMonth() + 1}" required></label>
      </div>
      <div class="acciones">
        <button type="submit">Generar</button>
        <button type="button" class="secundario" id="descargar-csv">Descargar CSV</button>
      </div>
    </form>
    <div id="resultado-reporte"></div>`);

  const formulario = $("#form-reporte");
  const consulta = () => {
    const f = formulario.elements;
    return `periodo=${encodeURIComponent(f.periodo.value)}&anio=${f.anio.value}&numero=${f.numero.value}`;
  };

  formulario.elements.periodo.addEventListener("change", () => {
    const semestral = formulario.elements.periodo.value === "Semestral";
    const numero = formulario.elements.numero;
    numero.max = semestral ? 2 : 12;
    numero.value = semestral ? (hoy.getMonth() < 6 ? 1 : 2) : hoy.getMonth() + 1;
    $("#etiqueta-numero").textContent = semestral ? "Semestre (1 o 2)" : "Mes (1-12)";
  });

  formulario.addEventListener("submit", async (e) => {
    e.preventDefault();
    const reporte = await intentar(() => api.get(`/api/reportes?${consulta()}`));
    if (reporte) pintarReporte(reporte);
  });

  $("#descargar-csv").addEventListener("click", async () => {
    const archivo = await intentar(() => api.archivo(`/api/reportes/exportar?${consulta()}`));
    if (archivo) descargarArchivo(archivo, "reporte.csv");
  });

  const inicial = await intentar(() => api.get(`/api/reportes?${consulta()}`));
  if (inicial) pintarReporte(inicial);
}

function pintarReporte(r) {
  const tabla = (titulo, filas) => `<h3>${esc(titulo)}</h3>` + (filas.length
    ? `<div class="tabla-envoltura"><table><thead><tr><th>Concepto</th><th>Cantidad</th></tr></thead><tbody>${filas.map((f) => `<tr><td>${esc(f.etiqueta)}</td><td>${f.cantidad}</td></tr>`).join("")}</tbody></table></div>`
    : `<p class="vacio">Sin registros en este periodo.</p>`);

  $("#resultado-reporte").innerHTML = `
    <p>Periodo: <strong>${esc(r.periodo)}</strong> (${esc(r.desde)} a ${esc(r.hasta)})</p>
    <div class="rejilla">
      <div class="dato">Volumen de atención<strong>${r.volumenAtencion}</strong></div>
      <div class="dato">Consultas<strong>${r.totalConsultas}</strong></div>
      <div class="dato">Lesiones deportivas<strong>${r.lesionesDeportivas}</strong></div>
      <div class="dato">Ausencias por salud<strong>${r.ausenciasPorSalud}</strong></div>
      <div class="dato">Primeros auxilios<strong>${r.totalPrimerosAuxilios}</strong></div>
      <div class="dato">Urgencias<strong>${r.totalUrgencias}</strong></div>
    </div>
    ${tabla("Consultas por motivo", r.consultasPorMotivo)}
    ${tabla("Primeros auxilios por motivo", r.primerosAuxiliosPorMotivo)}
    ${tabla("Urgencias por nivel de triage", r.urgenciasPorTriage)}`;
}
