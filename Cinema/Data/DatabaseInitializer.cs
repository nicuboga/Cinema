using System.Data;
using System.Data.SQLite;
using System.IO;
using Cinema.Services;
using Microsoft.Data.SqlClient;

namespace Cinema.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        CreateDatabase();
        CreateSchema();
        SeedUsers();
        SeedFilms();
        SeedRooms();
        SeedActors();
        EnsureUpcomingScreenings();
        ImportLegacyTickets();
    }

    public static void Verify()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            SELECT
                (SELECT COUNT(*) FROM dbo.Users),
                (SELECT COUNT(*) FROM dbo.Films),
                (SELECT COUNT(*) FROM dbo.Rooms),
                (SELECT COUNT(*) FROM dbo.Actors),
                (SELECT COUNT(*) FROM dbo.Screenings WHERE StartTime > SYSDATETIME()),
                (SELECT COUNT(*) FROM dbo.LegacyTickets);", connection);
        using var reader = command.ExecuteReader();
        reader.Read();
        if (reader.GetInt32(0) < 2 || reader.GetInt32(1) < 39 || reader.GetInt32(2) < 10 ||
            reader.GetInt32(3) < 12 || reader.GetInt32(4) == 0 || reader.GetInt32(5) < 9)
            throw new InvalidOperationException("Verificarea datelor inițiale din CinemaModernDB nu a reușit.");
    }

    private static void CreateDatabase()
    {
        using var connection = new SqlConnection(SqlConnectionFactory.MasterConnectionString);
        connection.Open();
        using var command = new SqlCommand($@"
            IF DB_ID(N'{SqlConnectionFactory.DatabaseName}') IS NULL
                CREATE DATABASE [{SqlConnectionFactory.DatabaseName}];", connection);
        command.ExecuteNonQuery();
    }

    private static void CreateSchema()
    {
        const string sql = @"
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username NVARCHAR(60) NOT NULL,
        FullName NVARCHAR(120) NOT NULL,
        PasswordHash VARBINARY(32) NOT NULL,
        PasswordSalt VARBINARY(16) NOT NULL,
        Role NVARCHAR(20) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'User')),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
END;

