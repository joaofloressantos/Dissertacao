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
          HelpText = "Desired algorithm for workflow scheduling. Can be Original, FDWS, OWM, RankHybd and MW-DBS")]
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

        // For original algorithm
        public static bool VIPprocessing;
        public static bool lt6processing;
        public static bool moet6processing;
        public static double queueThreshold = 7;
        public static List<Workflow> VIP;
        public static List<Workflow> lt6;
        public static List<Workflow> moet6;

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

                case "Original":
                    Original(source);
                    break;

                default:
                    Console.WriteLine("Chosen algorithm not available. Exiting...");
                    return;
            }

            Console.ReadKey();
        }

        private static void Original(string source)
        {
            VIP = new List<Workflow>();
            lt6 = new List<Workflow>();
            moet6 = new List<Workflow>();
            VIPprocessing = false;
            lt6processing = false;
            moet6processing = false;

            // Adding files in source folder to Task list
            AddWorkflows(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher(source);
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);

            while (workflows.Count() > 0 || VIP.Count() > 0 || lt6.Count() > 0 || moet6.Count() > 0)
            {
                VIP.AddRange(getVIPworkflows());
                lt6.AddRange(getLt6workflows());
                moet6.AddRange(getMoet6workflows());

                if (!VIPprocessing)
                {
                    new Thread(delegate ()
                    {
                        Utilities.ProcessFile(VIP.First().filePath, "VIP");
                    }).Start();
                    break;
                }

                if (!lt6processing)
                {
                    new Thread(delegate ()
                    {
                        Utilities.ProcessFile(lt6.First().filePath, "lt6");
                    }).Start();
                    break;
                }

                if (!moet6processing)
                {
                    new Thread(delegate ()
                    {
                        Utilities.ProcessFile(moet6.First().filePath, "moet6");
                    }).Start();
                    break;
                }

            }
        }

        private static IEnumerable<Workflow> getMoet6workflows()
        {
            List<Workflow> result = workflows.Where(x => x.fileDuration >= queueThreshold).ToList();
            workflows.RemoveAll(x => x.fileDuration >= queueThreshold);
            return result;
        }

        private static IEnumerable<Workflow> getLt6workflows()
        {
            List<Workflow> result = workflows.Where(x => x.fileDuration < queueThreshold).ToList();
            workflows.RemoveAll(x => x.fileDuration < queueThreshold);
            return result;
        }

        private static List<Workflow> getVIPworkflows()
        {
            List<Workflow> result = workflows.Where(x => x.filePath.Contains("VIP")).ToList();
            workflows.RemoveAll(x => x.filePath.Contains("VIP"));
            return result;
        }

        private static void FDWS(string source, double chunkDuration, int cores)
        {
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

            while (workflows.Count() > 0)
            {
                Thread.Sleep(100);
            }
        }

        private static List<Task> GetAllReadyTasks()
        {
            List<Task> readyTasks = new List<Task>();

            // TODO : Fix this

            List<Workflow> undividedWorkflows = new List<Workflow>();
            try
            {
                undividedWorkflows = workflows.Where(x => x.isDivided == false).ToList();
            }
            catch
            {
                Thread.Sleep(100);
                undividedWorkflows = workflows.Where(x => x.isDivided == false).ToList();
            }
            List<Workflow> dividedWorkflows = new List<Workflow>();
            try
            {
                dividedWorkflows = workflows.Where(x => x.isDivided == true).ToList();
            }
            catch
            {
                Thread.Sleep(100);
                dividedWorkflows = workflows.Where(x => x.isDivided == true).ToList();
            }

            // If not only the divide task can be added
            foreach (Workflow workflow in dividedWorkflows)
            {
                List<Task> tasksToRemove = new List<Task>();
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

                foreach (Task t in tasksToRemove)
                {
                    workflow.tasks.Remove(t);
                }
            }

            // If not only the divide task can be added
            foreach (Workflow workflow in undividedWorkflows)
            {
                List<Task> tasksToRemove = new List<Task>();
                foreach (Task task in workflow.tasks)
                {
                    if (task.Type == "Divide")
                    {
                        readyTasks.Add(task);
                        tasksToRemove.Add(task);
                        break;
                    }
                }

                foreach (Task t in tasksToRemove)
                {
                    workflow.tasks.Remove(t);
                }
            }

            return readyTasks;
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


        private static void RankHybd(string source, double chunkDuration, int cores)
        {
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
                readyTasks.AddRange(RankHybdCheckWorkflows());
                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.First());
                    readyTasks.Remove(readyTasks.First());
                }
            }
        }

        private static void ProcessTask(Task task)
        {
            Interlocked.Increment(ref Program.availableCores);
            try
            {
                Program.availableCores--;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref Program.availableCores);
            }

            switch (task.Type)
            {
                case "Divide":
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
            List<Workflow> unrankedWorkflows = workflows.Where(x => x.rankusCalculated == false).ToList();

            foreach (Workflow workflow in unrankedWorkflows)
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

        private static string[] ReadAllFilesInFolder(string source)
        {
            return Directory
                .EnumerateFiles(source)
                .Where(file => file.ToLower().EndsWith("mp4") ||
                file.ToLower().EndsWith("mpeg") ||
                file.ToLower().EndsWith("mpg") ||
                file.ToLower().EndsWith("wmv") ||
                file.ToLower().EndsWith("mov") ||
                file.ToLower().EndsWith("mkv")).ToArray();
        }

        private static void OnFolderChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo f = new FileInfo(e.FullPath);

            if (f.Extension.Equals(".mp4") || f.Extension.Equals(".mpeg") || f.Extension.Equals(".mpg") || f.Extension.Equals(".mov") || f.Extension.Equals(".wmv") || f.Extension.Equals(".mkv"))
            {
                Thread.Sleep(1000); // To make sure file is completely stable in Windows
                AddWorkflows(new string[] { e.FullPath });
            }
        }
    }
}