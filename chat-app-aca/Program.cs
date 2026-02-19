using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using chat_app_aca.Data;
using chat_app_aca.Dto;
using chat_app_aca.Entities;
using chat_app_aca.Extensions;
using chat_app_aca.Services;

namespace chat_app_aca;

class Program
{
    private const string ConnectionString =
        "Host=localhost;Port=5434;Database=chat_app_aca_db;Username=devuser;Password=devpass";

    private static IDbConnectionFactory _dbConnectionFactory;
    private static AuthService _authService;
    private static UserService _userService;

    private static UserAccount? _currentUser;

    static async Task Main(string[] args)
    {
        InitializeServices();

        while (true)
        {
            PrintMenu();
            Console.Write("> ");

            var choice = Console.ReadLine();

            try
            {
                if (_currentUser == null)
                {
                    await HandleUnauthenticated(choice);
                    continue;
                }

                await HandleAuthenticated(choice);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
            }
        }
    }

    private static async Task HandleAuthenticated(string? choice)
    {
        switch (choice)
        {
            case "1":
                await ShowMyAccount();
                break;

            case "2":
                await UpdateMyAccount();
                break;

            case "3":
                await ListUsers();
                break;

            case "0":
                Logout();
                break;
        }
    }

    private static async Task ShowMyAccount()
    {
        if (_currentUser == null)
        {
            Console.WriteLine("Not authenticated!");
            return;
        }

        var dto = await _userService.GetCurrentUserAsync(_currentUser.Id);

        if (dto == null)
        {
            Console.WriteLine("User not found!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("==== MY ACCOUNT ====");
        Console.WriteLine($"Id         : {dto.Id}");
        Console.WriteLine($"Username   : {dto.Username}");
        Console.WriteLine($"Email      : {dto.Email}");
        Console.WriteLine($"Name:      : {dto.FirstName} {dto.LastName}");
        Console.WriteLine($"Birth date : {dto.DateOfBirth?.ToString("yyyy-MM-dd") ?? "-"}");
        Console.WriteLine($"Gender     : {dto.Gender}");
        Console.WriteLine($"Created    : {dto.CreatedAt.FromMs():yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Updated    : {dto.UpdatedAt.FromMs():yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("===================");
        Console.WriteLine();
    }

    private static async Task UpdateMyAccount()
    {
        if (_currentUser == null)
        {
            Console.WriteLine("Not authenticated!");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("==== UPDATE ACCOUNT ====");

        Console.Write("Email: ");
        var email = Console.ReadLine();
        Console.Write("First name: ");
        var firstName = Console.ReadLine();
        Console.Write("Last name: ");
        var lastName = Console.ReadLine();
        Console.Write("Birth date (yyyy-MM-dd) or empty: ");
        var birthDateInput = Console.ReadLine();
        var birthDate = string.IsNullOrWhiteSpace(birthDateInput)
            ? (DateTime?)null
            : DateTime.Parse(birthDateInput);
        Console.Write("Gender (M/F) or empty: ");
        var genderInput = Console.ReadLine();
        var gender = string.IsNullOrWhiteSpace(genderInput)
            ? null
            : genderInput;

        var request = new UpdateAccountDto
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = birthDate,
            Gender = gender
        };

        await _userService.UpdateAccountAsync(_currentUser.Id, request);

        Console.WriteLine("Account updated successfully!");
        Console.WriteLine();
    }

    private static async Task ListUsers()
    {
        if (_currentUser == null)
        {
            Console.WriteLine("Not authenticated!");
            return;
        }

        var users = await _userService.GetAllWithProfilesAsync();
        foreach (var user in users)
        {
            Console.WriteLine(
                $"{user.Username}\t" +
                $"{user.FirstName} {user.LastName}\t" +
                $"{user.Email}\t" +
                $"created: {user.CreatedAt.FromMs():yyyy-MM-dd HH:mm}\t" +
                $"updated: {user.UpdatedAt.FromMs():yyyy-MM-dd HH:mm}"
            );
        }
    }

    private static void Logout()
    {
        _currentUser = null;
        Console.WriteLine("Logged out successfully!");
    }

    private static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("===== CHAT APP =====");
        if (_currentUser == null)
        {
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Exit");
            return;
        }

        Console.WriteLine($"Logged in as {_currentUser.Username}");
        Console.WriteLine();
        Console.WriteLine("1. Show my account");
        Console.WriteLine("2. Update my account");
        Console.WriteLine("3. List users");
        Console.WriteLine("4. Create chat");
        Console.WriteLine("5. Invite user (@username)");
        Console.WriteLine("6. Respond to invite");
        Console.WriteLine("7. List my chats");
        Console.WriteLine("8. Enter chat");
        Console.WriteLine("9. Delete chat (owner only)");
        Console.WriteLine("0. Logout");
    }

    private static async Task HandleUnauthenticated(string? choice)
    {
        switch (choice)
        {
            case "1":
                await Register();
                break;

            case "2":
                await Login();
                break;

            case "0":
                Environment.Exit(0);
                break;
        }
    }


    private static async Task Register()
    {
        //User Account
        Console.Write("Username: ");
        var username = Console.ReadLine();
        Console.Write("Email: ");
        var email = Console.ReadLine();
        Console.Write("Password: ");
        var password = ReadPassword();

        //User Profile
        Console.Write("First name: ");
        var firstName = Console.ReadLine();
        Console.Write("Last name: ");
        var lastName = Console.ReadLine();

        var hash = password.HashPassword();
        var userId = Guid.NewGuid();

        var userAccount = new UserAccount
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = hash,
        };

        var userProfile = new UserProfile
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
        };

        await _authService.RegisterAsync(userAccount, userProfile);

        Console.WriteLine("Registration successful!");
    }

    private static string ReadPassword()
    {
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Length--;
                Console.WriteLine("\b \b");
                continue;
            }

            sb.Append(key.KeyChar);
            Console.Write("*");
        }

        Console.WriteLine();
        return sb.ToString();
    }

    private static async Task Login()
    {
        Console.Write("Username: ");
        var username = Console.ReadLine();
        Console.Write("Password: ");
        var password = ReadPassword();

        _currentUser = await _authService.AuthenticateAsync(username, password);

        if (_currentUser == null)
        {
            Console.WriteLine("Invalid credentials.");
            return;
        }

        // handle other things.....

        Console.WriteLine("Logged in successfully!");
    }

    private static void InitializeServices()
    {
        _dbConnectionFactory = new PgDbConnectionFactory(ConnectionString);
        _authService = new AuthService(_dbConnectionFactory);
        _userService = new UserService(_dbConnectionFactory);
    }
}