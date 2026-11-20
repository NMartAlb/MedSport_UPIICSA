using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ServiciosEstudiantiles.Aplicacion.Documentos;

namespace ServiciosEstudiantiles.Infraestructura.Archivos;

public record OpcionesAlmacenamiento(string RutaBase, byte[] ClaveCifrado);


/// Guarda los archivos en una carpeta fuera de la parte pública, con nombre aleatorio y cifrados (AES-GCM).
/// Formato del archivo: nonce (12 bytes) + etiqueta (16 bytes) + contenido cifrado.

public class AlmacenArchivosLocal : IAlmacenArchivos
{
    private const int TamanoNonce = 12;
    private const int TamanoEtiqueta = 16;
    private static readonly Regex NombreValido = new(@"^[0-9a-f]{32}\.(pdf|jpg|png)$", RegexOptions.Compiled);

    private readonly string _ruta;
    private readonly byte[] _clave;

    public AlmacenArchivosLocal(OpcionesAlmacenamiento opciones)
    {
        if (opciones.ClaveCifrado.Length != 32)
            throw new ArgumentException("La clave de cifrado debe tener 32 bytes.");
        _ruta = Path.GetFullPath(opciones.RutaBase);
        _clave = opciones.ClaveCifrado;
        Directory.CreateDirectory(_ruta);
    }

    public async Task<string> GuardarAsync(Stream contenido, string extension)
    {
        if (extension is not (".pdf" or ".jpg" or ".png"))
            throw new ArgumentException("Extensión no permitida.", nameof(extension));

        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria);
        var claro = memoria.ToArray();

        var nonce = RandomNumberGenerator.GetBytes(TamanoNonce);
        var cifrado = new byte[claro.Length];
        var etiqueta = new byte[TamanoEtiqueta];
        using (var aes = new AesGcm(_clave, TamanoEtiqueta))
            aes.Encrypt(nonce, claro, cifrado, etiqueta);

        var salida = new byte[TamanoNonce + TamanoEtiqueta + cifrado.Length];
        Buffer.BlockCopy(nonce, 0, salida, 0, TamanoNonce);
        Buffer.BlockCopy(etiqueta, 0, salida, TamanoNonce, TamanoEtiqueta);
        Buffer.BlockCopy(cifrado, 0, salida, TamanoNonce + TamanoEtiqueta, cifrado.Length);

        var nombre = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_ruta, nombre), salida);
        return nombre;
    }

    public async Task<Stream?> AbrirAsync(string nombreAlmacenado)
    {
        // Solo se aceptan los nombres que el propio sistema genera (evita rutas como ..\..\)
        if (!NombreValido.IsMatch(nombreAlmacenado)) return null;
        var ruta = Path.Combine(_ruta, nombreAlmacenado);
        if (!File.Exists(ruta)) return null;

        var datos = await File.ReadAllBytesAsync(ruta);
        if (datos.Length < TamanoNonce + TamanoEtiqueta) return null;

        var nonce = datos.AsSpan(0, TamanoNonce).ToArray();
        var etiqueta = datos.AsSpan(TamanoNonce, TamanoEtiqueta).ToArray();
        var cifrado = datos.AsSpan(TamanoNonce + TamanoEtiqueta).ToArray();
        var claro = new byte[cifrado.Length];

        try
        {
            using var aes = new AesGcm(_clave, TamanoEtiqueta);
            aes.Decrypt(nonce, cifrado, etiqueta, claro);
        }
        catch (CryptographicException)
        {
            return null; // el archivo fue alterado o la clave no coincide
        }
        return new MemoryStream(claro);
    }
}
