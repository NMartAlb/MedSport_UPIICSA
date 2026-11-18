namespace ServiciosEstudiantiles.Aplicacion.Actividades;

public record ActividadDto(
    int Id, string Nombre, string Tipo, string Horario,
    string Coach, int CupoMaximo, int CupoDisponible);