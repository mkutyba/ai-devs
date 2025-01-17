using System.Text;
using Agent.Application.Abstractions;
using Agent.Application.ArticleProcessor;
using Microsoft.Extensions.VectorData;

namespace Agent.Infrastructure.VectorDatabase;

public class VectorDatabaseService : IVectorDatabaseService
{
    private readonly IAiService _aiService;
    private readonly IVectorStore _vectorStore;
    private readonly IVectorStoreRecordCollection<Guid, RagRecord> _recordCollection;

    public VectorDatabaseService(IAiService aiService, IVectorStore vectorStore)
    {
        _aiService = aiService;
        _vectorStore = vectorStore;
        _recordCollection = _vectorStore.GetCollection<Guid, RagRecord>("learnings");
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
                    Source = VectorDatabaseCollection.Articles,
                    Type = "Text",
                    CreatedAt = DateTimeOffset.UtcNow
                });
        }

        // Save to vector store
        await _recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await _recordCollection.UpsertBatchAsync(records, null, ct).ToListAsync(ct);
    }

    public async Task<IEnumerable<string>> GetMatchingRecordsAsync(string query, string source, int numberOfRecords, CancellationToken ct)
    {
        ReadOnlyMemory<float> queryEmbedding = await _aiService.GetEmbeddingAsync(
            AiModelType.TextEmbedding3Large,
            query,
            ct);

        var options = new VectorSearchOptions
        {
            Top = numberOfRecords,
            Filter = new VectorSearchFilter().EqualTo(nameof(RagRecord.Source), source)
        };

        var searchResults = await _recordCollection.VectorizedSearchAsync(queryEmbedding, options, ct);
        var searchResultsRecords = await searchResults.Results.ToListAsync(ct);
        var allSourcesForResults = searchResultsRecords.Select(x => x.Record.Content);
        return allSourcesForResults;
    }

    public async Task RecreateCollection(CancellationToken ct)
    {
        await _recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await _recordCollection.DeleteCollectionAsync(ct);
        await _recordCollection.CreateCollectionIfNotExistsAsync(ct);
    }

    public async Task SaveFactToVectorDbAsync(string factText, string factContents, string source, CancellationToken ct)
    {
        var embedding = await _aiService.GetEmbeddingAsync(
            AiModelType.TextEmbedding3Large,
            factContents,
            ct);

        var record = new RagRecord
        {
            Id = Guid.NewGuid(),
            Embedding = embedding,
            Content = factText,
            Source = source,
            Type = "Text",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Save to vector store
        await _recordCollection.CreateCollectionIfNotExistsAsync(ct);
        await _recordCollection.UpsertAsync(record, null, ct);
    }
}