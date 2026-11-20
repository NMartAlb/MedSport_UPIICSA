using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Dominio.Documentos;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public record TramiteNuevoIngresoDto(int Id, DateTimeOffset Fecha, string EstadoTramite,
    IReadOnlyList<RequisitoDto> Requisitos);

/// El alumno inicia su trámite de nuevo ingreso y carga sus documentos antes de ir al servicio médico (RF13).
public class NuevoIngresoServicio
{
    private readonly ITramiteNuevoIngresoRepositorio _tramites;
    private readonly ITipoDocumentoRepositorio _tipos;
    private readonly IDocumentoRepositorio _documentos;
    private readonly TimeProvider _reloj;

    public NuevoIngresoServicio(ITramiteNuevoIngresoRepositorio tramites, ITipoDocumentoRepositorio tipos,
        IDocumentoRepositorio documentos, TimeProvider reloj)
    {
        _tramites = tramites;
        _tipos = tipos;
        _documentos = documentos;
        _reloj = reloj;
    }

    /// Si el alumno ya inició su trámite, devuelve el mismo.
    public async Task<TramiteNuevoIngresoDto> IniciarAsync(UsuarioActual alumno)
    {
        var tramite = (await _tramites.BuscarAsync(t => t.AlumnoId == alumno.Id)).FirstOrDefault()
                      ?? await _tramites.AgregarAsync(new TramiteNuevoIngreso
                      {
                          AlumnoId = alumno.Id,
                          Fecha = _reloj.GetUtcNow()
                      });
        return await ConstruirAsync(tramite);
    }

    public async Task<TramiteNuevoIngresoDto?> MiTramiteAsync(UsuarioActual alumno)
    {
        var tramite = (await _tramites.BuscarAsync(t => t.AlumnoId == alumno.Id)).FirstOrDefault();
        return tramite is null ? null : await ConstruirAsync(tramite);
    }

    private async Task<TramiteNuevoIngresoDto> ConstruirAsync(TramiteNuevoIngreso tramite)
    {
        var tramiteId = tramite.Id;
        var tipos = await _tipos.BuscarAsync(t => t.Contexto == ContextoTramite.NuevoIngresoMedico);
        var documentos = await _documentos.BuscarAsync(d =>
            d.Contexto == ContextoTramite.NuevoIngresoMedico && d.ReferenciaId == tramiteId);
        var (estado, requisitos) = RequisitosTramite.Evaluar(tipos.OrderBy(t => t.Id), documentos);
        return new TramiteNuevoIngresoDto(tramite.Id, tramite.Fecha, estado.ToString(), requisitos);
    }
}
