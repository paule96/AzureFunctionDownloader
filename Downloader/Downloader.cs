
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Internal;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Linq;
using Downloader.Model;

namespace Downloader
{
    public static class Downloader
    {
        private static HttpClient Client { get; } = new HttpClient();
        [FunctionName("Downloader")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            List<FileAndUrl> filenamesAndUrls;
            if (req.Method == HttpMethod.Get)
            {
                filenamesAndUrls = req.RequestUri.Query
                    .Remove(0, 1) // remove '?' at the start of the query
                    .Split('&') // all comands are seperated by '&'
                    .Select(s =>
                    {
                        var keyValue = s.Split('=');
                        return new FileAndUrl() { FileName = keyValue[0], Url = keyValue[1] };
                    }).ToList();
            }
            else
            {
                filenamesAndUrls = await req.Content.ReadAsAsync<List<FileAndUrl>>();
            }
            return CreateResult(filenamesAndUrls);
        }
        private static IActionResult CreateResult(List<FileAndUrl> source)
        {
            // see sample here: https://stackoverflow.com/questions/18852389/generate-a-zip-file-from-azure-blob-storage-files
            // .net core way is maybe this: https://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
            return new FileCallbackResult(new MediaTypeHeaderValue("application/octet-stream"), (outputStream, _) => ZipData(outputStream, source))
            {
                FileDownloadName = "test.zip"
            };
        }

        private static async Task ZipData(Stream outputStream, IEnumerable<FileAndUrl> sourceList)
        {
            using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
            {
                foreach (var kvp in sourceList)
                {
                    var zipEntry = zipArchive.CreateEntry(kvp.FileName);
                    using (var zipStream = zipEntry.Open())
                    using (var stream = await Client.GetStreamAsync(kvp.Url))
                    {
                        await stream.CopyToAsync(zipStream);
                        Console.WriteLine($"Add File {kvp.FileName}");
                    }
                }
            }
        }
    }
}
