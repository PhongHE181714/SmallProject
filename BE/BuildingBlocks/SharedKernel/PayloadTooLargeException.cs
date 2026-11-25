namespace BE.BuildingBlocks.SharedKernel;

public sealed class PayloadTooLargeException : Exception
{
    public string Title { get; }

    public PayloadTooLargeException(string title, string message)
        : base(message)
    {
        Title = title;
    }
}
