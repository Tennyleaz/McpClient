using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaProgressRing;
using DynamicData;
using McpClient.Models;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace McpClient.Views;

public partial class McpStore : UserControl
{
    private AiNexusService _service;
    private McpConfigService _configService;
    private StoreMcpViewModel storeMcpViewModel;
    private Pagination pagination;
    private int currentPage = 1;
    private string currentTag, currentQuery, currentCategory, currentType;
    private List<string> installedMcpServers = new ();

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
        _configService = new McpConfigService(settings.McpConfigToken);
        _service = new AiNexusService(settings.AiNexusToken);

        StoreCategory.StoreCategoryList.SelectionChanged += StoreCategoryList_SelectionChanged;
    }

    private async Task GetInstalledList()
    {
        var response = await _configService.GetConfig();
        if (response != null)
        {
            installedMcpServers = response.mcp_servers.Where(x => x.source == "cloud").Select(x => x.server_name).ToList();
        }
    }

    private async void StoreCategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StoreCategory.StoreCategoryList.SelectedItem is StoreCategoryItem item)
        {
            if (TabTypes.SelectedIndex == 2)
            {
                ShowCatrgory(item.Name);
                await LoadStoreItems(null, item.Value, null, null, currentPage);
            }
            else if (TabTypes.SelectedIndex == 3)
            {
                ShowType(item.Name);
                await LoadStoreItems(null, null, item.Value, null, currentPage);
            }
        }
    }

    private async void TabTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabTypes == null || Design.IsDesignMode)
            return;

        currentPage = 1;
        if (TabTypes.SelectedIndex == 0)
        {
            // Latest
            StoreCategory.IsVisible = false;
            McpListBox.IsVisible = true;
            await LoadStoreItems("latest", null, null, null, currentPage);
        }
        else if (TabTypes.SelectedIndex == 1)
        {
            // Featured
            StoreCategory.IsVisible = false;
            McpListBox.IsVisible = true;
            await LoadStoreItems("featured", null, null, null, currentPage);
        }
        else if (TabTypes.SelectedIndex == 2)
        {
            // Show category list
            StoreCategory.IsVisible = true;
            StoreCategory.StoreCategoryList.ItemsSource = Constants.STORE_CATEGORIES;
            StoreCategory.StoreCategoryList.SelectedIndex = -1;
            McpListBox.IsVisible = false;
            LbEmptyList.IsVisible = false;
        }
        else if (TabTypes.SelectedIndex == 3)
        {
            // Show type list
            StoreCategory.IsVisible = true;
            StoreCategory.StoreCategoryList.ItemsSource = Constants.STORE_TYPES;
            StoreCategory.StoreCategoryList.SelectedIndex = -1;
            McpListBox.IsVisible = false;
            LbEmptyList.IsVisible = false;
        }
    }

    internal async Task LoadDefault()
    {
        currentPage = 1;
        if (TabTypes.SelectedIndex == 0)
        {
            await LoadStoreItems("latest", null, null, null, currentPage);
        }
        TabTypes.SelectedIndex = 0;
        TabTypes.IsVisible = true;
        SearchPanel.IsVisible = false;
        IsUpdateNeeded = false;
        await GetInstalledList();
    }

    private async Task LoadStoreItems(string tag, string category, string type, string query, int page)
    {
        LbEmptyList.IsVisible = false;
        ProgressRing.IsVisible = true;

        StoreMcpResponse response;
        if (string.IsNullOrWhiteSpace(query))
        {
            // Normal tags
            response = await _service.GetStoreMcpServers(tag, category, type, page);
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

        currentTag = tag;
        currentCategory = category;
        currentType = type;
        currentQuery = query;
        pagination = response.Pagination;
        TbPageNumber.Content = $"{currentPage}/{pagination.TotalPages}";
        BtnPrev.IsEnabled = currentPage > 1;
        BtnNext.IsEnabled = currentPage < pagination.TotalPages;

        storeMcpViewModel = new StoreMcpViewModel(response.Servers, installedMcpServers);
        DataContext = storeMcpViewModel;

        ProgressRing.IsVisible = false;
        LbEmptyList.IsVisible = storeMcpViewModel.Servers.Count == 0;
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
        await LoadStoreItems(null, null, null, TbSearch.Text, currentPage);
    }

    private void ShowSearch(string query)
    {
        TabTypes.IsVisible = false;
        SearchPanel.IsVisible = true;
        StoreCategory.IsVisible = false;
        McpListBox.IsVisible = true;
        TbQueryTitle.Text = "Search: " + query;
    }

    private void ShowCatrgory(string category)
    {
        TabTypes.IsVisible = false;
        SearchPanel.IsVisible = true;
        StoreCategory.IsVisible = false;
        McpListBox.IsVisible = true;
        TbQueryTitle.Text = "Category: " + category;
    }

    private void ShowType(string type)
    {
        TabTypes.IsVisible = false;
        SearchPanel.IsVisible = true;
        StoreCategory.IsVisible = false;
        McpListBox.IsVisible = true;
        TbQueryTitle.Text = "Type: " + type;
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
        // Go back to "Featured" or "Category"
        TabTypes_OnSelectionChanged(null, null);
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
        if (pagination == null || currentPage >= pagination.TotalPages)
        {
            return;
        }

        currentPage++;
        await UpdatePages();
    }

    private async Task UpdatePages()
    {
        await LoadStoreItems(currentTag, currentCategory, currentType, currentQuery, currentPage);
    }

    private async void BtnInstall_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StoreMcpServer storeMcpServer)
        {
            StoreItemWindow window = new StoreItemWindow(storeMcpServer, _service, installedMcpServers);
            await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            IsUpdateNeeded = window.IsInstalled;
        }
    }

    private async void TbSearch_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await Search();
        }
    }
}