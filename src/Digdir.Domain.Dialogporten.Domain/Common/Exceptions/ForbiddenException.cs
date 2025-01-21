namespace Digdir.Domain.Dialogporten.Domain.Common.Exceptions;

public sealed class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message) : base(message) { }
}
