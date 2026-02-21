namespace chat_app_aca.Entities;

public sealed class ChatInvitation
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid InvitedUserId { get; set; }
    public Guid InvitedBy { get; set; }
    public string Status { get; set; } // Pending | Approved | Rejected

    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}