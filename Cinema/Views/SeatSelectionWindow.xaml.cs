using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using Cinema.Models;
using Cinema.Repositories;

namespace Cinema.Views;

public partial class SeatSelectionWindow : Window
{
    private static readonly Brush AvailableBrush = new SolidColorBrush(Color.FromRgb(240, 242, 247));
    private static readonly Brush AvailableTextBrush = new SolidColorBrush(Color.FromRgb(44, 48, 67));
    private readonly AppUser _currentUser;
    private readonly Screening _screening;
    private readonly CinemaRepository _repository = new();
    private readonly HashSet<string> _selectedSeats = new();

    public SeatSelectionWindow(AppUser currentUser, Screening screening)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _screening = screening;
        FilmTitleText.Text = screening.FilmTitle;
        ScreeningDetailsText.Text = $"{screening.StartTime.ToString("dddd, d MMMM yyyy · HH:mm", new CultureInfo("ro-RO"))}  •  {screening.RoomName}";
        BuildSeats();
    }

    private void BuildSeats()
    {
        var occupied = _repository.GetOccupiedSeats(_screening.Id);
        SeatsGrid.Children.Clear();
        SeatsGrid.RowDefinitions.Clear();
        SeatsGrid.ColumnDefinitions.Clear();
        SeatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        for (var column = 1; column <= _screening.SeatsPerRow; column++)
            SeatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

        for (var row = 0; row < _screening.RoomRows; row++)
        {
            SeatsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            var rowLetter = ((char)('A' + row)).ToString();
            var label = new TextBlock
            {
                Text = rowLetter,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            SeatsGrid.Children.Add(label);

            for (var column = 1; column <= _screening.SeatsPerRow; column++)
            {
                var seatCode = $"{rowLetter}-{column}";
                var isOccupied = occupied.Contains(seatCode);
                var button = new Button
                {
                    Content = column.ToString(),
                    Tag = seatCode,
                    Width = 40,
                    Height = 38,
                    Margin = new Thickness(4),
                    Padding = new Thickness(0),
                    Background = isOccupied ? new SolidColorBrush(Color.FromRgb(245, 200, 205)) : AvailableBrush,
                    Foreground = isOccupied ? new SolidColorBrush(Color.FromRgb(153, 58, 68)) : AvailableTextBrush,
                    IsEnabled = !isOccupied,
                    ToolTip = isOccupied ? $"{seatCode} este ocupat" : $"Loc {seatCode}"
                };
                button.Click += Seat_Click;
                Grid.SetRow(button, row);
                Grid.SetColumn(button, column);
                SeatsGrid.Children.Add(button);
            }
        }
    }

    private void Seat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string seatCode } button)
            return;

        if (_selectedSeats.Remove(seatCode))
        {
            button.Background = AvailableBrush;
            button.Foreground = AvailableTextBrush;
        }
        else
        {
            if (_selectedSeats.Count >= 10)
            {
                MessageBox.Show("Poți selecta maximum 10 locuri.", "Limită locuri", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _selectedSeats.Add(seatCode);
            button.Background = (Brush)FindResource("AccentBrush");
            button.Foreground = Brushes.White;
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var ordered = _selectedSeats.OrderBy(s => s).ToList();
        SelectedSeatsText.Text = ordered.Count == 0 ? "Niciun loc" : string.Join(", ", ordered);
        TotalText.Text = $"{_screening.Price * ordered.Count:0.00} lei";
        ConfirmButton.IsEnabled = ordered.Count > 0;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConfirmButton.IsEnabled = false;
            var reservation = _repository.CreateReservation(_currentUser.Id, _screening, _selectedSeats);
            var ticket = new TicketWindow(reservation) { Owner = this };
            ticket.ShowDialog();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Rezervarea nu a putut fi salvată", MessageBoxButton.OK, MessageBoxImage.Warning);
            BuildSeats();
            _selectedSeats.Clear();
            UpdateSummary();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
