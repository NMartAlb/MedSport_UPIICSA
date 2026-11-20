import { api } from "../api.js";
import { $, esc, mostrar, aviso, fecha, intentar, insignia } from "../ui.js";
import { panelRevision, panelReportes, selectorAlumno, campanasHtml } from "./comun.js";

const TIPOS_SANGRE = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

// ---------- Valoración médica ----------
async function valoracion() {
  mostrar(`<h2>Valoración médica</h2>
    <div class="tarjeta" id="buscador"></div><div id="detalle"></div>`);

  selectorAlumno($("#buscador"), { alElegir: cargar });

  async function cargar(alumno) {
    await intentar(async () => {
      const lista = await api.get(`/api/servicio-medico/valoraciones/alumno/${alumno.id}`);
      $("#detalle").innerHTML = `
        <form id="form-valoracion" class="tarjeta">
          <h3>Nueva valoración para ${esc(alumno.nombre)}</h3>
          <label>Resultado <input name="resultado" maxlength="500" required></label>
          <label>Observaciones <textarea name="observaciones" maxlength="1000"></textarea></label>
          <button type="submit">Guardar valoración</button>
        </form>
        <h3>Valoraciones anteriores</h3>
        ${lista.map((v) => `<article class="tarjeta"><strong>${fecha(v.fecha)}</strong>
            <p>${esc(v.resultado)}</p>${v.observaciones ? `<p>${esc(v.observaciones)}</p>` : ""}</article>`).join("") || '<p class="vacio">Sin valoraciones registradas.</p>'}`;

      $("#form-valoracion").addEventListener("submit", async (e) => {
        e.preventDefault();
        const f = e.target.elements;
        let listo = false;
        await intentar(async () => {
          await api.post("/api/servicio-medico/valoraciones", {
            alumnoId: alumno.id, resultado: f.resultado.value, observaciones: f.observaciones.value,
          });
          listo = true;
        });
        if (listo) {
          aviso("Valoración guardada. Se notificó al alumno.");
          await cargar(alumno);
        }
      });
    });
  }
}

// ---------- Expediente clínico ----------
function parsearEntregas(texto) {
  return texto.split("\n").map((l) => l.trim()).filter(Boolean).map((linea) => {
    const m = linea.match(/^(.*?)\s*[x×]\s*(\d+)$/i);
    return m ? { descripcion: m[1].trim(), cantidad: Number(m[2]) } : { descripcion: linea, cantidad: 1 };
  });
}

