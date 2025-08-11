namespace GitHubRaport.Models;

public class CommitEntity
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Repository { get; set; } = null!;
    public string Sha { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Committer { get; set; } = null!;
}