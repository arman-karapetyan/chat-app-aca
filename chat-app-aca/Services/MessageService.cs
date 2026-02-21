using chat_app_aca.Data;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class MessageService
{
    private readonly IDbConnectionFactory _factory;

    public MessageService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Message>> GetMessagesAsync(Guid chatId, long afterTimestamp,
        CancellationToken cancellationToken = default)
    {
        var result = new List<Message>();

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT 
                                  m.id,
                                  m.chat_id,
                                  m.sender_id,
                                  u.username as sender_username,
                                  m.content,
                                  m.created_at
                              FROM messages m
                              LEFT JOIN user_accounts u ON u.id=m.sender_id
                              WHERE m.chat_id=@chatId AND m.created_at>@timestamp
                              ORDER BY m.created_at;
                              """;

        command.AddParameter("@chatId", chatId);
        command.AddParameter("@timestamp", afterTimestamp);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.MapToEntity<Message>());
        }

        return result;
    }

    public async Task SendMessageAsync(Guid chatId, Guid senderId, string content,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = uow.Transaction;
            command.CommandText =
                "INSERT INTO messages (id, chat_id, sender_id, content) VALUES (uuid_generate_v4(), @chatId, @senderId, @content)";
            command.AddParameter("@chatId", chatId);
            command.AddParameter("@senderId", senderId);
            command.AddParameter("@content", content);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }
}