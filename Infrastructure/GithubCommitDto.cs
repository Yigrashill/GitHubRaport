namespace GitHubRaport.Infrastructure;

using System.Text.Json.Serialization;

public sealed class GithubCommitDto
{
    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    [JsonPropertyName("commit")]
    public Commit? Committ { get; set; }

    public sealed class Commit
    {
        [JsonPropertyName("author")]
        public PersonBlock? Author { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public sealed class PersonBlock
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}