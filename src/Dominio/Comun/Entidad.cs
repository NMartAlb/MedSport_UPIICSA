namespace ServiciosEstudiantiles.Dominio.Comun;

/// Toda cosa que se guarda en la base de datos tiene un Id.
public abstract class Entidad
{
    public int Id { get; set; }
}
