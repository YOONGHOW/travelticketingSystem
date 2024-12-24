using Microsoft.EntityFrameworkCore;

    using System.ComponentModel.DataAnnotations;

namespace MobileWebAssignment.Models;
    public class DB : DbContext
    {
        public DB(DbContextOptions<DB> options) : base(options) { }

        // DbSet
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<Attraction> Attraction { get; set; }
        public DbSet<AttractionType> AttractionType { get; set; }
        public DbSet<Ticket> Ticket { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Promotion> Promotion { get; set; }
        public DbSet<Purchase> Purchase { get; set; }
        public DbSet<PurchaseItem> PurchaseItem { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Member> Members { get; set; }
    }
      
    // Entity Classes -------------------------------------------------------------



#nullable disable warnings

 public class Promotion //PM0001
    {
        [Key, MaxLength(10)]
        public string Id { get; set; }

        [MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(8)]
        public string Code{ get; set; }

        [Precision(2, 2)]
        public decimal PriceDeduction { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [MaxLength(20)]
        public string PromoStatus { get; set; }

        //Navigation
         public List<Purchase> Purchases { get; set; } = [];
 }//end of promotion

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
    [MaxLength(800)]
    public string ImagePath { get; set; }

    //FK
    public string AttractionTypeId { get; set; }

    //Navigation
    public AttractionType AttractionType { get; set; }
    public List<Feedback> Feedbacks { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];


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


public class Ticket //TK0001
{
    //Column
    [Key, MaxLength(6)] 
    public string Id { get; set; }
    [MaxLength(200)]
    public string ticketName { get; set; }
    public int stockQty { get; set; }   
    [Precision(4,2)]
    public decimal ticketPrice { get; set; }
    public string ticketStatus { get; set; }
    [MaxLength(1000)]
    public string ticketDetails { get; set; }
    public string ticketType { get; set; }

    //FK
    public string AttractionId { get; set; }
    
    //navigation 
    public Attraction Attraction { get; set; }
    public List<PurchaseItem> PurchaseItems { get; set; } = [];
    public List<Cart> Carts { get; set; } = [];

}//end of ticket

public class Cart
{
    [Key, MaxLength(8)] //CART0001
    public string CartID { get; set; }
    //FK USER ID
    public string UserId { get; set; }
    //FK ticketID
    public string TicketId { get; set; }
    public int quantity { get; set; }
    
    //navigation
    public Ticket Ticket { get; set; }
    public User User { get; set; }
}
//end of cart


public class Purchase//P0001
{ 
    [Key, MaxLength(6)] 
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
    public string? PromotionId {get; set;}
    public string UserId { get; set; }
    
    //Navigation
    public List<PurchaseItem> PurchaseItems { get; set; } = [];
    public Promotion Promotion {get; set;}
    public User User { get; set; }
    

}//end of purchase


public class PurchaseItem // PI0001
{
    //Column
    [Key,MaxLength(6)] 
    public string Id { get; set; }
    public int Quantity {  get; set; }
    public DateTime validDate { get; set; }

    //FK
    public string TicketId { get; set; }
    public String PurchaseId { get; set; }

    //Navigation
    public Purchase Purchase { get; set; }
    public Ticket Ticket { get; set; }

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

    //Navigation
    public Purchase Purchase {get; set;}

}//PA0001

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

    [Required, MaxLength(500)]
    public string Password { get; set; }

    [MaxLength(15)]
    public string IC { get; set; }

    [MaxLength(20)]
    public string PhoneNumber { get; set; }

    [MaxLength(5)]
    public string Gender { get; set; }

    public bool Freeze { get; set; }

    public string Role => GetType().Name;

    //Navigation
    public List<Feedback> Feedbacks { get; set; } = [];
    public List<Purchase> Purchases { get; set; } = [];
    public List<Cart> Carts { get; set; } = [];
} //end of user

public class Admin : User
{

}//end of Admin

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }
}//end of Member
