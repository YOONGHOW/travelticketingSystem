namespace MobileWebAssignment.Models
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;

    public class DB : DbContext
    {
        public DB(DbContextOptions<DB> options) : base(options) { }

        // DbSet
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Attraction> Attraction { get; set; }
        public DbSet<AttractionType> AttractionType { get; set; }
        public DbSet<Ticket> Ticket { get; set; }
    }
      
    // Entity Classes -------------------------------------------------------------

#nullable disable warnings

   

    public class Attraction
    {
        //Column
        [Key, MaxLength(5)]
        public string Id { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(1000)]
        public string Description { get; set; }
        [MaxLength(1000)]
        public string Location { get; set; }
        [MaxLength(500)]
        public string OperatingHours { get; set; }

        //FK
        public string AttractionTypeId { get; set; }

        //Navigation
        public AttractionType AttractionType { get; set; }

    }//end of attractionType

    public class AttractionType
    {
        //Column
        [Key, MaxLength(6)]
        public string Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; }

        //Navigation
        public List<Attraction> Attractions { get; set; } = [];
    }//end of attraction

    public class Feedback
    {
        //Column
        [Key, MaxLength(5)]
        public string Id { get; set; }
        [MaxLength(1000)]
        public string Comment { get; set; }
        [MaxLength(1)]
        public int Rating { get; set; }
        public DateTime SubmitDate { get; set; }

        //FK
        public string AttractionId { get; set; }
        //public string UserId { get; set};

        //Navigation
        public Attraction Attraction { get; set; }
        //public User User { get; set; }

    }// end of feedback

    public class Ticket
    {
        [Key, MaxLength(6)] //TK0001
        public string ticketID { get; set; }
        [MaxLength(200)]
        public string ticketName { get; set; }
        public int stockQty { get; set; }   
        public double ticketPrice { get; set; }
        public string ticketStatus { get; set; }
        [MaxLength(1000)]
        public string ticektDetails { get; set; }
        public string ticketType { get; set; }

        //FK
        public string AttractionId { get; set; }
        //navigation 
        public Attraction Attraction { get; set; }

    }
    
}
