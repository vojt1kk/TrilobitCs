using TrilobitCS.Models;

namespace TrilobitCS.Responses;

public record OrganisationInviteResponse(
    int Id,
    int OrganisationId,
    int InvitedUserId,
    string InvitedUserNickname,
    int? InvitedById,
    OrganisationInviteStatus Status,
    DateTime CreatedAt
);
