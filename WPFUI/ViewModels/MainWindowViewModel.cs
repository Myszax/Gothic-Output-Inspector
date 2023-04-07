using CommunityToolkit.Mvvm.ComponentModel;
using Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using WPFUI.Models;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ICollectionView ConversationCollection { get; set; }

    [ObservableProperty]
    private object _selectedTreeItem;

    [ObservableProperty]
    private string _filterValue = string.Empty;

    private List<Conversation> _conversationList = new();

    private List<Dialogue> _parsedDialogues = new();

    public MainWindowViewModel()
    {
        var path = @"../../../../Parser/g2nk_ou.bin";
        var parser = new Reader(path, 1250);

        try
        {
            _parsedDialogues = parser.Parse();
        }
        catch (Exception)
        {
        }

        foreach (var dialogue in _parsedDialogues)
        {
            _conversationList.Add(Conversation.CreateConversationFromDialogue(dialogue));
        }

        ConversationCollection = CollectionViewSource.GetDefaultView(_conversationList);
        SetGroupingAndSortingOnConversationCollection();
        ConversationCollection.Filter = FilterCollection;
    }

    partial void OnFilterValueChanged(string value) => ConversationCollection.Refresh();

    private bool FilterCollection(object obj)
    {
        if (obj is not Conversation)
            return false;

        var conversation = (Conversation)obj;

        return conversation.Name.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
    }

    private void SetGroupingAndSortingOnConversationCollection()
    {
        var pgd = new PropertyGroupDescription(nameof(Conversation.NpcName));
        pgd.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        ConversationCollection.GroupDescriptions.Add(pgd);

        pgd = new PropertyGroupDescription(nameof(Conversation.Context));
        pgd.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        ConversationCollection.GroupDescriptions.Add(pgd);

        ConversationCollection.SortDescriptions.Add(new SortDescription(nameof(Conversation.Number), ListSortDirection.Ascending));
    }
}