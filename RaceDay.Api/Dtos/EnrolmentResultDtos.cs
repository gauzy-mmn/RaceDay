using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EnrolRequest
{
    [Required] public int? CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }

    /// <summary>Expects Participant, Event and Category to be loaded.</summary>
    public static EnrolmentResponse From(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        EventId = e.EventId,
        EventName = e.Event.Name,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.Name,
        ParticipantId = e.ParticipantId,
        ParticipantName = $"{e.Participant.FirstName} {e.Participant.LastName}",
        ParticipantEmail = e.Participant.Email,
        EnrolmentDate = e.EnrolmentDate
    };
}

public class UpdateResultRequest
{
    /// <summary>Finish time as hh:mm:ss (must be greater than zero).</summary>
    [Required] public TimeSpan? FinishTime { get; set; }
    [Required, Range(1, int.MaxValue)] public int? FinishPosition { get; set; }
}

public class CreateResultRequest : UpdateResultRequest
{
    [Required] public int? EnrolmentId { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public DateTime CapturedAt { get; set; }

    /// <summary>Expects Enrolment (with Participant, Event, Category) to be loaded.</summary>
    public static ResultResponse From(RaceResult r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment.EventId,
        EventName = r.Enrolment.Event.Name,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
        CategoryName = r.Enrolment.Category.Name,
        FinishTime = r.FinishTime,
        FinishPosition = r.FinishPosition,
        CapturedAt = r.CapturedAt
    };
}