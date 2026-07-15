using Cinema.Models;
using Cinema.Repositories;
using Cinema.Views;
using Cinema.Views.Pages;
using Cinema.Data;
using Microsoft.Data.SqlClient;

namespace Cinema.Services;

public static class SmokeTestRunner
{
    public static void Run()
    {
        var auth = new AuthService();
        var admin = auth.Authenticate("admin", "Admin123!")
            ?? throw new InvalidOperationException("Autentificarea contului admin nu funcționează.");
        var user = auth.Authenticate("user", "User123!")
            ?? throw new InvalidOperationException("Autentificarea contului user nu funcționează.");
        var repository = new CinemaRepository();
        var films = repository.GetFilms();
        var rooms = repository.GetRooms();
        var screenings = repository.GetScreenings(DateTime.Today, DateTime.Today.AddDays(32), true);
        _ = repository.GetActors();
        _ = repository.GetUsers();
        _ = repository.GetReservations();
        _ = repository.GetLegacyTickets();
        _ = repository.GetDashboardStats();

        if (screenings.Count == 0)
            throw new InvalidOperationException("Nu există proiecții disponibile pentru test.");

        VerifyReservationFlow(repository, user, screenings[0]);

        var login = new LoginWindow();
        var main = new MainWindow(admin);
        _ = new DashboardPage(user);
        _ = new BookingPage(user);
        _ = new MyReservationsPage(user);
        _ = new AdminPage(admin);
        var seats = new SeatSelectionWindow(user, screenings[0]);
        var ticket = new TicketWindow(new Reservation
        {
            BookingCode = "TEST",
            FilmTitle = screenings[0].FilmTitle,
            RoomName = screenings[0].RoomName,
            StartTime = screenings[0].StartTime,
            Seats = "A-1",
            TotalAmount = screenings[0].Price
        });
        var editor = EntityEditorWindow.ForFilm(films[0]);

        editor.Close();
        ticket.Close();
        seats.Close();
        main.Close();
        login.Close();
        GC.KeepAlive(rooms);
    }

    private static void VerifyReservationFlow(CinemaRepository repository, AppUser user, Screening screening)
    {
        var occupied = repository.GetOccupiedSeats(screening.Id);
        var testSeat = Enumerable.Range(0, screening.RoomRows)
            .SelectMany(row => Enumerable.Range(1, screening.SeatsPerRow)
                .Select(column => $"{(char)('A' + row)}-{column}"))
            .First(seat => !occupied.Contains(seat));
        Reservation? reservation = null;
        try
        {
            reservation = repository.CreateReservation(user.Id, screening, new[] { testSeat });
            if (!repository.GetOccupiedSeats(screening.Id).Contains(testSeat))
                throw new InvalidOperationException("Rezervarea locului nu a fost salvată.");
            repository.CancelReservation(reservation.Id, user.Id);
            if (repository.GetOccupiedSeats(screening.Id).Contains(testSeat))
                throw new InvalidOperationException("Anularea nu a eliberat locul rezervat.");
        }
        finally
        {
            if (reservation is not null)
            {
                using var connection = SqlConnectionFactory.Create();
                connection.Open();
                using var command = new SqlCommand("DELETE FROM dbo.Reservations WHERE Id=@Id;", connection);
                command.Parameters.AddWithValue("@Id", reservation.Id);
                command.ExecuteNonQuery();
            }
        }
    }
}
