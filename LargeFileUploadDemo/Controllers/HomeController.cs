using LargeFileUploadDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace LargeFileUploadDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var data = Request.Form.Files["data"];
            string lastModified = Request.Form["lastModified"].ToString();
            var total = Request.Form["total"];
            var fileName = Request.Form["filename"];
            var index = Request.Form["index"];

            // 定义一次上传操作的标识, GUID string
            var fileid = Request.Form["fileid"].ToString();
            if (string.IsNullOrEmpty(fileid))
            {
                fileid = Guid.NewGuid().ToString("N");
            }

            string temporary = Path.Combine(GetTempDir(), fileid);
            try
            {
                if (!Directory.Exists(temporary))
                {
                    Directory.CreateDirectory(temporary);
                }
                string filePath = Path.Combine(temporary, index.ToString());
                if (!Convert.IsDBNull(data))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            data.CopyTo(fs);
                        }
                        _logger.LogInformation($"Uploading, {index}/{total}");
                    });
                }
                bool mergeOk = false;
                if(total == index)
                {
                    _logger.LogInformation($"Merging");
                    mergeOk = await FileMerge(fileid, fileName);
                }

                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("fileid", fileid);
                result.Add("number", index);
                result.Add("mergeOk", mergeOk);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed");
                Directory.Delete(temporary);
                throw ex;
            }
        }

        private async Task<bool> FileMerge(string fileId, string fileName)
        {
            bool ok = false;
            try
            {
                var temporary = Path.Combine(GetTempDir(), fileId);
                var filenameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var fileExt = Path.GetExtension(fileName);
                var files = Directory.GetFiles(temporary);
                var finalFolder = GetUploadDir();
                if (!Directory.Exists(finalFolder))
                {
                    Directory.CreateDirectory(finalFolder);
                }
                var finalPath = Path.Combine(finalFolder, $"{filenameWithoutExt}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{fileExt}");
                FileStream fs = new FileStream(finalPath, FileMode.Create);
                for (int i = 0; i < files.Length; i++)
                {
                    var tempPath = Path.Combine(temporary, i.ToString());
                    var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                    bytes = null;
                    System.IO.File.Delete(tempPath);
                    _logger.LogInformation($"Merging, {i}/{files.Length}");
                }
                fs.Close();
                Directory.Delete(temporary);
                fs.Dispose();
                ok = true;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                
            }
            return ok;
        }
    
        private string GetTempDir()
        {
            return System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Upload", "temp");
        }

        private string GetUploadDir()
        {
            return System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Upload");
        }
    }
}
