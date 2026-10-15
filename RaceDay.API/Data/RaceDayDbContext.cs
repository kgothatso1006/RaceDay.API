using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        // These DbSets tell EF Core which tables to create in SQL Server
        public DbSet<User> Users { get; set; }
        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. One-to-One: User -> Organiser
            modelBuilder.Entity<Organiser>()
                .HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. One-to-One: User -> Participant
            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. One-to-One: Enrolment -> Result
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. FIX: Prevent multiple cascade paths on Enrolments
            // This tells EF Core to NOT cascade delete enrolments when a Participant or Category is deleted.
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(e => e.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Unique Constraints (Emails and Phone numbers must be unique)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Phone)
                .IsUnique();

            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => e.RaceNumber)
                .IsUnique();

            // 6. Check Constraints (Matches your SQL Script exactly)
            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));

            modelBuilder.Entity<Event>()
                .ToTable(t => t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Upcoming','Open','Completed','Cancelled')"));

            modelBuilder.Entity<Enrolment>()
                .ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Registered','Cancelled','Completed')"));

            modelBuilder.Entity<Result>()
                .ToTable(t => t.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished','DidNotFinish','Disqualified')"));

            modelBuilder.Entity<Category>()
                .ToTable(t => t.HasCheckConstraint("CK_Categories_Distance", "[DistanceKm] > 0"));

            // 7. Seed Data (Matches your SQL Script exactly)
            modelBuilder.Entity<User>().HasData(
                new User { UserID = 1, FirstName = "Thabo", LastName = "Mokoena", Email = "thabo@raceday.co.za", PasswordHash = "HASHED_PASSWORD_1", Phone = "0821112233", Role = "Organiser", CreatedAt = new DateTime(2026, 1, 1) },
                new User { UserID = 2, FirstName = "Lerato", LastName = "Maseko", Email = "lerato@raceday.co.za", PasswordHash = "HASHED_PASSWORD_2", Phone = "0832223344", Role = "Organiser", CreatedAt = new DateTime(2026, 1, 1) },
                new User { UserID = 3, FirstName = "Rich", LastName = "Ringane", Email = "rich@example.com", PasswordHash = "HASHED_PASSWORD_3", Phone = "0843334455", Role = "Participant", CreatedAt = new DateTime(2026, 1, 1) },
                new User { UserID = 4, FirstName = "Kabelo", LastName = "Ndlovu", Email = "kabelo@example.com", PasswordHash = "HASHED_PASSWORD_4", Phone = "0854445566", Role = "Participant", CreatedAt = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<Organiser>().HasData(
                new Organiser { OrganiserID = 1, UserID = 1, OrganisationName = "Limpopo Race Events" },
                new Organiser { OrganiserID = 2, UserID = 2, OrganisationName = "Makhado Sports Management" }
            );

            modelBuilder.Entity<Participant>().HasData(
                new Participant { ParticipantID = 1, UserID = 3, DateOfBirth = new DateTime(2005, 04, 15), EmergencyContact = "Mpho Ringane", EmergencyPhone = "0715556677" },
                new Participant { ParticipantID = 2, UserID = 4, DateOfBirth = new DateTime(2004, 09, 22), EmergencyContact = "Neo Ndlovu", EmergencyPhone = "0726667788" }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event { EventID = 1, OrganiserID = 1, EventName = "Limpopo Spring Run", Description = "Community road running event with multiple race categories.", EventDate = new DateTime(2026, 09, 20), StartTime = new TimeSpan(7, 0, 0), Venue = "Polokwane Sports Complex", Location = "Polokwane, Limpopo", Status = "Open", CreatedAt = new DateTime(2026, 1, 1) },
                new Event { EventID = 2, OrganiserID = 1, EventName = "Makhado Heritage Marathon", Description = "Annual marathon celebrating sport and local heritage.", EventDate = new DateTime(2026, 10, 18), StartTime = new TimeSpan(6, 30, 0), Venue = "Makhado Show Grounds", Location = "Makhado, Limpopo", Status = "Upcoming", CreatedAt = new DateTime(2026, 1, 1) },
                new Event { EventID = 3, OrganiserID = 2, EventName = "Thohoyandou Charity Race", Description = "Charity race supporting youth development initiatives.", EventDate = new DateTime(2026, 11, 08), StartTime = new TimeSpan(7, 30, 0), Venue = "Thohoyandou Stadium", Location = "Thohoyandou, Limpopo", Status = "Upcoming", CreatedAt = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryID = 1, EventID = 1, CategoryName = "5 km Fun Run", DistanceKm = 5.00m, MaxParticipants = 300, EntryFee = 80.00m },
                new Category { CategoryID = 2, EventID = 1, CategoryName = "10 km Challenge", DistanceKm = 10.00m, MaxParticipants = 250, EntryFee = 120.00m },
                new Category { CategoryID = 3, EventID = 2, CategoryName = "21 km Half Marathon", DistanceKm = 21.10m, MaxParticipants = 500, EntryFee = 220.00m },
                new Category { CategoryID = 4, EventID = 2, CategoryName = "42 km Marathon", DistanceKm = 42.20m, MaxParticipants = 600, EntryFee = 350.00m },
                new Category { CategoryID = 5, EventID = 3, CategoryName = "5 km Charity Run", DistanceKm = 5.00m, MaxParticipants = 400, EntryFee = 60.00m },
                new Category { CategoryID = 6, EventID = 3, CategoryName = "10 km Charity Challenge", DistanceKm = 10.00m, MaxParticipants = 300, EntryFee = 100.00m }
            );

            modelBuilder.Entity<Enrolment>().HasData(
                new Enrolment { EnrolmentID = 1, ParticipantID = 1, CategoryID = 1, EnrolmentDate = new DateTime(2026, 08, 01, 9, 0, 0), EnrolmentStatus = "Registered", RaceNumber = "LR001" },
                new Enrolment { EnrolmentID = 2, ParticipantID = 1, CategoryID = 3, EnrolmentDate = new DateTime(2026, 08, 05, 10, 30, 0), EnrolmentStatus = "Registered", RaceNumber = "MH001" },
                new Enrolment { EnrolmentID = 3, ParticipantID = 2, CategoryID = 2, EnrolmentDate = new DateTime(2026, 08, 03, 11, 15, 0), EnrolmentStatus = "Registered", RaceNumber = "LR002" },
                new Enrolment { EnrolmentID = 4, ParticipantID = 2, CategoryID = 5, EnrolmentDate = new DateTime(2026, 08, 10, 14, 0, 0), EnrolmentStatus = "Registered", RaceNumber = "TC001" }
            );

            modelBuilder.Entity<Result>().HasData(
                new Result { ResultID = 1, EnrolmentID = 1, FinishTime = new TimeSpan(0, 31, 42), Position = 12, ResultStatus = "Finished", RecordedAt = new DateTime(2026, 9, 20) },
                new Result { ResultID = 2, EnrolmentID = 3, FinishTime = new TimeSpan(0, 55, 18), Position = 8, ResultStatus = "Finished", RecordedAt = new DateTime(2026, 9, 20) }
            );
        }
    }
}