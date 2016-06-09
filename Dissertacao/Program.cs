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
                //destination = options.Destination;

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

            Console.ReadKey();
        }

        private static void MWDBS(string source, double chunkDuration)
        {
            /*
                2: for ti;j 2 Ready Tasks pool do
                    3: Assign a priority rankD(ti;j)
                4: end for
                5: Pfree   free processors pk 2 P
                6: while (Ready Tasks 6=  & Pfree 6= ) do
                    7: tcurr   task with highest priority
                    8: for pk 2 Padmissible do
                        9: Calculate Quality Measure Q(tcurr; pk)
                    10: end for
                    11: Psel   Processor pk with highest Q
                    12: Assign current task tcurr to Psel
                    13: Update RBj and RCAj
                    14: Pfree   Pfree 􀀀 Psel
                    15: Remove Task tcurr from Ready Tasks pool
                16: end while
            */
        }

        private static void Original(string source)
        {
            VIP = new List<Workflow>();
            lt6 = new List<Workflow>();
            moet6 = new List<Workflow>();
            workflowsInProgress = new List<Workflow>();

            // Adding files in source folder to Task list
            AddWorkflows(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher(source);
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);

            while (workflowsInProgress.Count() > 0 || workflows.Count() > 0 || VIP.Count() > 0 || lt6.Count() > 0 || moet6.Count() > 0)
            {
                if (availableCores > 0)
                {
                    VIP.AddRange(getVIPworkflows());
                    lt6.AddRange(getLt6workflows());
                    moet6.AddRange(getMoet6workflows());
                    Random r = new Random();
                    int rInt = r.Next(0, 100);

                    if (rInt <= 40 && VIP.Count() > 0)
                    {
                        string filePath = VIP.First().filePath;
                        decrementAvailableCores();
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
                        decrementAvailableCores();
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
                        decrementAvailableCores();
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

        private static List<Workflow> getMoet6workflows()
        {
            List<Workflow> result = workflows.Where(x => x.fileDuration >= queueThreshold).ToList();
            workflows.RemoveAll(x => x.fileDuration >= queueThreshold);
            return result;
        }

        private static List<Workflow> getLt6workflows()
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

        // DONE

        private static void FDWS(string source, double chunkDuration)
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
                List<Task> readyTasks = new List<Task>();
                List<Workflow> unrankedWorkflows = new List<Workflow>();
                try
                {
                    unrankedWorkflows = workflows.Where(x => x.rankusCalculated == false).ToList();
                }
                catch
                {
                    Thread.Sleep(100);
                    unrankedWorkflows = workflows.Where(x => x.rankusCalculated == false).ToList();
                }

                // TODO: Check if working and refactor (RankHybd also has this)
                foreach (Workflow workflow in unrankedWorkflows)
                {
                    foreach (Task task in workflow.tasks)
                    {
                        CalculateRanku(task);
                    }
                    workflow.rankusCalculated = true;
                }

                readyTasks = getHighestRankuTaskFromEachWorkflow();
                readyTasks = computeRankr(readyTasks);

                while (readyTasks.Count > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.OrderByDescending(x => x.rankr).ToList().First());
                    readyTasks.Remove(readyTasks.OrderByDescending(x => x.rankr).ToList().First());
                }
            }
        }

        private static List<Task> computeRankr(List<Task> readyTasks)
        {
            foreach (Task task in readyTasks)
            {
                Workflow w = workflows.Where(x => x.tasks.Contains(task)).ToList().First();
                w.tasks.Remove(task);
                int totalTasks = (w.chunkTasks + 2);
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

        private static List<Task> getHighestRankuTaskFromEachWorkflow()
        {
            List<Task> result = new List<Task>();

            List<Workflow> undividedWorkflows = new List<Workflow>();
            try
            {
                undividedWorkflows = workflows.Where(x => x.isDivided == false && x.isDividing == false).ToList();
            }
            catch
            {
                Thread.Sleep(100);
                undividedWorkflows = workflows.Where(x => x.isDivided == false && x.isDividing == false).ToList();
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

            foreach (Workflow workflow in undividedWorkflows)
            {
                result.Add(workflow.tasks.Where(x => x.Type == "Divide").ToList().First());
                //workflow.tasks.Remove(workflow.tasks.Where(x => x.Type == "Divide").ToList().First());
            }

            foreach (Workflow workflow in dividedWorkflows)
            {
                // If there are no more chunk tasks it should finally join
                if (workflow.tasks.Count() > 1)
                {
                    result.Add(workflow.tasks.Where(x => x.Type == "Chunk").ToList().OrderByDescending(x => x.ranku).ToList().First());
                    //workflow.tasks.Remove(workflow.tasks.Where(x => x.Type == "Chunk").ToList().OrderByDescending(x => x.ranku).ToList().First());
                }
                else if (workflow.chunksProcessing == 0 && workflow.tasks.Count() == 1)
                {
                    result.Add(workflow.tasks.Where(x => x.Type == "Join").ToList().First());
                    //workflow.tasks.Remove(workflow.tasks.Where(x => x.Type == "Join").ToList().First());
                }
            }

            return result;
        }

        private static void OWM(string source, double chunkDuration)
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

                readyTasks.AddRange(getHighestRankuTaskFromEachWorkflow());

                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    Task t = readyTasks.First();
                    readyTasks.Remove(readyTasks.First());
                    ProcessTask(t);
                }
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

        private static void RankHybd(string source, double chunkDuration)
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
                readyTasks.AddRange(CheckWorkflows());
                while (readyTasks.Count() > 0 && availableCores > 0)
                {
                    ProcessTask(readyTasks.First());
                    readyTasks.Remove(readyTasks.First());
                }
            }
        }

        private static void ProcessTask(Task task)
        {
            decrementAvailableCores();

            switch (task.Type)
            {
                case "Divide":
                    foreach (Workflow workflow in Program.workflows)
                    {
                        if (workflow.filePath == task.FilePath)
                            workflow.isDividing = true;
                    }
                    new Thread(delegate ()
                    {
                        Utilities.DivideToChunks(task.FilePath, chunkDuration);
                    }).Start();
                    break;

                case "Chunk":
                    foreach (Workflow workflow in Program.workflows)
                    {
                        if (workflow.filePath == Path.GetDirectoryName(task.FilePath) + Path.GetExtension(task.FilePath))
                        {
                            incrementChunksProcessing(workflow);
                        }
                    }

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

        private static List<Task> CheckWorkflows()
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

        public static void incrementChunksProcessing(Workflow workflow)
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

        public static void decrementChunksProcessing(Workflow w)
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

        public static void incrementAvailableCores()
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

        public static void decrementAvailableCores()
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

        public static void incrementChunkTasksDone(Workflow w)
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