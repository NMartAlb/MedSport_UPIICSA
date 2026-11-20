using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Asistencias;

public class Asistencia : Entidad
{
    public int ActividadId { get; set; }
    public int AlumnoId { get; set; }
    public DateOnly Fecha { get; set; }
    public bool Presente { get; set; }
    public int RegistradoPorId { get; set; }
}
