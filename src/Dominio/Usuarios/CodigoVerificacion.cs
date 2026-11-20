using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Dominio.Usuarios;

///Código de 6 dígitos que se envía por correo para crear o restablecer la contraseña.
public class CodigoVerificacion : Entidad
{
    public string Correo { get; set; } = string.Empty;
    public string HashCodigo { get; set; } = string.Empty;
    public DateTimeOffset Creado { get; set; }
    public DateTimeOffset Expira { get; set; }
    public int Intentos { get; set; }
    public bool Verificado { get; set; }
    public string? HashTokenRegistro { get; set; }
    public bool Usado { get; set; }
}
