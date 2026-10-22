using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Users;

public record GetUserQuery(int UserId, int ViewerId) : IRequest<UserProfileResponse>;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserProfileResponse>
{
    private readonly AppDbContext _db;

    public GetUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var viewerId = query.ViewerId;

        // Counts are projected unconditionally so this stays a single DB command; they are nulled below for hidden profiles.
        var profile = await _db.Users
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserProfileResponse
            {
                Id = u.Id,
                Nickname = u.Nickname,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePicture = u.ProfilePicture,
                CreatedAt = u.CreatedAt,
                IsPrivate = u.IsPrivate,
                IsFollowing = u.Id != viewerId && u.Followers.Any(f => f.FollowerId == viewerId),
                FollowerCount = u.Followers.Count,
                FollowingCount = u.Following.Count,
                ApprovedFeatherCount = u.EagleFeathers.Count(f => f.Status == EagleFeatherStatus.Approved),
                PostCount = u.Posts.Count,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (profile.Id == viewerId || !profile.IsPrivate || profile.IsFollowing)
            return profile;

        return profile with
        {
            FollowerCount = null,
            FollowingCount = null,
            ApprovedFeatherCount = null,
            PostCount = null,
        };
    }
}
