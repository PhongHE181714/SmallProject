namespace BE.BuildingBlocks.SharedKernel;

public static class ProblemExceptionFactory
{
    public static TException WithTitle<TException>(this TException exception, string problemTitle)
        where TException : Exception
    {
        exception.Data["ProblemTitle"] = problemTitle;
        return exception;
    }
}
