namespace Enerflow.Domain.Exceptions;

public class InvalidTopologyException : Exception
{
    public InvalidTopologyException(string message) : base(message)
    {
    }

    public InvalidTopologyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
