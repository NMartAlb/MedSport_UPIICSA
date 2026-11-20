namespace ServiciosEstudiantiles.Aplicacion.Auth;

/// Solo se aceptan correos del dominio ipn.mx o de sus subdominios (por ejemplo alumno.ipn.mx).
public static class ValidadorCorreoInstitucional
{
    public static bool EsValido(string? correo, out string correoNormalizado)
    {
        correoNormalizado = (correo ?? string.Empty).Trim().ToLowerInvariant();
        if (correoNormalizado.Length is 0 or > 254) return false;

        var partes = correoNormalizado.Split('@');
        if (partes.Length != 2) return false;

        var local = partes[0];
        var dominio = partes[1];
        if (local.Length is 0 or > 64) return false;
        if (dominio != "ipn.mx" && !dominio.EndsWith(".ipn.mx", StringComparison.Ordinal)) return false;
        if (dominio.Contains("..", StringComparison.Ordinal) || dominio.StartsWith('.')) return false;

        var localValido = local.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '+');
        var dominioValido = dominio.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-');
        return localValido && dominioValido;
    }
}
