using System.Data;
using System.Text.RegularExpressions;
using Cinema.Data;
using Cinema.Models;
using Cinema.Services;
using Microsoft.Data.SqlClient;

namespace Cinema.Repositories;

public sealed class CinemaRepository
{
    public List<Film> GetFilms(bool activeOnly = false)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, Title, Genre, ReleaseYear, Rating, DurationMinutes, BasePrice, Description, IsActive
            FROM dbo.Films
            WHERE @ActiveOnly = 0 OR IsActive = 1
            ORDER BY Title;", connection);
        command.Parameters.AddWithValue("@ActiveOnly", activeOnly);
        using var reader = command.ExecuteReader();
        var items = new List<Film>();
        while (reader.Read())
            items.Add(ReadFilm(reader));
        return items;
    }

    public void SaveFilm(Film film)
    {
        ValidateFilm(film);
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        const string insertSql = @"
            INSERT INTO dbo.Films (Title, Genre, ReleaseYear, Rating, DurationMinutes, BasePrice, Description, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Title, @Genre, @Year, @Rating, @Duration, @Price, @Description, @Active);";
        const string updateSql = @"
            UPDATE dbo.Films SET Title=@Title, Genre=@Genre, ReleaseYear=@Year, Rating=@Rating,
                DurationMinutes=@Duration, BasePrice=@Price, Description=@Description, IsActive=@Active
            WHERE Id=@Id;";
        using var command = new SqlCommand(film.Id == 0 ? insertSql : updateSql, connection);
        AddFilmParameters(command, film);
        if (film.Id == 0)
            film.Id = Convert.ToInt32(command.ExecuteScalar());
        else
            command.ExecuteNonQuery();
    }

    public List<CinemaRoom> GetRooms(bool activeOnly = false)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, Name, Type, Floor, RowsCount, SeatsPerRow, IsActive
            FROM dbo.Rooms
            WHERE @ActiveOnly = 0 OR IsActive = 1
            ORDER BY Name;", connection);
        command.Parameters.AddWithValue("@ActiveOnly", activeOnly);
        using var reader = command.ExecuteReader();
        var items = new List<CinemaRoom>();
        while (reader.Read())
            items.Add(new CinemaRoom
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                Floor = reader.GetInt32(3),
                Rows = reader.GetInt32(4),
                SeatsPerRow = reader.GetInt32(5),
                IsActive = reader.GetBoolean(6)
            });
        return items;
    }

    public void SaveRoom(CinemaRoom room)
    {
        if (string.IsNullOrWhiteSpace(room.Name) || string.IsNullOrWhiteSpace(room.Type))
            throw new InvalidOperationException("Numele și tipul sălii sunt obligatorii.");
        if (room.Rows is < 1 or > 26 || room.SeatsPerRow is < 1 or > 30)
            throw new InvalidOperationException("Sala poate avea între 1 și 26 rânduri și între 1 și 30 locuri pe rând.");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        const string insertSql = @"
            INSERT INTO dbo.Rooms (Name, Type, Floor, RowsCount, SeatsPerRow, IsActive)
            OUTPUT INSERTED.Id VALUES (@Name, @Type, @Floor, @Rows, @Columns, @Active);";
        const string updateSql = @"
            UPDATE dbo.Rooms SET Name=@Name, Type=@Type, Floor=@Floor, RowsCount=@Rows,
                SeatsPerRow=@Columns, IsActive=@Active WHERE Id=@Id;";
        using var command = new SqlCommand(room.Id == 0 ? insertSql : updateSql, connection);
        command.Parameters.AddWithValue("@Name", room.Name.Trim());
        command.Parameters.AddWithValue("@Type", room.Type.Trim());
        command.Parameters.AddWithValue("@Floor", room.Floor);
        command.Parameters.AddWithValue("@Rows", room.Rows);
        command.Parameters.AddWithValue("@Columns", room.SeatsPerRow);
        command.Parameters.AddWithValue("@Active", room.IsActive);
        command.Parameters.AddWithValue("@Id", room.Id);
        if (room.Id == 0)
            room.Id = Convert.ToInt32(command.ExecuteScalar());
        else
            command.ExecuteNonQuery();
    }

    public List<Actor> GetActors()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT a.Id, a.Name, a.BirthYear, a.Nationality, a.FilmId, f.Title
            FROM dbo.Actors a INNER JOIN dbo.Films f ON f.Id=a.FilmId
            ORDER BY a.Name;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<Actor>();
        while (reader.Read())
            items.Add(new Actor
            {
                Id = reader.GetInt32(0), Name = reader.GetString(1), BirthYear = reader.GetInt32(2),
                Nationality = reader.GetString(3), FilmId = reader.GetInt32(4), FilmTitle = reader.GetString(5)
            });
        return items;
    }

    public void SaveActor(Actor actor)
    {
        if (string.IsNullOrWhiteSpace(actor.Name) || string.IsNullOrWhiteSpace(actor.Nationality) || actor.FilmId <= 0)
            throw new InvalidOperationException("Completează numele, naționalitatea și filmul actorului.");
        if (actor.BirthYear is < 1850 or > 2020)
            throw new InvalidOperationException("Anul nașterii nu este valid.");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        const string insertSql = @"
            INSERT INTO dbo.Actors (Name, BirthYear, Nationality, FilmId)
            OUTPUT INSERTED.Id VALUES (@Name, @Year, @Nationality, @FilmId);";
        const string updateSql = @"
            UPDATE dbo.Actors SET Name=@Name, BirthYear=@Year, Nationality=@Nationality, FilmId=@FilmId
            WHERE Id=@Id;";
        using var command = new SqlCommand(actor.Id == 0 ? insertSql : updateSql, connection);
        command.Parameters.AddWithValue("@Name", actor.Name.Trim());
        command.Parameters.AddWithValue("@Year", actor.BirthYear);
        command.Parameters.AddWithValue("@Nationality", actor.Nationality.Trim());
        command.Parameters.AddWithValue("@FilmId", actor.FilmId);
        command.Parameters.AddWithValue("@Id", actor.Id);
        if (actor.Id == 0)
            actor.Id = Convert.ToInt32(command.ExecuteScalar());
        else
            command.ExecuteNonQuery();
    }

    public void DeleteActor(int actorId)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand("DELETE FROM dbo.Actors WHERE Id=@Id;", connection);
        command.Parameters.AddWithValue("@Id", actorId);
        command.ExecuteNonQuery();
    }

    public List<Screening> GetScreenings(DateTime from, DateTime to, bool activeOnly = false)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT s.Id, s.FilmId, f.Title, s.RoomId, r.Name, s.StartTime, s.Price, s.IsActive,
                   r.RowsCount, r.SeatsPerRow
            FROM dbo.Screenings s
            INNER JOIN dbo.Films f ON f.Id=s.FilmId
            INNER JOIN dbo.Rooms r ON r.Id=s.RoomId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND (@ActiveOnly = 0 OR (s.IsActive=1 AND f.IsActive=1 AND r.IsActive=1))
            ORDER BY s.StartTime, f.Title;", connection);
        command.Parameters.AddWithValue("@From", from);
        command.Parameters.AddWithValue("@To", to);
        command.Parameters.AddWithValue("@ActiveOnly", activeOnly);
        using var reader = command.ExecuteReader();
        var items = new List<Screening>();
        while (reader.Read())
            items.Add(ReadScreening(reader));
        return items;
    }

    public void SaveScreening(Screening screening)
    {
        if (screening.FilmId <= 0 || screening.RoomId <= 0)
            throw new InvalidOperationException("Selectează filmul și sala.");
        if (screening.StartTime <= DateTime.Now)
            throw new InvalidOperationException("Programarea trebuie să fie în viitor.");
        if (screening.Price <= 0)
            throw new InvalidOperationException("Prețul trebuie să fie mai mare decât zero.");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        const string insertSql = @"
            INSERT INTO dbo.Screenings (FilmId, RoomId, StartTime, Price, IsActive)
            OUTPUT INSERTED.Id VALUES (@FilmId, @RoomId, @StartTime, @Price, @Active);";
        const string updateSql = @"
            UPDATE dbo.Screenings SET FilmId=@FilmId, RoomId=@RoomId, StartTime=@StartTime,
                Price=@Price, IsActive=@Active WHERE Id=@Id;";
        using var command = new SqlCommand(screening.Id == 0 ? insertSql : updateSql, connection);
        command.Parameters.AddWithValue("@FilmId", screening.FilmId);
        command.Parameters.AddWithValue("@RoomId", screening.RoomId);
        command.Parameters.AddWithValue("@StartTime", screening.StartTime);
        command.Parameters.AddWithValue("@Price", screening.Price);
        command.Parameters.AddWithValue("@Active", screening.IsActive);
        command.Parameters.AddWithValue("@Id", screening.Id);
        try
        {
            if (screening.Id == 0)
                screening.Id = Convert.ToInt32(command.ExecuteScalar());
            else
                command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Sala are deja o proiecție programată la această dată și oră.", ex);
        }
    }

    public bool DeleteScreening(int screeningId)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.Reservations WHERE ScreeningId=@Id;", connection);
        count.Parameters.AddWithValue("@Id", screeningId);
        if (Convert.ToInt32(count.ExecuteScalar()) > 0)
        {
            using var deactivate = new SqlCommand("UPDATE dbo.Screenings SET IsActive=0 WHERE Id=@Id;", connection);
            deactivate.Parameters.AddWithValue("@Id", screeningId);
            deactivate.ExecuteNonQuery();
            return false;
        }

        using var delete = new SqlCommand("DELETE FROM dbo.Screenings WHERE Id=@Id;", connection);
        delete.Parameters.AddWithValue("@Id", screeningId);
        delete.ExecuteNonQuery();
        return true;
    }

    public HashSet<string> GetOccupiedSeats(int screeningId)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT rs.SeatCode FROM dbo.ReservationSeats rs
            INNER JOIN dbo.Reservations r ON r.Id=rs.ReservationId
            WHERE rs.ScreeningId=@ScreeningId AND rs.IsActive=1 AND r.Status='Confirmed';", connection);
        command.Parameters.AddWithValue("@ScreeningId", screeningId);
        using var reader = command.ExecuteReader();
        var seats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            seats.Add(reader.GetString(0));
        return seats;
    }

    public Reservation CreateReservation(int userId, Screening screening, IReadOnlyCollection<string> seats)
    {
        if (seats.Count == 0)
            throw new InvalidOperationException("Selectează cel puțin un loc.");
        if (seats.Count > 10)
            throw new InvalidOperationException("Poți rezerva maximum 10 locuri simultan.");

        var normalizedSeats = seats.Select(s => s.Trim().ToUpperInvariant()).Distinct().OrderBy(s => s).ToList();
        if (normalizedSeats.Count != seats.Count || normalizedSeats.Any(s => !IsValidSeat(s, screening.RoomRows, screening.SeatsPerRow)))
            throw new InvalidOperationException("Selecția locurilor nu este validă.");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (var validate = new SqlCommand(@"
                SELECT COUNT(*) FROM dbo.Screenings s
                INNER JOIN dbo.Films f ON f.Id=s.FilmId
                INNER JOIN dbo.Rooms r ON r.Id=s.RoomId
                WHERE s.Id=@Id AND s.IsActive=1 AND f.IsActive=1 AND r.IsActive=1 AND s.StartTime>SYSDATETIME();",
                connection, transaction))
            {
                validate.Parameters.AddWithValue("@Id", screening.Id);
                if (Convert.ToInt32(validate.ExecuteScalar()) != 1)
                    throw new InvalidOperationException("Proiecția nu mai este disponibilă.");
            }

            foreach (var seat in normalizedSeats)
            {
                using var occupied = new SqlCommand(@"
                    SELECT COUNT(*) FROM dbo.ReservationSeats WITH (UPDLOCK, HOLDLOCK)
                    WHERE ScreeningId=@ScreeningId AND SeatCode=@Seat AND IsActive=1;", connection, transaction);
                occupied.Parameters.AddWithValue("@ScreeningId", screening.Id);
                occupied.Parameters.AddWithValue("@Seat", seat);
                if (Convert.ToInt32(occupied.ExecuteScalar()) > 0)
                    throw new InvalidOperationException($"Locul {seat} tocmai a fost rezervat de alt utilizator.");
            }

            var bookingCode = $"CM{DateTime.Now:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            var total = screening.Price * normalizedSeats.Count;
            long reservationId;
            using (var insertReservation = new SqlCommand(@"
                INSERT INTO dbo.Reservations (BookingCode, UserId, ScreeningId, TotalAmount, Status)
                OUTPUT INSERTED.Id VALUES (@Code, @UserId, @ScreeningId, @Total, 'Confirmed');", connection, transaction))
            {
                insertReservation.Parameters.AddWithValue("@Code", bookingCode);
                insertReservation.Parameters.AddWithValue("@UserId", userId);
                insertReservation.Parameters.AddWithValue("@ScreeningId", screening.Id);
                insertReservation.Parameters.AddWithValue("@Total", total);
                reservationId = Convert.ToInt64(insertReservation.ExecuteScalar());
            }

            foreach (var seat in normalizedSeats)
            {
                using var insertSeat = new SqlCommand(@"
                    INSERT INTO dbo.ReservationSeats (ReservationId, ScreeningId, SeatCode)
                    VALUES (@ReservationId, @ScreeningId, @Seat);", connection, transaction);
                insertSeat.Parameters.AddWithValue("@ReservationId", reservationId);
                insertSeat.Parameters.AddWithValue("@ScreeningId", screening.Id);
                insertSeat.Parameters.AddWithValue("@Seat", seat);
                insertSeat.ExecuteNonQuery();
            }

            transaction.Commit();
            return new Reservation
            {
                Id = reservationId,
                BookingCode = bookingCode,
                UserId = userId,
                ScreeningId = screening.Id,
                FilmTitle = screening.FilmTitle,
                RoomName = screening.RoomName,
                StartTime = screening.StartTime,
                CreatedAt = DateTime.Now,
                TotalAmount = total,
                Status = "Confirmed",
                Seats = string.Join(", ", normalizedSeats)
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<Reservation> GetReservations(int? userId = null)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT r.Id, r.BookingCode, r.UserId, u.FullName, r.ScreeningId, f.Title, rm.Name,
                   s.StartTime, r.CreatedAt, r.TotalAmount, r.Status,
                   ISNULL(STRING_AGG(rs.SeatCode, ', ') WITHIN GROUP (ORDER BY rs.SeatCode), '') AS Seats
            FROM dbo.Reservations r
            INNER JOIN dbo.Users u ON u.Id=r.UserId
            INNER JOIN dbo.Screenings s ON s.Id=r.ScreeningId
            INNER JOIN dbo.Films f ON f.Id=s.FilmId
            INNER JOIN dbo.Rooms rm ON rm.Id=s.RoomId
            LEFT JOIN dbo.ReservationSeats rs ON rs.ReservationId=r.Id
            WHERE @UserId IS NULL OR r.UserId=@UserId
            GROUP BY r.Id, r.BookingCode, r.UserId, u.FullName, r.ScreeningId, f.Title, rm.Name,
                     s.StartTime, r.CreatedAt, r.TotalAmount, r.Status
            ORDER BY r.CreatedAt DESC;", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId.HasValue ? userId.Value : DBNull.Value;
        using var reader = command.ExecuteReader();
        var items = new List<Reservation>();
        while (reader.Read())
            items.Add(ReadReservation(reader));
        return items;
    }

    public void CancelReservation(long reservationId, int? ownerUserId = null)
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var update = new SqlCommand(@"
            UPDATE r SET Status='Cancelled'
            FROM dbo.Reservations r
            INNER JOIN dbo.Screenings s ON s.Id=r.ScreeningId
            WHERE r.Id=@Id AND r.Status='Confirmed'
              AND (@OwnerId IS NULL OR (r.UserId=@OwnerId AND s.StartTime>SYSDATETIME()));", connection, transaction);
        update.Parameters.AddWithValue("@Id", reservationId);
        update.Parameters.Add("@OwnerId", SqlDbType.Int).Value = ownerUserId.HasValue ? ownerUserId.Value : DBNull.Value;
        if (update.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("Rezervarea nu poate fi anulată.");

        using var releaseSeats = new SqlCommand(
            "UPDATE dbo.ReservationSeats SET IsActive=0 WHERE ReservationId=@Id;", connection, transaction);
        releaseSeats.Parameters.AddWithValue("@Id", reservationId);
        releaseSeats.ExecuteNonQuery();
        transaction.Commit();
    }

    public List<AppUser> GetUsers()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, Username, FullName, Role, IsActive, CreatedAt
            FROM dbo.Users ORDER BY Role, FullName;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<AppUser>();
        while (reader.Read())
            items.Add(new AppUser
            {
                Id = reader.GetInt32(0), Username = reader.GetString(1), FullName = reader.GetString(2),
                Role = reader.GetString(3), IsActive = reader.GetBoolean(4), CreatedAt = reader.GetDateTime(5)
            });
        return items;
    }

    public void SaveUser(AppUser user, string? newPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.FullName))
            throw new InvalidOperationException("Numele și utilizatorul sunt obligatorii.");
        if (user.Role is not (Roles.Admin or Roles.User))
            throw new InvalidOperationException("Rolul nu este valid.");
        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 8)
            throw new InvalidOperationException("Parola trebuie să conțină cel puțin 8 caractere.");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        try
        {
            if (user.Id == 0)
            {
                if (string.IsNullOrEmpty(newPassword))
                    throw new InvalidOperationException("Parola este obligatorie pentru un cont nou.");
                var (hash, salt) = PasswordService.Hash(newPassword);
                using var insert = new SqlCommand(@"
                    INSERT INTO dbo.Users (Username, FullName, PasswordHash, PasswordSalt, Role, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@Username, @FullName, @Hash, @Salt, @Role, @Active);", connection);
                AddUserParameters(insert, user);
                insert.Parameters.Add("@Hash", SqlDbType.VarBinary, 32).Value = hash;
                insert.Parameters.Add("@Salt", SqlDbType.VarBinary, 16).Value = salt;
                user.Id = Convert.ToInt32(insert.ExecuteScalar());
            }
            else
            {
                using var update = new SqlCommand(@"
                    UPDATE dbo.Users SET Username=@Username, FullName=@FullName, Role=@Role, IsActive=@Active
                    WHERE Id=@Id;", connection);
                AddUserParameters(update, user);
                update.ExecuteNonQuery();

                if (!string.IsNullOrEmpty(newPassword))
                {
                    var (hash, salt) = PasswordService.Hash(newPassword);
                    using var password = new SqlCommand(@"
                        UPDATE dbo.Users SET PasswordHash=@Hash, PasswordSalt=@Salt WHERE Id=@Id;", connection);
                    password.Parameters.Add("@Hash", SqlDbType.VarBinary, 32).Value = hash;
                    password.Parameters.Add("@Salt", SqlDbType.VarBinary, 16).Value = salt;
                    password.Parameters.AddWithValue("@Id", user.Id);
                    password.ExecuteNonQuery();
                }
            }
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Acest nume de utilizator există deja.", ex);
        }
    }

    public DashboardStats GetDashboardStats()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM dbo.Films WHERE IsActive=1),
                (SELECT COUNT(*) FROM dbo.Screenings WHERE IsActive=1 AND StartTime>SYSDATETIME()),
                (SELECT COUNT(*) FROM dbo.Users WHERE IsActive=1 AND Role='User'),
                (SELECT COUNT(*) FROM dbo.Reservations WHERE Status='Confirmed'),
                (SELECT ISNULL(SUM(TotalAmount),0) FROM dbo.Reservations WHERE Status='Confirmed');", connection);
        using var reader = command.ExecuteReader();
        reader.Read();
        return new DashboardStats
        {
            ActiveFilms = reader.GetInt32(0), UpcomingScreenings = reader.GetInt32(1),
            ActiveUsers = reader.GetInt32(2), Reservations = reader.GetInt32(3), Revenue = reader.GetDecimal(4)
        };
    }

    public List<LegacyTicket> GetLegacyTickets()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT Id, LegacyId, LegacyFilmId, LegacyRoomId, ShowTime, PurchaseDate, Seats, Source
            FROM dbo.LegacyTickets ORDER BY LegacyId;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<LegacyTicket>();
        while (reader.Read())
            items.Add(new LegacyTicket
            {
                Id = reader.GetInt64(0), LegacyId = reader.GetInt64(1), LegacyFilmId = reader.GetInt32(2),
                LegacyRoomId = reader.GetInt32(3), ShowTime = reader.GetString(4), PurchaseDate = reader.GetString(5),
                Seats = reader.GetString(6), Source = reader.GetString(7)
            });
        return items;
    }

    private static Film ReadFilm(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), Title = reader.GetString(1), Genre = reader.GetString(2),
        ReleaseYear = reader.GetInt32(3), Rating = reader.GetDecimal(4), DurationMinutes = reader.GetInt32(5),
        BasePrice = reader.GetDecimal(6), Description = reader.GetString(7), IsActive = reader.GetBoolean(8)
    };

    private static Screening ReadScreening(SqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0), FilmId = reader.GetInt32(1), FilmTitle = reader.GetString(2),
        RoomId = reader.GetInt32(3), RoomName = reader.GetString(4), StartTime = reader.GetDateTime(5),
        Price = reader.GetDecimal(6), IsActive = reader.GetBoolean(7), RoomRows = reader.GetInt32(8),
        SeatsPerRow = reader.GetInt32(9)
    };

    private static Reservation ReadReservation(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0), BookingCode = reader.GetString(1), UserId = reader.GetInt32(2),
        UserName = reader.GetString(3), ScreeningId = reader.GetInt32(4), FilmTitle = reader.GetString(5),
        RoomName = reader.GetString(6), StartTime = reader.GetDateTime(7), CreatedAt = reader.GetDateTime(8),
        TotalAmount = reader.GetDecimal(9), Status = reader.GetString(10), Seats = reader.GetString(11)
    };

    private static void ValidateFilm(Film film)
    {
        if (string.IsNullOrWhiteSpace(film.Title) || string.IsNullOrWhiteSpace(film.Genre))
            throw new InvalidOperationException("Titlul și genul sunt obligatorii.");
        if (film.ReleaseYear is < 1888 or > 2100 || film.Rating is < 0 or > 10 || film.DurationMinutes <= 0 || film.BasePrice <= 0)
            throw new InvalidOperationException("Anul, ratingul, durata sau prețul nu sunt valide.");
    }

    private static void AddFilmParameters(SqlCommand command, Film film)
    {
        command.Parameters.AddWithValue("@Title", film.Title.Trim());
        command.Parameters.AddWithValue("@Genre", film.Genre.Trim());
        command.Parameters.AddWithValue("@Year", film.ReleaseYear);
        command.Parameters.AddWithValue("@Rating", film.Rating);
        command.Parameters.AddWithValue("@Duration", film.DurationMinutes);
        command.Parameters.AddWithValue("@Price", film.BasePrice);
        command.Parameters.AddWithValue("@Description", film.Description?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("@Active", film.IsActive);
        command.Parameters.AddWithValue("@Id", film.Id);
    }

    private static void AddUserParameters(SqlCommand command, AppUser user)
    {
        command.Parameters.AddWithValue("@Username", user.Username.Trim());
        command.Parameters.AddWithValue("@FullName", user.FullName.Trim());
        command.Parameters.AddWithValue("@Role", user.Role);
        command.Parameters.AddWithValue("@Active", user.IsActive);
        command.Parameters.AddWithValue("@Id", user.Id);
    }

    private static bool IsValidSeat(string seatCode, int rows, int columns)
    {
        var match = Regex.Match(seatCode, "^([A-Z])-([1-9][0-9]?)$");
        if (!match.Success)
            return false;
        var row = match.Groups[1].Value[0] - 'A' + 1;
        var column = int.Parse(match.Groups[2].Value);
        return row <= rows && column <= columns;
    }
}
