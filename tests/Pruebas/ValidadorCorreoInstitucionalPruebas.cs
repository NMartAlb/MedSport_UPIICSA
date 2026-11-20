using ServiciosEstudiantiles.Aplicacion.Auth;
using Xunit;

namespace ServiciosEstudiantiles.Pruebas;

public class ValidadorCorreoInstitucionalPruebas
{
    [Theory]
    [InlineData("alumno@alumno.ipn.mx")]
    [InlineData("coach@ipn.mx")]
    [InlineData("a.b-c+d@upiicsa.ipn.mx")]
    public void Acepta_correos_del_dominio_ipn(string correo)
    {
        Assert.True(ValidadorCorreoInstitucional.EsValido(correo, out _));
    }

    [Theory]
    [InlineData("alguien@gmail.com")]
    [InlineData("alguien@ipn.mx.evil.com")]
    [InlineData("alguien@evilipn.mx")]
    [InlineData("alguien@@ipn.mx")]
    [InlineData("alguien@.ipn.mx")]
    [InlineData("a b@ipn.mx")]
    [InlineData("sin-arroba")]
    [InlineData("")]
    public void Rechaza_correos_que_no_son_del_dominio_ipn(string correo)
    {
        Assert.False(ValidadorCorreoInstitucional.EsValido(correo, out _));
    }

    [Fact]
    public void Normaliza_a_minusculas_y_sin_espacios()
    {
        Assert.True(ValidadorCorreoInstitucional.EsValido("  MEDICO@IPN.MX  ", out var normalizado));
        Assert.Equal("medico@ipn.mx", normalizado);
    }
}
