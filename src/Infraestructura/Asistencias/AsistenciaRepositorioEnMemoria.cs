using ServiciosEstudiantiles.Aplicacion.Asistencias;
using ServiciosEstudiantiles.Dominio.Asistencias;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Asistencias;

public class AsistenciaRepositorioEnMemoria : RepositorioEnMemoria<Asistencia>, IAsistenciaRepositorio { }
