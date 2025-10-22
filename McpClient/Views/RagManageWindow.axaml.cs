using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Utils;
using McpClient.Services;
using McpClient.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace McpClient.Views;

public partial class RagManageWindow : Window
{
    private readonly ObservableCollection<RagDocuemntViewModel> _viewModel = new();
    private readonly RagService _ragService;

    public RagManageWindow()
    {
        InitializeComponent();

        DocumentList.ItemsSource = _viewModel;
        if (Design.IsDesignMode)
        {
            _viewModel.Add(new RagDocuemntViewModel { Name = "Test.txt", CreatedTime = DateTime.Today });
            _viewModel.Add(new RagDocuemntViewModel { Name = "114年度行事曆作業-群聯(更新版).pdf", CreatedTime = DateTime.Today });
            _viewModel.Add(new RagDocuemntViewModel { Name = "VSCODE AI程式套使用說明.pptx", CreatedTime = DateTime.Today });
            return;
        }

        if (GlobalService.RagBackendService?.State != CliServiceState.Running)
        {
            HeaderGrid.IsVisible = false;
            TbStatus.IsVisible = true;
            TbStatus.Text = "RAG service is not running.\n" +
                            "You may install the required runtimes and restart RAG service.";
        }
        else
        {
            TbStatus.IsVisible = false;
            _ragService = new RagService();
        }
    }

    private async void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || _ragService == null)
            return;

        List<RagDocuemntViewModel> documents = await _ragService.GetDocuments();
        if (documents != null)
        {
            _viewModel.AddRange(documents);

            if (_viewModel.Count == 0)
            {
                HeaderGrid.IsVisible = false;
                TbStatus.IsVisible = true;
                TbStatus.Text = "You don't have any document yet.";
            }
        }
        else
        {
            HeaderGrid.IsVisible = false;
            TbStatus.IsVisible = true;
            TbStatus.Text = $"Fail to get documents!";
        }
    }

    private async void BtnDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is RagDocuemntViewModel rag)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("RAG Setting", $"Delete document \"{rag.Name}\"?",
                ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info);
            var messageBoxResult = await box.ShowWindowDialogAsync(this);
            if (messageBoxResult != ButtonResult.Yes)
                return;

            bool result = await _ragService.DeleteDocument(rag.Id);
            if (result)
            {
                _viewModel.Remove(rag);
            }
            else
            {
                box = MessageBoxManager.GetMessageBoxStandard("RAG Setting", $"Fail to delete document!",
                    ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
                await box.ShowWindowDialogAsync(this);
            }
        }
    }
}