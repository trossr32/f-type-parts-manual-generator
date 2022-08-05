namespace App.Core.Exceptions;

public class ArgumentValidationException : Exception
{
    public ArgumentValidationException(string message) : base(message) { }
}