using chat_app_aca.Data;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class InvitationService
{
    private readonly IDbConnectionFactory _factory;

    public InvitationService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InviteAsync(Guid chatId, Guid inviterId, string username,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            Guid invitedUserId;
            await using (var findCommand = connection.CreateCommand())
            {
                findCommand.Transaction = uow.Transaction;
                findCommand.CommandText = "SELECT id FROM user_accounts WHERE username=@username";
                findCommand.AddParameter("@username", username);

                invitedUserId = (Guid)(await findCommand.ExecuteScalarAsync(cancellationToken) ??
                                       throw new InvalidOperationException($"User: {username} not found."));
            }

            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = uow.Transaction;
            insertCommand.CommandText = """
                                        INSERT INTO chat_invitations (id, chat_id, invited_user_id, invited_by,status)
                                        VALUES (@id,@chatId,@invitedUserId,@invitedBy,'Pending');
                                        """;
            insertCommand.AddParameter("@id", Guid.NewGuid());
            insertCommand.AddParameter("@chatId", chatId);
            insertCommand.AddParameter("@invitedUserId", invitedUserId);
            insertCommand.AddParameter("@invitedBy", inviterId);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RespondAsync(Guid invitationId, bool approve, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            ChatInvitation chatInvitation;
            await using (var readInvitationCommand = connection.CreateCommand())
            {
                readInvitationCommand.Transaction = uow.Transaction;
                readInvitationCommand.CommandText = "SELECT * FROM chat_invitations WHERE id=@id";
                readInvitationCommand.AddParameter("@id", invitationId);

                await using var reader = await readInvitationCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException($"Invitation: {invitationId} not found.");
                }

                chatInvitation = reader.MapToEntity<ChatInvitation>();
            }

            await using (var updateInvitationCommand = connection.CreateCommand())
            {
                updateInvitationCommand.Transaction = uow.Transaction;
                updateInvitationCommand.CommandText = "UPDATE chat_invitations SET status = @status WHERE id=@id";
                updateInvitationCommand.AddParameter("@status", approve ? "Approved" : "Rejected");
                updateInvitationCommand.AddParameter("@id", invitationId);

                await updateInvitationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (approve)
            {
                await using var addMemberCommand = connection.CreateCommand();
                addMemberCommand.Transaction = uow.Transaction;
                addMemberCommand.CommandText = """
                                               INSERT INTO chat_members (chat_id, user_id)
                                               VALUES (@chatId,@userId);
                                               """;
                addMemberCommand.AddParameter("@chatId", chatInvitation.ChatId);
                addMemberCommand.AddParameter("@userId", chatInvitation.InvitedUserId);
                await addMemberCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var systemMessageCommand = connection.CreateCommand())
            {
                systemMessageCommand.Transaction = uow.Transaction;
                systemMessageCommand.CommandText = """
                                                   INSERT INTO messages (id, chat_id, sender_id, content)
                                                       SELECT 
                                                           uuid_generate_v4(),
                                                           @chatId,
                                                           NULL,
                                                           first_name || ' ' || last_name || 
                                                            CASE 
                                                                WHEN @approve THEN ' has been added to the chat.'
                                                                ELSE ' has rejected the invitation.'
                                                            END
                                                       FROM user_profiles
                                                       WHERE user_id = @userId;
                                                   """;
                systemMessageCommand.AddParameter("@chatId", chatInvitation.ChatId);
                systemMessageCommand.AddParameter("@userId", chatInvitation.InvitedUserId);
                systemMessageCommand.AddParameter("@approve", approve);
                await systemMessageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }
}