using System;

namespace RunSqlScript
{
    public abstract class Job
    {
        public void Execute()
        {
            try
            {
                RaiseStatusChanged("Initialising", 0);
                _totalTasks = GetTotalTasks();
                ExecuteTasks();
            }
            catch (Exception e)
            {
                RaiseStatusChanged(e.Message, _processedTasks);
            }
        }

        private double _totalTasks;

        protected abstract void ExecuteTasks();

        public string Description { get; set; }

        public double Progress { get; set; }

        protected abstract double GetTotalTasks();

        private int _processedTasks;

        public JobStatus Status { get; set; }

        public void RaiseStatusChanged(string description)
        {
            _processedTasks++;
            RaiseStatusChanged(description, _processedTasks);
        }

        private void RaiseStatusChanged(string description, int processedTasks)
        {
            Description = description;
            Progress = processedTasks / _totalTasks * 100;
            StatusChanged?.Invoke(this, null);
        }

        public event EventHandler StatusChanged;

        protected bool IsCancelled;

        public void Cancel()
        {
            IsCancelled = true;
            Status = JobStatus.Cancelling;
            Description = "Cancelling";
            StatusChanged?.Invoke(this, null);
        }

        public void Cancelled()
        {
            Description = "Cancelled";
            Status = JobStatus.Cancelled;
            StatusChanged?.Invoke(this, null);
        }
    }
}