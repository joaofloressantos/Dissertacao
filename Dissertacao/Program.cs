using CommandLine;
using CommandLine.Text;
using System;
using System.IO;

namespace Dissertacao
{
    internal class Options
    {
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

        private static void Main(string[] args)
        {
            //var options = new Options();
            //if (CommandLine.Parser.Default.ParseArguments(args, options))
            //{
            //    // Values are available here
            //    Console.WriteLine("Selected Options");
            //    Console.WriteLine("Source: " + options.Source);
            //    Console.WriteLine("Destination: " + options.Destination);
            //    Console.WriteLine("Duration: " + options.Duration);
            //    Console.WriteLine("Cores: " + options.Cores);
            //    Console.WriteLine("Algorithm: " + options.Algorithm);
            //}

            //if (options.Cores <= cores)
            //{
            //    cores = options.Cores;
            //}

            //chunkDuration = options.Duration;

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

            string filePath = "C:\\Users\\t-jom\\Downloads\\AP_V6\\V6.1.mp4";

            Utilities.DivideToChunks(filePath, chunkDuration);

            Console.WriteLine("Finished Dividing");

            Utilities.RebuildFile("C:\\Users\\t-jom\\Downloads\\AP_V6\\V6.1", "C:\\Users\\t-jom\\Downloads\\AP_V6\\V6.1\\final.mp4");

            Console.WriteLine("Finished Rebuilding");

            Console.ReadKey();

        }

        
    }
}