using chat_app_aca.Data;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class ChatService
{
    private readonly IDbConnectionFactory _factory;

    public ChatService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Guid> CreateChatAsync(Guid ownerId, string title, CancellationToken cancellationToken = default)
    {
        var chatId = Guid.NewGuid();

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = uow.Transaction;
            command.CommandText = """
                                  INSERT INTO chats (id, title,owner_id) 
                                  VALUES (@chatId,@title,@ownerId);
                                  """;
            command.AddParameter("@chatId", chatId);
            command.AddParameter("@title", title);
            command.AddParameter("@ownerId", ownerId);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await uow.CommitAsync(cancellationToken);
            return chatId;
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<Chat>> GetMyChatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new List<Chat>();

        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT DISTINCT c.*
                              FROM chats c
                              LEFT JOIN chat_members cm ON cm.chat_id = c.id
                              WHERE c.owner_id=@userId OR cm.user_id=@userId
                              ORDER BY c.created_at;
                              """;
        command.AddParameter("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.MapToEntity<Chat>());
        }

        return result;
    }
}