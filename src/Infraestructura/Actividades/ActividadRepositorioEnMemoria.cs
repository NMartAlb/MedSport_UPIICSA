using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Dominio.Actividades;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Actividades;

public class ActividadRepositorioEnMemoria : RepositorioEnMemoria<Actividad>, IActividadRepositorio
{
    public ActividadRepositorioEnMemoria() : base(Semilla()) { }

    // CoachId = 2 es el coach de prueba. Los documentos requeridos 1, 2 y 3 son los tipos de prueba.
    private static IEnumerable<Actividad> Semilla() => new[]
    {
        new Actividad { Nombre = "Básquetbol", Tipo = TipoActividad.Deportiva, Horario = "Lun y Mié 14:00",
            CoachId = 2, CupoMaximo = 30, TiposDocumentoRequeridos = new List<int> { 1, 2, 3 } },
        new Actividad { Nombre = "Taekwondo", Tipo = TipoActividad.Deportiva, Horario = "Mar y Jue 16:00",
            CoachId = 2, CupoMaximo = 25, TiposDocumentoRequeridos = new List<int> { 1, 2, 3 } },
        new Actividad { Nombre = "Teatro", Tipo = TipoActividad.Cultural, Horario = "Vie 15:00",
            CoachId = 2, CupoMaximo = 20, TiposDocumentoRequeridos = new List<int> { 1, 2 } }
    };
}
