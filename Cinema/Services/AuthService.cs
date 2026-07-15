using Cinema.Data;
using Cinema.Models;
using Microsoft.Data.SqlClient;

namespace Cinema.Services;

public sealed class AuthService
{
    public AppUser? Authenticate(string username, string password)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, Username, FullName, Role, IsActive, CreatedAt, PasswordHash, PasswordSalt
            FROM dbo.Users
            WHERE Username = @Username;", connection);
        command.Parameters.AddWithValue("@Username", username.Trim());

        using var reader = command.ExecuteReader();
        if (!reader.Read() || !reader.GetBoolean(reader.GetOrdinal("IsActive")))
            return null;

        var hash = (byte[])reader["PasswordHash"];
        var salt = (byte[])reader["PasswordSalt"];
        if (!PasswordService.Verify(password, hash, salt))
            return null;

        return new AppUser
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            IsActive = true,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public AppUser Register(string fullName, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Numele și utilizatorul sunt obligatorii.");
        if (password.Length < 8)
            throw new InvalidOperationException("Parola trebuie să conțină cel puțin 8 caractere.");

        var (hash, salt) = PasswordService.Hash(password);
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            INSERT INTO dbo.Users (Username, FullName, PasswordHash, PasswordSalt, Role, IsActive)
            OUTPUT INSERTED.Id, INSERTED.CreatedAt
            VALUES (@Username, @FullName, @Hash, @Salt, 'User', 1);", connection);
        command.Parameters.AddWithValue("@Username", username.Trim());
        command.Parameters.AddWithValue("@FullName", fullName.Trim());
        command.Parameters.Add("@Hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
        command.Parameters.Add("@Salt", System.Data.SqlDbType.VarBinary, 16).Value = salt;

        try
        {
            using var reader = command.ExecuteReader();
            reader.Read();
            return new AppUser
            {
                Id = reader.GetInt32(0),
                Username = username.Trim(),
                FullName = fullName.Trim(),
                Role = Roles.User,
                IsActive = true,
                CreatedAt = reader.GetDateTime(1)
            };
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Acest nume de utilizator este deja folosit.", ex);
        }
    }
}
