using GongSolutions.Wpf.DragDrop;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.SqlClient;
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
            ScriptsDropHandler = new FileDropHandler(Files, collection_CollectionChanged);
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

        private bool _isProgressVisible;

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private TaskViewModel _taskViewModel;

        public TaskViewModel TaskViewModel
        {
            get => _taskViewModel;
            set => SetProperty(ref _taskViewModel, value);
        }

        public void RunCommand()
        {
            IsProgressVisible = true;
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
            File.WriteAllText(SettingFile, JsonConvert.SerializeObject(_settings, Formatting.Indented));
        }

        private void DeleteSelectedItem<T>(ObservableCollection<T> collection, ref T selectedKey, string selectedKeyName)
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

    public sealed class RunSqlScriptJob : Job
    {
        private readonly string _connectionString;
        private readonly string[] _files;

        public RunSqlScriptJob(string connectionString, string[] files)
        {
            _connectionString = connectionString;
            _files = files;
        }

        protected override double GetTotalTasks()
        {
            return _files.Length + 1;
        }

        protected override void ExecuteTasks()
        {
            RaiseStateChanged("Connecting to sql server");
            var sqlConnection = new SqlConnection(_connectionString);
            var server = new Server(new ServerConnection(sqlConnection));
            foreach (var file in _files)
            {
                RaiseStateChanged("Executing " + Path.GetFileName(file));
                var script = File.ReadAllText(file);
                server.ConnectionContext.ExecuteNonQuery(script);
            }
            RaiseStateChanged("Completed");
        }
    }

    public abstract class Job
    {
        public void Execute()
        {
            try
            {
                RaiseStateChanged("Initialising", 0);
                _totalTasks = GetTotalTasks();
                ExecuteTasks();
            }
            catch (Exception e)
            {
                RaiseStateChanged(e.Message);
            }
        }

        private double _totalTasks;

        protected abstract void ExecuteTasks();

        public string Description { get; set; }

        public double Progress { get; set; }

        protected abstract double GetTotalTasks();

        private int _processedTasks;

        public void RaiseStateChanged(string description)
        {
            _processedTasks++;
            RaiseStateChanged(description, _processedTasks);
        }

        private void RaiseStateChanged(string description, int processedTasks)
        {
            Description = description;
            Progress = processedTasks / _totalTasks * 100;
            StateChanged(this, null);
        }

        public event EventHandler StateChanged;

        public void Cancel()
        {

        }
    }

    public sealed class TaskViewModel : BindableBase
    {
        private readonly Job _job;
        private readonly Dispatcher _dispatcher;

        public TaskViewModel(Job job, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _job = job;
            CancelCommand = new DelegateCommand(Cancel);
            _job.StateChanged += Job_StateChanged;
        }

        private string _status;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private double _progress;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public DelegateCommand CancelCommand { get; }

        private void Cancel()
        {
            _job.Cancel();
            Status = "Canceling";
        }

        private void Job_StateChanged(object sender, EventArgs e)
        {
            Status = _job.Description;
            Progress = _job.Progress;
        }
    }
}