IF OBJECT_ID('dbo.Films', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Films (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Films PRIMARY KEY,
        Title NVARCHAR(160) NOT NULL,
        Genre NVARCHAR(60) NOT NULL,
        ReleaseYear INT NOT NULL,
        Rating DECIMAL(3,1) NOT NULL,
        DurationMinutes INT NOT NULL,
        BasePrice DECIMAL(10,2) NOT NULL CONSTRAINT DF_Films_BasePrice DEFAULT 30,
        Description NVARCHAR(800) NOT NULL CONSTRAINT DF_Films_Description DEFAULT '',
        IsActive BIT NOT NULL CONSTRAINT DF_Films_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Films_Title ON dbo.Films(Title);
END;

IF OBJECT_ID('dbo.Rooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL,
        Type NVARCHAR(40) NOT NULL,
        Floor INT NOT NULL,
        RowsCount INT NOT NULL,
        SeatsPerRow INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Rooms_Name ON dbo.Rooms(Name);
END;

IF OBJECT_ID('dbo.Actors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Actors (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Actors PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        BirthYear INT NOT NULL,
        Nationality NVARCHAR(80) NOT NULL,
        FilmId INT NOT NULL,
        CONSTRAINT FK_Actors_Films FOREIGN KEY (FilmId) REFERENCES dbo.Films(Id)
    );
END;

IF OBJECT_ID('dbo.Screenings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Screenings (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Screenings PRIMARY KEY,
        FilmId INT NOT NULL,
        RoomId INT NOT NULL,
        StartTime DATETIME2(0) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Screenings_IsActive DEFAULT 1,
        CONSTRAINT FK_Screenings_Films FOREIGN KEY (FilmId) REFERENCES dbo.Films(Id),
        CONSTRAINT FK_Screenings_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id)
    );
    CREATE UNIQUE INDEX UX_Screenings_Room_Start ON dbo.Screenings(RoomId, StartTime);
    CREATE INDEX IX_Screenings_Film_Start ON dbo.Screenings(FilmId, StartTime);
END;

IF OBJECT_ID('dbo.Reservations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservations (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reservations PRIMARY KEY,
        BookingCode NVARCHAR(20) NOT NULL,
        UserId INT NOT NULL,
        ScreeningId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Reservations_CreatedAt DEFAULT SYSDATETIME(),
        TotalAmount DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Reservations_Status DEFAULT 'Confirmed'
            CONSTRAINT CK_Reservations_Status CHECK (Status IN ('Confirmed', 'Cancelled')),
        CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Reservations_Screenings FOREIGN KEY (ScreeningId) REFERENCES dbo.Screenings(Id)
    );
    CREATE UNIQUE INDEX UX_Reservations_BookingCode ON dbo.Reservations(BookingCode);
END;

IF OBJECT_ID('dbo.ReservationSeats', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReservationSeats (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReservationSeats PRIMARY KEY,
        ReservationId BIGINT NOT NULL,
        ScreeningId INT NOT NULL,
        SeatCode NVARCHAR(12) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReservationSeats_IsActive DEFAULT 1,
        CONSTRAINT FK_ReservationSeats_Reservations FOREIGN KEY (ReservationId)
            REFERENCES dbo.Reservations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ReservationSeats_Screenings FOREIGN KEY (ScreeningId)
            REFERENCES dbo.Screenings(Id)
    );
END;

IF COL_LENGTH('dbo.ReservationSeats', 'IsActive') IS NULL
    ALTER TABLE dbo.ReservationSeats ADD IsActive BIT NOT NULL
        CONSTRAINT DF_ReservationSeats_IsActive DEFAULT 1;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ReservationSeats') AND name='UX_ReservationSeats_Screening_Seat')
    DROP INDEX UX_ReservationSeats_Screening_Seat ON dbo.ReservationSeats;

EXEC('CREATE UNIQUE INDEX UX_ReservationSeats_Screening_Seat
    ON dbo.ReservationSeats(ScreeningId, SeatCode) WHERE IsActive=1;');

IF OBJECT_ID('dbo.LegacyTickets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyTickets (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LegacyTickets PRIMARY KEY,
        LegacyId BIGINT NOT NULL,
        LegacyFilmId INT NOT NULL,
        LegacyRoomId INT NOT NULL,
        ShowTime NVARCHAR(20) NOT NULL,
        PurchaseDate NVARCHAR(40) NOT NULL,
        Seats NVARCHAR(500) NOT NULL,
        Source NVARCHAR(260) NOT NULL,
        ImportedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LegacyTickets_ImportedAt DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_LegacyTickets_Source_Id ON dbo.LegacyTickets(Source, LegacyId);
END;";

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        command.ExecuteNonQuery();
    }

    private static void SeedUsers()
    {
        SeedUser("admin", "Administrator Cinema", "Admin123!", "Admin");
        SeedUser("user", "Client Cinema", "User123!", "User");

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var cleanup = new SqlCommand(@"
            UPDATE dbo.Users SET FullName='Client Cinema'
            WHERE Username='user' AND FullName IN ('Utilizator Demo', 'Utilizator');", connection);
        cleanup.ExecuteNonQuery();
    }

    private static void SeedUser(string username, string fullName, string password, string role)
    {
        var (hash, salt) = PasswordService.Hash(password);
        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        using var command = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
                INSERT INTO dbo.Users (Username, FullName, PasswordHash, PasswordSalt, Role, IsActive)
                VALUES (@Username, @FullName, @Hash, @Salt, @Role, 1);", connection);
        command.Parameters.AddWithValue("@Username", username);
        command.Parameters.AddWithValue("@FullName", fullName);
        command.Parameters.Add("@Hash", SqlDbType.VarBinary, 32).Value = hash;
        command.Parameters.Add("@Salt", SqlDbType.VarBinary, 16).Value = salt;
        command.Parameters.AddWithValue("@Role", role);
        command.ExecuteNonQuery();
    }

    private static void SeedFilms()
    {
        var films = new (string Title, string Genre, int Year, decimal Rating, int Duration, bool Active)[]
        {
            ("The Shawshank Redemption", "Dramă", 1994, 9.3m, 142, true),
            ("The Godfather", "Crimă", 1972, 9.2m, 175, true),
            ("The Dark Knight", "Acțiune", 2008, 9.0m, 152, true),
            ("Forrest Gump", "Dramă", 1994, 8.8m, 142, true),
            ("Inception", "SF", 2010, 8.8m, 148, true),
            ("Fight Club", "Dramă", 1999, 8.8m, 139, false),
            ("The Matrix", "SF", 1999, 8.7m, 136, true),
            ("Interstellar", "SF", 2014, 8.6m, 169, true),
            ("Gladiator", "Acțiune", 2000, 8.5m, 155, false),
            ("Titanic", "Romantic", 1997, 7.9m, 195, true),
            ("Avatar", "SF", 2009, 7.8m, 162, true),
            ("Joker", "Dramă", 2019, 8.4m, 122, true),
            ("Whiplash", "Dramă", 2014, 8.5m, 106, true),
            ("Parasite", "Thriller", 2019, 8.5m, 132, false),
            ("The Lion King", "Animație", 1994, 8.5m, 88, true),
            ("Toy Story", "Animație", 1995, 8.3m, 81, true),
            ("Up", "Animație", 2009, 8.2m, 96, true),
            ("Coco", "Animație", 2017, 8.4m, 105, true),
            ("Finding Nemo", "Animație", 2003, 8.1m, 100, false),
            ("Spirited Away", "Animație", 2001, 8.6m, 125, true),
            ("Harry Potter 1", "Fantasy", 2001, 7.6m, 152, true),
            ("Harry Potter 2", "Fantasy", 2002, 7.4m, 161, true),
            ("LOTR Fellowship", "Fantasy", 2001, 8.8m, 178, true),
            ("LOTR Two Towers", "Fantasy", 2002, 8.7m, 179, false),
            ("LOTR Return King", "Fantasy", 2003, 9.0m, 201, true),
            ("Jurassic Park", "Aventură", 1993, 8.1m, 127, true),
            ("Pirates Caribbean", "Aventură", 2003, 8.0m, 143, true),
            ("Indiana Jones", "Aventură", 1989, 8.2m, 127, false),
            ("Alien", "Horror", 1979, 8.4m, 117, true),
            ("It", "Horror", 2017, 7.3m, 135, true),
            ("The Conjuring", "Horror", 2013, 7.5m, 112, false),
            ("Deadpool", "Comedie", 2016, 8.0m, 108, true),
            ("The Hangover", "Comedie", 2009, 7.7m, 100, true),
            ("Superbad", "Comedie", 2007, 7.6m, 113, true),
            ("Rocky", "Sport", 1976, 8.1m, 120, true),
            ("Creed", "Sport", 2015, 7.6m, 133, true),
            ("John Wick", "Acțiune", 2014, 7.9m, 101, true),
            ("Casino Royale", "Acțiune", 2006, 8.0m, 144, false),
            ("Skyfall", "Acțiune", 2012, 7.8m, 143, true)
        };

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        foreach (var film in films)
        {
            using var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.Films WHERE Title = @Title)
                    INSERT INTO dbo.Films
                        (Title, Genre, ReleaseYear, Rating, DurationMinutes, BasePrice, Description, IsActive)
                    VALUES
                        (@Title, @Genre, @Year, @Rating, @Duration, 30, '', @Active);", connection);
            command.Parameters.AddWithValue("@Title", film.Title);
            command.Parameters.AddWithValue("@Genre", film.Genre);
            command.Parameters.AddWithValue("@Year", film.Year);
            command.Parameters.AddWithValue("@Rating", film.Rating);
            command.Parameters.AddWithValue("@Duration", film.Duration);
            command.Parameters.AddWithValue("@Active", film.Active);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedRooms()
    {
        var rooms = new (string Name, int Rows, int Columns, string Type, int Floor, bool Active)[]
        {
            ("Sala 1", 8, 10, "Standard", 0, true),
            ("Sala 2", 10, 10, "3D", 0, true),
            ("Sala 3", 10, 12, "Standard", 1, true),
            ("Sala 4", 8, 10, "Standard", 1, true),
            ("Sala 5", 6, 10, "VIP", 2, true),
            ("Sala 6", 9, 10, "3D", 2, false),
            ("Sala 7", 10, 10, "Standard", 0, true),
            ("Sala 8", 10, 11, "IMAX", 2, false),
            ("Sala 9", 10, 12, "IMAX", 2, true),
            ("Sala 10", 8, 10, "VIP", 1, false)
        };

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        foreach (var room in rooms)
        {
            using var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE Name = @Name)
                    INSERT INTO dbo.Rooms (Name, Type, Floor, RowsCount, SeatsPerRow, IsActive)
                    VALUES (@Name, @Type, @Floor, @Rows, @Columns, @Active);", connection);
            command.Parameters.AddWithValue("@Name", room.Name);
            command.Parameters.AddWithValue("@Type", room.Type);
            command.Parameters.AddWithValue("@Floor", room.Floor);
            command.Parameters.AddWithValue("@Rows", room.Rows);
            command.Parameters.AddWithValue("@Columns", room.Columns);
            command.Parameters.AddWithValue("@Active", room.Active);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedActors()
    {
        var actors = new (string Name, int BirthYear, string Nationality, string FilmTitle)[]
        {
            ("Tim Robbins", 1958, "American", "The Shawshank Redemption"),
            ("Morgan Freeman", 1937, "American", "The Shawshank Redemption"),
            ("Marlon Brando", 1924, "American", "The Godfather"),
            ("Al Pacino", 1940, "American", "The Godfather"),
            ("Christian Bale", 1974, "British", "The Dark Knight"),
            ("Heath Ledger", 1979, "Australian", "The Dark Knight"),
            ("Leonardo DiCaprio", 1974, "American", "Inception"),
            ("Joseph Gordon-Levitt", 1981, "American", "Inception"),
            ("Matthew McConaughey", 1969, "American", "Interstellar"),
            ("Anne Hathaway", 1982, "American", "Interstellar"),
            ("Russell Crowe", 1964, "New Zealand", "Gladiator"),
            ("Joaquin Phoenix", 1974, "American", "Gladiator")
        };

        using var connection = SqlConnectionFactory.Create();
        connection.Open();
        foreach (var actor in actors)
        {
            using var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.Actors WHERE Name = @Name)
                    INSERT INTO dbo.Actors (Name, BirthYear, Nationality, FilmId)
                    SELECT @Name, @BirthYear, @Nationality, Id FROM dbo.Films WHERE Title = @FilmTitle;", connection);
            command.Parameters.AddWithValue("@Name", actor.Name);
            command.Parameters.AddWithValue("@BirthYear", actor.BirthYear);
            command.Parameters.AddWithValue("@Nationality", actor.Nationality);
            command.Parameters.AddWithValue("@FilmTitle", actor.FilmTitle);
            command.ExecuteNonQuery();
        }
    }

    private static void EnsureUpcomingScreenings()
    {
        using var connection = SqlConnectionFactory.Create();
        connection.Open();

        var filmIds = new List<(int Id, decimal Price)>();
        using (var filmsCommand = new SqlCommand(
            "SELECT TOP (12) Id, BasePrice FROM dbo.Films WHERE IsActive = 1 ORDER BY Rating DESC, Id;", connection))
        using (var reader = filmsCommand.ExecuteReader())
        {
            while (reader.Read())
                filmIds.Add((reader.GetInt32(0), reader.GetDecimal(1)));
        }

        var rooms = new List<(int Id, string Type)>();
        using (var roomsCommand = new SqlCommand(
            "SELECT TOP (5) Id, Type FROM dbo.Rooms WHERE IsActive = 1 ORDER BY Id;", connection))
        using (var reader = roomsCommand.ExecuteReader())
        {
            while (reader.Read())
                rooms.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        if (filmIds.Count == 0 || rooms.Count == 0)
            return;

        var times = new[] { new TimeSpan(14, 30, 0), new TimeSpan(19, 30, 0) };
        for (var day = 0; day < 21; day++)
        {
            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                for (var timeIndex = 0; timeIndex < times.Length; timeIndex++)
                {
                    var start = DateTime.Today.AddDays(day).Add(times[timeIndex]);
                    if (start <= DateTime.Now.AddMinutes(30))
                        continue;

                    var film = filmIds[(day * rooms.Count + roomIndex + timeIndex * 3) % filmIds.Count];
                    var room = rooms[roomIndex];
                    var premium = room.Type == "VIP" ? 20m : room.Type == "IMAX" ? 15m : room.Type == "3D" ? 10m : 0m;

                    using var command = new SqlCommand(@"
                        IF NOT EXISTS (SELECT 1 FROM dbo.Screenings WHERE RoomId = @RoomId AND StartTime = @StartTime)
                            INSERT INTO dbo.Screenings (FilmId, RoomId, StartTime, Price, IsActive)
                            VALUES (@FilmId, @RoomId, @StartTime, @Price, 1);", connection);
                    command.Parameters.AddWithValue("@FilmId", film.Id);
                    command.Parameters.AddWithValue("@RoomId", room.Id);
                    command.Parameters.AddWithValue("@StartTime", start);
                    command.Parameters.AddWithValue("@Price", film.Price + premium);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    private static void ImportLegacyTickets()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "CinemaDB.sqlite"),
            Path.Combine(AppContext.BaseDirectory, "LegacyData", "CinemaDB.sqlite")
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            return;

        try
        {
            using var legacy = new SQLiteConnection($"Data Source={path};Version=3;Read Only=True;");
            legacy.Open();
            var tickets = new List<(long Id, int FilmId, int RoomId, string ShowTime, string PurchaseDate)>();
            using (var ticketCommand = new SQLiteCommand(
                "SELECT Id, FilmId, SalaId, OraDifuzare, DataCumparare FROM Bilete ORDER BY Id;", legacy))
            using (var reader = ticketCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    tickets.Add((
                        reader.GetInt64(0),
                        Convert.ToInt32(reader["FilmId"]),
                        Convert.ToInt32(reader["SalaId"]),
                        Convert.ToString(reader["OraDifuzare"]) ?? string.Empty,
                        Convert.ToString(reader["DataCumparare"]) ?? string.Empty));
                }
            }

            using var sql = SqlConnectionFactory.Create();
            sql.Open();
            foreach (var ticket in tickets)
            {
                var seats = GetLegacySeats(legacy, ticket.Id);
                using var insert = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.LegacyTickets WHERE Source = @Source AND LegacyId = @LegacyId)
                        INSERT INTO dbo.LegacyTickets
                            (LegacyId, LegacyFilmId, LegacyRoomId, ShowTime, PurchaseDate, Seats, Source)
                        VALUES
                            (@LegacyId, @FilmId, @RoomId, @ShowTime, @PurchaseDate, @Seats, @Source);", sql);
                insert.Parameters.AddWithValue("@LegacyId", ticket.Id);
                insert.Parameters.AddWithValue("@FilmId", ticket.FilmId);
                insert.Parameters.AddWithValue("@RoomId", ticket.RoomId);
                insert.Parameters.AddWithValue("@ShowTime", ticket.ShowTime);
                insert.Parameters.AddWithValue("@PurchaseDate", ticket.PurchaseDate);
                insert.Parameters.AddWithValue("@Seats", seats);
                insert.Parameters.AddWithValue("@Source", Path.GetFileName(path));
                insert.ExecuteNonQuery();
            }
        }
        catch (SQLiteException)
        {
            // Baza veche rămâne intactă; aplicația poate continua chiar dacă fișierul este deteriorat.
        }
    }

    private static string GetLegacySeats(SQLiteConnection connection, long ticketId)
    {
        using var command = new SQLiteCommand(
            "SELECT Scaun FROM ScauneBilet WHERE BiletId = @TicketId ORDER BY Id;", connection);
        command.Parameters.AddWithValue("@TicketId", ticketId);
        using var reader = command.ExecuteReader();
        var seats = new List<string>();
        while (reader.Read())
            seats.Add(reader.GetString(0));
        return string.Join(", ", seats);
    }
}
