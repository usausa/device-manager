using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StorageController : ControllerBase
{
    private readonly string _rootPath;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IConfiguration configuration, ILogger<StorageController> logger)
    {
        _logger = logger;
        var configuredPath = configuration["Storage:RootPath"];
        if (!string.IsNullOrEmpty(configuredPath))
        {
            _rootPath = Path.GetFullPath(configuredPath);
        }
        else
        {
            _rootPath = Path.Combine(AppContext.BaseDirectory, "storage");
        }

        Directory.CreateDirectory(_rootPath);
    }

    [HttpGet("{**path}")]
    public IActionResult Get(string path)
    {
        if (string.IsNullOrEmpty(path))
            return BadRequest("Path is required.");

        var fullPath = GetSafePath(path);
        if (fullPath is null)
            return BadRequest("Invalid path.");

        // If path ends with "/" or is a directory, list contents
        if (path.EndsWith('/') || Directory.Exists(fullPath))
        {
            if (!Directory.Exists(fullPath))
                return NotFound();

            var entries = new List<object>();
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                entries.Add(new { Name = Path.GetFileName(dir), Type = "directory" });
            }
            foreach (var file in Directory.GetFiles(fullPath))
            {
                var info = new FileInfo(file);
                entries.Add(new { Name = info.Name, Type = "file", Size = info.Length, LastModified = info.LastWriteTimeUtc });
            }

            return Ok(entries);
        }

        // Return file
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = GetContentType(fullPath);
        return File(stream, contentType, Path.GetFileName(fullPath));
    }

    [HttpPost("{**path}")]
    public async Task<IActionResult> Upload(string path, IFormFile file)
    {
        if (string.IsNullOrEmpty(path))
            return BadRequest("Path is required.");

        if (file is null || file.Length == 0)
            return BadRequest("File is required.");

        var fullPath = GetSafePath(path);
        if (fullPath is null)
            return BadRequest("Invalid path.");

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(stream);

        _logger.LogInformation("File uploaded: {Path}", path);
        return CreatedAtAction(nameof(Get), new { path }, new { Path = path, Size = file.Length });
    }

    [HttpDelete("{**path}")]
    public IActionResult Delete(string path)
    {
        if (string.IsNullOrEmpty(path))
            return BadRequest("Path is required.");

        var fullPath = GetSafePath(path);
        if (fullPath is null)
            return BadRequest("Invalid path.");

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        System.IO.File.Delete(fullPath);
        _logger.LogInformation("File deleted: {Path}", path);
        return NoContent();
    }

    private string? GetSafePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

        // Path traversal protection: ensure the resolved path is within the root
        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }

    private static string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
    }
}
