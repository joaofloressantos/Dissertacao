using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dissertacao
{
    class Utilities
    {

        public static int DivideToChunks(string filePath, double chunkDuration)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("Invalid file path supplied.");
                return 0;
            }

            FileInfo source = new FileInfo(filePath);
            String fileName = Path.GetFileNameWithoutExtension(source.Name);
            String destinationFolder = source.Directory + "\\" + fileName + "\\";
            System.IO.Directory.CreateDirectory(destinationFolder);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -i " + source.ToString() + " -f segment -segment_time " +
                chunkDuration + " -reset_timestamps 1 -c copy " + destinationFolder + "%03d" + source.Extension;

            //startInfo.RedirectStandardOutput = true;
            //startInfo.UseShellExecute = false;
            //Console.WriteLine(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            //string q = "";
            //while (!process.HasExited)
            //{
            //    q += process.StandardOutput.ReadToEnd();
            //}

            WriteListFile(destinationFolder);

            return 1;
        }

        public static int ProcessChunk(string chunkPath)
        {

        }


        public static int RebuildFile(string dataFolder, string finalFilePath)
        {
            if (!File.Exists(dataFolder + "\\list.txt"))
            {
                Console.Error.WriteLine("Invalid data folder supplied.");
                return 0;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -f concat -i " + dataFolder + "\\list.txt" + " -c copy " + finalFilePath;

            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Console.WriteLine(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.Start();
            //process.WaitForExit();

            string q = "";
            while (!process.HasExited)
            {
                q += process.StandardOutput.ReadToEnd();
            }

            return 1;
        }

        private static void WriteListFile(string destinationFolder)
        {
            DirectoryInfo d = new DirectoryInfo(destinationFolder);
            FileInfo[] Files = d.GetFiles("*.*");
            string[] files = new string[Files.Length];
            int i = 0;
            foreach (FileInfo file in Files)
            {
                files[i] = "file '" + file.FullName + "'";
                i++;
            }
            System.IO.File.WriteAllLines(destinationFolder + "\\list.txt", files);
        }

    }
}
