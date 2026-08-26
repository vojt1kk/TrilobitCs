namespace TrilobitCS.Responses;

public record FollowResponse(
    int FollowerId,
    int FollowingId,
    DateTime CreatedAt
);
