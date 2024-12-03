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
        public DbSet<User> User { get; set; }
        public DbSet<Promotion> Promotion { get; set; }
    }
      
    // Entity Classes -------------------------------------------------------------

#nullable disable warnings

   

    public class Attraction //A0001
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
        [MaxLength(200)]
        public string ImagePath { get; set; }

        //FK
        public string AttractionTypeId { get; set; }

        //Navigation
        public AttractionType AttractionType { get; set; }

    }//end of attractionType

    public class AttractionType //AT0001
    {
        //Column
        [Key, MaxLength(6)]
        public string Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; }

        //Navigation
        public List<Attraction> Attractions { get; set; } = [];
    }//end of attraction

    public class Feedback//F0001
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
        public string UserId { get; set; }

        //Navigation
        public Attraction Attraction { get; set; }
        public User User { get; set; }

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
    

    //User Table
    public class User //U0001
    {
        // Columns
        [Key, MaxLength(10)]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string Email { get; set; }

        [Required, MaxLength(30)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Password { get; set; }

        [MaxLength(15)]
        public string IC { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(5)]
        public string Gender { get; set; }

        public bool Freeze { get; set; }

        [MaxLength(10)]
        public string Role { get; set; }

        [MaxLength(50)]
        public string ImagePath { get; set; }
    }

    public class Promotion
    {
        [Key, MaxLength(10)]
        public string PromotionId { get; set; }

        [MaxLength(255)]
        public string Title { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PriceDeduction { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [MaxLength(20)]
        public string PromoStatus { get; set; }
    }
}
