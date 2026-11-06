using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace SemanticSearch.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IConfiguration config) : ControllerBase
{
    private string ConnectionString => config.GetConnectionString("Postgres")!;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFile(
        Guid id,
        [FromQuery] bool download = false,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        var nasPath = await conn.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                "SELECT nas_path FROM file_metadata WHERE id = @id",
                new { id },
                cancellationToken: ct));

        if (nasPath is null)
            return NotFound("File record not found.");

        if (!System.IO.File.Exists(nasPath))
            return NotFound($"File not found on disk: {Path.GetFileName(nasPath)}");

        var ext = Path.GetExtension(nasPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf"  => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls"  => "application/vnd.ms-excel",
            ".doc"  => "application/msword",
            ".txt"  => "text/plain; charset=utf-8",
            _       => "application/octet-stream",
        };

        var disposition = download ? "attachment" : "inline";
        Response.Headers["Content-Disposition"] =
            $"{disposition}; filename=\"{Path.GetFileName(nasPath)}\"";

        var stream = System.IO.File.OpenRead(nasPath);
        return File(stream, contentType, enableRangeProcessing: true);
    }
}
