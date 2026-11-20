using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Documentos;

/// A qué trámite pertenece un documento.
public enum ContextoTramite { Actividad = 1, NuevoIngresoMedico = 2 }

public enum EstadoDocumento { Recibido = 1, EnRevision = 2, Aprobado = 3, Rechazado = 4 }

/// Estado general de un trámite, calculado a partir de sus documentos.
public enum EstadoTramite { PendienteDocumentos = 1, Recibido = 2, EnRevision = 3, Rechazado = 4, Completado = 5 }

public class TipoDocumento : Entidad
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ContextoTramite Contexto { get; set; }
}

public class Documento : Entidad
{
    public int AlumnoId { get; set; }
    public int TipoDocumentoId { get; set; }
    public ContextoTramite Contexto { get; set; }

    /// Id de la inscripción (Actividad) o del trámite de nuevo ingreso (NuevoIngresoMedico).
    public int ReferenciaId { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;
    public string ArchivoAlmacenado { get; set; } = string.Empty;
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Recibido;
    public string? MotivoRechazo { get; set; }
    public int? RevisadoPorId { get; set; }
    public DateTimeOffset FechaCarga { get; set; }
    public DateTimeOffset? FechaRevision { get; set; }
}
