using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Auditoria;

/// Quién consultó o modificó información sensible, cuándo y qué hizo.
public class RegistroAcceso : Entidad
{
    public int UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
}
