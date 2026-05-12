using System.ComponentModel.DataAnnotations;

namespace Midori.API.Components;

public class RequestValidationException : Exception
{
    public List<ValidationResult> Results { get; }

    public RequestValidationException(List<ValidationResult> results)
    {
        Results = results;
    }
}
