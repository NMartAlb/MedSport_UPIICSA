using System.Security.Cryptography;
using System.Text;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Usuarios;

namespace ServiciosEstudiantiles.Aplicacion.Auth;

public record SolicitarCodigoSolicitud(string Correo);
public record VerificarCodigoSolicitud(string Correo, string Codigo);
public record CrearContrasenaSolicitud(string Correo, string TokenRegistro, string Contrasena);
public record LoginSolicitud(string Correo, string Contrasena);
public record LoginRespuesta(string Token, int UsuarioId, string Correo, string NombreCompleto, string Rol);

/// Flujo: 1) pide código por correo, 2) verifica el código, 3) crea la contraseña, 4) inicia sesión.
/// El mismo flujo sirve para restablecer la contraseña.

public class AutenticacionServicio
{
    private const int MinutosVigenciaCodigo = 10;
    private const int SegundosEntreCodigos = 60;
    private const int MaxIntentosCodigo = 5;
    private const int MaxIntentosLogin = 5;
    private const int MinutosBloqueo = 15;
    private const string MensajeCodigoInvalido = "Código incorrecto o vencido.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly ICodigoVerificacionRepositorio _codigos;
    private readonly IHasheadorContrasena _hasheador;
    private readonly ITokenServicio _tokens;
    private readonly IServicioCorreo _correo;
    private readonly TimeProvider _reloj;

    public AutenticacionServicio(IUsuarioRepositorio usuarios, ICodigoVerificacionRepositorio codigos,
        IHasheadorContrasena hasheador, ITokenServicio tokens, IServicioCorreo correo, TimeProvider reloj)
    {
        _usuarios = usuarios;
        _codigos = codigos;
        _hasheador = hasheador;
        _tokens = tokens;
        _correo = correo;
        _reloj = reloj;
    }

    public async Task SolicitarCodigoAsync(string correoEntrada)
    {
        if (!ValidadorCorreoInstitucional.EsValido(correoEntrada, out var correo))
            throw new ReglaNegocioException("Solo se permiten correos institucionales del dominio ipn.mx.");

        // La respuesta siempre es la misma, exista o no la cuenta, para no revelar quién está registrado.
        var usuario = (await _usuarios.BuscarAsync(u => u.Correo == correo)).FirstOrDefault();
        if (usuario is not null && !usuario.Activo) return;

        var ahora = _reloj.GetUtcNow();
        var limite = ahora.AddSeconds(-SegundosEntreCodigos);
        var recientes = await _codigos.BuscarAsync(c => c.Correo == correo && c.Creado > limite);
        if (recientes.Count > 0) return; // evita el envío masivo de correos

        var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        await _codigos.AgregarAsync(new CodigoVerificacion
        {
            Correo = correo,
            HashCodigo = HashCodigo(correo, codigo),
            Creado = ahora,
            Expira = ahora.AddMinutes(MinutosVigenciaCodigo)
        });

        await _correo.EnviarAsync(correo, "Tu código de verificación",
            $"Tu código de verificación es {codigo}. Vence en {MinutosVigenciaCodigo} minutos. " +
            "Si no lo solicitaste, ignora este mensaje.");
    }

