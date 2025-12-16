namespace MToGo.FeedbackHubService.Exceptions;

public class DuplicateReviewException : Exception
{
    public int OrderId { get; }

    public DuplicateReviewException(int orderId)
        : base($"A review already exists for order {orderId}")
    {
        OrderId = orderId;
    }
}

public class InvalidRatingException : Exception
{
    public InvalidRatingException(string ratingType, int value)
        : base($"{ratingType} rating must be between 0 and 5, but was {value}")
    {
    }
}

public class CommentTooLongException : Exception
{
    public CommentTooLongException(string commentType, int length)
        : base($"{commentType} comment exceeds maximum length of 500 characters (was {length})")
    {
    }
}

public class NoRatingsProvidedException : Exception
{
    public NoRatingsProvidedException()
        : base("At least one rating must be provided")
    {
    }
}
