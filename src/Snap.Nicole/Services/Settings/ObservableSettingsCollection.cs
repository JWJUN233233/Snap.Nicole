using Snap.Nicole.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.Settings;

internal sealed class ObservableSettingsCollection<TItem, TId> : ObservableCollection<TItem>, ICopyFrom<ObservableSettingsCollection<TItem, TId>>
    where TItem : class, INotifyPropertyChanged, IIdentifiable<TId>, ICopyFrom<TItem>, new()
    where TId : struct, IEquatable<TId>
{
    private readonly HashSet<TItem> subscribedItems = [];
    private TItem? currentItem;
    private TId? currentItemId;

    [JsonIgnore]
    public TItem? CurrentItem
    {
        get => currentItemId is TId id ? CoerceCurrentItem(id) : CoerceCurrentItem(currentItem);
        set => SetCurrentItem(value);
    }

    [JsonIgnore]
    public TId? CurrentItemId
    {
        get => currentItemId;
        set => SetCurrentItemId(value);
    }

    public void MoveCurrentTo(TId id)
    {
        CurrentItemId = id;
    }

    public void MoveCurrentToFirst()
    {
        CurrentItem = this.FirstOrDefault();
    }

    public void CopyFrom(ObservableSettingsCollection<TItem, TId> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ReferenceEquals(this, source))
        {
            return;
        }

        Dictionary<TId, TItem> existingMap = [];
        foreach (TItem item in this)
        {
            existingMap.TryAdd(item.Id, item);
        }

        HashSet<TId> visitedIds = [];
        HashSet<TItem> retainedItems = [];

        int targetIndex = 0;
        foreach (TItem sourceItem in source)
        {
            TId id = sourceItem.Id;
            if (!visitedIds.Add(id))
            {
                continue;
            }

            if (existingMap.TryGetValue(id, out TItem? existingItem))
            {
                int currentIndex = IndexOf(existingItem);
                if (currentIndex != targetIndex)
                {
                    Move(currentIndex, targetIndex);
                }

                existingItem.CopyFrom(sourceItem);
                retainedItems.Add(existingItem);
            }
            else
            {
                TItem newItem = new();
                newItem.CopyFrom(sourceItem);
                Insert(targetIndex, newItem);
                retainedItems.Add(newItem);
            }

            targetIndex++;
        }

        for (int i = Count - 1; i >= 0; i--)
        {
            if (!retainedItems.Contains(this[i]))
            {
                RemoveAt(i);
            }
        }

        EnsureCurrentItem();
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        SyncItemSubscriptions();
        EnsureCurrentItem();
        base.OnCollectionChanged(e);
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        if (ReferenceEquals(sender, CurrentItem))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItem)));
        }
    }

    private void SyncItemSubscriptions()
    {
        HashSet<TItem> currentItems = [.. this];

        foreach (TItem item in subscribedItems.ToArray())
        {
            if (!currentItems.Contains(item))
            {
                UnsubscribeItem(item);
            }
        }

        foreach (TItem item in this)
        {
            SubscribeItem(item);
        }
    }

    private void SubscribeItem(TItem item)
    {
        if (subscribedItems.Add(item))
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void UnsubscribeItem(TItem item)
    {
        if (subscribedItems.Remove(item))
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    private void EnsureCurrentItem()
    {
        SetCurrentItemCore(currentItemId is TId id ? CoerceCurrentItem(id) : CoerceCurrentItem(currentItem));
    }

    private void SetCurrentItem(TItem? value)
    {
        SetCurrentItemCore(CoerceCurrentItem(value));
    }

    private void SetCurrentItemId(TId? value)
    {
        if (value is TId id)
        {
            TItem? item = CoerceCurrentItem(id);
            SetCurrentItemCore(item, item is null ? value : item.Id);
        }
        else
        {
            SetCurrentItemCore(this.FirstOrDefault(), null);
        }
    }

    private void SetCurrentItemCore(TItem? value)
    {
        SetCurrentItemCore(value, value?.Id);
    }

    private void SetCurrentItemCore(TItem? value, TId? id)
    {
        TItem? oldItem = currentItem;
        TId? oldId = currentItemId;

        currentItem = value;
        currentItemId = id;

        if (!ReferenceEquals(oldItem, currentItem))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItem)));
        }

        if (!IdEquals(oldId, currentItemId))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItemId)));
        }
    }

    private TItem? CoerceCurrentItem(TItem? value)
    {
        if (value is not null && Contains(value))
        {
            return value;
        }

        return this.FirstOrDefault();
    }

    private TItem? CoerceCurrentItem(TId id)
    {
        TItem? item = this.FirstOrDefault(item => EqualityComparer<TId>.Default.Equals(item.Id, id));
        if (item is not null)
        {
            return item;
        }

        return this.FirstOrDefault();
    }

    private static bool IdEquals(TId? x, TId? y)
    {
        return x.HasValue == y.HasValue
            && (!x.HasValue || EqualityComparer<TId>.Default.Equals(x.Value, y!.Value));
    }
}
