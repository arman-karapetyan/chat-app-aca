namespace chat_app_aca.Dto;

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}