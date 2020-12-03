using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ToraSearcher.UI.Binders
{
    public class SelectedItemsBinder
    {
        private ListBox _listBox;
        private IList _collection;
        private Action _collectionChanged;


        public SelectedItemsBinder(ListBox listView, IList collection, Action collectionChanged = null)
        {
            _listBox = listView;
            _collection = collection;
            _collectionChanged = collectionChanged;

            _listBox.SelectedItems.Clear();

            foreach (var item in _collection)
            {
                _listBox.SelectedItems.Add(item);
            }
        }

        public void Bind()
        {
            _listBox.SelectionChanged += ListView_SelectionChanged;

            if (_collection is INotifyCollectionChanged)
            {
                var observable = (INotifyCollectionChanged)_collection;
                observable.CollectionChanged += Collection_CollectionChanged;
            }
        }

        public void UnBind()
        {
            if (_listBox != null)
                _listBox.SelectionChanged -= ListView_SelectionChanged;

            if (_collection != null && _collection is INotifyCollectionChanged)
            {
                var observable = (INotifyCollectionChanged)_collection;
                observable.CollectionChanged -= Collection_CollectionChanged;
            }
        }

        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //SynchronizationContext.Current.Send(state =>
            //{

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _listBox.SelectedItems.Clear();
                return;
            }

            foreach (var item in e.NewItems ?? new object[0])
            {
                if (!_listBox.SelectedItems.Contains(item))
                    _listBox.SelectedItems.Add(item);
            }

            foreach (var item in e.OldItems ?? new object[0])
            {
                _listBox.SelectedItems.Remove(item);
            }
            //}, null);

            _collectionChanged?.Invoke();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //SynchronizationContext.Current.Send(state =>
            //{
            foreach (var item in e.AddedItems ?? new object[0])
            {
                if (!_collection.Contains(item))
                    _collection.Add(item);
            }

            foreach (var item in e.RemovedItems ?? new object[0])
            {
                _collection.Remove(item);
            }
            //}, null);
        }
    }
}
