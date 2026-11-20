using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Usuarios;

namespace ServiciosEstudiantiles.Api.Controllers;

[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ApiControladorBase
{
    private readonly UsuarioServicio _servicio;

    public UsuariosController(UsuarioServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar() => Ok(await _servicio.ListarAsync());

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioSolicitud solicitud) =>
        Ok(await _servicio.CrearAsync(solicitud));

    [HttpPut("{id:int}/rol")]
    public async Task<ActionResult<UsuarioDto>> CambiarRol(int id, CambiarRolSolicitud solicitud) =>
        Ok(await _servicio.CambiarRolAsync(Actual, id, solicitud.Rol));

    [HttpPut("{id:int}/activo")]
    public async Task<ActionResult<UsuarioDto>> CambiarActivo(int id, CambiarActivoSolicitud solicitud) =>
        Ok(await _servicio.CambiarActivoAsync(Actual, id, solicitud.Activo));
}

/// Búsqueda de alumnos para el personal médico y los coaches.
[Route("api/alumnos")]
[Authorize(Roles = "Coach,PersonalMedico,Administrador")]
public class AlumnosController : ApiControladorBase
{
    private readonly UsuarioServicio _servicio;

    public AlumnosController(UsuarioServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlumnoDto>>> Buscar([FromQuery] string? buscar) =>
        Ok(await _servicio.BuscarAlumnosAsync(buscar));
}
