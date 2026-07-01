namespace Bilreg.Application.Shared.Helpers;

[Serializable]
public class TooManyResultsException : Exception
{
    public int Limit { get; }

    public TooManyResultsException(int limit, string message)
        : base($"Result returned too many data (limit: {limit}). {message}")
    {
        Limit = limit;
    }
}
