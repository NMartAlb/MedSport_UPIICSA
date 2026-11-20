using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Dominio.Usuarios;

namespace ServiciosEstudiantiles.Infraestructura.Seguridad;

public record OpcionesJwt(string Clave, string Emisor, string Audiencia, int MinutosVigencia);

/// Crea el token de sesión. Lleva el id ("sub"), el correo ("email") y el rol ("role").
public class TokenJwtServicio : ITokenServicio
{
    private readonly OpcionesJwt _opciones;

    public TokenJwtServicio(OpcionesJwt opciones) => _opciones = opciones;

    public string Generar(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim("sub", usuario.Id.ToString()),
            new Claim("email", usuario.Correo),
            new Claim("role", usuario.Rol.ToString())
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var ahora = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: ahora,
            expires: ahora.AddMinutes(_opciones.MinutosVigencia),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
