using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServiciosEstudiantiles.Aplicacion.Auth;

namespace ServiciosEstudiantiles.Api.Controllers;

/// Registro con código por correo, restablecimiento de contraseña e inicio de sesión.
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class AutenticacionController : ApiControladorBase
{
    private readonly AutenticacionServicio _servicio;

    public AutenticacionController(AutenticacionServicio servicio) => _servicio = servicio;

    [HttpPost("solicitar-codigo")]
    public async Task<IActionResult> SolicitarCodigo(SolicitarCodigoSolicitud solicitud)
    {
        await _servicio.SolicitarCodigoAsync(solicitud.Correo);
        return Ok(new { mensaje = "Si el correo es válido, recibirás un código en unos minutos." });
    }

    [HttpPost("verificar-codigo")]
    public async Task<IActionResult> VerificarCodigo(VerificarCodigoSolicitud solicitud)
    {
        var token = await _servicio.VerificarCodigoAsync(solicitud.Correo, solicitud.Codigo);
        return Ok(new { tokenRegistro = token });
    }

    [HttpPost("crear-contrasena")]
    public async Task<IActionResult> CrearContrasena(CrearContrasenaSolicitud solicitud)
    {
        await _servicio.CrearContrasenaAsync(solicitud);
        return Ok(new { mensaje = "Contraseña guardada. Ya puedes iniciar sesión." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginRespuesta>> Login(LoginSolicitud solicitud) =>
        Ok(await _servicio.LoginAsync(solicitud));
}
