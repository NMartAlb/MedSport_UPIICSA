using ServiciosEstudiantiles.Dominio.Documentos;

namespace ServiciosEstudiantiles.Aplicacion.Documentos;

/// Estado de un documento requerido dentro de un trámite. Estado: Pendiente, Recibido, EnRevision, Aprobado o Rechazado
public record RequisitoDto(int TipoDocumentoId, string Nombre, string Estado, int? DocumentoId, string? MotivoRechazo);

/// Calcula el estado de un trámite a partir de los documentos entregados.
public static class RequisitosTramite
{
    public static (EstadoTramite Estado, IReadOnlyList<RequisitoDto> Requisitos) Evaluar(
        IEnumerable<TipoDocumento> requeridos, IEnumerable<Documento> documentos)
    {
        var requisitos = new List<RequisitoDto>();
        var ultimos = new List<Documento?>();
        var entregados = documentos.ToList();

        foreach (var tipo in requeridos)
        {
            // Si el alumno volvió a subir un documento, cuenta el más reciente.
            var ultimo = entregados
                .Where(d => d.TipoDocumentoId == tipo.Id)
                .OrderByDescending(d => d.FechaCarga)
                .ThenByDescending(d => d.Id)
                .FirstOrDefault();

            ultimos.Add(ultimo);
            requisitos.Add(new RequisitoDto(tipo.Id, tipo.Nombre,
                ultimo is null ? "Pendiente" : ultimo.Estado.ToString(),
                ultimo?.Id, ultimo?.MotivoRechazo));
        }
        return (CalcularEstado(ultimos), requisitos);
    }

    /// Recibe el último documento de cada requisito (null si aún no lo entrega).
    public static EstadoTramite CalcularEstado(IReadOnlyCollection<Documento?> ultimosPorRequisito)
    {
        if (ultimosPorRequisito.Count == 0) return EstadoTramite.Completado;
        if (ultimosPorRequisito.Any(d => d != null && d.Estado == EstadoDocumento.Rechazado)) return EstadoTramite.Rechazado;
        if (ultimosPorRequisito.Any(d => d == null)) return EstadoTramite.PendienteDocumentos;
        if (ultimosPorRequisito.All(d => d!.Estado == EstadoDocumento.Aprobado)) return EstadoTramite.Completado;
        if (ultimosPorRequisito.Any(d => d!.Estado == EstadoDocumento.EnRevision)) return EstadoTramite.EnRevision;
        return EstadoTramite.Recibido;
    }
}
