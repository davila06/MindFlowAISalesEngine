namespace Api.Application.Assignment;

public class AssignmentConflictException : Exception
{
    public AssignmentConflictException(string message) : base(message)
    {
    }
}
