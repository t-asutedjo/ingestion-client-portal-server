using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeTypes;
using Microsoft.Extensions.Configuration;

namespace ServeIngestionClientPortal
{
    public class ServeStaticFile
    {
        private readonly string contentRoot;
        // This key is used by Azure Functions to tell you what is the root of this website.
        private const string ConfigurationKeyApplicationRoot = "AzureWebJobsScriptRoot";
        private const string staticFilesFolder = "www";
        private readonly string defaultPage;
        // The configuration is available for injection.
        // The used settings can be in any config (environment, host.json local.settings.json)
        public ServeStaticFile(IConfiguration configuration)
        {
            this.contentRoot = Path.GetFullPath(Path.Combine(
              configuration.GetValue<string>(ConfigurationKeyApplicationRoot, GetScriptPath()),
              staticFilesFolder));
            this.defaultPage = configuration.GetValue<string>("DEFAULT_PAGE", "index.html");
        }

        [FunctionName("ServeStaticFile")]
        public async Task<IActionResult> Run(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var filePath = GetFilePath(req.Query["file"]);
                if (File.Exists(filePath))
                {
                    var stream = File.OpenRead(filePath);
                    return new FileStreamResult(stream, GetMimeType(filePath))
                    {
                        LastModified = File.GetLastWriteTime(filePath)
                    };
                }
                else
                {
                    return new NotFoundResult();
                }
            }
            catch
            {
                return new BadRequestResult();
            }
        }

        private string GetFilePath(string pathValue)
        {
            var path = pathValue ?? "";
            string fullPath = Path.GetFullPath(Path.Combine(contentRoot, pathValue));
            if (!IsInDirectory(this.contentRoot, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            if (Directory.Exists(fullPath))
            {
                fullPath = Path.Combine(fullPath, defaultPage);
            }
            return fullPath;
        }

        private static bool IsInDirectory(string parentPath, string childPath) => childPath.StartsWith(parentPath);

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypeMap.GetMimeType(fileInfo.Extension);
        }

        private static string GetScriptPath()
    => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