    /// Verifica el código y devuelve un token de un solo uso para crear la contraseña.
    public async Task<string> VerificarCodigoAsync(string correoEntrada, string codigo)
    {
        if (!ValidadorCorreoInstitucional.EsValido(correoEntrada, out var correo))
            throw new ReglaNegocioException(MensajeCodigoInvalido);

        var ahora = _reloj.GetUtcNow();
        var registros = await _codigos.BuscarAsync(c => c.Correo == correo && !c.Usado && !c.Verificado);
        var registro = registros.Where(c => c.Expira > ahora).OrderByDescending(c => c.Creado).FirstOrDefault();
        if (registro is null) throw new ReglaNegocioException(MensajeCodigoInvalido);

        registro.Intentos++;
        if (registro.Intentos > MaxIntentosCodigo)
        {
            registro.Usado = true;
            await _codigos.ActualizarAsync(registro);
            throw new ReglaNegocioException("Demasiados intentos. Solicita un nuevo código.");
        }

        if (!SonIguales(registro.HashCodigo, HashCodigo(correo, (codigo ?? string.Empty).Trim())))
        {
            await _codigos.ActualizarAsync(registro);
            throw new ReglaNegocioException(MensajeCodigoInvalido);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        registro.Verificado = true;
        registro.HashTokenRegistro = Sha256Hex(token);
        registro.Expira = ahora.AddMinutes(MinutosVigenciaCodigo);
        await _codigos.ActualizarAsync(registro);
        return token;
    }

    public async Task CrearContrasenaAsync(CrearContrasenaSolicitud solicitud)
    {
        ValidarContrasena(solicitud.Contrasena);
        if (!ValidadorCorreoInstitucional.EsValido(solicitud.Correo, out var correo))
            throw new ReglaNegocioException("La verificación venció. Solicita un nuevo código.");

        var ahora = _reloj.GetUtcNow();
        var registros = await _codigos.BuscarAsync(c => c.Correo == correo && c.Verificado && !c.Usado);
        var registro = registros.Where(c => c.Expira > ahora).OrderByDescending(c => c.Creado).FirstOrDefault();
        if (registro is null || registro.HashTokenRegistro is null ||
            !SonIguales(registro.HashTokenRegistro, Sha256Hex(solicitud.TokenRegistro ?? string.Empty)))
            throw new ReglaNegocioException("La verificación venció. Solicita un nuevo código.");

        var usuario = (await _usuarios.BuscarAsync(u => u.Correo == correo)).FirstOrDefault();
        if (usuario is null)
        {
            // Todo correo institucional nuevo se registra como alumno. El administrador puede cambiar el rol.
            await _usuarios.AgregarAsync(new Usuario
            {
                Correo = correo,
                NombreCompleto = correo.Split('@')[0],
                Rol = Rol.Alumno,
                HashContrasena = _hasheador.Hashear(solicitud.Contrasena)
            });
        }
        else
        {
            if (!usuario.Activo) throw new AccesoDenegadoException("La cuenta está desactivada.");
            usuario.HashContrasena = _hasheador.Hashear(solicitud.Contrasena);
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            await _usuarios.ActualizarAsync(usuario);
        }

        registro.Usado = true;
        await _codigos.ActualizarAsync(registro);
    }

    public async Task<LoginRespuesta> LoginAsync(LoginSolicitud solicitud)
    {
        const string mensaje = "Correo o contraseña incorrectos.";
        if (!ValidadorCorreoInstitucional.EsValido(solicitud.Correo, out var correo))
            throw new NoAutenticadoException(mensaje);

        var usuario = (await _usuarios.BuscarAsync(u => u.Correo == correo)).FirstOrDefault();
        if (usuario is null || !usuario.Activo || !usuario.TieneContrasena)
            throw new NoAutenticadoException(mensaje);

        var ahora = _reloj.GetUtcNow();
        if (usuario.BloqueadoHasta > ahora)
            throw new NoAutenticadoException("Cuenta bloqueada temporalmente por intentos fallidos. Intenta más tarde.");

        if (!_hasheador.Verificar(solicitud.Contrasena ?? string.Empty, usuario.HashContrasena!))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentosLogin)
            {
                usuario.BloqueadoHasta = ahora.AddMinutes(MinutosBloqueo);
                usuario.IntentosFallidos = 0;
            }
            await _usuarios.ActualizarAsync(usuario);
            throw new NoAutenticadoException(mensaje);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _usuarios.ActualizarAsync(usuario);

        return new LoginRespuesta(_tokens.Generar(usuario), usuario.Id, usuario.Correo,
            usuario.NombreCompleto, usuario.Rol.ToString());
    }

    private static void ValidarContrasena(string? contrasena)
    {
        var c = contrasena ?? string.Empty;
        if (c.Length < 10 || c.Length > 128 || !c.Any(char.IsLetter) || !c.Any(char.IsDigit))
            throw new ReglaNegocioException("La contraseña debe tener entre 10 y 128 caracteres, con letras y números.");
    }

    private static string HashCodigo(string correo, string codigo) => Sha256Hex($"{correo}:{codigo}");

    private static string Sha256Hex(string texto) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));

    private static bool SonIguales(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
