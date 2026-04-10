using TrilobitCS.Models;

namespace TrilobitCS.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindWithUser(string token, CancellationToken cancellationToken = default);
    Task<RefreshToken> Create(RefreshToken token, CancellationToken cancellationToken = default);
    Task Save(CancellationToken cancellationToken = default);
}
