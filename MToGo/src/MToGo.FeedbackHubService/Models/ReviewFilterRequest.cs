using System;

namespace MToGo.FeedbackHubService.Models;

// Query parameters for filtering reviews
public class ReviewFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Amount { get; set; }
}
