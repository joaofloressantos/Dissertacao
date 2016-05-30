namespace Dissertacao
{
    internal class Task
    {
        public string Type { get; set; }
        public string FilePath { get; set; }
        public double TimeToProcess { get; set; }
        public double ranku { get; set; }
        public double rankr { get; set; }

        public Task(string Type, string FilePath, double TimeToProcess)
        {
            this.Type = Type;
            this.TimeToProcess = TimeToProcess;
            this.FilePath = FilePath;
            this.ranku = 0;
            this.rankr = 0;
        }
        
    }
}