namespace Fmc.Domain.Abstractions;

/// <summary>Entidad atribuida a un autor (usuario + rol) dentro de una cafetería.</summary>
public interface IAuthorOwnedEntity
{
    Guid Id { get; set; }
    Guid CafeteriaId { get; set; }
    Guid AuthorUserId { get; set; }
    string AuthorRole { get; set; }
}
