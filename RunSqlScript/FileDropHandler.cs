using GongSolutions.Wpf.DragDrop;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace RunSqlScript
{
    public class FileDropHandler : IDropTarget
    {
        private readonly ICollection<string> _collection;

        public FileDropHandler(ICollection<string> collection)
        {
            _collection = collection;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
            dropInfo.Effects = dragFileList.Any(item =>
            {
                var extension = Path.GetExtension(item);
                return extension != null && extension.Equals(".sql");
            }) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public void Drop(IDropInfo dropInfo)
        {
            var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>().ToList();
            dropInfo.Effects = dragFileList.Any(item =>
            {
                var extension = Path.GetExtension(item);
                return extension != null && extension.Equals(".sql");
            }) ? DragDropEffects.Copy : DragDropEffects.None;
            if (dropInfo.Effects == DragDropEffects.Copy)
            {
                foreach (var file in dragFileList)
                {
                    _collection.Add(file);
                }
            }
        }
    }
}
