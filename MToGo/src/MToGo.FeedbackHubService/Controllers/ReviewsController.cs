using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.FeedbackHubService.Exceptions;
using MToGo.FeedbackHubService.Models;
using MToGo.FeedbackHubService.Services;

namespace MToGo.FeedbackHubService.Controllers;

[ApiController]
[Route("api/v1/feedback-hub/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var review = await _reviewService.CreateReviewAsync(request);
            return CreatedAtAction(nameof(GetReviewByOrderId), new { orderId = review.OrderId }, review);
        }
        catch (DuplicateReviewException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidRatingException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CommentTooLongException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NoRatingsProvidedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("order/{orderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetReviewByOrderId(int orderId)
    {
        var review = await _reviewService.GetReviewByOrderIdAsync(orderId);
        if (review == null)
        {
            return NotFound(new { message = $"No review found for order {orderId}" });
        }
        return Ok(review);
    }

    [HttpGet("order/{orderId:int}/exists")]
    [Authorize]
    public async Task<IActionResult> CheckReviewExists(int orderId)
    {
        var exists = await _reviewService.HasReviewForOrderAsync(orderId);
        return Ok(new { exists });
    }
}
