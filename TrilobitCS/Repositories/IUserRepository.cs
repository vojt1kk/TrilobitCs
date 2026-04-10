using TrilobitCS.Dto;
using TrilobitCS.Models;

namespace TrilobitCS.Repositories;

public interface IUserRepository
{
    Task<User?> FindByNickname(string nickname, CancellationToken cancellationToken = default);
    Task<bool> EmailExists(string email, CancellationToken cancellationToken = default);
    Task<bool> NicknameExists(string nickname, CancellationToken cancellationToken = default);
    Task<User> Create(CreateUserDto dto, CancellationToken cancellationToken = default);
}
