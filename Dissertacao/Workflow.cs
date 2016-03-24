using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissertacao
{
    class Workflow
    {
        public string filePath;

        public List<Task> tasks;
        public Workflow(string filePath)
        {
            this.filePath = filePath;
            
            tasks = new List<Task>();

            tasks.Add(new Task("Divide", filePath, Utilities.CalculateDivideTime(filePath)));
        }
    }
}
