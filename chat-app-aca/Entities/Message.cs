namespace chat_app_aca.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid? SenderId { get; set; }
    public string? SenderUsername { get; set; }
    public string Content { get; set; }
    public long CreatedAt { get; set; }
}