using Marten;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.tech.Issues;

[ApiController]
public class TechIssueController : ControllerBase
{

    [HttpPost("/tech/problems/{problemId:guid}/assign")]
    public async Task<ActionResult> AssignProblemToTechAsync(
        Guid problemId,
        [FromServices] IDocumentSession session,
        [FromServices] IProvideUserInfo userService,
        [FromServices] IHubContext<HelpDeskHub, IHelpDeskClient> hubContext,
        CancellationToken token)
    {
        var entity = await session.Query<EmployeeProblemEntity>().SingleOrDefaultAsync(p => p.Id == problemId);

        // If the problem doesn't exist
        if (entity is null)
        {
            return NotFound(new { error = "Problem not found" });
        }

        // If the problem is already assigned to a tech
        if (entity.Status == SubmittedIssueStatus.AssignedToTech)
        {
            return BadRequest(new { error = "Problem is already assigned to another tech." });
        }

        // Only allow assignment if the problem is awaiting tech assignment
        if (entity.Status != SubmittedIssueStatus.AwaitingTechAssignment)
        {
            return BadRequest(new { error = $"Problem cannot be assigned. Current status: {entity.Status}" });
        }

        string techSub = await userService.GetUserSubAsync(token);
        entity.Status = SubmittedIssueStatus.AssignedToTech;
        entity.AssignedTo = techSub; // Make sure EmployeeProblemEntity has an AssignedTo property

        session.Update(entity);
        await session.SaveChangesAsync(token);

        var response = entity.MapToResponse();
        await hubContext.Clients.Group("employee").IssueUpdated(response);

        return Ok(new
        {
            problemId = entity.Id,
            assignedTo = techSub,
            status = "AssignedToTech"
        });
    }

}