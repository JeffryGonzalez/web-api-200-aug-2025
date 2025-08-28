

using HelpDesk.Api.Users;
using Marten;
using Microsoft.AspNetCore.SignalR;
using RTools_NTS.Util;

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
        [FromServices]IDocumentSession session,
        CancellationToken token)
    {
        var entity = await session.Query<EmployeeProblemEntity>().SingleOrDefaultAsync(p => p.Id == problemId);

        if(entity is null)
        {
            return NotFound();
        } else
        {
            var response = entity.MapToResponse();
            return Ok(response);
        }
    }
    // GET /employees/problems
    // GET /employees/problems?softwareId=someguid
    [HttpGet("/employee/problems")]
    public async Task<ActionResult> GetAllEmployeeProblems(
        [FromServices] IProvideUserInfo userService,
        [FromServices] IDocumentSession session,
        [FromQuery] Guid softwareId,
        CancellationToken token)
    {
        var userSub = await userService.GetUserSubAsync(token);
        var problems = session.Query<EmployeeProblemEntity>().Where(p => p.ReportedBy == userSub);
        if(softwareId != Guid.Empty)
        {
            problems = problems.Where(p => p.ReportedProblem!.SoftwareId == softwareId);
        }


        var data = await problems.ToListAsync();
        var response = new EmployeeProblemCollectionResponse(data);
        response.FilteringBy = [softwareId.ToString()];
        return Ok(response); // { number: 3, params: ["softwareId=383983"], problems: [] }
    }

    [HttpDelete("/employees/problems/{id:guid}")]
    public async Task<ActionResult> DeleteProblem(
        Guid id, 
        [FromServices] IDocumentSession session, 
        [FromServices] IProvideUserInfo userService,
        CancellationToken token)
    {
       var sub =  await userService.GetUserSubAsync(token);
        var problem = await session.Query<EmployeeProblemEntity>()
            .Where(p => p.Id == id).SingleOrDefaultAsync();

        if(problem is null)
        {
            return NoContent();
        }
        if(problem.ReportedBy != sub)
        {
            return Unauthorized();
        }
        if(problem.Status != SubmittedIssueStatus.AwaitingTechAssignment)
        {
            return Conflict();
        }
         session.HardDelete<EmployeeProblemEntity>(problem);
        await session.SaveChangesAsync();
        return NoContent();
    }
 }
