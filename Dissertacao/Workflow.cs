﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Dissertacao
{
    internal class Workflow
    {
        public string filePath;
        public double fileDuration;
        public List<Task> tasks;
        public int chunkTasks;
        public int chunkTasksDone;
        public int nChunks;
        public double lastChunkDuration;
        public bool isDivided { get; set; }
        public DateTime beginTime { get; set; }
        public DateTime endTime { get; set; }
        public bool rankusCalculated { get; set; }

        public Workflow(string filePath, double chunkDuration)
        {
            beginTime = DateTime.Now;
            this.filePath = filePath;
            isDivided = false;
            rankusCalculated = false;
            chunkTasks = 0;
            chunkTasksDone = 0;

            fileDuration = (double)Utilities.GetFileDuration(filePath);

            tasks = new List<Task>();

            tasks.Add(new Task("Divide", filePath, Utilities.CalculateDivideTime(filePath)));

            nChunks = (int)Math.Floor(fileDuration / chunkDuration);

            lastChunkDuration = fileDuration % chunkDuration;

            int i = 0;

            for (; i < nChunks; i++)
            {
                string chunkPath =
                    Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath),
                    i.ToString("D3") + Path.GetExtension(filePath));

                tasks.Add(new Task("Chunk", chunkPath, Utilities.CalculateChunkProcessingTime(chunkDuration)));
                chunkTasks++;
            }

            if (lastChunkDuration > 0)
            {
                //i += 1;
                string chunkPath =
                    Path.Combine(Path.GetDirectoryName(filePath),
                    Path.GetFileNameWithoutExtension(filePath),
                    i.ToString("D3") + Path.GetExtension(filePath));

                tasks.Add(new Task("Chunk", chunkPath, Utilities.CalculateChunkProcessingTime(lastChunkDuration)));
                chunkTasks++;
            }

            tasks.Add(new Task("Join", filePath, Utilities.CalculateJoinTime(filePath)));
        }
    }
}