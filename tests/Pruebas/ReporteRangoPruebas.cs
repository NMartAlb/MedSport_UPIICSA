using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Reportes;
using Xunit;

namespace ServiciosEstudiantiles.Pruebas;

public class ReporteRangoPruebas
{
    [Fact]
    public void Mensual_cubre_del_primer_al_ultimo_dia_del_mes()
    {
        var (desde, hasta, _) = ReporteServicio.CalcularRango(PeriodoReporte.Mensual, 2026, 11);
        Assert.Equal(new DateOnly(2026, 11, 1), desde);
        Assert.Equal(new DateOnly(2026, 11, 30), hasta);
    }

    [Fact]
    public void Semestral_2_cubre_de_julio_a_diciembre()
    {
        var (desde, hasta, _) = ReporteServicio.CalcularRango(PeriodoReporte.Semestral, 2026, 2);
        Assert.Equal(new DateOnly(2026, 7, 1), desde);
        Assert.Equal(new DateOnly(2026, 12, 31), hasta);
    }

    [Fact]
    public void Un_mes_invalido_se_rechaza()
    {
        Assert.Throws<ReglaNegocioException>(() => ReporteServicio.CalcularRango(PeriodoReporte.Mensual, 2026, 13));
    }
}
