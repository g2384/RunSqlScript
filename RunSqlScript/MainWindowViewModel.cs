using GongSolutions.Wpf.DragDrop;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RunSqlScript
{
    public sealed class MainWindowViewModel : BindableBase
    {
        private const string SettingFile = "config.json";

        public MainWindowViewModel()
        {
            LoadSettings();
            Files = new ObservableCollection<string>(_settings.Files);
            ScriptsDropHandler = new FileDropHandler(Files, () => UseRelativePath, collection_CollectionChanged);
            DeleteFile = new DelegateCommand(() => DeleteSelectedItem(Files, ref _selectedFile, nameof(SelectedFile)));
            Run = new DelegateCommand(() => RunAsyncTask(RunCommand), CanExecuteMethod);
        }

        private async void RunAsyncTask(Action action)
        {
            await Task.Run(action);
        }

        private void collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Run.RaiseCanExecuteChanged();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingFile))
            {
                var json = File.ReadAllText(SettingFile);
                _settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            else
            {
                _settings = new Settings();
                _settings.Init();
            }

            _connectionString = _settings.ConnectionString;
            _useRelativePath = _settings.UseRelativePath;
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
                for (var i = 0; i < Files.Count; i++)
                {
                    Files[i] = FilePathHelper.GetRelativePath(Files[i]);
                }
            }
            else
            {
                for (var i = 0; i < Files.Count; i++)
                {
                    Files[i] = FilePathHelper.GetAbsolutePath(Files[i]);
                }
            }
        }

        private Settings _settings;

        public IDropTarget ScriptsDropHandler { get; }

        public ObservableCollection<string> Files { get; }

        private string _selectedFile;

        public string SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        private string _connectionString;

        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }

        public DelegateCommand DeleteFile { get; }

        public DelegateCommand Run { get; }

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
            var serialized = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            try
            {
                File.WriteAllText(SettingFile, serialized);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
            }
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
