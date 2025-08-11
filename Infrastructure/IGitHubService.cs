using GitHubRaport.Models;
namespace GitHubRaport.Infrastructure;

internal interface IGitHubService
{
    Task<bool> IsUserExist(string username, CancellationToken ct = default);
    Task<bool> IsRepositoryExist(string username, string repository, CancellationToken ct = default);
    Task<IReadOnlyList<SimpleCommit>> GetAllCommitsAsync(string username, string repository, CancellationToken ct = default);
}
