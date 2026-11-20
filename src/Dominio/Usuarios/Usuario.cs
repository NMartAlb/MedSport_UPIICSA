using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Usuarios;

public class Usuario : Entidad
{
    public string Correo { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public Rol Rol { get; set; }
    public string? HashContrasena { get; set; }
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }

    public bool TieneContrasena => !string.IsNullOrEmpty(HashContrasena);
}
