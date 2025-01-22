using Agent.Application.Abstractions.GraphDatabase;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Agent.Infrastructure.GraphDatabase;

public class GraphDatabaseService : IGraphDatabaseService
{
    private readonly IDriver _driver;
    private readonly ILogger<GraphDatabaseService> _logger;

    public GraphDatabaseService(IDriver driver, ILogger<GraphDatabaseService> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public async Task RunWriteQuery(string query)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            _logger.LogDebug("Running write query: {Query}", query);
            await tx.RunAsync(query);
        });
    }

    public async Task<string?> GetTheShortestPathBetweenPeople(string person1, string person2)
    {
        _logger.LogDebug("Getting the shortest path between {Person1} and {Person2}", person1, person2);
        var (queryResults, _) = await _driver
            .ExecutableQuery("""
                             MATCH p = shortestPath((a:User {name: $person1})-[:CONNECTED_TO*]-(b:User {name: $person2}))
                             RETURN reduce(names = '', n IN nodes(p) | names + CASE WHEN names='' THEN '' ELSE ',' END + n.name) AS pathNames;
                             """)
            .WithParameters(new
            {
                person1,
                person2
            })
            .ExecuteAsync();

        return queryResults.Select(r => r["pathNames"].As<string>()).FirstOrDefault();
    }
}