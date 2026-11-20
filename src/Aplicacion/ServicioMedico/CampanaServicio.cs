using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public record CrearCampanaSolicitud(string Titulo, string Descripcion, DateOnly FechaInicio, DateOnly FechaFin);
public record CampanaDto(int Id, string Titulo, string Descripcion, DateOnly FechaInicio, DateOnly FechaFin);

/// El personal médico publica campañas de salud y los alumnos las consultan (RF20).
public class CampanaServicio
{
    private readonly ICampanaRepositorio _campanas;
    private readonly TimeProvider _reloj;

    public CampanaServicio(ICampanaRepositorio campanas, TimeProvider reloj)
    {
        _campanas = campanas;
        _reloj = reloj;
    }

    public async Task<CampanaDto> CrearAsync(UsuarioActual medico, CrearCampanaSolicitud s)
    {
        if (string.IsNullOrWhiteSpace(s.Titulo) || s.Titulo.Trim().Length > 100)
            throw new ReglaNegocioException("El título es obligatorio (máximo 100 caracteres).");
        if (string.IsNullOrWhiteSpace(s.Descripcion) || s.Descripcion.Trim().Length > 1000)
            throw new ReglaNegocioException("La descripción es obligatoria (máximo 1000 caracteres).");
        if (s.FechaFin < s.FechaInicio)
            throw new ReglaNegocioException("La fecha de fin no puede ser anterior a la de inicio.");

        var campana = await _campanas.AgregarAsync(new Campana
        {
            Titulo = s.Titulo.Trim(),
            Descripcion = s.Descripcion.Trim(),
            FechaInicio = s.FechaInicio,
            FechaFin = s.FechaFin,
            CreadaPorId = medico.Id
        });
        return Mapear(campana);
    }

    /// Campañas vigentes: las que aún no terminan.
    public async Task<IReadOnlyList<CampanaDto>> ListarVigentesAsync()
    {
        var hoy = DateOnly.FromDateTime(_reloj.GetUtcNow().UtcDateTime);
        var campanas = await _campanas.BuscarAsync(c => c.FechaFin >= hoy);
        return campanas.OrderBy(c => c.FechaInicio).Select(Mapear).ToList();
    }

    private static CampanaDto Mapear(Campana c) => new(c.Id, c.Titulo, c.Descripcion, c.FechaInicio, c.FechaFin);
}
