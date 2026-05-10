using Snap.Nicole.Core;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Snap.Nicole.ViewModels;

internal sealed class OptionsObservableCollection<T, TItem, TId> : ObservableCollection<TItem>
    where T : class
    where TItem : INotifyPropertyChanged, IIdentifiable<TId>, ICopyFrom<TItem>
    where TId : IEquatable<TId>
{
    private readonly IOptionsProvider<T> optionsProvider;
    private readonly Action<T, IList<TItem>> setAction;

    // When updating, prevent recursive options updates
    private bool updating;

    public OptionsObservableCollection(IOptionsProvider<T> optionsProvider, Func<T, IList<TItem>> getAction, Action<T, IList<TItem>> setAction)
        : base(getAction(optionsProvider.CurrentValue))
    {
        this.optionsProvider = optionsProvider;
        this.setAction = setAction;

        foreach (TItem item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        CollectionChanged += OnCollectionChanged;
    }

    public void Update(IList<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        updating = true;
        try
        {
            Dictionary<TId, TItem> existingMap = Items.ToDictionary(item => item.Id);
            HashSet<TId> visitedIds = [];

            for (int newIndex = 0; newIndex < items.Count; newIndex++)
            {
                TItem newItem = items[newIndex];
                TId id = newItem.Id;

                if (existingMap.TryGetValue(id, out TItem? existingItem))
                {
                    visitedIds.Add(id);

                    int currentIndex = IndexOf(existingItem);
                    if (currentIndex != newIndex)
                    {
                        MoveItem(currentIndex, newIndex);
                    }

                    existingItem.CopyFrom(newItem);
                }
                else
                {
                    InsertItem(newIndex, newItem);
                    visitedIds.Add(id);
                }
            }

            for (int i = Count - 1; i >= 0; i--)
            {
                TItem existingItem = this[i];
                if (!visitedIds.Contains(existingItem.Id))
                {
                    RemoveItem(i);
                }
            }
        }
        finally
        {
            updating = false;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (TItem item in e.OldItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (TItem item in e.NewItems)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        if (!updating)
        {
            setAction(optionsProvider.CurrentValue, Items);
            optionsProvider.Update();
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!updating)
        {
            setAction(optionsProvider.CurrentValue, Items);
            optionsProvider.Update();
        }
    }
}