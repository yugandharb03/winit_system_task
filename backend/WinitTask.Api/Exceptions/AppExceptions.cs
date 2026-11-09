namespace WinitTask.Api.Exceptions;

/// <summary>Thrown when a requested record does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string entity, string key) =>
        new($"{entity} '{key}' was not found.");
}

/// <summary>Thrown when business rules reject a request. Mapped to HTTP 400 with field errors.</summary>
public class BusinessValidationException : Exception
{
    public BusinessValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [string.Empty] = new[] { message } };
    }

    public BusinessValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
