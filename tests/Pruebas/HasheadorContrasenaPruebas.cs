using ServiciosEstudiantiles.Infraestructura.Seguridad;
using Xunit;

namespace ServiciosEstudiantiles.Pruebas;

public class HasheadorContrasenaPruebas
{
    private readonly HasheadorContrasenaPbkdf2 _hasheador = new();

    [Fact]
    public void Nunca_guarda_la_contrasena_en_claro()
    {
        var hash = _hasheador.Hashear("MiClaveSegura123");
        Assert.DoesNotContain("MiClaveSegura123", hash);
    }

    [Fact]
    public void Verifica_la_contrasena_correcta_y_rechaza_la_incorrecta()
    {
        var hash = _hasheador.Hashear("MiClaveSegura123");
        Assert.True(_hasheador.Verificar("MiClaveSegura123", hash));
        Assert.False(_hasheador.Verificar("OtraClave456", hash));
    }

    [Fact]
    public void La_misma_contrasena_produce_hashes_distintos_por_la_sal()
    {
        Assert.NotEqual(_hasheador.Hashear("MiClaveSegura123"), _hasheador.Hashear("MiClaveSegura123"));
    }

    [Fact]
    public void Un_hash_con_formato_invalido_no_verifica()
    {
        Assert.False(_hasheador.Verificar("cualquiera", "esto-no-es-un-hash"));
    }
}
