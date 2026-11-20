using ServiciosEstudiantiles.Aplicacion.ServicioMedico;
using ServiciosEstudiantiles.Dominio.ServicioMedico;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.ServicioMedico;

public class TramiteNuevoIngresoRepositorioEnMemoria : RepositorioEnMemoria<TramiteNuevoIngreso>, ITramiteNuevoIngresoRepositorio { }
public class ValoracionRepositorioEnMemoria : RepositorioEnMemoria<Valoracion>, IValoracionRepositorio { }
public class ExpedienteRepositorioEnMemoria : RepositorioEnMemoria<ExpedienteClinico>, IExpedienteRepositorio { }
public class ConsultaRepositorioEnMemoria : RepositorioEnMemoria<Consulta>, IConsultaRepositorio { }
public class PrimerosAuxiliosRepositorioEnMemoria : RepositorioEnMemoria<AtencionPrimerosAuxilios>, IPrimerosAuxiliosRepositorio { }
public class UrgenciaRepositorioEnMemoria : RepositorioEnMemoria<Urgencia>, IUrgenciaRepositorio { }
public class CampanaRepositorioEnMemoria : RepositorioEnMemoria<Campana>, ICampanaRepositorio { }
