using System.Windows;
using System.Windows.Input;
using Cinema.Services;

namespace Cinema.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService = new();

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginErrorText.Text = string.Empty;
        var user = _authService.Authenticate(UsernameTextBox.Text, PasswordInput.Password);
        if (user is null)
        {
            LoginErrorText.Text = "Utilizatorul sau parola sunt incorecte ori contul este inactiv.";
            return;
        }

        new MainWindow(user).Show();
        Close();
    }

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        RegisterErrorText.Text = string.Empty;
        if (RegisterPasswordInput.Password != RegisterConfirmPasswordInput.Password)
        {
            RegisterErrorText.Text = "Parolele nu coincid.";
            return;
        }

        try
        {
            var user = _authService.Register(
                RegisterFullNameTextBox.Text,
                RegisterUsernameTextBox.Text,
                RegisterPasswordInput.Password);
            new MainWindow(user).Show();
            Close();
        }
        catch (Exception ex)
        {
            RegisterErrorText.Text = ex.Message;
        }
    }

    private void ShowRegister_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Visible;
        RegisterFullNameTextBox.Focus();
    }

    private void ShowLogin_Click(object sender, RoutedEventArgs e)
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        UsernameTextBox.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && LoginPanel.Visibility == Visibility.Visible)
            Login_Click(sender, e);
    }
}
