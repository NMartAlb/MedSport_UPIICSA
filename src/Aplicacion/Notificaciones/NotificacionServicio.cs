using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.Notificaciones;

namespace ServiciosEstudiantiles.Aplicacion.Notificaciones;

public interface INotificacionRepositorio : IRepositorio<Notificacion> { }

public record NotificacionDto(int Id, string Mensaje, DateTimeOffset Fecha, bool Leida);

public class NotificacionServicio
{
    private readonly INotificacionRepositorio _notificaciones;
    private readonly TimeProvider _reloj;

    public NotificacionServicio(INotificacionRepositorio notificaciones, TimeProvider reloj)
    {
        _notificaciones = notificaciones;
        _reloj = reloj;
    }

    public async Task NotificarAsync(int usuarioId, string mensaje) =>
        await _notificaciones.AgregarAsync(new Notificacion
        {
            UsuarioId = usuarioId,
            Mensaje = mensaje,
            Fecha = _reloj.GetUtcNow()
        });

    public async Task<IReadOnlyList<NotificacionDto>> MisNotificacionesAsync(UsuarioActual quien)
    {
        var mias = await _notificaciones.BuscarAsync(n => n.UsuarioId == quien.Id);
        return mias.OrderByDescending(n => n.Fecha).Take(50)
            .Select(n => new NotificacionDto(n.Id, n.Mensaje, n.Fecha, n.Leida)).ToList();
    }

    public async Task MarcarLeidaAsync(UsuarioActual quien, int id)
    {
        var n = await _notificaciones.ObtenerAsync(id);
        if (n is null || n.UsuarioId != quien.Id) throw new NoEncontradoException("Notificación no encontrada.");
        n.Leida = true;
        await _notificaciones.ActualizarAsync(n);
    }
}
