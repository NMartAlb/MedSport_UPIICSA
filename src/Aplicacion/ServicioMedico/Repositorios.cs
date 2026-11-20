using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public interface ITramiteNuevoIngresoRepositorio : IRepositorio<TramiteNuevoIngreso> { }
public interface IValoracionRepositorio : IRepositorio<Valoracion> { }
public interface IExpedienteRepositorio : IRepositorio<ExpedienteClinico> { }
public interface IConsultaRepositorio : IRepositorio<Consulta> { }
public interface IPrimerosAuxiliosRepositorio : IRepositorio<AtencionPrimerosAuxilios> { }
public interface IUrgenciaRepositorio : IRepositorio<Urgencia> { }
public interface ICampanaRepositorio : IRepositorio<Campana> { }
