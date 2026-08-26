using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public record LikeResponse(int Id, int UserId, LikeableType LikeableType, int LikeableId, DateTime CreatedAt);
