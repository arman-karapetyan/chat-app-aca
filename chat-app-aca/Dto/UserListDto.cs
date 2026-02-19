namespace chat_app_aca.Dto;

public class UserListDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}