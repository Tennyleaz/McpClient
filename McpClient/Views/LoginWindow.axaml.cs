using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Avalonia.Input;
using McpClient.Services;
using McpClient.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace McpClient.Views;

public partial class LoginWindow : Window
{
    private readonly McpConfigService _service;
    private readonly AiNexusService _aiNexusService;
    private Settings settings;

    public string Token { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        _service = new McpConfigService(null);  // no token if login
        _aiNexusService = new AiNexusService(null);  // no token if login
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        settings = SettingsManager.Local.Load();
        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            TbUserName.Text = settings.UserName;
            CheckRememberName.IsChecked = true;
            TbPassword.Focus();
        }
        else
        {
            TbUserName.Focus();
        }
    }


    private void InputElement_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (TbUserName.IsFocused)
            {
                TbPassword.Focus();
                return;
            }

            if (TbPassword.IsFocused)
            {
                BtnLogin_OnClick(null, null);
            }
        }
    }

    private async void BtnLogin_OnClick(object sender, RoutedEventArgs e)
    {
        // check fields
        if (string.IsNullOrWhiteSpace(TbUserName.Text) || string.IsNullOrWhiteSpace(TbPassword.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Login", "Please fill in username and password.",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info);
             await box.ShowAsync();
            return;
        }

        // generate JWT and login
        LoginResponse resposne = await _service.Login(TbUserName.Text, TbPassword.Text);
        if (resposne == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Login fail", "Please fill in correct username and password.",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }

        // Also login to AI Nexus
        LoginResponse aiResponse = await _aiNexusService.Login(TbUserName.Text, TbPassword.Text);
        if (aiResponse == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Login fail", "Please fill in correct username and password.",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }

        // Remember username
        if (CheckRememberName.IsChecked == true)
        {
            settings.UserName = TbUserName.Text;
        }
        else
        {
            settings.UserName = null;
        }

        // save and close
        settings.McpConfigToken = resposne.Token;
        settings.AiNexusToken = aiResponse.Token;
        settings.ExpiredAt = resposne.ExpiresAt;
        await SettingsManager.Local.SaveAsync(settings);
        Token = resposne.Token;
        Close();
    }
}