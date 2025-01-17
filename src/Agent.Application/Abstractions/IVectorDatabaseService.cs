using Agent.Application.ArticleProcessor;

namespace Agent.Application.Abstractions;

public interface IVectorDatabaseService
{
    Task SaveArticleToVectorDbAsync(List<ArticleParagraph> paragraphs, CancellationToken ct);
    Task<IEnumerable<string>> GetMatchingRecordsAsync(string query, string source, int numberOfRecords, CancellationToken ct);
    Task RecreateCollection(CancellationToken ct);
    Task SaveFactToVectorDbAsync(string factText, string factKeywords, CancellationToken ct);
}