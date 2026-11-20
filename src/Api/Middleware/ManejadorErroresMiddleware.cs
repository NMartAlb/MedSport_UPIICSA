using ServiciosEstudiantiles.Aplicacion.Comun;

namespace ServiciosEstudiantiles.Api.Middleware;

/// Convierte las excepciones de negocio en respuestas HTTP claras.
/// Los errores inesperados no muestran detalles internos al usuario.
public class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _log;

    public ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> log)
    {
        _siguiente = siguiente;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            if (contexto.Response.HasStarted) throw;

            var (codigo, mensaje) = ex switch
            {
                ReglaNegocioException => (StatusCodes.Status400BadRequest, ex.Message),
                NoEncontradoException => (StatusCodes.Status404NotFound, ex.Message),
                AccesoDenegadoException => (StatusCodes.Status403Forbidden, ex.Message),
                NoAutenticadoException => (StatusCodes.Status401Unauthorized, ex.Message),
                BadHttpRequestException => (StatusCodes.Status400BadRequest, "La solicitud no es válida."),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error interno. Intenta de nuevo.")
            };

            if (codigo == StatusCodes.Status500InternalServerError)
                _log.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);

            contexto.Response.StatusCode = codigo;
            await contexto.Response.WriteAsJsonAsync(new { error = mensaje });
        }
    }
}
