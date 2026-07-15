using System.Windows;
using Cinema.Models;
using System.Globalization;

namespace Cinema.Views;

public partial class TicketWindow : Window
{
    public TicketWindow(Reservation reservation)
    {
        InitializeComponent();
        FilmText.Text = reservation.FilmTitle;
        DateText.Text = reservation.StartTime.ToString("dddd, d MMMM yyyy · HH:mm", new CultureInfo("ro-RO"));
        RoomText.Text = reservation.RoomName;
        SeatsText.Text = reservation.Seats;
        TotalText.Text = $"{reservation.TotalAmount:0.00} lei";
        CodeText.Text = reservation.BookingCode;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
