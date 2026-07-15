using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cinema.Models;

namespace Cinema.Views;

public partial class EntityEditorWindow : Window
{
    private Action? _apply;
    public string? NewPassword { get; private set; }

    private EntityEditorWindow()
    {
        InitializeComponent();
    }

    public static EntityEditorWindow ForFilm(Film film)
    {
        var window = new EntityEditorWindow();
        window.EditorTitleText.Text = film.Id == 0 ? "Film nou" : "Editează filmul";
        var title = window.AddText("Titlu", film.Title);
        var genre = window.AddText("Gen", film.Genre);
        var year = window.AddText("An apariție", film.ReleaseYear.ToString());
        var rating = window.AddText("Rating (0 - 10)", film.Rating.ToString(CultureInfo.CurrentCulture));
        var duration = window.AddText("Durată (minute)", film.DurationMinutes.ToString());
        var price = window.AddText("Preț de bază (lei)", film.BasePrice.ToString(CultureInfo.CurrentCulture));
        var description = window.AddText("Descriere", film.Description, true);
        var active = window.AddCheck("Film activ", film.IsActive);
        window._apply = () =>
        {
            film.Title = Required(title.Text, "Titlul");
            film.Genre = Required(genre.Text, "Genul");
            film.ReleaseYear = ParseInt(year.Text, "Anul");
            film.Rating = ParseDecimal(rating.Text, "Ratingul");
            film.DurationMinutes = ParseInt(duration.Text, "Durata");
            film.BasePrice = ParseDecimal(price.Text, "Prețul");
            film.Description = description.Text.Trim();
            film.IsActive = active.IsChecked == true;
        };
        return window;
    }

    public static EntityEditorWindow ForRoom(CinemaRoom room)
    {
        var window = new EntityEditorWindow();
        window.EditorTitleText.Text = room.Id == 0 ? "Sală nouă" : "Editează sala";
        var name = window.AddText("Numele sălii", room.Name);
        var type = window.AddCombo("Tip", new[] { "Standard", "3D", "VIP", "IMAX" }, room.Type);
        var floor = window.AddText("Etaj", room.Floor.ToString());
        var rows = window.AddText("Număr rânduri", room.Rows.ToString());
        var columns = window.AddText("Locuri pe rând", room.SeatsPerRow.ToString());
        var active = window.AddCheck("Sală activă", room.IsActive);
        window._apply = () =>
        {
            room.Name = Required(name.Text, "Numele sălii");
            room.Type = Convert.ToString(type.SelectedItem) ?? "Standard";
            room.Floor = ParseInt(floor.Text, "Etajul");
            room.Rows = ParseInt(rows.Text, "Numărul de rânduri");
            room.SeatsPerRow = ParseInt(columns.Text, "Locurile pe rând");
            room.IsActive = active.IsChecked == true;
        };
        return window;
    }

    public static EntityEditorWindow ForScreening(Screening screening, List<Film> films, List<CinemaRoom> rooms)
    {
        var window = new EntityEditorWindow();
        window.EditorTitleText.Text = screening.Id == 0 ? "Programare nouă" : "Editează programarea";
        var film = window.AddObjectCombo("Film", films, nameof(Film.DisplayName), screening.FilmId, x => ((Film)x).Id);
        var room = window.AddObjectCombo("Sală", rooms, nameof(CinemaRoom.DisplayName), screening.RoomId, x => ((CinemaRoom)x).Id);
        var date = window.AddDate("Data", screening.StartTime == default ? DateTime.Today.AddDays(1) : screening.StartTime.Date);
        var time = window.AddText("Ora (HH:mm)", screening.StartTime == default ? "19:30" : screening.StartTime.ToString("HH:mm"));
        var price = window.AddText("Preț (lei)", screening.Price.ToString(CultureInfo.CurrentCulture));
        var active = window.AddCheck("Programare activă", screening.IsActive);
        window._apply = () =>
        {
            if (film.SelectedItem is not Film selectedFilm || room.SelectedItem is not CinemaRoom selectedRoom)
                throw new InvalidOperationException("Selectează filmul și sala.");
            if (date.SelectedDate is not DateTime selectedDate || !TimeSpan.TryParse(time.Text, out var selectedTime))
                throw new InvalidOperationException("Data sau ora nu este validă. Folosește formatul HH:mm.");
            screening.FilmId = selectedFilm.Id;
            screening.RoomId = selectedRoom.Id;
            screening.StartTime = selectedDate.Date.Add(selectedTime);
            screening.Price = ParseDecimal(price.Text, "Prețul");
            screening.IsActive = active.IsChecked == true;
        };
        return window;
    }

