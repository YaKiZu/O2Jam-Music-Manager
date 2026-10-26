using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace O2JamManager.Core;

public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public ObservableRangeCollection() : base() { }
    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// Replaces the entire contents of the collection in a single batch operation,
    /// raising a single NotifyCollectionChangedAction.Reset event to avoid UI thread freezes.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        CheckReentrancy();
        Items.Clear();
        foreach (var item in collection)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
