using ServiciosEstudiantiles.Aplicacion.Notificaciones;
using ServiciosEstudiantiles.Dominio.Notificaciones;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Notificaciones;

public class NotificacionRepositorioEnMemoria : RepositorioEnMemoria<Notificacion>, INotificacionRepositorio { }
