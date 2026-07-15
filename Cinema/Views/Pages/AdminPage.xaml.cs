using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cinema.Models;
using Cinema.Repositories;

namespace Cinema.Views.Pages;

public partial class AdminPage : Page
{
    private readonly AppUser _currentUser;
    private readonly CinemaRepository _repository = new();

    public AdminPage(AppUser currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        if (currentUser.Role != Roles.Admin)
            throw new UnauthorizedAccessException("Această pagină este disponibilă doar administratorilor.");
        RefreshAll();
    }

    private void RefreshAll()
    {
        FilmsGrid.ItemsSource = _repository.GetFilms();
        RoomsGrid.ItemsSource = _repository.GetRooms();
        ScreeningsGrid.ItemsSource = _repository.GetScreenings(DateTime.Today.AddDays(-1), DateTime.Today.AddMonths(4));
        ActorsGrid.ItemsSource = _repository.GetActors();
        UsersGrid.ItemsSource = _repository.GetUsers();
        LoadReservations();
        LegacyGrid.ItemsSource = _repository.GetLegacyTickets();
    }

    private void NewFilm_Click(object sender, RoutedEventArgs e) => EditFilm(new Film
    {
        ReleaseYear = DateTime.Today.Year, Rating = 7m, DurationMinutes = 120, BasePrice = 30m, IsActive = true
    });
    private void EditFilm_Click(object sender, RoutedEventArgs e)
    {
        if (FilmsGrid.SelectedItem is Film film) EditFilm(Clone(film)); else ShowSelectMessage("filmul");
    }
    private void FilmsGrid_DoubleClick(object sender, MouseButtonEventArgs e) => EditFilm_Click(sender, e);
    private void EditFilm(Film film)
    {
        var dialog = EntityEditorWindow.ForFilm(film);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true) return;
        Execute(() => _repository.SaveFilm(film), () => FilmsGrid.ItemsSource = _repository.GetFilms());
    }
    private void ToggleFilm_Click(object sender, RoutedEventArgs e)
    {
        if (FilmsGrid.SelectedItem is not Film film) { ShowSelectMessage("filmul"); return; }
        film.IsActive = !film.IsActive;
        Execute(() => _repository.SaveFilm(film), () => FilmsGrid.ItemsSource = _repository.GetFilms());
    }

    private void NewRoom_Click(object sender, RoutedEventArgs e) => EditRoom(new CinemaRoom
    {
        Type = "Standard", Rows = 8, SeatsPerRow = 10, IsActive = true
    });
    private void EditRoom_Click(object sender, RoutedEventArgs e)
    {
        if (RoomsGrid.SelectedItem is CinemaRoom room) EditRoom(Clone(room)); else ShowSelectMessage("sala");
    }
    private void RoomsGrid_DoubleClick(object sender, MouseButtonEventArgs e) => EditRoom_Click(sender, e);
    private void EditRoom(CinemaRoom room)
    {
        var dialog = EntityEditorWindow.ForRoom(room);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true) return;
        Execute(() => _repository.SaveRoom(room), () => RoomsGrid.ItemsSource = _repository.GetRooms());
    }
    private void ToggleRoom_Click(object sender, RoutedEventArgs e)
    {
        if (RoomsGrid.SelectedItem is not CinemaRoom room) { ShowSelectMessage("sala"); return; }
        room.IsActive = !room.IsActive;
        Execute(() => _repository.SaveRoom(room), () => RoomsGrid.ItemsSource = _repository.GetRooms());
    }

    private void NewScreening_Click(object sender, RoutedEventArgs e) => EditScreening(new Screening
    {
        StartTime = DateTime.Today.AddDays(1).AddHours(19).AddMinutes(30), Price = 30m, IsActive = true
    });
    private void EditScreening_Click(object sender, RoutedEventArgs e)
    {
        if (ScreeningsGrid.SelectedItem is Screening screening) EditScreening(Clone(screening)); else ShowSelectMessage("programarea");
    }
    private void ScreeningsGrid_DoubleClick(object sender, MouseButtonEventArgs e) => EditScreening_Click(sender, e);
    private void EditScreening(Screening screening)
    {
        var dialog = EntityEditorWindow.ForScreening(screening, _repository.GetFilms(true), _repository.GetRooms(true));
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true) return;
        Execute(() => _repository.SaveScreening(screening),
            () => ScreeningsGrid.ItemsSource = _repository.GetScreenings(DateTime.Today.AddDays(-1), DateTime.Today.AddMonths(4)));
    }
    private void DeleteScreening_Click(object sender, RoutedEventArgs e)
    {
        if (ScreeningsGrid.SelectedItem is not Screening screening) { ShowSelectMessage("programarea"); return; }
        if (MessageBox.Show("Ștergi programarea? Dacă are rezervări, va fi doar dezactivată.", "Confirmare",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Execute(() => _repository.DeleteScreening(screening.Id),
            () => ScreeningsGrid.ItemsSource = _repository.GetScreenings(DateTime.Today.AddDays(-1), DateTime.Today.AddMonths(4)));
    }

    private void NewActor_Click(object sender, RoutedEventArgs e) => EditActor(new Actor { BirthYear = 1980 });
    private void EditActor_Click(object sender, RoutedEventArgs e)
    {
        if (ActorsGrid.SelectedItem is Actor actor) EditActor(Clone(actor)); else ShowSelectMessage("actorul");
    }
    private void ActorsGrid_DoubleClick(object sender, MouseButtonEventArgs e) => EditActor_Click(sender, e);
    private void EditActor(Actor actor)
    {
        var dialog = EntityEditorWindow.ForActor(actor, _repository.GetFilms());
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true) return;
        Execute(() => _repository.SaveActor(actor), () => ActorsGrid.ItemsSource = _repository.GetActors());
    }
    private void DeleteActor_Click(object sender, RoutedEventArgs e)
    {
        if (ActorsGrid.SelectedItem is not Actor actor) { ShowSelectMessage("actorul"); return; }
        if (MessageBox.Show($"Ștergi actorul {actor.Name}?", "Confirmare", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Execute(() => _repository.DeleteActor(actor.Id), () => ActorsGrid.ItemsSource = _repository.GetActors());
    }

    private void NewUser_Click(object sender, RoutedEventArgs e) => EditUser(new AppUser { Role = Roles.User, IsActive = true });
    private void EditUser_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is AppUser user) EditUser(Clone(user)); else ShowSelectMessage("utilizatorul");
    }
    private void UsersGrid_DoubleClick(object sender, MouseButtonEventArgs e) => EditUser_Click(sender, e);
    private void EditUser(AppUser user)
    {
        var isSelf = user.Id == _currentUser.Id;
        var dialog = EntityEditorWindow.ForUser(user, isSelf);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() != true) return;
        Execute(() => _repository.SaveUser(user, dialog.NewPassword), () => UsersGrid.ItemsSource = _repository.GetUsers());
    }
    private void ToggleUser_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not AppUser user) { ShowSelectMessage("utilizatorul"); return; }
        if (user.Id == _currentUser.Id)
        {
            MessageBox.Show("Nu îți poți dezactiva propriul cont în sesiunea curentă.", "Acțiune blocată",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        user.IsActive = !user.IsActive;
        Execute(() => _repository.SaveUser(user, null), () => UsersGrid.ItemsSource = _repository.GetUsers());
    }

    private void RefreshReservations_Click(object sender, RoutedEventArgs e) => LoadReservations();
    private void CancelReservation_Click(object sender, RoutedEventArgs e)
    {
        if (ReservationsGrid.SelectedItem is not Reservation reservation) { ShowSelectMessage("rezervarea"); return; }
        if (reservation.Status != "Confirmed")
        {
            MessageBox.Show("Rezervarea este deja anulată.", "Informație", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Anulezi rezervarea {reservation.BookingCode}?", "Confirmare",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Execute(() => _repository.CancelReservation(reservation.Id), LoadReservations);
    }

    private void LoadReservations()
    {
        var reservations = _repository.GetReservations();
        ReservationsGrid.ItemsSource = reservations;
        EmptyAdminReservationsPanel.Visibility = reservations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ShowSelectMessage(string item) => MessageBox.Show($"Selectează {item} din listă.", "Nicio selecție",
        MessageBoxButton.OK, MessageBoxImage.Information);

    private static void Execute(Action action, Action refresh)
    {
        try
        {
            action();
            refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Operația nu a reușit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static Film Clone(Film x) => new() { Id=x.Id, Title=x.Title, Genre=x.Genre, ReleaseYear=x.ReleaseYear,
        Rating=x.Rating, DurationMinutes=x.DurationMinutes, BasePrice=x.BasePrice, Description=x.Description, IsActive=x.IsActive };
    private static CinemaRoom Clone(CinemaRoom x) => new() { Id=x.Id, Name=x.Name, Type=x.Type, Floor=x.Floor,
        Rows=x.Rows, SeatsPerRow=x.SeatsPerRow, IsActive=x.IsActive };
    private static Screening Clone(Screening x) => new() { Id=x.Id, FilmId=x.FilmId, FilmTitle=x.FilmTitle, RoomId=x.RoomId,
        RoomName=x.RoomName, StartTime=x.StartTime, Price=x.Price, IsActive=x.IsActive, RoomRows=x.RoomRows, SeatsPerRow=x.SeatsPerRow };
    private static Actor Clone(Actor x) => new() { Id=x.Id, Name=x.Name, BirthYear=x.BirthYear, Nationality=x.Nationality,
        FilmId=x.FilmId, FilmTitle=x.FilmTitle };
    private static AppUser Clone(AppUser x) => new() { Id=x.Id, Username=x.Username, FullName=x.FullName, Role=x.Role,
        IsActive=x.IsActive, CreatedAt=x.CreatedAt };
}
