using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Dominio.Actividades;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Infraestructura.Actividades;
using ServiciosEstudiantiles.Infraestructura.Documentos;
using ServiciosEstudiantiles.Infraestructura.Inscripciones;
using ServiciosEstudiantiles.Infraestructura.Usuarios;
using Xunit;

namespace ServiciosEstudiantiles.Pruebas;

public class InscripcionServicioPruebas
{
    private static async Task<(InscripcionServicio Servicio, int ActividadId)> PrepararAsync(int cupo)
    {
        var actividades = new ActividadRepositorioEnMemoria();
        var actividad = await actividades.AgregarAsync(new Actividad { Nombre = "Prueba", CupoMaximo = cupo, CoachId = 2 });
        var servicio = new InscripcionServicio(new InscripcionRepositorioEnMemoria(), actividades,
            new UsuarioRepositorioEnMemoria(), new DocumentoRepositorioEnMemoria(),
            new TipoDocumentoRepositorioEnMemoria(), TimeProvider.System);
        return (servicio, actividad.Id);
    }

    [Fact]
    public async Task No_permite_inscribirse_cuando_ya_no_hay_cupo()
    {
        var (servicio, actividadId) = await PrepararAsync(cupo: 1);
        await servicio.InscribirseAsync(new UsuarioActual(10, Rol.Alumno), actividadId);

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.InscribirseAsync(new UsuarioActual(11, Rol.Alumno), actividadId));
        Assert.Contains("cupo", error.Message);
    }

    [Fact]
    public async Task No_permite_inscribirse_dos_veces_a_la_misma_actividad()
    {
        var (servicio, actividadId) = await PrepararAsync(cupo: 5);
        var alumno = new UsuarioActual(10, Rol.Alumno);
        await servicio.InscribirseAsync(alumno, actividadId);

        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.InscribirseAsync(alumno, actividadId));
    }
}
