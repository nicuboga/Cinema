using System.Windows.Controls;
using System.Windows;
using Cinema.Models;
using Cinema.Repositories;

namespace Cinema.Views.Pages;

public partial class DashboardPage : Page
{
    public DashboardPage(AppUser currentUser)
    {
        InitializeComponent();
        var repository = new CinemaRepository();
        var stats = repository.GetDashboardStats();
        GreetingText.Text = $"Bine ai venit, {currentUser.FullName.Split(' ')[0]}!";
        FilmsText.Text = stats.ActiveFilms.ToString();
        ScreeningsText.Text = stats.UpcomingScreenings.ToString();

        if (currentUser.Role == Roles.Admin)
        {
            UsersText.Text = stats.ActiveUsers.ToString();
            RevenueText.Text = $"{stats.Revenue:0.00} lei";
            var recentReservations = repository.GetReservations().Take(8).ToList();
            RecentGrid.ItemsSource = recentReservations;
            RecentEmptyPanel.Visibility = recentReservations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            SubtitleText.Text = "Descoperă programul și rezervă locurile tale preferate.";
            ThirdCardLabel.Text = "REZERVĂRILE MELE";
            FourthCardLabel.Text = "TOTAL REZERVAT";
            var reservations = repository.GetReservations(currentUser.Id);
            UsersText.Text = reservations.Count(r => r.Status == "Confirmed").ToString();
            RevenueText.Text = $"{reservations.Where(r => r.Status == "Confirmed").Sum(r => r.TotalAmount):0.00} lei";
            RecentTitleText.Text = "Rezervările mele recente";
            var recentReservations = reservations.Take(8).ToList();
            RecentGrid.ItemsSource = recentReservations;
            RecentEmptyPanel.Visibility = recentReservations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
