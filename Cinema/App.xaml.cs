using System.Windows;
using System.IO;
using System.Globalization;
using System.Windows.Markup;
using Cinema.Data;
using Cinema.Services;
using Cinema.Views;

namespace Cinema;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("ro-RO");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        base.OnStartup(e);

        try
        {
            DatabaseInitializer.Initialize();
            if (e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
            {
                DatabaseInitializer.Verify();
                SmokeTestRunner.Run();
                Shutdown(0);
                return;
            }
            new LoginWindow().Show();
        }
        catch (Exception ex)
        {
            if (e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke-test-error.txt"), ex.ToString());
                Shutdown(1);
                return;
            }
            MessageBox.Show(
                "Conexiunea la SQL Server nu a putut fi inițializată.\n\n" +
                "Verifică dacă serviciul SQL Server (SQLEXPRESS) este pornit și dacă instanța .\\SQLEXPRESS acceptă autentificarea Windows.\n\n" +
                $"Detalii: {ex.Message}",
                "Cinema — eroare bază de date",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
