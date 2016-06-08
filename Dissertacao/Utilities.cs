using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Dissertacao
{
    public class Utilities
    {
        public static double divideRatio = 0.0108;
        public static double chunkRatio = 1.079;
        public static double joinRatio = 0.005;

        public static double? GetFileDuration(string filePath)
        {
            double? duration;
            MediaFile videoFile = new MediaFile { Filename = filePath };

            using (var engine = new Engine())
            {
                engine.GetMetadata(videoFile);
                duration = videoFile.Metadata.Duration.TotalSeconds;
            }

            return duration;
        }

        public static double CalculateDivideTime(string filePath)
        {
            return (double)GetFileDuration(filePath) * divideRatio;
        }

        public static double CalculateChunkProcessingTime(double chunkDuration)
        {
            return chunkDuration * chunkRatio;
        }

        public static double CalculateJoinTime(string filePath)
        {
            return (double)GetFileDuration(filePath) * joinRatio;
        }

        public static void DivideToChunks(string filePath, double chunkDuration)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("Invalid file path supplied.");
                return;
            }

            FileInfo source = new FileInfo(filePath);
            String fileName = Path.GetFileNameWithoutExtension(source.Name);
            String destinationFolder = source.Directory + "\\" + fileName + "\\";
            Directory.CreateDirectory(destinationFolder);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -i " + source.ToString() + " -f segment -segment_time " +
                chunkDuration + " -reset_timestamps 1 -c copy \"" + destinationFolder + "%03d" + source.Extension + "\"";

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

            foreach (Workflow workflow in Program.workflows)
            {
                if (workflow.filePath == filePath)
                {
                    workflow.isDivided = true;
                    workflow.isDividing = false;
                }
            }

            WriteListFile(destinationFolder);

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

        public static void ProcessChunk(string chunkPath)
        {
            if (!File.Exists(chunkPath))
            {
                Console.Error.WriteLine("Invalid chunk path supplied. Path: " + chunkPath);
                return;
            }

            FileInfo source = new FileInfo(chunkPath);
            String pass1FilePath = source.Directory + "\\" + Path.GetFileNameWithoutExtension(source.Name) + "pass1.mp4";
            String pass2FilePath = source.Directory + "\\" + Path.GetFileNameWithoutExtension(source.Name) + "pass2.mp4";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -y -i " + source.FullName + " -threads 1 -pass 1 "
                + "-s 1280x720 -preset medium -vprofile baseline -c:v libx264 -level 3.0 -vf "
                + "\"format=yuv420p\" -b:v 2000k -maxrate:v 2688k -bufsize:v 2688k -r 25 -g 25 "
                + "-keyint_min 50 -x264opts \"keyint=50:min-keyint=50:no-scenecut\" "
                + "-an -f mp4 -passlogfile " + pass1FilePath + " -movflags faststart NUL";

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

            //string filePath = source.FullName;
            //File.Delete(filePath);
            //File.Move(pass1FilePath, filePath);

            //Console.WriteLine("Acabou pass 1 ");

            process = new System.Diagnostics.Process();
            startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -y -i " + source.FullName + " -threads 1 -pass 2 "
                + "-s 1280x720 -preset medium -vprofile baseline -c:v libx264 -strict 2 -passlogfile " + pass1FilePath + " -level 3.0 -vf "
                + "\"format=yuv420p\" -b:v 2000k -maxrate:v 2688k -bufsize:v 2688k -r 25 -g "
                + "25 -keyint_min 50 -x264opts \"keyint=50:min-keyint=50:no-scenecut\" "
                + "-acodec aac -ac 2 -ar 48000 -ab 128k -f mp4 -movflags faststart " + pass2FilePath;

            //startInfo.RedirectStandardOutput = true;
            //startInfo.UseShellExecute = false;
            //Console.WriteLine(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            //string q = "";
            //q = "";
            //while (!process.HasExited)
            //{
            //    q += process.StandardOutput.ReadToEnd();
            //}

            // Deleting original chunk

            File.Delete(source.FullName);
            File.Delete(pass1FilePath);
            File.Copy(pass2FilePath, source.FullName);
            File.Delete(pass2FilePath);

            Interlocked.Increment(ref Program.availableCores);
            try
            {
                Program.availableCores++;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref Program.availableCores);
            }

            foreach (Workflow w in Program.workflows)
            {
                if (w.filePath == Path.GetDirectoryName(chunkPath) + Path.GetExtension(chunkPath))
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
            }
        }

        public static void ProcessFile(string filePath, string queue)
        {
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("Invalid file path supplied. Path: " + filePath);
                return;
            }

            FileInfo source = new FileInfo(filePath);
            String pass1FilePath = source.Directory + "\\" + Path.GetFileNameWithoutExtension(source.Name) + "pass1.mp4";
            String pass2FilePath = source.Directory + "\\" + Path.GetFileNameWithoutExtension(source.Name) + "pass2.mp4";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -y -i " + source.FullName + " -threads 1 -pass 1 "
                + "-s 1280x720 -preset medium -vprofile baseline -c:v libx264 -level 3.0 -vf "
                + "\"format=yuv420p\" -b:v 2000k -maxrate:v 2688k -bufsize:v 2688k -r 25 -g 25 "
                + "-keyint_min 50 -x264opts \"keyint=50:min-keyint=50:no-scenecut\" "
                + "-an -f mp4 -passlogfile " + pass1FilePath + " -movflags faststart NUL";

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

            //string filePath = source.FullName;
            //File.Delete(filePath);
            //File.Move(pass1FilePath, filePath);

            //Console.WriteLine("Acabou pass 1 ");

            process = new System.Diagnostics.Process();
            startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -y -i " + source.FullName + " -threads 1 -pass 2 "
                + "-s 1280x720 -preset medium -vprofile baseline -c:v libx264 -strict 2 -passlogfile " + pass1FilePath + " -level 3.0 -vf "
                + "\"format=yuv420p\" -b:v 2000k -maxrate:v 2688k -bufsize:v 2688k -r 25 -g "
                + "25 -keyint_min 50 -x264opts \"keyint=50:min-keyint=50:no-scenecut\" "
                + "-acodec aac -ac 2 -ar 48000 -ab 128k -f mp4 -movflags faststart " + pass2FilePath;

            //startInfo.RedirectStandardOutput = true;
            //startInfo.UseShellExecute = false;
            //Console.WriteLine(startInfo.Arguments);

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            //string q = "";
            //q = "";
            //while (!process.HasExited)
            //{
            //    q += process.StandardOutput.ReadToEnd();
            //}

            // Deleting original chunk

            File.Delete(source.FullName);
            File.Copy(pass2FilePath, source.FullName);
            File.Delete(pass2FilePath);

            switch (queue)
            {
                case "VIP":
                    Program.VIPprocessing = false;
                    Program.VIP.Remove(Program.VIP.First());
                    break;

                case "lt6":
                    Program.lt6processing = false;
                    Program.lt6.Remove(Program.lt6.First());
                    break;

                case "moet6":
                    Program.moet6processing = false;
                    Program.moet6.Remove(Program.moet6.First());
                    break;
            }
        }

        public static void RebuildFile(string filePath)
        {
            string dataFolder =
                    Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath));

            if (!File.Exists(dataFolder + "\\list.txt"))
            {
                Console.Error.WriteLine("Invalid data folder supplied.");
                return;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C ffmpeg -f concat -safe 0 -i " + dataFolder + "\\list.txt" + " -c copy " +
                Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "p" + Path.GetExtension(filePath));

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

            Interlocked.Increment(ref Program.availableCores);
            try
            {
                Program.availableCores++;
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref Program.availableCores);
            }

            Program.workflows.Remove(Program.workflows.Find(x => x.filePath.Equals(filePath)));

            //Console.WriteLine("Finished rebuilding");
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
            File.WriteAllLines(destinationFolder + "\\list.txt", files);
        }
    }
}