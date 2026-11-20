using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Usuarios;
using ServiciosEstudiantiles.Infraestructura.Comun;

namespace ServiciosEstudiantiles.Infraestructura.Usuarios;


/// Cuentas de prueba: no tienen contraseña; cada persona la crea con el código que llega al correo.
/// Cualquier otro correo @ipn.mx que se registre entra como alumno.

public class UsuarioRepositorioEnMemoria : RepositorioEnMemoria<Usuario>, IUsuarioRepositorio
{
    public UsuarioRepositorioEnMemoria() : base(Semilla()) { }

    private static IEnumerable<Usuario> Semilla() => new[]
    {
        new Usuario { Correo = "admin@ipn.mx", NombreCompleto = "Administrador del sistema", Rol = Rol.Administrador },
        new Usuario { Correo = "coach@ipn.mx", NombreCompleto = "Coach de prueba", Rol = Rol.Coach },
        new Usuario { Correo = "medico@ipn.mx", NombreCompleto = "Personal médico de prueba", Rol = Rol.PersonalMedico },
        new Usuario { Correo = "directivo@ipn.mx", NombreCompleto = "Directivo de prueba", Rol = Rol.Directivo }
    };
}

public class CodigoVerificacionRepositorioEnMemoria : RepositorioEnMemoria<CodigoVerificacion>, ICodigoVerificacionRepositorio { }
