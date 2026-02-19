namespace chat_app_aca.Entities;

public sealed class UserProfile
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}