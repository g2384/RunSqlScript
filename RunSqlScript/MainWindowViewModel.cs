using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RunSqlScript.Extensions;

namespace RunSqlScript
{
    public sealed class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            _settings = Config.GetSettings();
            _connectionString = _settings.ConnectionString;
            _useRelativePath = _settings.UseRelativePath;
            Files = new ObservableCollection<string>(_settings.Files);
            ScriptsDropHandler = new FileDropHandler(Files, () => UseRelativePath, collection_CollectionChanged);
            DeleteFile = new DelegateCommand(DeleteSelectedFile);
            Run = new DelegateCommand(() => RunAsyncTask(RunCommand), CanExecuteMethod);
            Add = new DelegateCommand(AddFile, () => true);
            Remove = new DelegateCommand(DeleteSelectedFile, CanExecuteRemove);
        }

        private void DeleteSelectedFile()
        {
            DeleteSelectedItem(Files, ref _selectedFile, nameof(SelectedFile));
        }

        private bool CanExecuteRemove()
        {
            return SelectedFile != null;
        }

        private void AddFile()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Sql Scripts (*.sql)|*.sql|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                Files.AddRange(dialog.FileNames);
            }
        }

        private async void RunAsyncTask(Action action)
        {
            await Task.Run(action);
        }

        private void collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Run.RaiseCanExecuteChanged();
        }

        private bool _useRelativePath;

        public bool UseRelativePath
        {
            get => _useRelativePath;
            set
            {
                if (SetProperty(ref _useRelativePath, value))
                {
                    ChangeFilePaths();
                }
            }
        }

        private void ChangeFilePaths()
        {
            if (UseRelativePath)
            {
                Files.UpdateEachItem(FilePathHelper.GetRelativePath);
            }
            else
            {
                Files.UpdateEachItem(FilePathHelper.GetAbsolutePath);
            }
        }

        private readonly Settings _settings;

        public IDropTarget ScriptsDropHandler { get; }

        public ObservableCollection<string> Files { get; }

        private string _selectedFile;

        public string SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    Remove.RaiseCanExecuteChanged();
                }
            }
        }

        private string _connectionString;

        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }

        public DelegateCommand DeleteFile { get; }

        public DelegateCommand Run { get; }

        public DelegateCommand Add { get; }

        public DelegateCommand Remove { get; }

        private bool _hasRunningTasks;

        private bool CanExecuteMethod()
        {
            return !_hasRunningTasks && Files.Any();
        }

        private bool _isTaskBarVisible;

        public bool IsTaskBarVisible
        {
            get => _isTaskBarVisible;
            set => SetProperty(ref _isTaskBarVisible, value);
        }

        private TaskViewModel _taskViewModel;

        public TaskViewModel TaskViewModel
        {
            get => _taskViewModel;
            set => SetProperty(ref _taskViewModel, value);
        }

        public void RunCommand()
        {
            IsTaskBarVisible = true;
            ChangeIsExecutingTasks(true);
            try
            {
                SaveSettings();
                var task = new RunSqlScriptJob(_settings.ConnectionString, _settings.Files);
                TaskViewModel = new TaskViewModel(task, GetDispatcher());
                task.Execute();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
            }
            finally
            {
                ChangeIsExecutingTasks(false);
            }
        }

        private void ChangeIsExecutingTasks(bool hasRunningTasks)
        {
            var dispatcher = GetDispatcher();
            dispatcher?.Invoke(() =>
            {
                _hasRunningTasks = hasRunningTasks;
                Run.RaiseCanExecuteChanged();
            }, DispatcherPriority.Send);
        }

        private void SaveSettings()
        {
            _settings.Files = Files.ToArray();
            _settings.ConnectionString = ConnectionString;
            _settings.UseRelativePath = UseRelativePath;
            Config.Save(_settings);
        }

        private void DeleteSelectedItem<T>(IList<T> collection, ref T selectedKey, string selectedKeyName)
        {
            var index = collection.IndexOf(selectedKey);
            if (index >= 0)
            {
                collection.RemoveAt(index);
                var nextIndex = Math.Min(index, collection.Count - 1);
                selectedKey = nextIndex >= 0 ? collection[nextIndex] : default(T);
                RaisePropertyChanged(selectedKeyName);
            }
        }

        private static Dispatcher GetDispatcher()
        {
            var app = Application.Current;
            return app?.Dispatcher;
        }
    }
}
