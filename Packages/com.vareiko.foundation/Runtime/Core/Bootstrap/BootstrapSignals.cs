namespace Vareiko.Foundation.Bootstrap
{
    public readonly struct ApplicationBootStartedSignal
    {
        public readonly int TotalTasks;

        public ApplicationBootStartedSignal(int totalTasks)
        {
            TotalTasks = totalTasks;
        }
    }

    public readonly struct ApplicationBootTaskStartedSignal
    {
        public readonly string TaskName;
        public readonly int TaskOrder;
        public readonly int Index;
        public readonly int Total;

        public ApplicationBootTaskStartedSignal(string taskName, int taskOrder, int index, int total)
        {
            TaskName = taskName;
            TaskOrder = taskOrder;
            Index = index;
            Total = total;
        }
    }

    public readonly struct ApplicationBootTaskCompletedSignal
    {
        public readonly string TaskName;
        public readonly int TaskOrder;
        public readonly int Index;
        public readonly int Total;

        public ApplicationBootTaskCompletedSignal(string taskName, int taskOrder, int index, int total)
        {
            TaskName = taskName;
            TaskOrder = taskOrder;
            Index = index;
            Total = total;
        }
    }

    public readonly struct ApplicationBootCompletedSignal
    {
        public readonly int TotalTasks;

        public ApplicationBootCompletedSignal(int totalTasks)
        {
            TotalTasks = totalTasks;
        }
    }

    public readonly struct ApplicationBootFailedSignal
    {
        public readonly string TaskName;
        public readonly string Error;

        public ApplicationBootFailedSignal(string taskName, string error)
        {
            TaskName = taskName;
            Error = error;
        }
    }
}
