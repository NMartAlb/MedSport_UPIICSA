using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Dominio.Documentos;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Documentos;

public class TipoDocumentoRepositorioEnMemoria : RepositorioEnMemoria<TipoDocumento>, ITipoDocumentoRepositorio
{
    public TipoDocumentoRepositorioEnMemoria() : base(Semilla()) { }

    // Lista provisional: debe confirmarse con la Coordinación y con el Servicio Médico (RF7).
    private static IEnumerable<TipoDocumento> Semilla() => new[]
    {
        new TipoDocumento { Nombre = "Credencial vigente del IPN", Descripcion = "Foto o escaneo legible por ambos lados.", Contexto = ContextoTramite.Actividad },
        new TipoDocumento { Nombre = "Responsiva firmada", Descripcion = "Carta responsiva de la actividad, con firma.", Contexto = ContextoTramite.Actividad },
        new TipoDocumento { Nombre = "Certificado de aptitud física", Descripcion = "Expedido por un médico, con fecha reciente.", Contexto = ContextoTramite.Actividad },
        new TipoDocumento { Nombre = "Certificado médico", Descripcion = "Certificado médico para el nuevo ingreso al servicio médico.", Contexto = ContextoTramite.NuevoIngresoMedico }
    };
}

public class DocumentoRepositorioEnMemoria : RepositorioEnMemoria<Documento>, IDocumentoRepositorio { }
