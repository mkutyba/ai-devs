using System.Text;
using Agent.Application.Abstractions;
using Agent.Application.ArticleProcessor;
using Microsoft.Extensions.VectorData;

namespace Agent.Infrastructure.VectorDatabase;

public class VectorDatabaseService : IVectorDatabaseService
{
    private readonly IAiService _aiService;
    private readonly IVectorStore _vectorStore;
    private readonly IVectorizedSearch<RagRecord> _vectorSearch;

    public VectorDatabaseService(IAiService aiService, IVectorStore vectorStore, IVectorizedSearch<RagRecord> vectorSearch)
    {
        _aiService = aiService;
        _vectorStore = vectorStore;
        _vectorSearch = vectorSearch;
    }

    public async Task SaveArticleToVectorDbAsync(List<ArticleParagraph> paragraphs, CancellationToken ct)
    {
        var records = new List<RagRecord>();

        foreach (var paragraph in paragraphs)
        {
            var content = new StringBuilder();
            if (!string.IsNullOrEmpty(paragraph.Heading))
            {
                content.AppendLine($"Heading: {paragraph.Heading}");
            }

            content.AppendLine(paragraph.FullText);

            var embedding = await _aiService.GetEmbeddingAsync(
                AiModelType.TextEmbedding3Large,
                content.ToString(),
                ct);

            records.Add(
                new RagRecord
                {
                    Id = Guid.NewGuid(),
                    Embedding = embedding,
                    Content = content.ToString(),
                    Source = "Article",
                    Type = "Text",
                    CreatedAt = DateTimeOffset.UtcNow
                });
        }

        // Save to vector store
        var recordCollection = _vectorStore.GetCollection<Guid, RagRecord>("articles");
        await recordCollection.DeleteCollectionAsync(ct);
        await recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await recordCollection.UpsertBatchAsync(records, null, ct).ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetMatchingRecordsAsync(string search, int numberOfRecords, CancellationToken ct)
    {
        ReadOnlyMemory<float> questionEmbedding = await _aiService.GetEmbeddingAsync(
            AiModelType.TextEmbedding3Large,
            search,
            ct);

        var searchResults = await _vectorSearch.VectorizedSearchAsync(questionEmbedding, new VectorSearchOptions
        {
            Top = numberOfRecords
        }, ct);
        var searchResultsRecords = await searchResults.Results.ToListAsync(ct);
        var allSourcesForResults = searchResultsRecords.Select(x => x.Record.Content);
        return allSourcesForResults;
    }
}