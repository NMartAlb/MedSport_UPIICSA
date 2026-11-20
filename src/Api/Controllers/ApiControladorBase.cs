using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Api.Controllers;

/// Base de los controllers: sabe quién hizo la petición según su token de sesión.
[ApiController]
public abstract class ApiControladorBase : ControllerBase
{
    protected UsuarioActual Actual
    {
        get
        {
            var id = User.FindFirst("sub")?.Value;
            var rol = User.FindFirst("role")?.Value;
            if (!int.TryParse(id, out var usuarioId) || !Enum.TryParse<Rol>(rol, out var rolUsuario))
                throw new NoAutenticadoException("La sesión no es válida.");
            return new UsuarioActual(usuarioId, rolUsuario);
        }
    }
}
