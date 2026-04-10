using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken?> FindWithUser(string token, CancellationToken cancellationToken = default)
        => await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public async Task<RefreshToken> Create(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task Save(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
