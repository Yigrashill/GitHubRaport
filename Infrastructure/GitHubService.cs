using GitHubRaport.Models;
using GitHubRaport.Errors;
using System.Net;
using System.Text.Json;

namespace GitHubRaport.Infrastructure;

public partial class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsUserExist(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new NotFoundUserException(username);

        using var resp = await _httpClient.GetAsync($"https://api.github.com/users/{username}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            throw new NotFoundUserException(username);

        Console.WriteLine($"User: {username} exist.");
        return true;
    }

    public async Task<bool> IsRepositoryExist(string username, string repository, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(repository))
            throw new NotFoundRepositoryException(repository);


        using var resp = await _httpClient.GetAsync($"https://api.github.com/repos/{username}/{repository}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            throw new NotFoundRepositoryException(repository);


        Console.WriteLine($"Repository: {repository} exist.");
        return true;
    }

    public async Task<IReadOnlyList<SimpleCommit>> GetAllCommitsAsync(string username, string repository, CancellationToken ct = default)
    {
        var commits = new List<SimpleCommit>();
        var page = 1;
        var baseUrl = $"https://api.github.com/repos/{username}/{repository}/commits";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

       
        Console.WriteLine("Loading data...");
        Task.Delay(1000).Wait();

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var url = $"{baseUrl}?per_page=100&page={page}";
            using var resp = await _httpClient.GetAsync(url, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                throw new NotFoundUserException(username);

            var json = await resp.Content.ReadAsStringAsync(ct);
            var dtos = JsonSerializer.Deserialize<List<GithubCommitDto>>(json, options) ?? new();

            Console.WriteLine("Loading data...");
            foreach (var dto in dtos)
            {
                var sha = dto.Sha ?? string.Empty;
                var message = (dto.Committ.Message ?? string.Empty).Split('\n')[0];
                var committer = dto.Committ.Author.Name ?? string.Empty;

                commits.Add(new SimpleCommit(sha, message, committer));
            }
            var hasNext = resp.Headers.TryGetValues("Link", out var links) &&
                  links.Any(l => l.Contains("rel=\"next\""));
            if (!hasNext) break;
            page++;

        }

        return commits;
    }
}