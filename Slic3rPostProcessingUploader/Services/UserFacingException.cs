namespace Slic3rPostProcessingUploader.Services;

/// <summary>
/// An error the user can act on. The message is shown as-is; the hint tells them what to do about it.
/// Anything else that escapes to the top level is treated as unexpected and reported generically.
/// </summary>
internal class UserFacingException : Exception
{
    public string? Hint { get; }

    public UserFacingException(string message, string? hint = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Hint = hint;
    }
}
