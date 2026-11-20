using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Dominio.Inscripciones;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Inscripciones;

public class InscripcionRepositorioEnMemoria : RepositorioEnMemoria<Inscripcion>, IInscripcionRepositorio { }
