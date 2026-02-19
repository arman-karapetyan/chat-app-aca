namespace chat_app_aca.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }

    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}