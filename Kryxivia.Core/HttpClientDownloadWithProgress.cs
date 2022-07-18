using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public class HttpClientDownloadWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;

        private HttpClient _httpClient;

        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern long StrFormatByteSize(
        long fileSize,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
        int bufferSize);

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage, string speed);

        public event ProgressChangedHandler ProgressChanged;


        public delegate void SpeedChangeHandler (string speed);
        public event SpeedChangeHandler SpeedChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;
            long _oldTotalBytesRead = 0L;
            Stopwatch _sw = new Stopwatch();
            _sw.Start();
            using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, "");
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalBytesRead += bytesRead;

                    readCount += 1;


                    if(_sw.ElapsedMilliseconds >= 1000)
                    {
                        var elapsedTime = _sw.Elapsed;
                        long bytesChanged = totalBytesRead - _oldTotalBytesRead;
                        _oldTotalBytesRead = totalBytesRead;
                        SpeedChanged($" - {System.Math.Round(((bytesChanged / elapsedTime.TotalSeconds) / 1024) / 1024, 2)} MB/s");
                        _sw.Reset();
                        _sw.Start();
                    }
                    if (readCount % 100 == 0)
                    {
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, "");
                    }
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead, string speed)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage, speed);
        }

        public string StrFormatByteSize(long filesize)
        {
            StringBuilder sb = new StringBuilder(11);
            StrFormatByteSize(filesize, sb, sb.Capacity);
            return sb.ToString();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
