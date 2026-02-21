namespace chat_app_aca.Entities;

public sealed class Chat
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid OwnerId { get; set; }
    
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}