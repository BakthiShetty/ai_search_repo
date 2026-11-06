using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Api.Services;

namespace SemanticSearch.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(SearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("query is required");

        var results = await searchService.SearchAsync(query.Trim(), top, ct);
        return Ok(results);
    }
}

public record SearchResultDto(
    Guid Id,
    string Filename,
    string? FolderPath,
    string? Author,
    DateTime? CreatedAt,
    string NasPath,
    double Score);
