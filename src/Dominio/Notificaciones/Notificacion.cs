using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Notificaciones;

public class Notificacion : Entidad
{
    public int UsuarioId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
    public bool Leida { get; set; }
}
