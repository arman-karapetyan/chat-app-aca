using chat_app_aca.Data;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class PollingService
{
    private readonly IDbConnectionFactory _factory;

    public PollingService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task PoolInvitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var seenInvites = new HashSet<Guid>();
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                                  SELECT *
                                  FROM chat_invitations
                                  WHERE invited_user_id=@userId AND status='Pending'
                                  ORDER BY created_at;
                                  """;
            command.AddParameter("@userId", userId);
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var invite = reader.MapToEntity<ChatInvitation>();
                if (seenInvites.Add(invite.Id))
                {
                    Console.WriteLine($"INVITE: {invite.Id} to chat {invite.ChatId}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}