using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Dto;
using TrilobitCS.Models;

namespace TrilobitCS.Repositories;

// Laravel: App\Repositories\UserRepository
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    // Laravel: User::where('nickname', $nickname)->first()
    public async Task<User?> FindByNickname(string nickname, CancellationToken cancellationToken = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname, cancellationToken);

    // Laravel: User::where('email', $email)->exists()
    public async Task<bool> EmailExists(string email, CancellationToken cancellationToken = default)
        => await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    // Laravel: User::where('nickname', $nickname)->exists()
    public async Task<bool> NicknameExists(string nickname, CancellationToken cancellationToken = default)
        => await _db.Users.AnyAsync(u => u.Nickname == nickname, cancellationToken);

    // Laravel: User::create([...])
    public async Task<User> Create(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = User.FromDto(dto);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return user;
    }
}
