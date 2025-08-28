using System.ComponentModel.DataAnnotations;
namespace HelpDesk.Vip.Api;

// All these models were under the Vip Controller and made it more complex than it should be
// I refactored it just a little so it looks a little cleaner and easier to navigate within the models

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

// This is a "DTO" ("Data Transfer Object") - just a way for .NET to deserialize JSON into a .NET object we can work with.
public record VipCreateModel
{

    [Required]
    public string Sub { get; set; } = string.Empty;
    [Required, MaxLength(500), MinLength(10)]
    public string Description { get; set; } = string.Empty;
}

// This is what we are storing in the database.
// It has "technical" details we don't share from our API, like if this VIP "isRetired"
public record VipEntity
{
    public Guid Id { get; set; }
    public string Sub { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset AddedOn { get; set; }
    public bool IsRetired { get; set; } = false;
}
