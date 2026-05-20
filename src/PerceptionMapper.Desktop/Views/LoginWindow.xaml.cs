using System.Windows;
using PerceptionMapper.Desktop.Models;
using PerceptionMapper.Desktop.Services;

namespace PerceptionMapper.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly ApiClient _api = new();

    public LoginWindow() => InitializeComponent();

    private async void Login_Click(object sender, RoutedEventArgs e) => await Authenticate(false);

    private async void Register_Click(object sender, RoutedEventArgs e) => await Authenticate(true);

    private async Task Authenticate(bool register)
    {
        StatusText.Text = "Connecting to local API...";
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        var result = register
            ? await _api.RegisterAsync(username, password)
            : await _api.LoginAsync(username, password);

        if (result is null)
        {
            StatusText.Text = "Cannot reach API. Start PerceptionMapper.Api first (port 5050).";
            return;
        }

        if (!result.Success)
        {
            StatusText.Text = result.Message;
            return;
        }

        Session.UserId = result.UserId ?? 0;
        Session.Username = result.Username ?? username;

        var main = new MainWindow();
        main.Show();
        Close();
    }
}
