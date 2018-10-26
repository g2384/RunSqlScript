using System;

namespace RunSqlScript
{
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
                RaiseStateChanged(e.Message, _processedTasks);
            }
        }

        private double _totalTasks;

        protected abstract void ExecuteTasks();

        public string Description { get; set; }

        public double Progress { get; set; }

        protected abstract double GetTotalTasks();

        private int _processedTasks;

        public JobStatus Status { get; private set; }
        
        protected void Completed()
        {
            Status = JobStatus.Completed;
            RaiseStateChanged("Completed");
        }

        public void RaiseStateChanged(string description)
        {
            _processedTasks++;
            RaiseStateChanged(description, _processedTasks);
        }

        private void RaiseStateChanged(string description, int processedTasks)
        {
            Description = description;
            Progress = processedTasks / _totalTasks * 100;
            StateChanged?.Invoke(this, null);
        }

        public event EventHandler StateChanged;

        protected bool IsCancelled;

        public void Cancel()
        {
            IsCancelled = true;
            Status = JobStatus.Cancelling;
            Description = "Cancelling";
            StateChanged?.Invoke(this, null);
        }

        public void Cancelled()
        {
            Description = "Cancelled";
            Status = JobStatus.Cancelled;
            StateChanged?.Invoke(this, null);
        }
    }
}