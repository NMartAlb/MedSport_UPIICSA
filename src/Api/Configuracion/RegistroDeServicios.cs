using System.Security.Cryptography;
using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Aplicacion.Asistencias;
using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Aplicacion.Notificaciones;
using ServiciosEstudiantiles.Aplicacion.Reportes;
using ServiciosEstudiantiles.Aplicacion.ServicioMedico;
using ServiciosEstudiantiles.Aplicacion.Usuarios;
using ServiciosEstudiantiles.Infraestructura.Actividades;
using ServiciosEstudiantiles.Infraestructura.Archivos;
using ServiciosEstudiantiles.Infraestructura.Asistencias;
using ServiciosEstudiantiles.Infraestructura.Auditoria;
using ServiciosEstudiantiles.Infraestructura.Correo;
using ServiciosEstudiantiles.Infraestructura.Documentos;
using ServiciosEstudiantiles.Infraestructura.Inscripciones;
using ServiciosEstudiantiles.Infraestructura.Notificaciones;
using ServiciosEstudiantiles.Infraestructura.Seguridad;
using ServiciosEstudiantiles.Infraestructura.ServicioMedico;
using ServiciosEstudiantiles.Infraestructura.Usuarios;

namespace ServiciosEstudiantiles.Api.Configuracion;

/// Aquí se conectan las capas: qué clase concreta cumple cada contrato.
public static class RegistroDeServicios
{
    /// Servicios con las reglas de negocio.
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<AutenticacionServicio>();
        servicios.AddScoped<UsuarioServicio>();
        servicios.AddScoped<ActividadServicio>();
        servicios.AddScoped<InscripcionServicio>();
        servicios.AddScoped<DocumentoServicio>();
        servicios.AddScoped<NotificacionServicio>();
        servicios.AddScoped<AsistenciaServicio>();
        servicios.AddScoped<AuditoriaServicio>();
        servicios.AddScoped<NuevoIngresoServicio>();
        servicios.AddScoped<ValoracionServicio>();
        servicios.AddScoped<ExpedienteServicio>();
        servicios.AddScoped<AtencionesServicio>();
        servicios.AddScoped<CampanaServicio>();
        servicios.AddScoped<ReporteServicio>();
        return servicios;
    }

    
    /// Datos, correo, archivos y seguridad. Cuando esté lista la base de datos PostgreSQL,
    /// aquí se cambia cada "...EnMemoria" por su versión de PostgreSQL.
    
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios,
        IConfiguration configuracion, IHostEnvironment entorno, OpcionesJwt jwt)
    {
        // ----- Datos (en memoria por ahora; Singleton para que no se pierdan entre peticiones) -----
        servicios.AddSingleton<IUsuarioRepositorio, UsuarioRepositorioEnMemoria>();
        servicios.AddSingleton<ICodigoVerificacionRepositorio, CodigoVerificacionRepositorioEnMemoria>();
        servicios.AddSingleton<IActividadRepositorio, ActividadRepositorioEnMemoria>();
        servicios.AddSingleton<IInscripcionRepositorio, InscripcionRepositorioEnMemoria>();
        servicios.AddSingleton<ITipoDocumentoRepositorio, TipoDocumentoRepositorioEnMemoria>();
        servicios.AddSingleton<IDocumentoRepositorio, DocumentoRepositorioEnMemoria>();
        servicios.AddSingleton<INotificacionRepositorio, NotificacionRepositorioEnMemoria>();
        servicios.AddSingleton<IAsistenciaRepositorio, AsistenciaRepositorioEnMemoria>();
        servicios.AddSingleton<ITramiteNuevoIngresoRepositorio, TramiteNuevoIngresoRepositorioEnMemoria>();
        servicios.AddSingleton<IValoracionRepositorio, ValoracionRepositorioEnMemoria>();
        servicios.AddSingleton<IExpedienteRepositorio, ExpedienteRepositorioEnMemoria>();
        servicios.AddSingleton<IConsultaRepositorio, ConsultaRepositorioEnMemoria>();
        servicios.AddSingleton<IPrimerosAuxiliosRepositorio, PrimerosAuxiliosRepositorioEnMemoria>();
        servicios.AddSingleton<IUrgenciaRepositorio, UrgenciaRepositorioEnMemoria>();
        servicios.AddSingleton<ICampanaRepositorio, CampanaRepositorioEnMemoria>();
        servicios.AddSingleton<IRegistroAccesoRepositorio, RegistroAccesoRepositorioEnMemoria>();

        //  Seguridad 
        servicios.AddSingleton(jwt);
        servicios.AddSingleton<ITokenServicio, TokenJwtServicio>();
        servicios.AddSingleton<IHasheadorContrasena, HasheadorContrasenaPbkdf2>();

        // Correo (en desarrollo va al buzón falso Mailpit) 
        servicios.AddSingleton(new OpcionesCorreo(
            configuracion["Correo:Host"] ?? "localhost",
            configuracion.GetValue<int?>("Correo:Puerto") ?? 1025,
            configuracion.GetValue<bool>("Correo:UsarSsl"),
            configuracion["Correo:Usuario"],
            configuracion["Correo:Contrasena"],
            configuracion["Correo:Remitente"] ?? "no-responder@ipn.mx"));
        servicios.AddSingleton<IServicioCorreo, ServicioCorreoSmtp>();

        // Archivos cifrados 
        var ruta = configuracion["Almacenamiento:Ruta"] ?? Path.Combine(entorno.ContentRootPath, "ArchivosSubidos");
        servicios.AddSingleton(new OpcionesAlmacenamiento(ruta, ResolverClaveCifrado(configuracion, entorno, ruta)));
        servicios.AddSingleton<IAlmacenArchivos, AlmacenArchivosLocal>();

        return servicios;
    }

    private static byte[] ResolverClaveCifrado(IConfiguration configuracion, IHostEnvironment entorno, string ruta)
    {
        var base64 = configuracion["Almacenamiento:ClaveCifradoBase64"];
        if (!string.IsNullOrWhiteSpace(base64))
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length != 32)
                throw new InvalidOperationException("Almacenamiento:ClaveCifradoBase64 debe ser una clave de 32 bytes en Base64.");
            return bytes;
        }

        if (!entorno.IsDevelopment())
            throw new InvalidOperationException("Falta Almacenamiento:ClaveCifradoBase64 (clave de 32 bytes en Base64).");

        // Solo desarrollo: se crea una clave una vez y se guarda junto a los archivos de prueba.
        Directory.CreateDirectory(ruta);
        var archivoClave = Path.Combine(ruta, ".clave-desarrollo");
        if (File.Exists(archivoClave)) return Convert.FromBase64String(File.ReadAllText(archivoClave).Trim());

        var nueva = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(archivoClave, Convert.ToBase64String(nueva));
        return nueva;
    }
}
