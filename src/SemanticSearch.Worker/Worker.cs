using System.Diagnostics;
using SemanticSearch.Worker.Services;

namespace SemanticSearch.Worker;

public class Worker(
    IndexingPipeline pipeline,
    IConfiguration config,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var configured = config["IndexingFolder"];
        var folder = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "sample-files")
            : configured;

        folder = Path.GetFullPath(folder);

        if (!Directory.Exists(folder))
        {
            logger.LogError("Indexing folder not found: {Folder}", folder);
            return;
        }

        logger.LogInformation("Scanning {Folder}", folder);

        var extensions = new[] { ".pdf", ".docx", ".xlsx" };
        var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant())
                     && !Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal))
            .ToList();

        logger.LogInformation("Found {Count} files to index", files.Count);

        var indexed = 0;
        var skipped = 0;
        var noContent = 0;
        var failed = 0;
        var sw = Stopwatch.StartNew();

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;

            var outcome = IndexOutcome.Failed;
            try
            {
                outcome = await pipeline.IndexAsync(file, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Safety net: IndexAsync already reports failures via IndexOutcome, but a
                // single bad file must never abort the rest of the batch, so catch here too.
                logger.LogError(ex, "Unexpected error indexing {File}", file);
            }

            switch (outcome)
            {
                case IndexOutcome.Indexed: indexed++; break;
                case IndexOutcome.AlreadyIndexed: skipped++; break;
                case IndexOutcome.NoContentExtracted: noContent++; break;
                case IndexOutcome.Failed: failed++; break;
            }
        }

        logger.LogInformation(
            "Indexing complete in {Elapsed}: {Indexed} indexed, {Skipped} already indexed, " +
            "{NoContent} without extractable content, {Failed} failed (of {Total} files)",
            sw.Elapsed, indexed, skipped, noContent, failed, files.Count);
    }
}
