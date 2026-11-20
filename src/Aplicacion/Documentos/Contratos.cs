using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Documentos;

namespace ServiciosEstudiantiles.Aplicacion.Documentos;

public interface ITipoDocumentoRepositorio : IRepositorio<TipoDocumento> { }

public interface IDocumentoRepositorio : IRepositorio<Documento> { }

/// Dónde se guardan físicamente los archivos (cifrados)
public interface IAlmacenArchivos
{
    /// Guarda el archivo y devuelve el nombre interno con el que se guardó.
    Task<string> GuardarAsync(Stream contenido, string extension);

    ///Devuelve el archivo descifrado, o null si no existe.
    Task<Stream?> AbrirAsync(string nombreAlmacenado);
}
