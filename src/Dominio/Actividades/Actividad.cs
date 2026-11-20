using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Actividades;

public enum TipoActividad { Deportiva = 1, Cultural = 2 }

public class Actividad : Entidad
{
    public string Nombre { get; set; } = string.Empty;
    public TipoActividad Tipo { get; set; }
    public string Horario { get; set; } = string.Empty;
    public int CoachId { get; set; }
    public int CupoMaximo { get; set; }
    public bool Activa { get; set; } = true;

    /// Ids de los tipos de documento que el alumno debe entregar para esta actividad.
    public List<int> TiposDocumentoRequeridos { get; set; } = new();
}
