using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Aplicacion.Actividades;

public record ActividadDto(
    int Id, string Nombre, string Tipo, string Horario,
    int CoachId, string Coach, int CupoMaximo, int CupoDisponible,
    IReadOnlyList<int> TiposDocumentoRequeridos);

public record GuardarActividadSolicitud(
    string Nombre, TipoActividad Tipo, string Horario, int CupoMaximo,
    List<int>? TiposDocumentoRequeridos, int? CoachId);
