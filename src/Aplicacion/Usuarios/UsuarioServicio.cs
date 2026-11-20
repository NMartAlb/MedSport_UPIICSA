using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Usuarios;

namespace ServiciosEstudiantiles.Aplicacion.Usuarios;

public record UsuarioDto(int Id, string Correo, string NombreCompleto, string Rol, bool Activo);
public record AlumnoDto(int Id, string Correo, string NombreCompleto);
public record CrearUsuarioSolicitud(string Correo, string NombreCompleto, Rol Rol);
public record CambiarRolSolicitud(Rol Rol);
public record CambiarActivoSolicitud(bool Activo);

public static class UsuarioRepositorioExtensiones
{
    /// <summary>Devuelve al alumno o falla si no existe o no es alumno.</summary>
    public static async Task<Usuario> ObtenerAlumnoAsync(this IUsuarioRepositorio repo, int alumnoId)
    {
        var usuario = await repo.ObtenerAsync(alumnoId);
        if (usuario is null || usuario.Rol != Rol.Alumno)
            throw new NoEncontradoException("Alumno no encontrado.");
        return usuario;
    }
}

/// <summary>Administración de usuarios y roles (solo el Administrador).</summary>
public class UsuarioServicio
{
    private readonly IUsuarioRepositorio _usuarios;

    public UsuarioServicio(IUsuarioRepositorio usuarios) => _usuarios = usuarios;

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync() =>
        (await _usuarios.ListarAsync()).OrderBy(u => u.Correo).Select(Mapear).ToList();

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioSolicitud solicitud)
    {
        if (!ValidadorCorreoInstitucional.EsValido(solicitud.Correo, out var correo))
            throw new ReglaNegocioException("Solo se permiten correos institucionales del dominio ipn.mx.");
        if (string.IsNullOrWhiteSpace(solicitud.NombreCompleto) || solicitud.NombreCompleto.Length > 120)
            throw new ReglaNegocioException("El nombre es obligatorio (máximo 120 caracteres).");
        if (!Enum.IsDefined(solicitud.Rol))
            throw new ReglaNegocioException("Rol no válido.");
        if ((await _usuarios.BuscarAsync(u => u.Correo == correo)).Count > 0)
            throw new ReglaNegocioException("Ya existe un usuario con ese correo.");

        // Se crea sin contraseña: la persona la crea después con el código que llega a su correo.
        var nuevo = await _usuarios.AgregarAsync(new Usuario
        {
            Correo = correo,
            NombreCompleto = solicitud.NombreCompleto.Trim(),
            Rol = solicitud.Rol
        });
        return Mapear(nuevo);
    }

    public async Task<UsuarioDto> CambiarRolAsync(UsuarioActual quien, int id, Rol rol)
    {
        if (!Enum.IsDefined(rol)) throw new ReglaNegocioException("Rol no válido.");
        if (quien.Id == id) throw new ReglaNegocioException("No puedes cambiar tu propio rol.");
        var usuario = await _usuarios.ObtenerAsync(id) ?? throw new NoEncontradoException("Usuario no encontrado.");
        usuario.Rol = rol;
        await _usuarios.ActualizarAsync(usuario);
        return Mapear(usuario);
    }

    public async Task<UsuarioDto> CambiarActivoAsync(UsuarioActual quien, int id, bool activo)
    {
        if (quien.Id == id && !activo) throw new ReglaNegocioException("No puedes desactivar tu propia cuenta.");
        var usuario = await _usuarios.ObtenerAsync(id) ?? throw new NoEncontradoException("Usuario no encontrado.");
        usuario.Activo = activo;
        await _usuarios.ActualizarAsync(usuario);
        return Mapear(usuario);
    }

    /// <summary>Búsqueda de alumnos por nombre o correo (para coaches y personal médico).</summary>
    public async Task<IReadOnlyList<AlumnoDto>> BuscarAlumnosAsync(string? texto)
    {
        var t = (texto ?? string.Empty).Trim().ToLowerInvariant();
        if (t.Length < 3) throw new ReglaNegocioException("Escribe al menos 3 caracteres para buscar.");
        var alumnos = await _usuarios.BuscarAsync(u => u.Rol == Rol.Alumno && u.Activo);
        return alumnos
            .Where(u => u.Correo.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        u.NombreCompleto.Contains(t, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(u => new AlumnoDto(u.Id, u.Correo, u.NombreCompleto))
            .ToList();
    }

    private static UsuarioDto Mapear(Usuario u) => new(u.Id, u.Correo, u.NombreCompleto, u.Rol.ToString(), u.Activo);
}
