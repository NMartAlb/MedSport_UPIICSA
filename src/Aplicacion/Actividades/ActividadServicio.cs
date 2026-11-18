using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Aplicacion.Actividades;

public class ActividadServicio
{
    private readonly IActividadRepositorio _repositorio;

    public ActividadServicio(IActividadRepositorio repositorio) => _repositorio = repositorio;

    public async Task<IReadOnlyList<ActividadDto>> ListarAsync(TipoActividad? tipo)
    {
        var actividades = await _repositorio.ListarAsync(tipo);
        return actividades
            .Select(a => new ActividadDto(a.Id, a.Nombre, a.Tipo.ToString(),
                a.Horario, a.Coach, a.CupoMaximo, a.CupoDisponible))
            .ToList();
    }
}