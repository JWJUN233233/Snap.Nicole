using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace Snap.Nicole.Services.AI.Observables;

internal sealed class ObservableAIContentCollection(SynchronizationContext synchronizationContext) : IList<ObservableAIContent>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly SynchronizationContext synchronizationContext = synchronizationContext;
    private readonly List<ObservableAIContent> messages = [];
    private int blockReentrancyCount;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count { get => messages.Count; }

    public bool IsReadOnly => false;

    public ObservableAIContent this[int index]
    {
        get => messages[index];
        set
        {
            if (blockReentrancyCount > 0)
            {
                throw new InvalidOperationException();
            }

            ObservableAIContent originalItem = messages[index];
            messages[index] = value;
            OnPropertyChanged(ObservableCollectionEventArgs.IndexerPropertyChanged);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, originalItem, index);
        }
    }

    public int IndexOf(ObservableAIContent item)
    {
        return messages.IndexOf(item);
    }

    public void Insert(int index, ObservableAIContent item)
    {
        if (blockReentrancyCount > 0)
        {
            throw new InvalidOperationException();
        }

        messages.Insert(index, item);
        OnPropertyChanged(ObservableCollectionEventArgs.CountPropertyChanged);
        OnPropertyChanged(ObservableCollectionEventArgs.IndexerPropertyChanged);
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    public void RemoveAt(int index)
    {
        if (blockReentrancyCount > 0)
        {
            throw new InvalidOperationException();
        }

        ObservableAIContent removedItem = messages[index];
        messages.RemoveAt(index);
        OnPropertyChanged(ObservableCollectionEventArgs.CountPropertyChanged);
        OnPropertyChanged(ObservableCollectionEventArgs.IndexerPropertyChanged);
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
    }

    public void Add(ObservableAIContent item)
    {
        Insert(messages.Count, item);
    }

    public void Clear()
    {
        if (blockReentrancyCount > 0)
        {
            throw new InvalidOperationException();
        }

        messages.Clear();
        OnPropertyChanged(ObservableCollectionEventArgs.CountPropertyChanged);
        OnPropertyChanged(ObservableCollectionEventArgs.IndexerPropertyChanged);
        OnCollectionChanged(ObservableCollectionEventArgs.ResetCollectionChanged);
    }

    public bool Contains(ObservableAIContent item)
    {
        return messages.Contains(item);
    }

    public void CopyTo(ObservableAIContent[] array, int arrayIndex)
    {
        messages.CopyTo(array, arrayIndex);
    }

    public bool Remove(ObservableAIContent item)
    {
        int index = messages.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public IEnumerator<ObservableAIContent> GetEnumerator()
    {
        return messages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return messages.GetEnumerator();
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        synchronizationContext.Send(static state =>
        {
            if (state is not (ObservableAIContentCollection self, PropertyChangedEventArgs args))
            {
                return;
            }

            self.PropertyChanged?.Invoke(self, args);
        }, (this, e));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (CollectionChanged is not { } handler)
        {
            return;
        }

        blockReentrancyCount++;
        try
        {
            synchronizationContext.Send(static state =>
            {
                if (state is not (NotifyCollectionChangedEventHandler handler, object self, NotifyCollectionChangedEventArgs args))
                {
                    return;
                }

                handler(self, args);
            }, (handler, this, e));
        }
        finally
        {
            blockReentrancyCount--;
        }
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
    }
}
