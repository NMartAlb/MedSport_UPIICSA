using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Dominio.Auditoria;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Auditoria;

public class RegistroAccesoRepositorioEnMemoria : RepositorioEnMemoria<RegistroAcceso>, IRegistroAccesoRepositorio { }
