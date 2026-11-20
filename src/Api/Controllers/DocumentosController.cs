using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Dominio.Documentos;

namespace ServiciosEstudiantiles.Api.Controllers;

/// Datos que llegan del formulario al subir un documento.
public class CargarDocumentoFormulario
{
    public ContextoTramite Contexto { get; set; }
    public int ReferenciaId { get; set; }
    public int TipoDocumentoId { get; set; }
    public IFormFile? Archivo { get; set; }
}

[Route("api/documentos")]
[Authorize]
public class DocumentosController : ApiControladorBase
{
    private readonly DocumentoServicio _servicio;

    public DocumentosController(DocumentoServicio servicio) => _servicio = servicio;

    [HttpGet("tipos")]
    public async Task<ActionResult<IReadOnlyList<TipoDocumentoDto>>> Tipos([FromQuery] ContextoTramite? contexto) =>
        Ok(await _servicio.TiposAsync(contexto));

    [HttpPost]
    [Authorize(Roles = "Alumno")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentoCargadoDto>> Cargar([FromForm] CargarDocumentoFormulario formulario)
    {
        if (formulario.Archivo is null || formulario.Archivo.Length == 0)
            return BadRequest(new { error = "Selecciona un archivo." });

        await using var flujo = formulario.Archivo.OpenReadStream();
        var solicitud = new CargarDocumentoSolicitud(formulario.Contexto, formulario.ReferenciaId,
            formulario.TipoDocumentoId, formulario.Archivo.FileName, flujo);
        return Ok(await _servicio.CargarAsync(Actual, solicitud));
    }

    [HttpGet("pendientes")]
    [Authorize(Roles = "Coach,PersonalMedico")]
    public async Task<ActionResult<IReadOnlyList<DocumentoRevisionDto>>> Pendientes() =>
        Ok(await _servicio.PendientesRevisionAsync(Actual));

    [HttpPost("{id:int}/revision")]
    [Authorize(Roles = "Coach,PersonalMedico")]
    public async Task<IActionResult> Revisar(int id, RevisarDocumentoSolicitud solicitud)
    {
        await _servicio.RevisarAsync(Actual, id, solicitud);
        return NoContent();
    }

    [HttpGet("{id:int}/archivo")]
    [Authorize(Roles = "Alumno,Coach,PersonalMedico")]
    public async Task<IActionResult> Archivo(int id)
    {
        var archivo = await _servicio.AbrirArchivoAsync(Actual, id);
        return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreOriginal);
    }
}
