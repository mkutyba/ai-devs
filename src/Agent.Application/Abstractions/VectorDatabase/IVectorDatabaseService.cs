using Agent.Application.ArticleProcessor;

namespace Agent.Application.Abstractions.VectorDatabase;

public interface IVectorDatabaseService
{
    Task SaveArticleToVectorDbAsync(List<ArticleParagraph> paragraphs, CancellationToken ct);
    Task<IEnumerable<string>> GetMatchingRecordsAsync(string query, string source, int numberOfRecords, CancellationToken ct);
    Task RecreateCollection(CancellationToken ct);
    Task SaveFactToVectorDbAsync(string factText, string factContents, string source, CancellationToken ct);
}