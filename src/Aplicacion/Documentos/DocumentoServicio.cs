using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Aplicacion.Notificaciones;
using ServiciosEstudiantiles.Aplicacion.ServicioMedico;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Documentos;

namespace ServiciosEstudiantiles.Aplicacion.Documentos;

public record TipoDocumentoDto(int Id, string Nombre, string Descripcion, string Contexto);

public record CargarDocumentoSolicitud(ContextoTramite Contexto, int ReferenciaId, int TipoDocumentoId,
    string NombreOriginal, Stream Contenido);

public record DocumentoCargadoDto(int Id, string Estado);

public record DocumentoRevisionDto(int Id, int AlumnoId, string AlumnoCorreo, string AlumnoNombre,
    string TipoDocumento, string Contexto, string Referencia, string Estado, DateTimeOffset FechaCarga);

public record RevisarDocumentoSolicitud(bool Aprobar, string? Motivo);

public record ArchivoDescarga(Stream Contenido, string NombreOriginal, string TipoContenido);

public class DocumentoServicio
{
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;
    private static readonly string[] ExtensionesPermitidas = { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly IDocumentoRepositorio _documentos;
    private readonly ITipoDocumentoRepositorio _tipos;
    private readonly IInscripcionRepositorio _inscripciones;
    private readonly IActividadRepositorio _actividades;
    private readonly ITramiteNuevoIngresoRepositorio _tramites;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAlmacenArchivos _almacen;
    private readonly NotificacionServicio _notificaciones;
    private readonly TimeProvider _reloj;

    public DocumentoServicio(IDocumentoRepositorio documentos, ITipoDocumentoRepositorio tipos,
        IInscripcionRepositorio inscripciones, IActividadRepositorio actividades,
        ITramiteNuevoIngresoRepositorio tramites, IUsuarioRepositorio usuarios,
        IAlmacenArchivos almacen, NotificacionServicio notificaciones, TimeProvider reloj)
    {
        _documentos = documentos;
        _tipos = tipos;
        _inscripciones = inscripciones;
        _actividades = actividades;
        _tramites = tramites;
        _usuarios = usuarios;
        _almacen = almacen;
        _notificaciones = notificaciones;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<TipoDocumentoDto>> TiposAsync(ContextoTramite? contexto)
    {
        var tipos = await _tipos.ListarAsync();
        return tipos.Where(t => contexto is null || t.Contexto == contexto)
            .OrderBy(t => t.Id)
            .Select(t => new TipoDocumentoDto(t.Id, t.Nombre, t.Descripcion, t.Contexto.ToString()))
            .ToList();
    }

    // Carga (alumno) 

    public async Task<DocumentoCargadoDto> CargarAsync(UsuarioActual alumno, CargarDocumentoSolicitud solicitud)
    {
        var tipo = await _tipos.ObtenerAsync(solicitud.TipoDocumentoId)
                   ?? throw new ReglaNegocioException("El tipo de documento no existe.");
        if (tipo.Contexto != solicitud.Contexto)
            throw new ReglaNegocioException("Ese documento no corresponde a este trámite.");

        await ValidarPertenenciaAsync(alumno, solicitud, tipo);

        var nombreOriginal = solicitud.NombreOriginal ?? string.Empty;
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            throw new ReglaNegocioException("Solo se permiten archivos PDF, JPG o PNG.");

        var bytes = await LeerConLimiteAsync(solicitud.Contenido);
        if (!ContenidoCoincideConExtension(bytes, extension))
            throw new ReglaNegocioException("El contenido del archivo no corresponde a su formato.");

        string almacenado;
        using (var flujo = new MemoryStream(bytes))
            almacenado = await _almacen.GuardarAsync(flujo, extension == ".jpeg" ? ".jpg" : extension);

        var documento = await _documentos.AgregarAsync(new Documento
        {
            AlumnoId = alumno.Id,
            TipoDocumentoId = tipo.Id,
            Contexto = solicitud.Contexto,
            ReferenciaId = solicitud.ReferenciaId,
            NombreOriginal = LimpiarNombre(nombreOriginal),
            ArchivoAlmacenado = almacenado,
            Estado = EstadoDocumento.Recibido,
            FechaCarga = _reloj.GetUtcNow()
        });
        return new DocumentoCargadoDto(documento.Id, documento.Estado.ToString());
    }

    private async Task ValidarPertenenciaAsync(UsuarioActual alumno, CargarDocumentoSolicitud s, TipoDocumento tipo)
    {
        if (s.Contexto == ContextoTramite.Actividad)
        {
            var inscripcion = await _inscripciones.ObtenerAsync(s.ReferenciaId);
            if (inscripcion is null || inscripcion.AlumnoId != alumno.Id || inscripcion.Cancelada)
                throw new NoEncontradoException("Inscripción no encontrada.");
            var actividad = await _actividades.ObtenerAsync(inscripcion.ActividadId);
            if (actividad is null || !actividad.TiposDocumentoRequeridos.Contains(tipo.Id))
                throw new ReglaNegocioException("Ese documento no se requiere para esta actividad.");
        }
        else
        {
            var tramite = await _tramites.ObtenerAsync(s.ReferenciaId);
            if (tramite is null || tramite.AlumnoId != alumno.Id)
                throw new NoEncontradoException("Trámite no encontrado.");
        }
    }

    private static async Task<byte[]> LeerConLimiteAsync(Stream contenido)
    {
        using var memoria = new MemoryStream();
        var buffer = new byte[81920];
        int leidos;
        while ((leidos = await contenido.ReadAsync(buffer)) > 0)
        {
            memoria.Write(buffer, 0, leidos);
            if (memoria.Length > TamanoMaximoBytes)
                throw new ReglaNegocioException("El archivo excede el tamaño máximo de 5 MB.");
        }
        if (memoria.Length == 0) throw new ReglaNegocioException("El archivo está vacío.");
        return memoria.ToArray();
    }

    /// Revisa los primeros bytes para que nadie suba un ejecutable renombrado como PDF.
    private static bool ContenidoCoincideConExtension(byte[] b, string extension) => extension switch
    {
        ".pdf" => b.Length > 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,
        ".jpg" or ".jpeg" => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        ".png" => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
        _ => false
    };

    private static string LimpiarNombre(string nombre)
    {
        var limpio = new string(Path.GetFileName(nombre).Where(c => !char.IsControl(c)).ToArray());
        return limpio.Length > 120 ? limpio[..120] : limpio;
    }

    // Revisión (coach / personal médico) 

    public async Task<IReadOnlyList<DocumentoRevisionDto>> PendientesRevisionAsync(UsuarioActual revisor)
    {
        List<Documento> pendientes;
        if (revisor.Rol == Rol.Coach)
        {
            var misActividades = (await _actividades.BuscarAsync(a => a.CoachId == revisor.Id)).Select(a => a.Id).ToList();
            var inscripciones = await _inscripciones.BuscarAsync(i => misActividades.Contains(i.ActividadId) && !i.Cancelada);
            var idsInscripciones = inscripciones.Select(i => i.Id).ToList();
            pendientes = (await _documentos.BuscarAsync(d =>
                d.Contexto == ContextoTramite.Actividad && idsInscripciones.Contains(d.ReferenciaId) &&
                (d.Estado == EstadoDocumento.Recibido || d.Estado == EstadoDocumento.EnRevision))).ToList();
        }
        else if (revisor.Rol == Rol.PersonalMedico)
        {
            pendientes = (await _documentos.BuscarAsync(d =>
                d.Contexto == ContextoTramite.NuevoIngresoMedico &&
                (d.Estado == EstadoDocumento.Recibido || d.Estado == EstadoDocumento.EnRevision))).ToList();
        }
        else
        {
            throw new AccesoDenegadoException("No tienes permiso para revisar documentos.");
        }

        var resultado = new List<DocumentoRevisionDto>();
        foreach (var d in pendientes.OrderBy(d => d.FechaCarga))
        {
            var alumno = await _usuarios.ObtenerAsync(d.AlumnoId);
            var tipo = await _tipos.ObtenerAsync(d.TipoDocumentoId);
            var referencia = d.Contexto == ContextoTramite.Actividad
                ? await NombreDeActividadAsync(d.ReferenciaId)
                : "Nuevo ingreso al servicio médico";
            resultado.Add(new DocumentoRevisionDto(d.Id, d.AlumnoId, alumno?.Correo ?? "", alumno?.NombreCompleto ?? "",
                tipo?.Nombre ?? "", d.Contexto.ToString(), referencia, d.Estado.ToString(), d.FechaCarga));
        }
        return resultado;
    }

    public async Task RevisarAsync(UsuarioActual revisor, int documentoId, RevisarDocumentoSolicitud solicitud)
    {
        var documento = await _documentos.ObtenerAsync(documentoId)
                        ?? throw new NoEncontradoException("Documento no encontrado.");
        await ExigirPermisoDeRevisionAsync(revisor, documento);

        if (documento.Estado is EstadoDocumento.Aprobado or EstadoDocumento.Rechazado)
            throw new ReglaNegocioException("Este documento ya fue revisado.");

        var motivo = (solicitud.Motivo ?? string.Empty).Trim();
        if (!solicitud.Aprobar && motivo.Length < 5)
            throw new ReglaNegocioException("Indica el motivo del rechazo (mínimo 5 caracteres).");
        if (motivo.Length > 300)
            throw new ReglaNegocioException("El motivo no puede exceder 300 caracteres.");

        documento.Estado = solicitud.Aprobar ? EstadoDocumento.Aprobado : EstadoDocumento.Rechazado;
        documento.MotivoRechazo = solicitud.Aprobar ? null : motivo;
        documento.RevisadoPorId = revisor.Id;
        documento.FechaRevision = _reloj.GetUtcNow();
        await _documentos.ActualizarAsync(documento);

        var tipo = await _tipos.ObtenerAsync(documento.TipoDocumentoId);
        var nombreTipo = tipo?.Nombre ?? "documento";
        if (solicitud.Aprobar)
        {
            await _notificaciones.NotificarAsync(documento.AlumnoId, $"Tu documento «{nombreTipo}» fue aprobado.");
            if (await TramiteCompletoAsync(documento))
                await _notificaciones.NotificarAsync(documento.AlumnoId, "Tu trámite quedó completo.");
        }
        else
        {
            await _notificaciones.NotificarAsync(documento.AlumnoId,
                $"Tu documento «{nombreTipo}» fue rechazado. Motivo: {motivo}");
        }
    }

    // Descarga 
    public async Task<ArchivoDescarga> AbrirArchivoAsync(UsuarioActual quien, int documentoId)
    {
        var documento = await _documentos.ObtenerAsync(documentoId)
                        ?? throw new NoEncontradoException("Documento no encontrado.");

        var esDueno = quien.Rol == Rol.Alumno && documento.AlumnoId == quien.Id;
        if (!esDueno) await ExigirPermisoDeRevisionAsync(quien, documento);

        var flujo = await _almacen.AbrirAsync(documento.ArchivoAlmacenado)
                    ?? throw new NoEncontradoException("El archivo ya no está disponible.");

        // Al abrirlo, quien revisa lo marca "en revisión".
        if (!esDueno && documento.Estado == EstadoDocumento.Recibido)
        {
            documento.Estado = EstadoDocumento.EnRevision;
            await _documentos.ActualizarAsync(documento);
        }

        var extension = Path.GetExtension(documento.ArchivoAlmacenado).ToLowerInvariant();
        var tipoContenido = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "image/jpeg"
        };
        return new ArchivoDescarga(flujo, documento.NombreOriginal, tipoContenido);
    }

    // Apoyo 

    private async Task ExigirPermisoDeRevisionAsync(UsuarioActual quien, Documento documento)
    {
        if (documento.Contexto == ContextoTramite.NuevoIngresoMedico)
        {
            if (quien.Rol != Rol.PersonalMedico)
                throw new AccesoDenegadoException("No tienes permiso para este documento.");
            return;
        }

        if (quien.Rol != Rol.Coach) throw new AccesoDenegadoException("No tienes permiso para este documento.");
        var inscripcion = await _inscripciones.ObtenerAsync(documento.ReferenciaId);
        var actividad = inscripcion is null ? null : await _actividades.ObtenerAsync(inscripcion.ActividadId);
        if (actividad is null || actividad.CoachId != quien.Id)
            throw new AccesoDenegadoException("Este documento pertenece a una actividad de otro coach.");
    }

    private async Task<string> NombreDeActividadAsync(int inscripcionId)
    {
        var inscripcion = await _inscripciones.ObtenerAsync(inscripcionId);
        var actividad = inscripcion is null ? null : await _actividades.ObtenerAsync(inscripcion.ActividadId);
        return actividad?.Nombre ?? "Actividad";
    }

    private async Task<bool> TramiteCompletoAsync(Documento documento)
    {
        IEnumerable<TipoDocumento> requeridos;
        if (documento.Contexto == ContextoTramite.Actividad)
        {
            var inscripcion = await _inscripciones.ObtenerAsync(documento.ReferenciaId);
            var actividad = inscripcion is null ? null : await _actividades.ObtenerAsync(inscripcion.ActividadId);
            var ids = (actividad?.TiposDocumentoRequeridos ?? new List<int>()).ToList();
            requeridos = await _tipos.BuscarAsync(t => ids.Contains(t.Id));
        }
        else
        {
            requeridos = await _tipos.BuscarAsync(t => t.Contexto == ContextoTramite.NuevoIngresoMedico);
        }

        var contexto = documento.Contexto;
        var referenciaId = documento.ReferenciaId;
        var entregados = await _documentos.BuscarAsync(d => d.Contexto == contexto && d.ReferenciaId == referenciaId);
        return RequisitosTramite.Evaluar(requeridos, entregados).Estado == EstadoTramite.Completado;
    }
}
