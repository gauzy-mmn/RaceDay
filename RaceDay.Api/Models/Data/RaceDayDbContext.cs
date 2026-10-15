using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;


public class  RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.RoleId).HasName("PK_Roles");
            e.Property(r => r.RoleName).HasColumnType("varchar(20)").IsRequired();
            e.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_RoleName");

            e.HasData(
               new Role { RoleId = 1, RoleName = AppRoles.Organiser },
               new Role { RoleId = 2, RoleName = AppRoles.Participant });
        });

        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId).HasName("PK_Users");
            e.Property(u => u.FirstName).HasColumnType("varchar(50)").IsRequired();
            e.Property(u => u.LastName).HasColumnType("varchar(50)").IsRequired();
            e.Property(u => u.Email).HasColumnType("varchar(100)").IsRequired();
            e.Property(u => u.PasswordHash).HasColumnType("varchar(255)").IsRequired();
            e.Property(u => u.PhoneNumber).HasColumnType("varchar(20)");
            e.Property(u => u.RegisteredAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Users_Roles");
        });

        mb.Entity<RaceEvent>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')");
                t.HasCheckConstraint("CK_Events_Distance", "[Distance] > 0");
            });
            e.HasKey(x => x.EventId).HasName("PK_Events");
            e.Property(x => x.Name).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Description).HasColumnType("varchar(500)");
            e.Property(x => x.EventDate).HasColumnType("datetime");
            e.Property(x => x.Location).HasColumnType("varchar(150)").IsRequired();
            e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
            e.Property(x => x.EventType).HasColumnType("varchar(10)").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Events_Users");
        });

        mb.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId).HasName("PK_Categories");
            e.Property(c => c.Name).HasColumnType("varchar(50)").IsRequired();
            e.Property(c => c.Description).HasColumnType("varchar(200)");
            e.HasIndex(c => new { c.EventId, c.Name }).IsUnique().HasDatabaseName("UQ_Categories_Event_Name");
            e.HasOne(c => c.Event).WithMany(x => x.Categories).HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Categories_Events");
        });

        mb.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentId).HasName("PK_Enrolments");
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique().HasDatabaseName("UQ_Enrolments_Participant_Event");
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments).HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Enrolments_Users");
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Enrolments_Events");
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments).HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Enrolments_Categories");
        });

        mb.Entity<RaceResult>(e =>
        {
            e.ToTable("Results", t =>
                t.HasCheckConstraint("CK_Results_FinishPosition", "[FinishPosition] IS NULL OR [FinishPosition] > 0"));
            e.HasKey(r => r.ResultId).HasName("PK_Results");
            e.Property(r => r.FinishTime).HasColumnType("time");
            e.Property(r => r.CapturedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
            e.HasOne(r => r.Enrolment).WithOne(x => x.Result).HasForeignKey<RaceResult>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Results_Enrolments");
        });

    }

}