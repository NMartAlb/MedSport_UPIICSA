using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.ServicioMedico;

public enum NivelTriage { Verde = 1, Amarillo = 2, Rojo = 3 }

public class TramiteNuevoIngreso : Entidad
{
    public int AlumnoId { get; set; }
    public DateTimeOffset Fecha { get; set; }
}

public class Valoracion : Entidad
{
    public int AlumnoId { get; set; }
    public int MedicoId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class ExpedienteClinico : Entidad
{
    public int AlumnoId { get; set; }
    public string? TipoSangre { get; set; }
    public string? AntecedentesAlergicos { get; set; }
    public string? AntecedentesCronicos { get; set; }
    public string? AntecedentesPatologicos { get; set; }
}

public class EntregaInsumo
{
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class Consulta : Entidad
{
    public int AlumnoId { get; set; }
    public int MedicoId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public bool EsLesionDeportiva { get; set; }
    public bool GeneraAusencia { get; set; }
    public List<EntregaInsumo> Entregas { get; set; } = new();
}

public class AtencionPrimerosAuxilios : Entidad
{
    public DateTimeOffset Fecha { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string AtencionBrindada { get; set; } = string.Empty;
    public int? AlumnoId { get; set; }
    public int MedicoId { get; set; }
}

public class Urgencia : Entidad
{
    public DateTimeOffset Fecha { get; set; }
    public int? AlumnoId { get; set; }
    public int MedicoId { get; set; }
    public NivelTriage Triage { get; set; }
    public string DiagnosticoPreliminar { get; set; } = string.Empty;
    public string? Derivacion { get; set; }
}

public class Campana : Entidad
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int CreadaPorId { get; set; }
}
