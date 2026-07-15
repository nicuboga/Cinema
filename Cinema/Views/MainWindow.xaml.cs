using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using Cinema.Models;
using Cinema.Views.Pages;

namespace Cinema.Views;

public partial class MainWindow : Window
{
    private readonly AppUser _currentUser;
    private readonly List<Button> _navigationButtons;

    public MainWindow(AppUser currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _navigationButtons = new List<Button> { HomeNavButton, BookingNavButton, MyReservationsNavButton, AdminNavButton };
        UserNameText.Text = currentUser.FullName;
        RoleText.Text = currentUser.Role == Roles.Admin ? "Administrator" : "Utilizator";
        InitialsText.Text = GetInitials(currentUser.FullName);
        var dateText = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("ro-RO"));
        CurrentDateText.Text = char.ToUpper(dateText[0], new CultureInfo("ro-RO")) + dateText[1..];

        if (currentUser.Role == Roles.Admin)
        {
            AdminNavigation.Visibility = Visibility.Visible;
            BookingNavButton.Visibility = Visibility.Collapsed;
            MyReservationsNavButton.Visibility = Visibility.Collapsed;
        }

        Navigate("home");
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string destination })
            Navigate(destination);
    }

    private void Navigate(string destination)
    {
        switch (destination)
        {
            case "booking":
                PageTitleText.Text = "Rezervă bilete";
                ContentFrame.Navigate(new BookingPage(_currentUser));
                break;
            case "my-reservations":
                PageTitleText.Text = "Rezervările mele";
                ContentFrame.Navigate(new MyReservationsPage(_currentUser));
                break;
            case "admin":
                PageTitleText.Text = "Administrare";
                ContentFrame.Navigate(new AdminPage(_currentUser));
                break;
            default:
                PageTitleText.Text = "Acasă";
                ContentFrame.Navigate(new DashboardPage(_currentUser));
                break;
        }

        SetActiveNavigation(destination);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new LoginWindow().Show();
        Close();
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }

    private void SetActiveNavigation(string destination)
    {
        foreach (var button in _navigationButtons)
        {
            var isActive = Equals(button.Tag, destination) || (destination == "home" && Equals(button.Tag, "home"));
            button.Background = isActive ? (Brush)FindResource("SidebarHoverBrush") : Brushes.Transparent;
            button.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(214, 218, 232));
            button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }
}
