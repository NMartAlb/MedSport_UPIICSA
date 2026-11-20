using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiciosEstudiantiles.Infraestructura.Seguridad;

namespace ServiciosEstudiantiles.Api.Configuracion;

public static class ConfiguracionSeguridad
{
    
    /// La clave del token NO va en el código. En producción se define con la variable Jwt__Clave
    /// (o con "dotnet user-secrets"). En desarrollo, si falta, se genera una temporal.
    
    public static OpcionesJwt CrearOpcionesJwt(IConfiguration configuracion, IHostEnvironment entorno)
    {
        var clave = configuracion["Jwt:Clave"];
        if (string.IsNullOrWhiteSpace(clave))
        {
            if (!entorno.IsDevelopment())
                throw new InvalidOperationException("Falta la configuración Jwt:Clave (mínimo 32 caracteres).");
            clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)); // las sesiones se cierran al reiniciar
        }

        if (Encoding.UTF8.GetByteCount(clave) < 32)
            throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres.");

        return new OpcionesJwt(clave, "ServiciosEstudiantiles", "ServiciosEstudiantiles.Web",
            configuracion.GetValue<int?>("Jwt:MinutosVigencia") ?? 60);
    }

    public static IServiceCollection AgregarSeguridad(this IServiceCollection servicios,
        IConfiguration configuracion, OpcionesJwt jwt)
    {
        // Autenticación: el token viaja en el encabezado Authorization: Bearer ...
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emisor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "email",
                    RoleClaimType = "role"
                };
            });
        servicios.AddAuthorization();

        // CORS: solo la web autorizada puede llamar a la API desde el navegador.
        var origenes = configuracion.GetSection("Cors:Origenes").Get<string[]>()
                       ?? new[] { "http://localhost:5500", "http://127.0.0.1:5500" };
        servicios.AddCors(o => o.AddPolicy("web", p => p
            .WithOrigins(origenes)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE")));

        // Límite de intentos por IP en las rutas de autenticación (frena fuerza bruta y envío masivo de correos).
        var confiarEnProxy = configuracion.GetValue<bool>("ConfiarEnProxy");
        servicios.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("auth", contexto =>
            {
                var ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
                // Detrás de NGINX, la IP real del usuario viene en X-Forwarded-For.
                if (confiarEnProxy && contexto.Request.Headers.TryGetValue("X-Forwarded-For", out var reenviada))
                {
                    var primera = reenviada.ToString().Split(',')[0].Trim();
                    if (primera.Length > 0) ip = primera;
                }
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return servicios;
    }
}
