namespace chat_app_aca.Dto;

public class UpdateAccountDto
{
    public string Email { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}