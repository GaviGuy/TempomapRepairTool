using System;
using ChartHelper.Parsing;
using System.Globalization;

namespace TempomapRepairTool {
    class Program {
        static void Main(string[] args) {
            string sourcePath, input;
            do {
                Console.WriteLine("\nEnter filename of chart to repair:");
                input = Console.ReadLine();
            } while (!FileHelper.TryGetSrtbWithFileName(input, out sourcePath));

            var sourceSRTB = SRTB.DeserializeFromFile(sourcePath);

            int nTooClose = 0, nStacked = 0;
            bool sorted = false;

            for (int i = 0; i < sourceSRTB.ClipInfoCount; i++) {
                var bpms = sourceSRTB.GetClipInfo(i).BpmMarkers;

                float prevTime = bpms[0].ClipTime;

                
                for (int j = 1; j < bpms.Count; j++) {
                    if (bpms[j].ClipTime < prevTime) {
                        bpms.Sort((x, y) => x.ClipTime.CompareTo(y.ClipTime));
                        sorted = true;
                    }
                    prevTime = bpms[j].ClipTime;
                }

                prevTime = bpms[0].ClipTime;

                for (int j = 1; j < bpms.Count; j++) {
                    if (bpms[j].ClipTime == prevTime) {
                        nStacked++;
                        bpms.RemoveAt(j);
                        j--;
                    }
                    else
                        prevTime = bpms[j].ClipTime;
                }

                prevTime = bpms[0].ClipTime;

                for (int j = 1; j < bpms.Count; j++) {
                    if (bpms[j].ClipTime < prevTime + 0.0021f) {
                        nTooClose++;
                        bpms[j].ClipTime = prevTime + 0.0021f;
                    }
                    prevTime = bpms[j].ClipTime;
                }

                var destClipInfo = sourceSRTB.GetClipInfo(i);
                destClipInfo.BpmMarkers = bpms;
                sourceSRTB.SetClipInfo(i, destClipInfo);

            }

            var destInfo = sourceSRTB.GetTrackInfo();
            destInfo.Title += "-FIXED";
            sourceSRTB.SetTrackInfo(destInfo);
            string destPath = sourcePath.Substring(0, sourcePath.Length - 5) + "-FIXED.srtb";
            if(sorted)
                Console.WriteLine("Found out-of-order bpm markers, sorting...");
            if (nStacked > 0)
                Console.WriteLine("Removed " + nStacked + " stacked bpm markers.");
            if (nTooClose > 0)
                Console.WriteLine("Modified " + nTooClose + " bpm markers that were too close to one another.");
            if (!sorted && nTooClose == 0 && nStacked == 0) {
                Console.WriteLine("No detectible errors to fix.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Saving changes to " + destPath);
            Console.ReadLine();
            sourceSRTB.SerializeToFile(destPath);
        }
    }
}
