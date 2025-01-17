using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Agent.Application.ArticleProcessor;

public sealed record RagRecord
{
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public required Guid Id { get; init; }

    [VectorStoreRecordVector(3072)]
    public required ReadOnlyMemory<float> Embedding { get; init; }

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public required string Content { get; init; }

    [VectorStoreRecordData(IsFilterable = true)]
    [TextSearchResultLink]
    public string? Source { get; init; }

    [VectorStoreRecordData]
    [TextSearchResultValue]
    public required string Type { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}