    public static EntityEditorWindow ForActor(Actor actor, List<Film> films)
    {
        var window = new EntityEditorWindow();
        window.EditorTitleText.Text = actor.Id == 0 ? "Actor nou" : "Editează actorul";
        var name = window.AddText("Nume", actor.Name);
        var year = window.AddText("An naștere", actor.BirthYear.ToString());
        var nationality = window.AddText("Naționalitate", actor.Nationality);
        var film = window.AddObjectCombo("Film", films, nameof(Film.DisplayName), actor.FilmId, x => ((Film)x).Id);
        window._apply = () =>
        {
            actor.Name = Required(name.Text, "Numele");
            actor.BirthYear = ParseInt(year.Text, "Anul nașterii");
            actor.Nationality = Required(nationality.Text, "Naționalitatea");
            actor.FilmId = film.SelectedItem is Film selected ? selected.Id : 0;
        };
        return window;
    }

    public static EntityEditorWindow ForUser(AppUser user, bool isCurrentUser)
    {
        var window = new EntityEditorWindow();
        window.EditorTitleText.Text = user.Id == 0 ? "Utilizator nou" : "Editează utilizatorul";
        var fullName = window.AddText("Nume complet", user.FullName);
        var username = window.AddText("Nume utilizator", user.Username);
        var role = window.AddCombo("Rol", new[] { Roles.User, Roles.Admin }, user.Role);
        var active = window.AddCheck("Cont activ", user.IsActive);
        if (isCurrentUser)
        {
            role.IsEnabled = false;
            active.IsEnabled = false;
            window.AddHint("Pentru siguranță, rolul și starea propriului cont nu pot fi schimbate aici.");
        }
        var password = window.AddPassword(user.Id == 0 ? "Parolă" : "Parolă nouă (opțional)");
        window._apply = () =>
        {
            user.FullName = Required(fullName.Text, "Numele complet");
            user.Username = Required(username.Text, "Numele de utilizator");
            if (!isCurrentUser)
            {
                user.Role = Convert.ToString(role.SelectedItem) ?? Roles.User;
                user.IsActive = active.IsChecked == true;
            }
            window.NewPassword = string.IsNullOrWhiteSpace(password.Password) ? null : password.Password;
        };
        return window;
    }

    private TextBox AddText(string label, string value, bool multiline = false)
    {
        AddLabel(label);
        var control = new TextBox
        {
            Text = value,
            MinHeight = multiline ? 85 : 42,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden
        };
        FormPanel.Children.Add(control);
        return control;
    }

    private PasswordBox AddPassword(string label)
    {
        AddLabel(label);
        var control = new PasswordBox { Height = 42 };
        FormPanel.Children.Add(control);
        return control;
    }

    private ComboBox AddCombo(string label, IEnumerable<string> values, string selected)
    {
        AddLabel(label);
        var control = new ComboBox { Height = 42, ItemsSource = values.ToList(), SelectedItem = selected };
        if (control.SelectedIndex < 0) control.SelectedIndex = 0;
        FormPanel.Children.Add(control);
        return control;
    }

    private ComboBox AddObjectCombo(string label, object values, string displayMemberPath, int selectedId, Func<object, int> idSelector)
    {
        AddLabel(label);
        var control = new ComboBox { Height = 42, ItemsSource = values as System.Collections.IEnumerable, DisplayMemberPath = displayMemberPath };
        foreach (var item in control.Items)
            if (idSelector(item) == selectedId) { control.SelectedItem = item; break; }
        if (control.SelectedIndex < 0 && control.Items.Count > 0) control.SelectedIndex = 0;
        FormPanel.Children.Add(control);
        return control;
    }

    private DatePicker AddDate(string label, DateTime selected)
    {
        AddLabel(label);
        var control = new DatePicker { Height = 42, SelectedDate = selected, DisplayDateStart = DateTime.Today };
        FormPanel.Children.Add(control);
        return control;
    }

    private CheckBox AddCheck(string label, bool value)
    {
        var control = new CheckBox
        {
            Content = label,
            IsChecked = value,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 18, 0, 2)
        };
        FormPanel.Children.Add(control);
        return control;
    }

    private void AddHint(string text) => FormPanel.Children.Add(new TextBlock
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
        FontSize = 12,
        Margin = new Thickness(0, 10, 0, 0)
    });

    private void AddLabel(string text) => FormPanel.Children.Add(new TextBlock
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 14, 0, 7)
    });

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _apply?.Invoke();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Date incomplete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{field} este obligatoriu.");
        return value.Trim();
    }

    private static int ParseInt(string value, string field)
    {
        if (!int.TryParse(value, out var result))
            throw new InvalidOperationException($"{field} trebuie să fie un număr întreg.");
        return result;
    }

    private static decimal ParseDecimal(string value, string field)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var result) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            return result;
        throw new InvalidOperationException($"{field} trebuie să fie un număr valid.");
    }
}
