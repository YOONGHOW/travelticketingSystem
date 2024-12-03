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
    public DbSet<Purchase> Purchase { get; set; }
    public DbSet<Payment> Payment { get; set; }
    }
      
    // Entity Classes -------------------------------------------------------------


#nullable disable warnings


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

//User Table
public class User //U0001
{
    // Columns
    [Key, MaxLength(10)]
    public string Id { get; set; }

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

    public List<Purchase> Purchases {get;set;}=[];
}
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

public class Purchase {
    [Key, MaxLength(6)] //P0001
    public string Id { get; set; }

    [Required]
    public DateTime PaymentDateTime { get; set; } // Combines Date and Time for better handling

    [Required]
    [MaxLength(1)]
    public string Status { get; set; }

    [Required]
    [Precision(18, 2)] // Ensures accuracy for monetary values
    public decimal Amount { get; set; } // Changed to decimal for currency values

    //FK
    public string UserId { get; set; }
    public User User { get; set; }
}


public class Payment
{
    [Key]
    [MaxLength(6)] // Example: PA0001
    public string Id { get; set; }

    [Required]
    [MaxLength(1)] // Example: B (for Bank)
    public string Type { get; set; }

    [Required]
    [MaxLength(1)] // Example: S (Success) or F (Fail)
    public string Status { get; set; }

    [MaxLength(100)]
    public string Reference { get; set; } // Renamed REF for clarity

    [Required]
    public DateTime PaymentDateTime { get; set; } // Combines Date and Time for better handling

    [Required]
    [Precision(18, 2)] // Ensures accuracy for monetary values
    public decimal Amount { get; set; } // Changed to decimal for currency values


    //fk
    public String PurchaseId { get; set; }

    public Purchase Purchase {get; set;}


    }

}
