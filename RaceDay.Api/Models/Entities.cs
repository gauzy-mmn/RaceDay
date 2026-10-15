namespace RaceDay.Api.Models;

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
    public static readonly string[] AllRoles = { Organiser, Participant };
}

public class  Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class User
    {
    public int UserId { get; set; }
    public string RoleId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime RegisteredAt { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<RaceEvent> OrganisedEvents { get; set; } = new List<RaceEvent>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

}

public class Category
{
    public int CategoryId { get; set; }
    public string EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public RaceEvent Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }

    public User Participant { get; set; } = null!;
    public RaceEvent Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public DateTime CapturedAt { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}