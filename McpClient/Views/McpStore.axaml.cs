using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaProgressRing;
using DynamicData;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using System.Threading.Tasks;
using Avalonia.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace McpClient.Views;

public partial class McpStore : UserControl
{
    private AiNexusService _service;
    private StoreMcpViewModel storeMcpViewModel;
    private Pagination pagination;
    private int currentPage = 1;
    private string currentTag, currentQuery, currentCategory;

    public bool IsUpdateNeeded { get; private set; }

    public McpStore()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        SetService();
    }

    internal void SetService()
    {
        Settings settings = SettingsManager.Local.Load();
        _service = new AiNexusService(settings.AiNexusToken);
    }

    private async void TabTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabTypes == null)
            return;

        if (TabTypes.SelectedIndex == 0)
        {
            await LoadStoreItems("latest", null, null, currentPage);
        }
        else if (TabTypes.SelectedIndex == 1)
        {
            await LoadStoreItems("hosting", null, null, currentPage);
        }
        else if (TabTypes.SelectedIndex == 2)
        {
            await LoadStoreItems("official", null, null, currentPage);
        }
        else if (TabTypes.SelectedIndex == 3)
        {
            await LoadStoreItems("featured", null, null, currentPage);
        }
    }

    internal async Task LoadDefault()
    {
        currentPage = 1;
        if (TabTypes.SelectedIndex == 0)
        {
            await LoadStoreItems("latest", null, null, currentPage);
        }
        TabTypes.SelectedIndex = 0;
        IsUpdateNeeded = false;
    }

    private async Task LoadStoreItems(string tag, string category, string query, int page)
    {
        LbEmptyList.IsVisible = false;
        ProgressRing.IsVisible = true;

        StoreMcpResponse response;
        if (string.IsNullOrWhiteSpace(query))
        {
            // Normal tags
            response = await _service.GetStoreMcpServers(tag, category, page);
        }
        else
        {
            // Search
            ShowSearch(query);
            response = await _service.SearchMcpServers(query, page);
        }

        if (response == null)
        {
            LbEmptyList.IsVisible = true;
            ProgressRing.IsVisible = false;

            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Failed to get store items from server.",
                ButtonEnum.Ok, Icon.Error);
            await box.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            return;
        }

        pagination = response.Pagination;
        TbPageNumber.Content = $"{currentPage}/{pagination.TotalPages}";

        storeMcpViewModel = new StoreMcpViewModel();
        storeMcpViewModel.Servers.AddRange(response.Servers);
        DataContext = storeMcpViewModel;

        ProgressRing.IsVisible = false;
        LbEmptyList.IsVisible = storeMcpViewModel.Servers.Count == 0;
    }

    private async void McpListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (McpListBox.SelectedItem is StoreMcpServer storeMcpServer)
        {
            StoreItemWindow window = new StoreItemWindow(storeMcpServer, _service);
            await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            
            IsUpdateNeeded = window.IsInstalled;
            McpListBox.SelectedIndex = -1;
        }
    }

    private async void BtnSearch_OnClick(object sender, RoutedEventArgs e)
    {
        await Search();
    }

    private async void TbQueryTitle_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await Search();
        }
    }

    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(TbSearch.Text))
        {
            return;
        }

        currentPage = 1;
        await LoadStoreItems(null, null, TbSearch.Text, currentPage);
    }

    private void ShowSearch(string query)
    {
        TabTypes.IsVisible = false;
        SearchPanel.IsVisible = true;
        TbQueryTitle.Text = "Search: " + query;
    }

    private void HideSearch()
    {
        TbSearch.Text = null;
        SearchPanel.IsVisible = false;
        TabTypes.IsVisible = true;
    }

    private void BtnLeaveSearch_OnClick(object sender, RoutedEventArgs e)
    {
        HideSearch();
    }

    private async void BtnPrev_OnClick(object sender, RoutedEventArgs e)
    {
        if (pagination == null || currentPage <= 1)
        {
            return;
        }

        currentPage--;
        await UpdatePages();
    }

    private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (pagination == null || currentPage > pagination.TotalPages)
        {
            return;
        }

        currentPage++;
        await UpdatePages();
    }

    private async Task UpdatePages()
    {
        await LoadStoreItems(currentTag, currentCategory, currentQuery, currentPage);
    }
}