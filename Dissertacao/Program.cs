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

        [Option('d', "destination", DefaultValue = "ProcessedVideos",
          HelpText = "Destination video file folder name/path")]
        public string Destination { get; set; }

        [Option('t', "duration", DefaultValue = 5,
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
        private static double chunkDuration = 5.0;
        private static int cores = Environment.ProcessorCount; // Setting max available cores by default
        private static string source;
        private static string destination;
        public List<Workflow> queuedWorkFlows;

        private static void Main(string[] args)
        {
            // Parsing args
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                Console.WriteLine("Selected Options");
                Console.WriteLine("Source: " + options.Source);
                Console.WriteLine("Destination: " + options.Destination);
                Console.WriteLine("Duration: " + options.Duration);
                Console.WriteLine("Cores: " + options.Cores);
                Console.WriteLine("Algorithm: " + options.Algorithm);
            }
            else
            {
                return;
            }

            // Getting arg data into variables
            if (options.Cores > cores)
            {
                Console.WriteLine("Number of processor cores exceeds the ones available. Defaulting to max availables cores: " + cores);
            }
            else if (options.Cores > 0)
            {
                cores = options.Cores;
            }

            chunkDuration = options.Duration;
            source = options.Source;
            destination = options.Destination;

            // Creating watcher for selected folder
            FileSystemWatcher watcher = new FileSystemWatcher("C:\\Users\\t-jom\\Downloads");
            watcher.EnableRaisingEvents = true;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnChanged);

            // Selecting algorithm
            //switch (options.Algorithm)
            //{
            //    case "FDWS":
            //        break;

            //    case "RankHybd":
            //        break;

            //    case "OWM":
            //        break;

            //    case "MW-DBS":
            //        break;

            //    default:
            //        Console.WriteLine("Chosen algorithm not available. Exiting...");
            //        return;
            //}

            // Testing file reading and task creation

            AddFilesToTaskList(ReadAllFilesInFolder(source));

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
                Console.WriteLine(file);
                
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

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileInfo f = new FileInfo(e.FullPath);

            if (f.Extension.Equals(".mp4") || f.Extension.Equals(".mpeg") || f.Extension.Equals(".mpg") || f.Extension.Equals(".wmv") || f.Extension.Equals(".mkv"))
            {
                AddFilesToTaskList(new string[] { e.FullPath });
            }
        }
    }
}