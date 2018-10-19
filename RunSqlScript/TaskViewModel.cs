using System;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;

namespace RunSqlScript
{
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
            IsProgressBarVisible = true;
        }

        private string _status;

        public string Description
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
            if (_job != null && _job.Status == JobStatus.Running) {
                _job.Cancel();
            }
        }

        private bool _isProgressBarVisible;

        public bool IsProgressBarVisible
        {
            get => _isProgressBarVisible;
            set => SetProperty(ref _isProgressBarVisible, value);
        }

        private void Job_StateChanged(object sender, EventArgs e)
        {
            Description = _job.Description;
            Progress = _job.Progress;
            var status = _job.Status;
            if (status == JobStatus.Cancelled || status == JobStatus.Completed) {
                IsProgressBarVisible = false;
            }
        }
    }
}
