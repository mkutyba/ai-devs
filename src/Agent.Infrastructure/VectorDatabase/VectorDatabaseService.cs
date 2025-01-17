using System.Text;
using Agent.Application.Abstractions;
using Agent.Application.ArticleProcessor;
using Microsoft.Extensions.VectorData;

namespace Agent.Infrastructure.VectorDatabase;

public class VectorDatabaseService : IVectorDatabaseService
{
    private readonly IAiService _aiService;
    private readonly IVectorStore _vectorStore;

    public VectorDatabaseService(IAiService aiService, IVectorStore vectorStore)
    {
        _aiService = aiService;
        _vectorStore = vectorStore;
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
        var recordCollection = _vectorStore.GetCollection<Guid, RagRecord>(VectorDatabaseCollection.Articles);
        await recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await recordCollection.UpsertBatchAsync(records, null, ct).ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetMatchingRecordsAsync(string query, string source, int numberOfRecords, CancellationToken ct)
    {
        ReadOnlyMemory<float> queryEmbedding = await _aiService.GetEmbeddingAsync(
            AiModelType.TextEmbedding3Large,
            query,
            ct);

        var options = new VectorSearchOptions
        {
            Top = numberOfRecords
        };

        var recordCollection = _vectorStore.GetCollection<Guid, RagRecord>(source);
        var searchResults = await recordCollection.VectorizedSearchAsync(queryEmbedding, options, ct);
        var searchResultsRecords = await searchResults.Results.ToListAsync(ct);
        var allSourcesForResults = searchResultsRecords.Select(x => x.Record.Content);
        return allSourcesForResults;
    }

    public async Task RecreateCollection(string collectionName, CancellationToken ct)
    {
        var recordCollection = _vectorStore.GetCollection<Guid, RagRecord>(collectionName);
        await recordCollection.DeleteCollectionAsync(ct);
        await recordCollection.CreateCollectionIfNotExistsAsync(ct);
    }

    public async Task SaveFactToVectorDbAsync(string factText, string factKeywords, CancellationToken ct)
    {
        var embedding = await _aiService.GetEmbeddingAsync(
            AiModelType.TextEmbedding3Large,
            factKeywords,
            ct);

        var record = new RagRecord
        {
            Id = Guid.NewGuid(),
            Embedding = embedding,
            Content = factText,
            Source = "Fact",
            Type = "Text",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Save to vector store
        var recordCollection = _vectorStore.GetCollection<Guid, RagRecord>(VectorDatabaseCollection.Facts);
        await recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await recordCollection.UpsertAsync(record, null, ct);
    }
}