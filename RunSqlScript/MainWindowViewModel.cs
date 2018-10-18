using GongSolutions.Wpf.DragDrop;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
            ScriptsDropHandler = new FileDropHandler(Files);
            DeleteFile = new DelegateCommand(() => DeleteSelectedItem(Files, ref _selectedFile, nameof(SelectedFile)));
            Run = new DelegateCommand(RunCommand, CanExecuteMethod);
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

        public ICommand DeleteFile { get; }

        public ICommand Run { get; }

        private bool CanExecuteMethod()
        {
            return Files.Any();
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
            SaveSettings();
            var sqlConnection = new SqlConnection(ConnectionString);
            var server = new Server(new ServerConnection(sqlConnection));
            foreach (var file in Files)
            {
                var script = File.ReadAllText(file);
                server.ConnectionContext.ExecuteNonQuery(script);
            }
            var taskManager = new RunSqlScriptTask();
            TaskViewModel = new TaskViewModel(taskManager, GetDispatcher());
            

            MessageBox.Show("Finished", "Message", MessageBoxButton.OK);
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

    public sealed class RunSqlScriptTask : Task
    {
        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class Task
    {
        public abstract void Execute();

        public event EventHandler StateChanged;

        public void Cancel()
        {

        }
    }

    public sealed class TaskViewModel : BindableBase
    {
        private readonly Task _task;
        private readonly Dispatcher _dispatcher;

        public TaskViewModel(Task task, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _task = task;
            CancelCommand = new DelegateCommand(Cancel);
            _task.StateChanged += Submission_StateChanged;
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
            _task.Cancel();
            Status = "Canceling";
        }

        private void Submission_StateChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
