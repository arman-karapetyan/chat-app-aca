using chat_app_aca.Data;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class AuthService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserAccount?> AuthenticateAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        var hash = password.HashPassword();

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT *
                              FROM user_accounts
                              WHERE username = @username AND password_hash = @hash; 
                              """;
        command.AddParameter("@username", username);
        command.AddParameter("@hash", hash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? reader.MapToEntity<UserAccount>() : null;
    }

    public async Task RegisterAsync(UserAccount account, UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command1 = connection.CreateCommand();
            command1.Transaction = uow.Transaction;
            command1.CommandText = "SELECT id FROM user_accounts WHERE username=@username";
            command1.AddParameter("@username", account.Username);
            var existingUserId = await command1.ExecuteScalarAsync(cancellationToken);
            if (existingUserId is not null)
            {
                throw new InvalidOperationException($"Username: {account.Username} already exists.");
            }

            await using var command2 = connection.CreateCommand();
            command2.Transaction = uow.Transaction;
            command2.CommandText = """
                                   INSERT INTO user_accounts (id, username, email, password_hash) 
                                   VALUES (@id, @username, @email, @hash);
                                   """;
            command2.AddParameter("@id", account.Id);
            command2.AddParameter("@username", account.Username);
            command2.AddParameter("@email", account.Email);
            command2.AddParameter("@hash", account.PasswordHash);
            await command2.ExecuteNonQueryAsync(cancellationToken);

            await using var command3 = connection.CreateCommand();
            command3.Transaction = uow.Transaction;
            command3.CommandText = """
                                   INSERT INTO user_profiles (user_id, first_name, last_name, date_of_birth, gender)
                                   VALUES (@userId, @firstName, @lastName, @dateOfBirth, @gender);
                                   """;
            command3.AddParameter("@userId", profile.UserId);
            command3.AddParameter("@firstName", profile.FirstName);
            command3.AddParameter("@lastName", profile.LastName);
            command3.AddParameter("@dateOfBirth", profile.DateOfBirth);
            command3.AddParameter("@gender", profile.Gender);
            await command3.ExecuteNonQueryAsync(cancellationToken);

            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }
}