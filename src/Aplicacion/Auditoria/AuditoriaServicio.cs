using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Auditoria;

namespace ServiciosEstudiantiles.Aplicacion.Auditoria;

public interface IRegistroAccesoRepositorio : IRepositorio<RegistroAcceso> { }

public record RegistroAccesoDto(int Id, DateTimeOffset Fecha, string Usuario, string Accion, string Recurso);

/// Bitácora de accesos y modificaciones a información sensible (RNF8).
public class AuditoriaServicio
{
    private readonly IRegistroAccesoRepositorio _registros;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly TimeProvider _reloj;

    public AuditoriaServicio(IRegistroAccesoRepositorio registros, IUsuarioRepositorio usuarios, TimeProvider reloj)
    {
        _registros = registros;
        _usuarios = usuarios;
        _reloj = reloj;
    }

    public async Task RegistrarAsync(int usuarioId, string accion, string recurso) =>
        await _registros.AgregarAsync(new RegistroAcceso
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Recurso = recurso,
            Fecha = _reloj.GetUtcNow()
        });

    public async Task<IReadOnlyList<RegistroAccesoDto>> ListarAsync(int maximo = 200)
    {
        var registros = (await _registros.ListarAsync()).OrderByDescending(r => r.Fecha).Take(maximo).ToList();
        var usuarios = (await _usuarios.ListarAsync()).ToDictionary(u => u.Id, u => u.Correo);
        return registros
            .Select(r => new RegistroAccesoDto(r.Id, r.Fecha,
                usuarios.TryGetValue(r.UsuarioId, out var correo) ? correo : $"Usuario {r.UsuarioId}",
                r.Accion, r.Recurso))
            .ToList();
    }
}
