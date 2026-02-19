using chat_app_aca.Data;
using chat_app_aca.Dto;
using chat_app_aca.Extensions;

namespace chat_app_aca.Services;

public class UserService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT 
                                  ua.id,
                                  ua.username,
                                  ua.email,
                                  up.first_name,
                                  up.last_name,
                                  up.date_of_birth,
                                  up.gender,
                                  ua.created_at,
                                  ua.updated_at
                                  FROM user_accounts ua
                                  JOIN user_profiles up ON ua.id = up.user_id
                                  WHERE ua.id = @userId;
                              """;

        command.AddParameter("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? reader.MapToEntity<CurrentUserDto>() : null;
    }

    public async Task UpdateAccountAsync(Guid userId, UpdateAccountDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);

        await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command1 = connection.CreateCommand();
            command1.Transaction = uow.Transaction;
            command1.CommandText = "UPDATE user_accounts SET email = @email WHERE id = @userId";
            command1.AddParameter("@userId", userId);
            command1.AddParameter("@email", request.Email);
            await command1.ExecuteNonQueryAsync(cancellationToken);

            await using var command2 = connection.CreateCommand();
            command2.Transaction = uow.Transaction;
            command2.CommandText = """
                                   UPDATE user_profiles 
                                   SET
                                       first_name=@firstName,
                                       last_name=@lastName,
                                       date_of_birth=@dateOfBirth,
                                       gender=@gender
                                   WHERE user_id=@userId;
                                   """;
            command2.AddParameter("@userId", userId);
            command2.AddParameter("@firstName", request.FirstName);
            command2.AddParameter("@lastName", request.LastName);
            command2.AddParameter("@dateOfBirth", request.DateOfBirth);
            command2.AddParameter("@gender", request.Gender);
            await command2.ExecuteNonQueryAsync(cancellationToken);


            await uow.CommitAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserListDto>> GetAllWithProfilesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<UserListDto>();

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var uow = new UnitOfWork(connection);
        await using var command = connection.CreateCommand();
        command.Transaction = uow.Transaction;
        command.CommandText = """
                              SELECT
                                  ua.id,
                                  ua.username,
                                  ua.email,
                                  up.first_name,
                                  up.last_name,
                                  up.created_at,
                                  up.updated_at
                              FROM user_accounts ua
                              JOIN user_profiles up ON ua.id = up.user_id
                              ORDER BY ua.username;
                              """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.MapToEntity<UserListDto>());
        }

        return result;
    }
}