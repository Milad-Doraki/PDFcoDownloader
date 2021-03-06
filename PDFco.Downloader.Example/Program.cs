﻿using System;
using System.Collections.Generic;
using System.Threading;
using PDFco.Downloader.Contract;
using PDFco.Downloader.Contract.Events;
using PDFco.Downloader.Download;
using PDFco.Downloader.DownloadBuilder;
using PDFco.Downloader.Observer;
using PDFco.Downloader.Utils;

namespace PDFco.Downloader.Example
{
    public class Program
    {
        static bool finished = false;

        public static void Main()
        {
            bool useDownloadSpeedThrottling = false;

            // Please insert an URL of a large file here, otherwise the download will be finished too quickly to really demonstrate the functionality.
            var url = new Uri("http://yourdomain.com/yourfile.zip");
            var file = new System.IO.FileInfo("yourfile.zip");
            var requestBuilder = new SimpleWebRequestBuilder();
            var dlChecker = new DownloadChecker();
            var httpDlBuilder = new SimpleDownloadBuilder(requestBuilder, dlChecker);
            var timeForHeartbeat = 3000;
            var timeToRetry = 5000;
            var maxRetries = 5;
            var resumingDlBuilder = new ResumingDownloadBuilder(timeForHeartbeat, timeToRetry, maxRetries, httpDlBuilder);
            List<DownloadRange> alreadyDownloadedRanges = null;
            var bufferSize = 4096;
            var numberOfParts = 4;
            var maxRetryDownloadParts = 2;
            var download = new MultiPartDownload(url, bufferSize, numberOfParts, resumingDlBuilder, requestBuilder, dlChecker, alreadyDownloadedRanges, maxRetryDownloadParts);
            var speedMonitor = new DownloadSpeedMonitor(maxSampleCount: 128);
            speedMonitor.Attach(download);
            var progressMonitor = new DownloadProgressMonitor();
            progressMonitor.Attach(download);

            if (useDownloadSpeedThrottling)
            {
                var downloadThrottling = new DownloadThrottling(maxBytesPerSecond: 200 * 1024, maxSampleCount: 128);
                downloadThrottling.Attach(download);
            }

            var dlSaver = new DownloadToFileSaver(file);
            dlSaver.Attach(download);
            download.DownloadCompleted += OnCompleted;
            download.DownloadCancelled += OnDownloadCancelled;
            download.DownloadStopped += OnDownloadStopped;
            download.Start();

            while (!finished)
            {
                Thread.Sleep(1000);

                var alreadyDownloadedSizeInBytes = progressMonitor.GetCurrentProgressInBytes(download);
                var totalDownloadSizeInBytes = progressMonitor.GetTotalFilesizeInBytes(download);
                var currentSpeedInBytesPerSecond = speedMonitor.GetCurrentBytesPerSecond();

                var currentProgressInPercent = progressMonitor.GetCurrentProgressPercentage(download) * 100;
                var alreadyDownloadedSizeInKiB = (alreadyDownloadedSizeInBytes / 1024);
                var totalDownloadSizeInKiB = (totalDownloadSizeInBytes / 1024);
                var currentSpeedInKiBPerSecond = (currentSpeedInBytesPerSecond / 1024);
                var remainingTimeInSeconds = currentSpeedInBytesPerSecond == 0 ? 0 : (totalDownloadSizeInBytes - alreadyDownloadedSizeInBytes) / currentSpeedInBytesPerSecond;

                Console.WriteLine(
                    "Progress: " + currentProgressInPercent + "% " + "(" + alreadyDownloadedSizeInKiB + " of " + totalDownloadSizeInKiB + " KiB)" +
                    "   Speed: " + currentSpeedInKiBPerSecond + " KiB/sec." +
                    "   Remaining time: " + remainingTimeInSeconds + " sec.");
            }
        }
         
        static void OnCompleted(DownloadEventArgs args)
        {
            // this is an important thing to do after a download isn't used anymore, otherwise you will run into a memory leak.
            args.Download.DetachAllHandlers();
            Console.WriteLine("Download has finished!");
            finished = true;
        }

        static void OnDownloadCancelled(DownloadCancelledEventArgs args)
        { 
            args.Download.DetachAllHandlers();
            Console.WriteLine("Download has cancelled!");
            finished = true;
        }

        private static void OnDownloadStopped(DownloadEventArgs args)
        {
            args.Download.DetachAllHandlers();
            Console.WriteLine("Download has stopped!");
            finished = true;
        }
    }
}