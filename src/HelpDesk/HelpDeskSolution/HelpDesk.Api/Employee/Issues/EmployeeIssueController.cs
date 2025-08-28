

using Marten;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.Employee.Issues;

[ApiController]
public class EmployeeIssueController : ControllerBase
{
    [HttpPost("/employee/problems")]
    public async Task<ActionResult> AddEmployeeProblemAsync(
        [FromBody] SubmitIssueRequest request,
        [FromServices] TimeProvider clock,
        [FromServices] IProvideUserInfo userService,
        [FromServices] IDocumentSession session,
        [FromServices] IssueMetrics metrics,
        CancellationToken token
        )
    {
        // we should validate the incoming request against the "rules"
        // - Field level stuff - did they send everything, does it meet the rules, etc. (FluentValidation)
        // - SoftwareId - 

        string userSub = await userService.GetUserSubAsync(token);
        // "Slime" (BS)
        var response = new SubmitIssueResponse
        {
            Id = Guid.NewGuid(), // Maybe slime? has to be the database id?
            ReportedAt = clock.GetLocalNow(),
            ReportedBy = userSub, // Slime. Fake - this has to come from authorization.
            ReportedProblem = request,
            Status = SubmittedIssueStatus.AwaitingTechAssignment
        };
        var entity = response.MapToEntity();
        session.Store(entity);
        await session.SaveChangesAsync();
        metrics.ProblemCreated(userSub, response.Id, request.SoftwareId);
        // save this thing somewhere. 
        // Slime, too - because you can't GET that location and get the same response.
        return Created($"/employee/problems/{response.Id}", response);
    }

    [HttpGet("/employee/problems/{problemId:guid}")]
    public async Task<ActionResult> GetProblemByIdAsync(Guid problemId,
        [FromServices] IDocumentSession session,
        CancellationToken token)
    {
        var entity = await session.Query<EmployeeProblemEntity>().SingleOrDefaultAsync(p => p.Id == problemId);

        if (entity is null)
        {
            return NotFound();
        }
        else
        {
            var response = entity.MapToResponse();
            return Ok(response);
        }
    }

    [HttpPost("/employee/problems/{problemId:guid}/cancel")]
    public async Task<ActionResult> CancelProblemByIdAsync(
        Guid problemId,
        [FromServices] IDocumentSession session,
        [FromServices] IProvideUserInfo userService,
        [FromServices] IHubContext<HelpDeskHub, IHelpDeskClient> hubContext,
        CancellationToken token)
    {
        var entity = await session.Query<EmployeeProblemEntity>().SingleOrDefaultAsync(p => p.Id == problemId);

        if (entity is null)
        {
            return NotFound(new { error = "Problem not found" });
        }

        // Check if the current user is the owner of the problem
        string userSub = await userService.GetUserSubAsync(token);
        if (entity.ReportedBy != userSub)
        {
            return Forbid();
        }

        if (entity.Status != SubmittedIssueStatus.AwaitingTechAssignment)
        {
            return BadRequest(new { error = $"Problem cannot be cancelled. Current status: {entity.Status}" });
        }

        entity.Status = SubmittedIssueStatus.CancelledByEmployee;
        session.Update(entity);
        await session.SaveChangesAsync(token);

        var response = entity.MapToResponse();
        await hubContext.Clients.Group("tech").IssueUpdated(response);

        return Ok(new
        {
            problemId = entity.Id,
            status = "Cancelled"
        });
    }
 }
