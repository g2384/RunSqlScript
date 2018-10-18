using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace RunSqlScript
{
    public class FileDropHandler : IDropTarget
    {
        private readonly ObservableCollection<string> _collection;

        public FileDropHandler(ObservableCollection<string> collection, NotifyCollectionChangedEventHandler collectionChanged)
        {
            _collection = collection;
            _collection.CollectionChanged += collectionChanged;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            SetDropEffects(dropInfo);
        }

        private static string[] SetDropEffects(IDropInfo dropInfo)
        {
            switch (dropInfo.Data)
            {
                case string str:
                    dropInfo.Effects = DragDropEffects.Move;
                    return new[] { str };
                case DataObject obj:
                    var fileList = obj.GetFileDropList().Cast<string>().ToArray();
                    dropInfo.Effects = fileList.Any(e => e.HasExtension(".sql")) ? DragDropEffects.Copy : DragDropEffects.None;
                    return fileList;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            var files = SetDropEffects(dropInfo);
            var insertIndex = dropInfo.InsertIndex;
            
            switch (dropInfo.Effects)
            {
                case DragDropEffects.None:
                    break;
                case DragDropEffects.Copy:
                    foreach (var file in files)
                    {
                        _collection.Insert(insertIndex, file);
                        insertIndex++;
                    }
                    break;
                case DragDropEffects.Move:
                    var sourceIndex = dropInfo.DragInfo.SourceIndex;
                    foreach (var file in files)
                    {
                        _collection.RemoveAt(sourceIndex);
                        insertIndex = Math.Min(_collection.Count, insertIndex);
                        _collection.Insert(insertIndex, file);
                        insertIndex++;
                    }
                    break;
                case DragDropEffects.Link:
                    break;
                case DragDropEffects.Scroll:
                    break;
                case DragDropEffects.All:
                    break;
            }
        }
    }
}
