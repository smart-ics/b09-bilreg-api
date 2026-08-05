using System.Runtime.Serialization;

namespace Bilreg.Application.Helpers;

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