async function expediente() {
  mostrar(`<h2>Expediente clínico</h2>
    <p class="vacio">Cada consulta o cambio queda registrado en la bitácora de accesos.</p>
    <div class="tarjeta" id="buscador"></div><div id="detalle"></div>`);

  selectorAlumno($("#buscador"), { alElegir: cargar });

  async function cargar(alumno) {
    await intentar(async () => {
      const e = await api.get(`/api/servicio-medico/expedientes/${alumno.id}`);
      $("#detalle").innerHTML = `
        <form id="form-expediente" class="tarjeta">
          <h3>Expediente de ${esc(e.alumnoNombre)}</h3>
          <label>Tipo de sangre
            <select name="sangre"><option value="">Sin dato</option>
              ${TIPOS_SANGRE.map((t) => `<option value="${t}" ${e.tipoSangre === t ? "selected" : ""}>${t}</option>`).join("")}
            </select></label>
          <label>Antecedentes alérgicos <textarea name="alergicos" maxlength="1000">${esc(e.antecedentesAlergicos)}</textarea></label>
          <label>Antecedentes crónicos <textarea name="cronicos" maxlength="1000">${esc(e.antecedentesCronicos)}</textarea></label>
          <label>Antecedentes patológicos <textarea name="patologicos" maxlength="1000">${esc(e.antecedentesPatologicos)}</textarea></label>
          <button type="submit">Guardar expediente</button>
        </form>

        <form id="form-consulta" class="tarjeta">
          <h3>Registrar consulta</h3>
          <label>Motivo <input name="motivo" maxlength="200" required></label>
          <label>Diagnóstico <textarea name="diagnostico" maxlength="1000"></textarea></label>
          <label class="opcion"><input type="checkbox" name="lesion"> Es una lesión deportiva</label>
          <label class="opcion"><input type="checkbox" name="ausencia"> Genera ausencia por salud</label>
          <label>Medicamentos o insumos entregados (uno por línea, por ejemplo: Paracetamol 500 mg x 2)
            <textarea name="entregas"></textarea></label>
          <button type="submit">Guardar consulta</button>
        </form>

        <h3>Historial de consultas</h3>
        ${e.consultas.map((c) => `<article class="tarjeta"><strong>${fecha(c.fecha)}</strong> — ${esc(c.motivo)}
            ${c.esLesionDeportiva ? insignia("Pendiente").replace("Pendiente", "Lesión deportiva") : ""}
            ${c.diagnostico ? `<p>${esc(c.diagnostico)}</p>` : ""}
            ${c.entregas.length ? `<ul>${c.entregas.map((x) => `<li>${esc(x.descripcion)} × ${x.cantidad}</li>`).join("")}</ul>` : ""}
          </article>`).join("") || '<p class="vacio">Sin consultas registradas.</p>'}`;

      $("#form-expediente").addEventListener("submit", async (ev) => {
        ev.preventDefault();
        const f = ev.target.elements;
        let listo = false;
        await intentar(async () => {
          await api.put(`/api/servicio-medico/expedientes/${alumno.id}`, {
            tipoSangre: f.sangre.value || null,
            antecedentesAlergicos: f.alergicos.value,
            antecedentesCronicos: f.cronicos.value,
            antecedentesPatologicos: f.patologicos.value,
          });
          listo = true;
        });
        if (listo) {
          aviso("Expediente guardado.");
          await cargar(alumno);
        }
      });

      $("#form-consulta").addEventListener("submit", async (ev) => {
        ev.preventDefault();
        const f = ev.target.elements;
        let listo = false;
        await intentar(async () => {
          await api.post(`/api/servicio-medico/expedientes/${alumno.id}/consultas`, {
            motivo: f.motivo.value,
            diagnostico: f.diagnostico.value,
            esLesionDeportiva: f.lesion.checked,
            generaAusencia: f.ausencia.checked,
            entregas: parsearEntregas(f.entregas.value),
          });
          listo = true;
        });
        if (listo) {
          aviso("Consulta registrada.");
          await cargar(alumno);
        }
      });
    });
  }
}

// ---------- Primeros auxilios ----------
async function primerosAuxilios() {
  const lista = await api.get("/api/servicio-medico/primeros-auxilios");
  mostrar(`<h2>Bitácora de primeros auxilios</h2>
    <form id="form-pa" class="tarjeta">
      <div id="buscador"></div>
      <p class="vacio">Elegir un alumno es opcional.</p>
      <div class="fila-formulario">
        <label>Lugar <input name="lugar" maxlength="100" required></label>
        <label>Motivo <input name="motivo" maxlength="200" required></label>
      </div>
      <label>Atención brindada <textarea name="atencion" maxlength="500" required></textarea></label>
      <button type="submit">Registrar</button>
    </form>
    <div class="tabla-envoltura"><table><thead><tr><th>Fecha</th><th>Lugar</th><th>Motivo</th><th>Atención</th></tr></thead><tbody>
      ${lista.map((a) => `<tr><td>${fecha(a.fecha)}</td><td>${esc(a.lugar)}</td><td>${esc(a.motivo)}</td><td>${esc(a.atencionBrindada)}</td></tr>`).join("")}
    </tbody></table></div>`);

  const buscador = selectorAlumno($("#buscador"));
  $("#form-pa").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target.elements;
    let listo = false;
    await intentar(async () => {
      await api.post("/api/servicio-medico/primeros-auxilios", {
        lugar: f.lugar.value, motivo: f.motivo.value, atencionBrindada: f.atencion.value,
        alumnoId: buscador.obtener()?.id ?? null,
      });
      listo = true;
    });
    if (listo) {
      aviso("Atención registrada.");
      await primerosAuxilios();
    }
  });
}

