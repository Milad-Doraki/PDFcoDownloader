﻿using System;

namespace PDFco.Downloader.Contract.Exceptions
{
    public class DownloadCheckNotSuccessfulException : Exception
    {
        public DownloadCheckNotSuccessfulException(string message, Exception ex, DownloadCheckResult downloadCheckResult) : base(message, ex)
        {
            this.DownloadCheckResult = downloadCheckResult;
        }

        public DownloadCheckResult DownloadCheckResult { get; private set; }
    }
}
