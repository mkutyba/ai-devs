namespace Agent.Application.Abstractions.GraphDatabase;

public interface IGraphDatabaseService
{
    Task RunWriteQuery(string query);
    Task<string?> GetTheShortestPathBetweenPeople(string person1, string person2);
}