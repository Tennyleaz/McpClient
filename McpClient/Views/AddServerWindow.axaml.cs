using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using McpClient.Models;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class AddServerWindow : Window
{
    private List<string> args = new();
    private Dictionary<string, string> env = new();

    internal McpServer Result { get; private set; }

    public AddServerWindow()
    {
        InitializeComponent();
    }


    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        CbTypes.SelectedIndex = -1;
        CbTypes_OnSelectionChanged(null, null);
    }

    private async void ButtonApply_OnClick(object sender, RoutedEventArgs e)
    {
        bool isValid = await Validate();
        if (!isValid)
        {
            return;
        }

        McpServer newServer = new McpServer();
        newServer.enabled = true;
        newServer.server_name = TbServerName.Text;
        newServer.command = TbCommand.Text;
        newServer.owner = TbOwner.Text;
        newServer.args = args;
        newServer.env = env;
        if (CbTypes.SelectedIndex == 1)
        {
            newServer.type = "sse";
            newServer.sse_url = TbUrl.Text;
        }
        else if (CbTypes.SelectedIndex == 2)
        {
            newServer.type = "streamableHttp";
            newServer.streamable_http_url = TbUrl.Text;
        }
        else
        {
            newServer.type = "stdio";
        }

        Result = newServer;
        Close();
    }

    private async void BtnEditArgs_OnClick(object sender, RoutedEventArgs e)
    {
        ArgsEditorViewModel argsEditorViewModel = new ArgsEditorViewModel();
        // Copy the data
        foreach (string arg in args)
        {
            argsEditorViewModel.Args.Add(new ArgsEditorItem(arg));
        }
        // Show edit window
        ArgsEditor editWin = new ArgsEditor();
        editWin.DataContext = argsEditorViewModel;
        await editWin.ShowDialog(this);
        // Replace whole collection, so UI could change
        args = argsEditorViewModel.ToCollenction().ToList();
        TbArgs.Text = string.Join(' ', args);
    }

    private async void BtnEditEnv_OnClick(object sender, RoutedEventArgs e)
    {
        EnvEditorViewModel envEditorViewModel = new EnvEditorViewModel();
        // Copy the data
        foreach (var pair in env)
        {
            envEditorViewModel.Env.Add(new EnvironmentItem()
            {
                Name = pair.Key,
                Value = pair.Value
            });
        }
        // Show edit window
        EnvEditor editWindow = new EnvEditor();
        editWindow.DataContext = envEditorViewModel;
        await editWindow.ShowDialog(this);
        // Replace whole collection, so UI could change
        env = envEditorViewModel.ToDictionary();
        TbEnv.Text = string.Join(", ", env);
    }

    private void CbTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CbTypes.SelectedIndex <= 0)
        {
            // stdio, has command, no URL
            TbUrlHeader.IsVisible = TbUrl.IsVisible = false;
            MainGrid.RowDefinitions[4].Height = GridLength.Parse("0");

            TbCommandHeader.IsVisible = TbCommand.IsVisible = true;
            MainGrid.RowDefinitions[3].Height = GridLength.Parse("40");
        }
        else
        {
            // sse, streamableHttp has URL, no command
            TbUrlHeader.IsVisible = TbUrl.IsVisible = true;
            MainGrid.RowDefinitions[4].Height = GridLength.Parse("40");

            TbCommandHeader.IsVisible = TbCommand.IsVisible = false;
            MainGrid.RowDefinitions[3].Height = GridLength.Parse("0");
        }
    }

    private async Task<bool> Validate()
    {
        if (string.IsNullOrWhiteSpace(TbServerName.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input your server name.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex == 0 && string.IsNullOrWhiteSpace(TbCommand.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input your command.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex < 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please select a server type.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        if (CbTypes.SelectedIndex > 0 && string.IsNullOrWhiteSpace(TbUrl.Text))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Info", "Please input a valid URL.",
                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowWindowDialogAsync(this);
            return false;
        }

        return true;
    }
}