// ---------- Urgencias ----------
async function urgencias() {
  const lista = await api.get("/api/servicio-medico/urgencias");
  mostrar(`<h2>Urgencias</h2>
    <form id="form-urgencia" class="tarjeta">
      <div id="buscador"></div>
      <p class="vacio">Elegir un alumno es opcional.</p>
      <div class="fila-formulario">
        <label>Nivel de triage
          <select name="triage"><option value="Verde">Verde (no urgente)</option><option value="Amarillo">Amarillo (urgente)</option><option value="Rojo">Rojo (emergencia)</option></select></label>
        <label>Derivación a hospital (si aplica) <input name="derivacion" maxlength="200"></label>
      </div>
      <label>Diagnóstico preliminar <textarea name="diagnostico" maxlength="500" required></textarea></label>
      <button type="submit">Registrar urgencia</button>
    </form>
    <div class="tabla-envoltura"><table><thead><tr><th>Fecha</th><th>Triage</th><th>Diagnóstico preliminar</th><th>Derivación</th></tr></thead><tbody>
      ${lista.map((u) => `<tr><td>${fecha(u.fecha)}</td><td>${esc(u.triage)}</td><td>${esc(u.diagnosticoPreliminar)}</td><td>${esc(u.derivacion ?? "—")}</td></tr>`).join("")}
    </tbody></table></div>`);

  const buscador = selectorAlumno($("#buscador"));
  $("#form-urgencia").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target.elements;
    let listo = false;
    await intentar(async () => {
      await api.post("/api/servicio-medico/urgencias", {
        triage: f.triage.value, diagnosticoPreliminar: f.diagnostico.value,
        derivacion: f.derivacion.value || null, alumnoId: buscador.obtener()?.id ?? null,
      });
      listo = true;
    });
    if (listo) {
      aviso("Urgencia registrada.");
      await urgencias();
    }
  });
}

// ---------- Campañas ----------
async function campanas() {
  const hoy = new Date().toISOString().slice(0, 10);
  const lista = await api.get("/api/campanas");
  mostrar(`<h2>Campañas de salud</h2>
    <form id="form-campana" class="tarjeta">
      <label>Título <input name="titulo" maxlength="100" required></label>
      <label>Descripción <textarea name="descripcion" maxlength="1000" required></textarea></label>
      <div class="fila-formulario">
        <label>Inicio <input type="date" name="inicio" value="${hoy}" required></label>
        <label>Fin <input type="date" name="fin" value="${hoy}" required></label>
      </div>
      <button type="submit">Publicar campaña</button>
    </form>
    <h3>Campañas vigentes</h3>${campanasHtml(lista)}`);

  $("#form-campana").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target.elements;
    let listo = false;
    await intentar(async () => {
      await api.post("/api/campanas", {
        titulo: f.titulo.value, descripcion: f.descripcion.value, fechaInicio: f.inicio.value, fechaFin: f.fin.value,
      });
      listo = true;
    });
    if (listo) {
      aviso("Campaña publicada.");
      await campanas();
    }
  });
}

export const secciones = [
  { id: "documentos", titulo: "Documentos de nuevo ingreso", mostrar: () => panelRevision("Documentos de nuevo ingreso") },
  { id: "valoracion", titulo: "Valoración médica", mostrar: valoracion },
  { id: "expediente", titulo: "Expediente", mostrar: expediente },
  { id: "auxilios", titulo: "Primeros auxilios", mostrar: primerosAuxilios },
  { id: "urgencias", titulo: "Urgencias", mostrar: urgencias },
  { id: "campanas", titulo: "Campañas", mostrar: campanas },
  { id: "reportes", titulo: "Reportes", mostrar: panelReportes },
];
