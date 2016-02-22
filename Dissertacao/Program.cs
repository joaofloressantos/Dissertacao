using CommandLine;
using CommandLine.Text;
using System;
using System.IO;

namespace Dissertacao
{

    class Options
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

        [Option('c', "cores", DefaultValue = 4,
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

    class Program
    {
        static double chunkDuration = 5.0;
        static int cores = Environment.ProcessorCount;

        static void Main(string[] args)
        {
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

            if (options.Cores <= cores)
            {
                cores = options.Cores;
            }
            chunkDuration = options.Duration;

            switch (options.Algorithm)
            {
                case "FDWS":
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

        static int DivideToChunks(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("Invalid file path supplied.");
                return 0;
            }

            FileInfo source = new FileInfo(filePath);
            String fileName = Path.GetFileNameWithoutExtension(source.Name);
            String destinationFolder = Path.GetFileNameWithoutExtension(source.FullName) + "\\";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -i " + source.ToString() + " segment - segment_time " +
                chunkDuration + " -reset_timestamps 1 -c copy " + destinationFolder + fileName + "%03d" + source.Extension;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            return 1;
        }

    }
}
