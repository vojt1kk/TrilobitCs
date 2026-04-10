namespace TrilobitCS.Auth;

// Laravel: Hash::make() a Hash::check()
public class BcryptPasswordHasher
{
    // Laravel: Hash::make($password)
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    // Laravel: Hash::check($password, $user->password)
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
