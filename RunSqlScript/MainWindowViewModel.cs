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
            return true;
            //return Files.Any();
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
    }
}
