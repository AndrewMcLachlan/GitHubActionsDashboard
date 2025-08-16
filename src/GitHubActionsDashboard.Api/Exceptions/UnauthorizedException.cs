namespace GitHubActionsDashboard.Api.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Unauthorized access")
    {
    }
}
