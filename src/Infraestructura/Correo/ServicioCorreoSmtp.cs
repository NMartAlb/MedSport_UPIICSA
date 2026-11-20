using System.Net;
using System.Net.Mail;
using ServiciosEstudiantiles.Aplicacion.Auth;

namespace ServiciosEstudiantiles.Infraestructura.Correo;

public record OpcionesCorreo(string Host, int Puerto, bool UsarSsl, string? Usuario, string? Contrasena, string Remitente);

/// En desarrollo envía al buzón falso (Mailpit, puerto 1025). En producción se configura el SMTP institucional.
public class ServicioCorreoSmtp : IServicioCorreo
{
    private readonly OpcionesCorreo _opciones;

    public ServicioCorreoSmtp(OpcionesCorreo opciones) => _opciones = opciones;

    public async Task EnviarAsync(string destino, string asunto, string cuerpo)
    {
        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto) { EnableSsl = _opciones.UsarSsl };
        if (!string.IsNullOrEmpty(_opciones.Usuario))
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena);

        using var mensaje = new MailMessage(_opciones.Remitente, destino, asunto, cuerpo);
        await cliente.SendMailAsync(mensaje);
    }
}
