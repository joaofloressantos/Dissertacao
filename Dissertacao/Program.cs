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
        public static double queueThreshold = 8;

        public static List<Workflow> VIP;
        public static List<Workflow> lt6;
        public static List<Workflow> moet6;
        public static List<Workflow> workflowsInProgress;

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

                // Values are available here
                Console.WriteLine("SELECTED OPTIONS: \n");
                Console.WriteLine("Source: " + source);
                //Console.WriteLine("Destination: " + options.Destination);
                Console.WriteLine("Chunk Size: " + chunkDuration);
                Console.WriteLine("Cores: " + availableCores);
                Console.WriteLine("Algorithm: " + algorithm);
                Console.WriteLine();
            }
            else
            {
                return;
            }

            // Selecting algorithm
            switch (algorithm)
            {
                case "FDWS":
                    FDWS(source, chunkDuration);
                    break;

                case "RankHybd":
                    RankHybd(source, chunkDuration);
                    break;

                case "OWM":
                    OWM(source, chunkDuration);
                    break;

                case "MW-DBS":
                    MWDBS(source, chunkDuration);
                    break;

                case "Original":
                    Original(source);
                    break;

                default:
                    Console.WriteLine("Chosen algorithm not available. Exiting...");
                    return;
            }
        }

        /// /////////////////////////////////////////
        // Algorithms
        /// /////////////////////////////////////////

        private static void MWDBS(string source, double chunkDuration)
        {
            InitialWorkflowCheck(source);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            while (workflows.Count() > 0)
            {
                List<Task> readyTasks = new List<Task>();
                List<Workflow> unrankedWorkflows = GetUnrankedWorkflows();

                CalculateRankus(unrankedWorkflows);

                readyTasks = GetHighestRankuTaskFromEachWorkflow();
                readyTasks = ComputeRankD(readyTasks);

                while (readyTasks.Count > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.OrderByDescending(x => x.rankd).ToList().First());
                    readyTasks.Remove(readyTasks.OrderByDescending(x => x.rankd).ToList().First());
                }
            }
        }

        private static void Original(string source)
        {
            VIP = new List<Workflow>();
            lt6 = new List<Workflow>();
            moet6 = new List<Workflow>();
            workflowsInProgress = new List<Workflow>();

            InitialWorkflowCheck(source);

            while (workflowsInProgress.Count() > 0 || workflows.Count() > 0 || VIP.Count() > 0 || lt6.Count() > 0 || moet6.Count() > 0)
            {
                if (availableCores > 0)
                {
                    VIP.AddRange(GetVIPworkflows());
                    lt6.AddRange(GetLt6workflows());
                    moet6.AddRange(GetMoet6workflows());

                    Random r = new Random();
                    int rInt = r.Next(0, 100);

                    if (rInt <= 40 && VIP.Count() > 0)
                    {
                        string filePath = VIP.First().filePath;
                        DecrementAvailableCores();
                        new Thread(delegate ()
                        {
                            Utilities.ProcessFile(filePath);
                        }).Start();
                        workflowsInProgress.Add(VIP.First());
                        VIP.Remove(VIP.First());
                        continue;
                    }

                    if (rInt <= 70 && lt6.Count() > 0)
                    {
                        string filePath = lt6.First().filePath;
                        DecrementAvailableCores();
                        new Thread(delegate ()
                        {
                            Utilities.ProcessFile(filePath);
                        }).Start();
                        workflowsInProgress.Add(lt6.First());
                        lt6.Remove(lt6.First());
                        continue;
                    }

                    if (moet6.Count() > 0)
                    {
                        string filePath = moet6.First().filePath;
                        DecrementAvailableCores();
                        new Thread(delegate ()
                        {
                            Utilities.ProcessFile(filePath);
                        }).Start();
                        workflowsInProgress.Add(moet6.First());
                        moet6.Remove(moet6.First());
                    }
                }
            }
        }

        private static void FDWS(string source, double chunkDuration)
        {
            InitialWorkflowCheck(source);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            while (workflows.Count() > 0)
            {
                List<Task> readyTasks = new List<Task>();
                List<Workflow> unrankedWorkflows = GetUnrankedWorkflows();

                // TODO: Check if working and refactor (RankHybd also has this)
                CalculateRankus(unrankedWorkflows);

                readyTasks = GetHighestRankuTaskFromEachWorkflow();
                readyTasks = ComputeRankr(readyTasks);

                while (readyTasks.Count > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.OrderByDescending(x => x.rankr).ToList().First());
                    readyTasks.Remove(readyTasks.OrderByDescending(x => x.rankr).ToList().First());
                }
            }
        }

        private static void OWM(string source, double chunkDuration)
        {
            InitialWorkflowCheck(source);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            while (workflows.Count() > 0)
            {
                List<Task> readyTasks = new List<Task>();
                List<Workflow> unrankedWorkflows = workflows.Where(x => x.rankusCalculated == false).ToList();

                // TODO: Check if working and refactor (RankHybd also has this)
                foreach (Workflow workflow in unrankedWorkflows)
                {
                    foreach (Task task in workflow.tasks)
                    {
                        CalculateRanku(task);
                    }
                    workflow.rankusCalculated = true;
                }

                readyTasks.AddRange(GetHighestRankuTaskFromEachWorkflow());

                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    Task t = readyTasks.First();
                    readyTasks.Remove(readyTasks.First());
                    ProcessTask(t);
                }
            }
        }

        private static void RankHybd(string source, double chunkDuration)
        {
            InitialWorkflowCheck(source);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/

            List<Task> readyTasks = new List<Task>();

            while (workflows.Count() > 0)
            {
                readyTasks.AddRange(GetReadyTasksRankHybd());
                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.First());
                    readyTasks.Remove(readyTasks.First());
                }
            }
        }

        /// /////////////////////////////////////////
        // Aux Functions
        /// /////////////////////////////////////////

        private static void InitialWorkflowCheck(string source)
        {
            // Adding files in source folder to Task list
            AddWorkflows(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher(source);
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);
        }

        private static List<Workflow> GetUnrankedWorkflows()
        {
            try
            {
                return workflows.Where(x => x.rankusCalculated == false).ToList();
            }
            catch
            {
                Thread.Sleep(200);
                return workflows.Where(x => x.rankusCalculated == false).ToList();
            }
        }

        public static Workflow GetWorkflowFromTask(Task task)
        {
            try
            {
                return Program.workflows.Where(x => x.tasks.Contains(task)).ToList().First();
            }
            catch
            {
                Thread.Sleep(200);
                return Program.workflows.Where(x => x.tasks.Contains(task)).ToList().First();
            }
        }

        public static Workflow GetWorkflowFromPath(string filePath)
        {
            return workflows.Where(x => x.filePath == filePath).ToList().First();
        }

        private static List<Task> GetReadyTasksRankHybd()
        {
            List<Task> readyTasks = new List<Task>();
            List<Workflow> unrankedWorkflows = workflows.Where(x => x.rankusCalculated == false).ToList();

            foreach (Workflow workflow in unrankedWorkflows)
            {
                foreach (Task task in workflow.tasks)
                {
                    CalculateRanku(task);
                }
                workflow.rankusCalculated = true;
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

        private static List<Workflow> GetMoet6workflows()
        {
            List<Workflow> result = workflows.Where(x => x.fileDuration >= queueThreshold).ToList();
            workflows.RemoveAll(x => x.fileDuration >= queueThreshold);
            return result;
        }

        private static List<Workflow> GetLt6workflows()
        {
            List<Workflow> result = workflows.Where(x => x.fileDuration < queueThreshold).ToList();
            workflows.RemoveAll(x => x.fileDuration < queueThreshold);
            return result;
        }

        private static List<Workflow> GetVIPworkflows()
        {
            List<Workflow> result = workflows.Where(x => x.filePath.Contains("VIP")).ToList();
            workflows.RemoveAll(x => x.filePath.Contains("VIP"));
            return result;
        }

        private static void ProcessTask(Task task)
        {
            DecrementAvailableCores();
            Workflow w = GetWorkflowFromTask(task);

            switch (task.Type)
            {
                case "Divide":
                    w.isDividing = true;
                    new Thread(delegate ()
                    {
                        Utilities.DivideToChunks(task.FilePath, chunkDuration);
                    }).Start();
                    break;

                case "Chunk":
                    IncrementChunksProcessing(w);
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

            w.tasks.Remove(task);
        }

        private static List<Task> ComputeRankr(List<Task> readyTasks)
        {
            foreach (Task task in readyTasks)
            {
                Workflow w = workflows.Where(x => x.tasks.Contains(task)).ToList().First();
                w.tasks.Remove(task);
                int totalTasks = w.chunkTasks + 2;
                if (w.isDivided)
                {
                    task.rankr = 1 / (((double)totalTasks - (1 + (double)w.chunkTasksDone)) / (double)totalTasks);
                }
                else
                {
                    task.rankr = 1;
                }
            }

            return readyTasks;
        }

        private static List<Task> ComputeRankD(List<Task> readyTasks)
        {
            foreach (Task task in readyTasks)
            {
                Workflow w = GetWorkflowFromTask(task);
                switch (task.Type)
                {
                    case "Join":
                        task.rankd = 1 / w.fileDuration * (w.chunkTasks + 1);
                        break;

                    case "Divide":
                        task.rankd = 0;
                        break;

                    case "Chunk":
                        task.rankd = 1 / w.fileDuration * (w.chunkTasksDone + 1);
                        break;

                    default:
                        break;
                }
            }

            return readyTasks;
        }

        private static void CalculateRankus(List<Workflow> unrankedWorkflows)
        {
            foreach (Workflow workflow in unrankedWorkflows)
            {
                foreach (Task task in workflow.tasks)
                {
                    CalculateRanku(task);
                }
                workflow.rankusCalculated = true;
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
                    task.ranku = task.TimeToProcess + GetNextTaskRanku(task);
                    break;

                case "Chunk":
                    task.ranku = task.TimeToProcess;
                    break;

                default:
                    break;
            }
        }

        private static List<Task> GetHighestRankuTaskFromEachWorkflow()
        {
            List<Task> result = new List<Task>();
            List<Workflow> undividedWorkflows = GetUndividedWorkflows();
            List<Workflow> dividedWorkflows = GetDividedWorkflows();

            foreach (Workflow workflow in undividedWorkflows)
            {
                result.Add(workflow.tasks.Where(x => x.Type == "Divide").ToList().First());
            }

            foreach (Workflow workflow in dividedWorkflows)
            {
                // If there are no more chunk tasks it should finally join
                if (workflow.tasks.Count() > 1)
                {
                    result.Add(workflow.tasks.Where(x => x.Type == "Chunk").ToList().OrderByDescending(x => x.ranku).ToList().First());
                }
                else if (workflow.chunksProcessing == 0 && workflow.tasks.Count() == 1)
                {
                    result.Add(workflow.tasks.Where(x => x.Type == "Join").ToList().First());
                }
            }

            return result;
        }

        private static List<Workflow> GetDividedWorkflows()
        {
            List<Workflow> dividedWorkflows = new List<Workflow>();
            try
            {
                dividedWorkflows = workflows.Where(x => x.isDivided == true).ToList();
            }
            catch
            {
                Thread.Sleep(200);
                dividedWorkflows = workflows.Where(x => x.isDivided == true).ToList();
            }

            return dividedWorkflows;
        }

        private static List<Workflow> GetUndividedWorkflows()
        {
            List<Workflow> undividedWorkflows = new List<Workflow>();
            try
            {
                undividedWorkflows = workflows.Where(x => x.isDivided == false && x.isDividing == false).ToList();
            }
            catch
            {
                Thread.Sleep(200);
                undividedWorkflows = workflows.Where(x => x.isDivided == false && x.isDividing == false).ToList();
            }

            return undividedWorkflows;
        }

        private static List<Task> GetAllReadyTasks()
        {
            List<Task> readyTasks = new List<Task>();

            // TODO : Fix this

            List<Workflow> undividedWorkflows = GetUndividedWorkflows();
            List<Workflow> dividedWorkflows = GetDividedWorkflows();

            // On divided workflows, Chunk and Join tasks can be added
            foreach (Workflow workflow in dividedWorkflows)
            {
                List<Task> tasksToRemove = new List<Task>();
                int tasksAdded = 0;
                foreach (Task task in workflow.tasks.Where(x => x.Type == "Chunk"))
                {
                    readyTasks.Add(task);
                    tasksToRemove.Add(task);
                    tasksAdded++;
                }

                // If there are no more chunk tasks it should finally join
                if (tasksAdded == 0 && workflow.chunkTasks == workflow.chunkTasksDone)
                {
                    foreach (Task task in workflow.tasks.Where(x => x.Type == "Join"))
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

            // If not only the Divide task can be added
            foreach (Workflow workflow in undividedWorkflows)
            {
                List<Task> tasksToRemove = new List<Task>();
                foreach (Task task in workflow.tasks.Where(x => x.Type == "Divide"))
                {
                    readyTasks.Add(task);
                    tasksToRemove.Add(task);
                    break;
                }

                foreach (Task t in tasksToRemove)
                {
                    workflow.tasks.Remove(t);
                }
            }

            return readyTasks;
        }

        private static double GetNextTaskRanku(Task task)
        {
            Workflow w = GetWorkflowFromPath(task.FilePath);
            Task t = w.tasks.Where(x => x.Type == "Chunk").ToList().First();
            return t.TimeToProcess;
        }

        public static void IncrementChunksProcessing(Workflow workflow)
        {
            Interlocked.Increment(ref workflow.chunksProcessing);
            try
            {
                workflow.chunksProcessing++;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref workflow.chunksProcessing);
            }
        }

        public static void DecrementChunksProcessing(Workflow w)
        {
            Interlocked.Increment(ref w.chunksProcessing);
            try
            {
                w.chunksProcessing--;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref w.chunksProcessing);
            }
        }

        public static void IncrementAvailableCores()
        {
            Interlocked.Increment(ref Program.availableCores);
            try
            {
                Program.availableCores++;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref Program.availableCores);
            }
        }

        public static void DecrementAvailableCores()
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
        }

        public static void IncrementChunkTasksDone(Workflow w)
        {
            Interlocked.Increment(ref w.chunkTasksDone);
            try
            {
                w.chunkTasksDone++;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref w.chunkTasksDone);
            }
        }
    }
}