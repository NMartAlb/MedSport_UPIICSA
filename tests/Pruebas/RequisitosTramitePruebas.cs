using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Dominio.Documentos;
using Xunit;

namespace ServiciosEstudiantiles.Pruebas;

public class RequisitosTramitePruebas
{
    private static Documento Doc(EstadoDocumento estado) => new() { Estado = estado };

    [Fact]
    public void Sin_requisitos_el_tramite_esta_completo()
    {
        Assert.Equal(EstadoTramite.Completado, RequisitosTramite.CalcularEstado(new List<Documento?>()));
    }

    [Fact]
    public void Si_falta_un_documento_queda_pendiente()
    {
        var ultimos = new List<Documento?> { Doc(EstadoDocumento.Aprobado), null };
        Assert.Equal(EstadoTramite.PendienteDocumentos, RequisitosTramite.CalcularEstado(ultimos));
    }

    [Fact]
    public void Un_documento_rechazado_hace_que_todo_el_tramite_quede_rechazado()
    {
        var ultimos = new List<Documento?> { Doc(EstadoDocumento.Rechazado), null };
        Assert.Equal(EstadoTramite.Rechazado, RequisitosTramite.CalcularEstado(ultimos));
    }

    [Fact]
    public void Con_todos_aprobados_el_tramite_esta_completo()
    {
        var ultimos = new List<Documento?> { Doc(EstadoDocumento.Aprobado), Doc(EstadoDocumento.Aprobado) };
        Assert.Equal(EstadoTramite.Completado, RequisitosTramite.CalcularEstado(ultimos));
    }

    [Fact]
    public void Si_alguno_esta_en_revision_el_tramite_esta_en_revision()
    {
        var ultimos = new List<Documento?> { Doc(EstadoDocumento.Aprobado), Doc(EstadoDocumento.EnRevision) };
        Assert.Equal(EstadoTramite.EnRevision, RequisitosTramite.CalcularEstado(ultimos));
    }

    [Fact]
    public void Si_todos_estan_recibidos_el_tramite_esta_recibido()
    {
        var ultimos = new List<Documento?> { Doc(EstadoDocumento.Recibido), Doc(EstadoDocumento.Recibido) };
        Assert.Equal(EstadoTramite.Recibido, RequisitosTramite.CalcularEstado(ultimos));
    }
}
