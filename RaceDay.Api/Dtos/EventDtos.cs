using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Required] public DateTime? EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = string.Empty;
    /// <summary>Distance in kilometres (greater than 0).</summary>
    [Required, Range(0.01, 9999.99)] public decimal? Distance { get; set; }
    /// <summary>One of: Run, Walk, Cycle.</summary>
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static EventResponse From(RaceEvent e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        CreatedAt = e.CreatedAt
    };
}

public class CategoryRequest
{
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    [StringLength(200)] public string? Description { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        Description = c.Description
    };
}