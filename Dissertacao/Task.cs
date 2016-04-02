namespace Dissertacao
{
    internal class Task
    {
        public string Type { get; set; }
        public string FilePath { get; set; }
        public double TimeToProcess { get; set; }

        public Task(string Type, string FilePath, double TimeToProcess)
        {
            this.Type = Type;
            this.TimeToProcess = TimeToProcess;
            this.FilePath = FilePath;
        }
    }
}