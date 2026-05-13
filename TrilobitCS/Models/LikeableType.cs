namespace TrilobitCS.Models;

// Discriminator for the polymorphic like relation (posts | comments).
public enum LikeableType
{
    Posts,
    Comments,
}
