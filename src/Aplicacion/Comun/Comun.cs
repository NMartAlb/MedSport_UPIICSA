using System.Linq.Expressions;
using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Aplicacion.Comun;

/// Quién está haciendo la petición (sale del token de sesión).
public record UsuarioActual(int Id, Rol Rol);

/// Una regla del negocio no se cumple (se responde 400).
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// Lo que se busca no existe (se responde 404)
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}

/// El usuario no tiene permiso para esto (se responde 403).
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje) : base(mensaje) { }
}

/// Credenciales incorrectas o sesión inválida (se responde 401).
public class NoAutenticadoException : Exception
{
    public NoAutenticadoException(string mensaje) : base(mensaje) { }
}

/// Contrato general de acceso a datos. Hoy lo cumple una lista en memoria;
/// después lo cumplirá PostgreSQL sin cambiar los servicios.

public interface IRepositorio<T> where T : Entidad
{
    Task<T?> ObtenerAsync(int id);
    Task<IReadOnlyList<T>> ListarAsync();
    Task<IReadOnlyList<T>> BuscarAsync(Expression<Func<T, bool>> filtro);
    Task<T> AgregarAsync(T entidad);
    Task ActualizarAsync(T entidad);
}
