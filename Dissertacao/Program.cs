using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Dissertacao
{
    internal class Options
    {
        //controls the current amount of threads performing tasks
        public volatile int workingThreads = 0;

        [Option('s', "source", Required = true,
          HelpText = "Source video file folder name/path")]
        public string Source { get; set; }

        //[Option('d', "destination", DefaultValue = "ProcessedVideos",
        //  HelpText = "Destination video file folder name/path")]
        //public string Destination { get; set; }

        [Option('t', "chunkSize", DefaultValue = 5,
          HelpText = "Desired duration of each video file chunk")]
        public double Duration { get; set; }

        [Option('c', "cores",
          HelpText = "Number of cores used for processing")]
        public int Cores { get; set; }

        [Option('a', "algorithm", DefaultValue = "FDWS",
          HelpText = "Desired algorithm for workflow scheduling. Can be FDWS, OWM, RankHybd and MW-DBS")]
        public string Algorithm { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    internal class Program
    {
        #region Globals

        // Definition Variables
        private static double chunkDuration;

        public static int availableCores = Environment.ProcessorCount; // Setting max available cores by default
        private static string source;

        //private static string destination;
        private static string algorithm;

        // Workflow List
        public static List<Workflow> workflows = new List<Workflow>();

        #endregion Globals

        private static void Main(string[] args)
        {
            // Parsing args
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Getting arg data into variables
                if (options.Cores > availableCores)
                {
                    Console.WriteLine("Number of processor cores selected (" + options.Cores + ") exceeds the ones available. Defaulting to max availables cores: " + availableCores);
                }
                else if (options.Cores > 0)
                {
                    availableCores = options.Cores;
                }

                chunkDuration = options.Duration;
                source = options.Source;
                algorithm = options.Algorithm;
                //destination = options.Destination;

                // Values are available here
                Console.WriteLine("SELECTED OPTIONS: \n");
                Console.WriteLine("Source: " + source);
                //Console.WriteLine("Destination: " + options.Destination);
                Console.WriteLine("Chunk Size: " + chunkDuration);
                Console.WriteLine("Cores: " + availableCores);
                Console.WriteLine("Algorithm: " + algorithm);
                Console.WriteLine("");
            }
            else
            {
                return;
            }

            // Selecting algorithm
            switch (algorithm)
            {
                case "FDWS":
                    FDWS(source, chunkDuration, availableCores);
                    break;

                case "RankHybd":
                    RankHybd(source, chunkDuration, availableCores);
                    break;

                case "OWM":
                    break;

                case "MW-DBS":
                    break;

                default:
                    Console.WriteLine("Chosen algorithm not available. Exiting...");
                    return;
            }
        }

        private static void RankHybd(string source, double chunkDuration, int cores)
        {
            Console.WriteLine("entrou");
            // Adding files in source folder to Task list
            AddWorkflows(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher(source);
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            List<Task> readyTasks = new List<Task>();

            while (workflows.Count() > 0)
            {
                readyTasks = RankHybdCheckWorkflows();
                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.First());
                    readyTasks.Remove(readyTasks.First());
                }
            }
        }

        private static void ProcessTask(Task task)
        {
            availableCores--;
            switch (task.Type)
            {
                case "Divide":
                    //var t = new Thread(() => Utilities.DivideToChunks(task.FilePath, chunkDuration));
                    //t.Start();
                    new Thread(delegate ()
                    {
                        Utilities.DivideToChunks(task.FilePath, chunkDuration);
                    }).Start();
                    break;

                case "Chunk":
                    new Thread(delegate ()
                    {
                        Utilities.ProcessChunk(task.FilePath);
                    }).Start();
                    break;

                case "Join":
                    new Thread(delegate ()
                    {
                        Utilities.RebuildFile(task.FilePath);
                    }).Start();
                    break;

                default:
                    break;
            }
        }

        private static List<Task> RankHybdCheckWorkflows()
        {
            List<Task> readyTasks = new List<Task>();

            foreach (Workflow workflow in workflows)
            {
                if (!workflow.rankusCalculated)
                {
                    foreach (Task task in workflow.tasks)
                    {
                        CalculateRanku(task);
                    }
                    workflow.rankusCalculated = true;
                }
            }
            readyTasks = GetAllReadyTasks();

            int multiple = GetNumberOfDifferentWorkflowsInPool(readyTasks);

            if (multiple == 1)
            {
                readyTasks = readyTasks.OrderByDescending(x => x.ranku).ToList();
            }
            else
            {
                readyTasks = readyTasks.OrderBy(x => x.ranku).ToList();
            }

            return readyTasks;
        }

        private static int GetNumberOfDifferentWorkflowsInPool(List<Task> readyTasks)
        {
            return readyTasks.Select(x => x.FilePath).Distinct().Count();
        }

        private static void FDWS(string source, double chunkDuration, int cores)
        {
            // Adding files in source folder to Task list
            AddWorkflows(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher("C:\\Users\\t-jom\\Downloads");
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            while (workflows.Count() > 0)
            {
                Thread.Sleep(100);
            }
        }

        private static List<Task> GetAllReadyTasks()
        {
            List<Task> readyTasks = new List<Task>();

            foreach (Workflow workflow in workflows)
            {
                List<Task> tasksToRemove = new List<Task>();
                // If not only the divide task can be added
                if (workflow.isDivided)
                {
                    int tasksAdded = 0;
                    foreach (Task task in workflow.tasks)
                    {
                        if (task.Type == "Chunk")
                        {
                            readyTasks.Add(task);
                            tasksToRemove.Add(task);
                            tasksAdded++;
                        }
                    }

                    // If there are no more chunk tasks it should finally join
                    if (tasksAdded == 0 && workflow.chunkTasks == workflow.chunkTasksDone)
                    {
                        foreach (Task task in workflow.tasks)
                        {
                            if (task.Type == "Join")
                            {
                                readyTasks.Add(task);
                                tasksToRemove.Add(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (Task task in workflow.tasks)
                    {
                        if (task.Type == "Divide")
                        {
                            readyTasks.Add(task);
                            tasksToRemove.Add(task);
                            break;
                        }
                    }
                }

                foreach (Task t in tasksToRemove)
                {
                    workflow.tasks.Remove(t);
                }
            }

            return readyTasks;
        }

        private static void AddOneReadyTaskPerWorkflow()
        {
        }

        private static void CalculateRanku(Task task)
        {
            switch (task.Type)
            {
                case "Join":
                    task.ranku = 0;
                    break;

                case "Divide":
                    task.ranku = task.TimeToProcess + getNextTaskRanku(task);
                    break;

                case "Chunk":
                    task.ranku = task.TimeToProcess;
                    break;

                default:
                    break;
            }
        }

        private static double getNextTaskRanku(Task task)
        {
            foreach (Workflow workflow in workflows)
            {
                foreach (Task t in workflow.tasks)
                {
                    if (task.FilePath == t.FilePath && t.Type == "Chunk")
                    {
                        if (t.ranku == 0) t.ranku = t.TimeToProcess;
                        return t.TimeToProcess;
                    }
                }
            }

            return 0;
        }

        // DONE

        private static void AddWorkflows(string[] files)
        {
            foreach (string file in files)
            {
                workflows.Add(new Workflow(file, chunkDuration));
                Console.WriteLine(
                    "Added file " + workflows.ElementAt(workflows.Count - 1).filePath + " at "
                    + workflows.ElementAt(workflows.Count - 1).beginTime + " with duration "
                    + workflows.ElementAt(workflows.Count - 1).fileDuration + " and last chunk duration "
                    + workflows.ElementAt(workflows.Count - 1).lastChunkDuration
                    );
            }
        }

        private static string[] ReadAllFilesInFolder(string source)
        {
            return Directory
                .EnumerateFiles(source)
                .Where(file => file.ToLower().EndsWith("mp4") ||
                file.ToLower().EndsWith("mpeg") ||
                file.ToLower().EndsWith("mpg") ||
                file.ToLower().EndsWith("wmv") ||
                file.ToLower().EndsWith("mkv")).ToArray();
        }

        private static void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo f = new FileInfo(e.FullPath);

            if (f.Extension.Equals(".mp4") || f.Extension.Equals(".mpeg") || f.Extension.Equals(".mpg") || f.Extension.Equals(".wmv") || f.Extension.Equals(".mkv"))
            {
                Thread.Sleep(1000); // To make sure file is completely stable in Windows
                AddWorkflows(new string[] { e.FullPath });
            }
        }
    }
}