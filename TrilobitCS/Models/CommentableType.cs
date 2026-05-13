namespace TrilobitCS.Models;

// Discriminator for the polymorphic comment relation (posts | comments).
public enum CommentableType
{
    Posts,
    Comments,
}
