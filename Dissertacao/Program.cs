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
        private static double chunkDuration;
        private static int cores = Environment.ProcessorCount; // Setting max available cores by default
        private static string source;
        //private static string destination;
        private static string algorithm;
        public static List<Workflow> queuedWorkFlows = new List<Workflow>();

        private static void Main(string[] args)
        {
            // Parsing args
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Getting arg data into variables
                if (options.Cores > cores)
                {
                    Console.WriteLine("Number of processor cores selected (" + options.Cores + ") exceeds the ones available. Defaulting to max availables cores: " + cores);
                }
                else if (options.Cores > 0)
                {
                    cores = options.Cores;
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
                Console.WriteLine("Cores: " + cores);
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
                    FDWS(source, chunkDuration, cores);
                    break;

                case "RankHybd":
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

        private static void FDWS(string source, double chunkDuration, int cores)
        { 
            // Adding files in source folder to Task list
            AddFilesToTaskList(ReadAllFilesInFolder(source));

            //// Creating file watcher for source folder
            FileSystemWatcher watcher = new FileSystemWatcher("C:\\Users\\t-jom\\Downloads");
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnFolderChanged);

            // CENAS PARA TIMING
            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            double elapsedMs = watch.ElapsedMilliseconds;*/
            
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private static void AddFilesToTaskList(string[] files)
        {
            foreach (string file in files)
            {
                queuedWorkFlows.Add(new Workflow(file, chunkDuration));
                Console.WriteLine("Added file " + queuedWorkFlows.ElementAt(queuedWorkFlows.Count - 1).filePath + " with duration "
                    + queuedWorkFlows.ElementAt(queuedWorkFlows.Count - 1).fileDuration + " and last chunk duration " +
                     queuedWorkFlows.ElementAt(queuedWorkFlows.Count - 1).lastChunkDuration);
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
                AddFilesToTaskList(new string[] { e.FullPath });
            }
        }
    }
}