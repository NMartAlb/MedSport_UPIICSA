namespace ServiciosEstudiantiles.Dominio.Actividades;

public enum TipoActividad { Deportiva = 1, Cultural = 2 }

public class Actividad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoActividad Tipo { get; set; }
    public string Horario { get; set; } = string.Empty;
    public string Coach { get; set; } = string.Empty;
    public int CupoMaximo { get; set; }
    public int Inscritos { get; set; } // temporal: luego saldrá de las inscripciones
    public int CupoDisponible => CupoMaximo - Inscritos;
}
