using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Usuarios;

namespace ServiciosEstudiantiles.Aplicacion.Auth;

public interface IUsuarioRepositorio : IRepositorio<Usuario> { }

public interface ICodigoVerificacionRepositorio : IRepositorio<CodigoVerificacion> { }

public interface IHasheadorContrasena
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hash);
}

public interface ITokenServicio
{
    /// Genera el token de sesión (JWT) con el id y el rol del usuario.
    string Generar(Usuario usuario);
}

public interface IServicioCorreo
{
    Task EnviarAsync(string destino, string asunto, string cuerpo);
}
