using System.Security.Cryptography;
using ServiciosEstudiantiles.Aplicacion.Auth;

namespace ServiciosEstudiantiles.Infraestructura.Seguridad;

/// Guarda contraseñas con PBKDF2-SHA256 y sal aleatoria. Nunca se guarda la contraseña real.
public class HasheadorContrasenaPbkdf2 : IHasheadorContrasena
{
    private const string Prefijo = "pbkdf2-sha256";
    private const int Iteraciones = 600_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public string Hashear(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
        return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string contrasena, string hashGuardado)
    {
        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Prefijo) return false;
        if (!int.TryParse(partes[1], out var iteraciones) || iteraciones < 1) return false;

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
