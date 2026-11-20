using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Inscripciones;

public class Inscripcion : Entidad
{
    public int AlumnoId { get; set; }
    public int ActividadId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public bool Cancelada { get; set; }
}
