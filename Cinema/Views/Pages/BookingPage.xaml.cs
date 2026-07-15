using System.Windows;
using System.Windows.Controls;
using Cinema.Models;
using Cinema.Repositories;
using Cinema.Views;

namespace Cinema.Views.Pages;

public partial class BookingPage : Page
{
    private readonly AppUser _currentUser;
    private readonly List<Screening> _screenings;
    private bool _ready;

    public BookingPage(AppUser currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        var repository = new CinemaRepository();
        _screenings = repository.GetScreenings(DateTime.Today, DateTime.Today.AddDays(32), true)
            .Where(s => s.StartTime > DateTime.Now)
            .ToList();

        var filmIds = _screenings.Select(s => s.FilmId).ToHashSet();
        FilmComboBox.ItemsSource = repository.GetFilms(true).Where(f => filmIds.Contains(f.Id)).ToList();
        ScreeningDatePicker.DisplayDateStart = DateTime.Today;
        ScreeningDatePicker.DisplayDateEnd = DateTime.Today.AddDays(31);
        _ready = true;

        if (FilmComboBox.Items.Count > 0)
            FilmComboBox.SelectedIndex = 0;
        else
            NoScreeningText.Visibility = Visibility.Visible;
    }

    private void Film_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || FilmComboBox.SelectedItem is not Film film)
            return;

        SelectedFilmTitle.Text = film.Title;
        SelectedFilmDetails.Text = $"{film.Genre}  •  {film.ReleaseYear}  •  {film.DurationMinutes} min  •  ★ {film.Rating:0.0}";
        var firstDate = _screenings.Where(s => s.FilmId == film.Id).Select(s => s.StartTime.Date).FirstOrDefault();
        ScreeningDatePicker.SelectedDate = firstDate == default ? DateTime.Today : firstDate;
        RefreshScreenings();
    }

    private void Date_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready)
            RefreshScreenings();
    }

    private void RefreshScreenings()
    {
        if (FilmComboBox.SelectedItem is not Film film || ScreeningDatePicker.SelectedDate is not DateTime date)
            return;

        var matches = _screenings.Where(s => s.FilmId == film.Id && s.StartTime.Date == date.Date).ToList();
        ScreeningComboBox.ItemsSource = matches;
        NoScreeningText.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ScreeningComboBox.SelectedIndex = matches.Count > 0 ? 0 : -1;
        UpdateSummary();
    }

    private void Screening_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSummary();

    private void UpdateSummary()
    {
        if (ScreeningComboBox.SelectedItem is Screening screening)
        {
            SelectedDateTimeText.Text = $"{screening.StartTime:dd.MM, HH:mm} · {screening.RoomName}";
            SelectedPriceText.Text = $"{screening.Price:0.00} lei";
            ChooseSeatsButton.IsEnabled = true;
        }
        else
        {
            SelectedDateTimeText.Text = "—";
            SelectedPriceText.Text = "—";
            ChooseSeatsButton.IsEnabled = false;
        }
    }

    private void ChooseSeats_Click(object sender, RoutedEventArgs e)
    {
        if (ScreeningComboBox.SelectedItem is not Screening screening)
            return;
        var window = new SeatSelectionWindow(_currentUser, screening)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }
}
