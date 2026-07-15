namespace Cinema.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public sealed class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string StatusText => IsActive ? "Activ" : "Inactiv";
}

public sealed class Film
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public decimal Rating { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string DisplayName => $"{Title} · {Genre} · {DurationMinutes} min";
    public string StatusText => IsActive ? "Activ" : "Inactiv";
}

public sealed class CinemaRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }
    public bool IsActive { get; set; } = true;
    public int Capacity => Rows * SeatsPerRow;
    public string DisplayName => $"{Name} · {Type} · {Capacity} locuri";
    public string StatusText => IsActive ? "Activă" : "Inactivă";
}

public sealed class Actor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public int FilmId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
}

public sealed class Screening
{
    public int Id { get; set; }
    public int FilmId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int RoomRows { get; set; }
    public int SeatsPerRow { get; set; }
    public string DateLabel => StartTime.ToString("dd.MM.yyyy");
    public string TimeLabel => StartTime.ToString("HH:mm");
    public string DisplayTime => $"{TimeLabel} · {RoomName} · {Price:0.00} lei";
    public string StatusText => IsActive ? "Activă" : "Inactivă";
}

public sealed class Reservation
{
    public long Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ScreeningId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string Seats { get; set; } = string.Empty;
    public string StatusText => Status == "Confirmed" ? "Confirmată" : "Anulată";
    public string StartLabel => StartTime.ToString("dd.MM.yyyy HH:mm");
    public bool CanCancel => Status == "Confirmed" && StartTime > DateTime.Now;
}

public sealed class DashboardStats
{
    public int ActiveFilms { get; set; }
    public int UpcomingScreenings { get; set; }
    public int ActiveUsers { get; set; }
    public int Reservations { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class LegacyTicket
{
    public long Id { get; set; }
    public long LegacyId { get; set; }
    public int LegacyFilmId { get; set; }
    public int LegacyRoomId { get; set; }
    public string ShowTime { get; set; } = string.Empty;
    public string PurchaseDate { get; set; } = string.Empty;
    public string Seats { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
