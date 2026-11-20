using System.Globalization;
using System.Text;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.Reportes;

public enum PeriodoReporte { Mensual = 1, Semestral = 2 }

public record ConteoDto(string Etiqueta, int Cantidad);

public record ReporteDto(string Periodo, DateOnly Desde, DateOnly Hasta, int VolumenAtencion, int TotalConsultas,
    int LesionesDeportivas, int AusenciasPorSalud, int TotalPrimerosAuxilios, int TotalUrgencias,
    IReadOnlyList<ConteoDto> ConsultasPorMotivo, IReadOnlyList<ConteoDto> PrimerosAuxiliosPorMotivo,
    IReadOnlyList<ConteoDto> UrgenciasPorTriage);

/// Reportes mensuales y semestrales (RF22). Solo contienen conteos: nunca datos de personas (RNF9).
public class ReporteServicio
{
    private readonly IConsultaRepositorio _consultas;
    private readonly IPrimerosAuxiliosRepositorio _primerosAuxilios;
    private readonly IUrgenciaRepositorio _urgencias;

    public ReporteServicio(IConsultaRepositorio consultas, IPrimerosAuxiliosRepositorio primerosAuxilios,
        IUrgenciaRepositorio urgencias)
    {
        _consultas = consultas;
        _primerosAuxilios = primerosAuxilios;
        _urgencias = urgencias;
    }

    public async Task<ReporteDto> GenerarAsync(PeriodoReporte periodo, int anio, int numero)
    {
        var (desde, hasta, etiqueta) = CalcularRango(periodo, anio, numero);
        var desdeDt = new DateTimeOffset(desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var hastaDt = new DateTimeOffset(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var consultas = await _consultas.BuscarAsync(c => c.Fecha >= desdeDt && c.Fecha < hastaDt);
        var auxilios = await _primerosAuxilios.BuscarAsync(a => a.Fecha >= desdeDt && a.Fecha < hastaDt);
        var urgencias = await _urgencias.BuscarAsync(u => u.Fecha >= desdeDt && u.Fecha < hastaDt);

        return new ReporteDto(etiqueta, desde, hasta,
            consultas.Count + auxilios.Count + urgencias.Count,
            consultas.Count,
            consultas.Count(c => c.EsLesionDeportiva),
            consultas.Count(c => c.GeneraAusencia),
            auxilios.Count,
            urgencias.Count,
            Agrupar(consultas.Select(c => c.Motivo)),
            Agrupar(auxilios.Select(a => a.Motivo)),
            Agrupar(urgencias.Select(u => u.Triage.ToString())));
    }

    public async Task<string> ExportarCsvAsync(PeriodoReporte periodo, int anio, int numero)
    {
        var r = await GenerarAsync(periodo, anio, numero);
        var sb = new StringBuilder();
        sb.AppendLine("Seccion,Etiqueta,Cantidad");
        Fila(sb, "Periodo", r.Periodo, r.VolumenAtencion);
        Fila(sb, "Resumen", "Total de consultas", r.TotalConsultas);
        Fila(sb, "Resumen", "Lesiones deportivas", r.LesionesDeportivas);
        Fila(sb, "Resumen", "Ausencias por salud", r.AusenciasPorSalud);
        Fila(sb, "Resumen", "Primeros auxilios", r.TotalPrimerosAuxilios);
        Fila(sb, "Resumen", "Urgencias", r.TotalUrgencias);
        foreach (var c in r.ConsultasPorMotivo) Fila(sb, "Consultas por motivo", c.Etiqueta, c.Cantidad);
        foreach (var c in r.PrimerosAuxiliosPorMotivo) Fila(sb, "Primeros auxilios por motivo", c.Etiqueta, c.Cantidad);
        foreach (var c in r.UrgenciasPorTriage) Fila(sb, "Urgencias por triage", c.Etiqueta, c.Cantidad);
        return sb.ToString();
    }

    public static (DateOnly Desde, DateOnly Hasta, string Etiqueta) CalcularRango(PeriodoReporte periodo, int anio, int numero)
    {
        if (anio is < 2020 or > 2100) throw new ReglaNegocioException("El año no es válido.");

        if (periodo == PeriodoReporte.Mensual)
        {
            if (numero is < 1 or > 12) throw new ReglaNegocioException("El mes debe estar entre 1 y 12.");
            var desde = new DateOnly(anio, numero, 1);
            return (desde, desde.AddMonths(1).AddDays(-1), $"{anio}-{numero:00}");
        }

        if (periodo == PeriodoReporte.Semestral)
        {
            if (numero is < 1 or > 2) throw new ReglaNegocioException("El semestre debe ser 1 o 2.");
            return numero == 1
                ? (new DateOnly(anio, 1, 1), new DateOnly(anio, 6, 30), $"{anio} semestre 1")
                : (new DateOnly(anio, 7, 1), new DateOnly(anio, 12, 31), $"{anio} semestre 2");
        }

        throw new ReglaNegocioException("El periodo no es válido.");
    }

    private static IReadOnlyList<ConteoDto> Agrupar(IEnumerable<string> textos) =>
        textos.Select(t => t.Trim())
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ConteoDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Cantidad)
            .ThenBy(c => c.Etiqueta)
            .ToList();

    private static void Fila(StringBuilder sb, string seccion, string etiqueta, int cantidad) =>
        sb.AppendLine(string.Join(',', EscaparCsv(seccion), EscaparCsv(etiqueta), cantidad.ToString(CultureInfo.InvariantCulture)));

    /// Entrecomilla el texto y neutraliza fórmulas (=, +, -, @) para que Excel no las ejecute.
    private static string EscaparCsv(string texto)
    {
        if (texto.Length > 0 && texto[0] is '=' or '+' or '-' or '@') texto = "'" + texto;
        return "\"" + texto.Replace("\"", "\"\"") + "\"";
    }
}
