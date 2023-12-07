using System;
using System.IO;
using System.Net.Http;

namespace BlueAir
{
    /// <summary>
    ///     The default download agent.
    /// </summary>
    public class HttpDownloadAgent : DownloadAgent
    {
        public override string Name => "HTTPDownloadAgent";

        public override async void Run(string fileName, string url, bool fileNameIsFolder, Action<float> progress,
            Action<string> output)
        {
            using (var client = new HttpClient())
            {
                using (var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)))
                {
                    var targetPath = fileNameIsFolder
                        ? Path.Combine(fileName, GetFileNameFromHeader(response))
                        : fileName;

                    using (var fileResponse = await client.GetAsync(url))
                    using (var fileStream = await fileResponse.Content.ReadAsStreamAsync())
                    using (var outputStream = new FileStream(targetPath,
                               File.Exists(targetPath) ? FileMode.Truncate : FileMode.Create, FileAccess.Write,
                               FileShare.ReadWrite))
                    {
                        while (fileStream.Position != fileStream.Length)
                        {
                            var buffer = new byte[8192];
                            var read = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read != buffer.Length && fileStream.Position != fileStream.Length)
                                throw new Exception(
                                    $"Error while reading from source to buffer, expected size {buffer.Length} got {read}.");
                            outputStream.Write(buffer, 0, buffer.Length);
                            progress?.Invoke((float)fileStream.Position / fileStream.Length);
                        }
                    }
                }
            }
        }

        private static string GetFileNameFromHeader(HttpResponseMessage response)
        {
            var contentDisposition = response.Content.Headers.ContentDisposition?.FileName;

            if (!string.IsNullOrWhiteSpace(contentDisposition))
                // If the server provides a Content-Disposition header, use the filename from it
                return contentDisposition;

            // If the header is not provided, extract the filename from the URL
            return Path.GetFileName(response.RequestMessage.RequestUri.LocalPath);
        }

        public override bool Exists()
        {
            return true;
        }
    }
}