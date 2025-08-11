namespace GitHubRaport.Errors;

public class NotFoundUserException : Exception
{
    public NotFoundUserException(string user)
      : base($"User not found: {user}.") { }
}
