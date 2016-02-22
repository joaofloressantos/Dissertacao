using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public float Duration { get; set; }

        [Option('c', "cores", DefaultValue = 4,
          HelpText = "Number of cores used for processing")]
        public int Cores { get; set; }

        [Option('a', "algorithm", DefaultValue = "FDWS",
          HelpText = "Desired algorithm for workflow scheduling")]
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

            switch (options.Algorithm)
            {
                case "FDWS":
                    break;
                case ""
            }
        }
    }
}
