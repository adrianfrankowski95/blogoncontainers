namespace Blog.Services.Identity.Domain.Exceptions;

public class IdentityDomainException : Exception
{
    public IdentityDomainException(string message)
        : base(message)
    { }
}