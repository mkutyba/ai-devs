using Agent.Application.ArticleProcessor;

namespace Agent.Application.Abstractions;

public interface IVectorDatabaseService
{
    Task SaveArticleToVectorDbAsync(List<ArticleParagraph> paragraphs, CancellationToken ct);
    Task<IEnumerable<string>> GetMatchingRecordsAsync(string search, int numberOfRecords, CancellationToken ct);
}