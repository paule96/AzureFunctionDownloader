
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
                        return new FileAndUrl() { Key = keyValue[0], Value = keyValue[1] };
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
            return new FileCallbackResult(new MediaTypeHeaderValue("application/octet-stream"), async (outputStream, _) =>
            {
                using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                {
                    foreach (var kvp in source)
                    {
                        var zipEntry = zipArchive.CreateEntry(kvp.Key);
                        using (var zipStream = zipEntry.Open())
                        using (var stream = await Client.GetStreamAsync(kvp.Value))
                        {
                            await stream.CopyToAsync(zipStream);
                            Console.WriteLine($"Add File {kvp.Key}");
                        }                            
                    }
                }

            })
            {
                FileDownloadName = "test.zip"
            };
        }
    }
    public class FileAndUrl
    {
        public string Key;
        public string Value;
    }


    public class FileCallbackResult : FileResult
    {
        private Func<Stream, ActionContext, Task> _callback;

        public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType?.ToString())
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null);
                return result._callback(context.HttpContext.Response.Body, context);
            }
        }
    }
    public class WriteOnlyStreamWrapper : Stream
    {
        private readonly Stream _stream;
        private long _position;

        public WriteOnlyStreamWrapper(Stream stream)
        {
            _stream = stream;
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override void Write(byte[] buffer, int offset, int count)
        {
            _position += count;
            _stream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            _position += count;
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

        public override void WriteByte(byte value)
        {
            _position += 1;
            _stream.WriteByte(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _position += count;
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
