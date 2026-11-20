using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Aplicacion.Reportes;

namespace ServiciosEstudiantiles.Api.Controllers;

[Route("api/reportes")]
[Authorize(Roles = "PersonalMedico,Directivo")]
public class ReportesController : ApiControladorBase
{
    private readonly ReporteServicio _servicio;

    public ReportesController(ReporteServicio servicio) => _servicio = servicio;

    /// Si no se indica año o número, se usa el mes o semestre actual.
    [HttpGet]
    public async Task<ActionResult<ReporteDto>> Generar([FromQuery] PeriodoReporte periodo = PeriodoReporte.Mensual,
        [FromQuery] int anio = 0, [FromQuery] int numero = 0)
    {
        (anio, numero) = Completar(periodo, anio, numero);
        return Ok(await _servicio.GenerarAsync(periodo, anio, numero));
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] PeriodoReporte periodo = PeriodoReporte.Mensual,
        [FromQuery] int anio = 0, [FromQuery] int numero = 0)
    {
        (anio, numero) = Completar(periodo, anio, numero);
        var csv = await _servicio.ExportarCsvAsync(periodo, anio, numero);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"reporte-{periodo}-{anio}-{numero}.csv");
    }

    private static (int Anio, int Numero) Completar(PeriodoReporte periodo, int anio, int numero)
    {
        var hoy = DateTime.UtcNow;
        if (anio == 0) anio = hoy.Year;
        if (numero == 0) numero = periodo == PeriodoReporte.Mensual ? hoy.Month : (hoy.Month <= 6 ? 1 : 2);
        return (anio, numero);
    }
}

/// Bitácora de accesos a información sensible (solo administrador).
[Route("api/auditoria")]
[Authorize(Roles = "Administrador")]
public class AuditoriaController : ApiControladorBase
{
    private readonly AuditoriaServicio _servicio;

    public AuditoriaController(AuditoriaServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegistroAccesoDto>>> Listar() => Ok(await _servicio.ListarAsync());
}
