namespace GitHubRaport.Errors;

public class NotFoundRepositoryException : Exception
{
    public NotFoundRepositoryException(string repo)
        : base($"Repository not found: {repo}.") { }
}
