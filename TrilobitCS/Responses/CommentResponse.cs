using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public record CommentResponse(
    int Id,
    PostAuthorResponse Author,
    CommentableType CommentableType,
    int CommentableId,
    string Content,
    DateTime CreatedAt
);
