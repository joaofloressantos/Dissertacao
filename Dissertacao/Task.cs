using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissertacao
{
    class Task
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
