using System.ComponentModel.DataAnnotations;
using JasperFx.CodeGeneration.Frames;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedContracts;

namespace HelpDesk.Vip.Api.Vips;
[ApiController]
public class VipsController : ControllerBase
{
    /*
     * Note: This code is intentionally not "clean" and refactored. It is presented to promote understanding */

    // When they do a post to http://localhost:1338/vips
    [HttpPost("/vips")]  // "Flags" = metadata. 
    public async Task<ActionResult> AddVipAsync(
        [FromServices] ISendVipsToTheSoftwareCenter softwareCenter,
        [FromBody] VipCreateModel request, // Read the JSON of the request and populate this. The [ApiController] attribute will validate it first.
        [FromServices] IDocumentSession session, // The service I'll use to add it to the databse
        [FromServices] TimeProvider clock) // I use this for ALL date/time stuff in APIs.
    {
        // Normalize the subject for consistent uniqueness checks
        var normalizedSub = request.Sub?.Trim().ToLowerInvariant() ?? string.Empty;

        // If an active VIP with that subject already exists -> 409
        var alreadyThere = await session.Query<VipEntity>().AnyAsync(v => v.Sub == normalizedSub && v.IsRetired == false);
        if (alreadyThere)
        {
            return Conflict();
        }

        // If a retired VIP exists with that subject, "reactivate" it instead of inserting a duplicate.
        var retired = await session.Query<VipEntity>().SingleOrDefaultAsync(v => v.Sub == normalizedSub && v.IsRetired == true);
        VipEntity entity;
        if (retired is not null)
        {
            retired.IsRetired = false;
            retired.Description = request.Description;
            retired.AddedOn = clock.GetLocalNow();
            entity = retired;
            session.Store(entity);
            await session.SaveChangesAsync();
        }
        else
        {
            // Insert new
            entity = new VipEntity
            {
                Id = Guid.NewGuid(),
                AddedOn = clock.GetLocalNow(),
                Description = request.Description,
                Sub = normalizedSub,
                IsRetired = false
            };
            session.Store(entity); // Marten does an "upsert" by default, if the Ids are the same. Your code may be more complex here.
            await session.SaveChangesAsync();
        }
        // Map it to a custom response model. I don' want to "leak" technical data like if it is retired or not to the client.
        var response = new VipDetailsModel
        {
            Id = entity.Id,
            Sub = entity.Sub,
            Description = entity.Description,
            AddedOn = entity.AddedOn
        };
        // Return a 201 Created, with a Location Header
        // TODO: Make an HTTP Post to the Software Center to give them this new VIP.
        try
        {

            await softwareCenter.SendSoftwareCenterNewVipAsync(response);
        }
        catch (Exception)
        {

            // Write this to a database table called "SoftwareCenterIsDrunkAgain", 
            // have a background worker that just keeps retrying.
        }
        return Created($"/vips/{response.Id}", response);
    }

    // The route constraint of `guid` will verify before calling this if that segment is a valid "guid", and return 404
    // if it is not. So this won't handle "/vips/tacos" - it will just return 404.
    [HttpGet("/vips/{id:guid}")]
    public async Task<ActionResult> GetVipAsync(Guid id,
        [FromServices] IDocumentSession session,
        CancellationToken token)
    {
        // Get it from the database, if it has the same Id and hasn't been retired.
        var entity = await session.Query<VipEntity>().SingleOrDefaultAsync(v => v.Id == id && v.IsRetired == false);
        if (entity is null)
        {
            return NotFound();
        }
        else
        {
            // Map to a response model. Again.
            var response = new VipDetailsModel
            {
                Id = entity.Id,
                Sub = entity.Sub,
                Description = entity.Description,
                AddedOn = entity.AddedOn,
            };
            return Ok(response);
        }
    }

    // Notice I'm not using the route constraint here. It's a debate, but I personally don't think
    // a "DELETE" should *ever* return a 404. DELETE (and PUT) should be idempotent. Deleting the same
    // thing more than once is NOT an error, and 404 is an error.

    [HttpDelete("/vips/{id}")]
    public async Task<ActionResult> DeleteVipAsync(
        [FromServices] IDocumentSession session,
        Guid id)
    {
        // Since I'm not using the route constraint, the default will be an empty guid for the id parameter, so we know not to bother the database.
        if (id == Guid.Empty)
        {
            return NoContent(); // No Content is the "Fine.", passive aggressive status code (204). I've got nothing else to tell you.
        }
        // See if it is in the database
        var vip = await session.Query<VipEntity>().SingleOrDefaultAsync(v => v.Id == id);
        if (vip is null)
        {
            // If it isn't, return a success status code. Take credit for it!
            return NoContent();
        }
        // But if it IS in the database, and they aren't retired, they should be now.
        if (vip.IsRetired == false)
        {
            vip.IsRetired = true;
            session.Store(vip);
            await session.SaveChangesAsync();
        }

        return NoContent();

    }

/* [ { id: "dd", sub: "jose", description: "big whiner, just take care of him", .. }, { ... } ] */    
    [HttpGet("/vips")]
    public async Task<ActionResult> GetAllVipsAsync(
        [FromServices] IDocumentSession session,
        CancellationToken token)
    {
        // Find that VIP and make sure they aren't retired, and map it to a VipDetailsModel list.
        var response = await session.Query<VipEntity>()
            .Where(v => v.IsRetired == false)
            .Select(v => new VipDetailsModel { Id = v.Id, Sub = v.Sub, AddedOn = v.AddedOn, Description = v.Description })
            .ToListAsync();
        return Ok(response);
    }

    // [Authorize(Roles="HelpDeskApi")]
    // [ "bob@aol.", "sue" ]
    [HttpGet("/help-desk/vips")]
    public async Task<ActionResult> GettheVipsFortheHelpDeskApi(CancellationToken token, [FromServices] IDocumentSession session)
    {
        var vips = await session.Query<VipEntity>().Where(v => v.IsRetired == false).Select(v => v.Sub).ToListAsync();


        return Ok(new HelpDeskVipResponse(vips, vips.Count()));
    }

}



// This is a "DTO" ("Data Transfer Object") - just a way for .NET to deserialize JSON into a .NET object we can work with.
public record VipCreateModel
{

    [Required]
    public string Sub { get; set; } = string.Empty;
    [Required, MaxLength(500), MinLength(10)]
    public string Description { get; set; } = string.Empty;
}

// Another "DTO" - what we are sending back.

/*
{
    "sub": "sue@company.com",
    "description": "Sue is the CEO, We need to make sure she is always able to be effective"
}
*/
public record VipDetailsModel
{
    public Guid Id { get; set; }
    public string Sub { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset AddedOn { get; set; }
}

// This is what we are storing in the database.
// It has "technical" details we don't share from our API, like if this VIP "isRetired"
public class VipEntity
{
    public Guid Id { get; set; }
    public string Sub { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset AddedOn { get; set; }
    public bool IsRetired { get; set; } = false;
}




