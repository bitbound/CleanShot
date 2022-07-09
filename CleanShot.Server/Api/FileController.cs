using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using FileIO = System.IO.File;

namespace CleanShot.Server.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private static readonly SemaphoreSlim _fileLock = new(1,1);
        private readonly string _appData;
        private readonly string _listPath;

        public FileController(IWebHostEnvironment hostEnv)
        {
            _appData = Path.Join(hostEnv.ContentRootPath, "App_Data");
            _listPath = Path.Combine(_appData, "FileSharing_Entries", "FileSharing_List.json");
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }


            try
            {
                await _fileLock.WaitAsync();

                var downloadDir = GetDownloadDir();

                var list = await GetList();

                if (!list.TryGetValue(id, out var savedFileName))
                {
                    return StatusCode(400, "Invalid download ID.");
                }



                var filePath = Path.Combine(downloadDir, savedFileName);

                if (!FileIO.Exists(filePath))
                {
                    list.Remove(id);
                    await FileIO.WriteAllTextAsync(_listPath, JsonSerializer.Serialize(list));
                    return StatusCode(400, "File not found.");
                }

                Response.ContentType = "application/octet-stream";
                Response.Headers.Add("content-disposition", @"attachment;filename=""" + savedFileName + @"""");
                Response.Headers.Add("cache-control", "no-cache");

                foreach (var entry in list)
                {
                    var fi = new FileInfo(Path.Combine(downloadDir, entry.Value));
                    if (DateTime.Now - fi.CreationTime > TimeSpan.FromDays(14) && !fi.FullName.Contains("FileSharing_List.json"))
                    {
                        fi.Delete();
                        list.Remove(entry.Key);
                    }
                }

                await FileIO.WriteAllTextAsync(_listPath, JsonSerializer.Serialize(list));

                return File(FileIO.OpenRead(filePath), "application/octet-stream", Path.GetFileName(filePath));

            }
            finally
            {
                _fileLock.Release();
            }
         
        }

        [IgnoreAntiforgeryToken]
        [RequestSizeLimit(100_000_000)]
        [HttpPost]
        public async Task<IActionResult> Post(IFormFile file)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            if (file == null)
            {
                return BadRequest();
            }

            try
            {
                await _fileLock.WaitAsync();


                var downloadDir = GetDownloadDir();
                var list = await GetList();
                var fileName = file.FileName;

                while (FileIO.Exists(Path.Combine(downloadDir, fileName)))
                {
                    fileName = Path.GetFileNameWithoutExtension(file.FileName) + "-" + Path.GetRandomFileName().Replace(".", "") + Path.GetExtension(file.FileName);
                }
                using (var fs = new FileStream(Path.Combine(downloadDir, fileName), FileMode.Create))
                {
                    file.CopyTo(fs);
                }
                var id = Path.GetRandomFileName().Replace(".", "");
                list.Add(id, fileName);
                await FileIO.WriteAllTextAsync(_listPath, JsonSerializer.Serialize(list));

                return Content($"{Request.Scheme}://{Request.Host}/api/file/{id}");
            }
            finally
            {
                _fileLock.Release();
            }

        }

        private string GetDownloadDir()
        {
            var downloadDir = Directory.CreateDirectory(Path.Combine(_appData, "FileSharing_Entries"));

            if (!FileIO.Exists(Path.Combine(downloadDir.FullName, "FileSharing_List.json")))
            {
                FileIO.Create(_listPath).Close();
            }
            return downloadDir.FullName;
        }

        private async Task<Dictionary<string, string>> GetList()
        {
            try
            {
                var strList = await FileIO.ReadAllTextAsync(_listPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(strList) ?? new();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
