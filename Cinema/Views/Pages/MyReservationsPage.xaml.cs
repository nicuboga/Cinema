using System.Windows;
using System.Windows.Controls;
using Cinema.Models;
using Cinema.Repositories;

namespace Cinema.Views.Pages;

public partial class MyReservationsPage : Page
{
    private readonly AppUser _currentUser;
    private readonly CinemaRepository _repository = new();

    public MyReservationsPage(AppUser currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        LoadReservations();
    }

    private void LoadReservations()
    {
        var reservations = _repository.GetReservations(_currentUser.Id);
        ReservationsGrid.ItemsSource = reservations;
        EmptyReservationsPanel.Visibility = reservations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadReservations();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (ReservationsGrid.SelectedItem is not Reservation reservation)
        {
            MessageBox.Show("Selectează rezervarea pe care vrei să o anulezi.", "Nicio selecție", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!reservation.CanCancel)
        {
            MessageBox.Show("Pot fi anulate doar rezervările confirmate pentru proiecții viitoare.", "Rezervare protejată", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (MessageBox.Show($"Anulezi rezervarea {reservation.BookingCode}? Locurile vor deveni din nou disponibile.",
                "Confirmare anulare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            _repository.CancelReservation(reservation.Id, _currentUser.Id);
            LoadReservations();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Anularea nu a reușit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
