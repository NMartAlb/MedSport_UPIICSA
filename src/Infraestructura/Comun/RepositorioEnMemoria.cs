using System.Linq.Expressions;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Comun;


/// Repositorio que guarda todo en una lista en memoria (se pierde al reiniciar la API).
/// Sirve para desarrollar mientras llega PostgreSQL: el compañero de la BD
/// escribe otra clase que cumpla los mismos contratos y se cambia el registro en el Program.

public abstract class RepositorioEnMemoria<T> : IRepositorio<T> where T : Entidad
{
    private readonly List<T> _datos = new();
    private readonly object _candado = new();
    private int _siguienteId = 1;

    protected RepositorioEnMemoria(IEnumerable<T>? semilla = null)
    {
        if (semilla is null) return;
        foreach (var entidad in semilla)
        {
            entidad.Id = _siguienteId++;
            _datos.Add(entidad);
        }
    }

    public Task<T?> ObtenerAsync(int id)
    {
        lock (_candado)
        {
            return Task.FromResult(_datos.FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<IReadOnlyList<T>> ListarAsync()
    {
        lock (_candado)
        {
            return Task.FromResult<IReadOnlyList<T>>(_datos.ToList());
        }
    }

    public Task<IReadOnlyList<T>> BuscarAsync(Expression<Func<T, bool>> filtro)
    {
        var funcion = filtro.Compile();
        lock (_candado)
        {
            return Task.FromResult<IReadOnlyList<T>>(_datos.Where(funcion).ToList());
        }
    }

    public Task<T> AgregarAsync(T entidad)
    {
        lock (_candado)
        {
            entidad.Id = _siguienteId++;
            _datos.Add(entidad);
        }
        return Task.FromResult(entidad);
    }

    public Task ActualizarAsync(T entidad)
    {
        lock (_candado)
        {
            var indice = _datos.FindIndex(e => e.Id == entidad.Id);
            if (indice >= 0) _datos[indice] = entidad;
        }
        return Task.CompletedTask;
    }
}
