
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MobileWebAssignment.Models;

// View Models ----------------------------------------------------------------

#nullable disable warnings

public class AttractionTypeInsertVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^[a-z A-Z]+$", ErrorMessage = "Invalid {0}.")]
    public string Name { get; set; }

}

public class AttractionInsertVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^[a-z A-Z]+$", ErrorMessage = "Invalid {0}.")]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(1000)]
    public string Location { get; set; }
    [StringLength(500)]
    public string? OperatingHours { get; set; }
    [StringLength(200)]
    public string? ImagePath { get; set; }

    public string AttractionTypeId { get; set; }

    public IFormFile Photo { get; set; }

    public List<OperatingHour>? operatingHours { get; set; }

}

public class AttractionUpdateVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^[a-z A-Z]+$", ErrorMessage = "Invalid {0}.")]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(1000)]
    public string Location { get; set; }
    [StringLength(500)]
    public string? OperatingHours { get; set; }

    [StringLength(200)]
    public string? ImagePath { get; set; }

    public string AttractionTypeId { get; set; }

    public IFormFile? Photo { get; set; }

    public List<TicketVM> Tickets { get; set; }
    public List<OperatingHour>? operatingHours { get; set; }
    public List<OperatingTime>? operatingTimes { get; set; }

}

public class OperatingHour
{
    public string Day { get; set; }
    public string Status { get; set; } 
    public TimeSpan? StartTime { get; set; } 
    public TimeSpan? EndTime { get; set; }   
}


public class OperatingTime
{
    public string Day { get; set; }
    public string Status { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
}


//Feedback
public class FeedbackInsertVM
{
    public string Id { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Range(1,5,ErrorMessage = "Please select your rating.")]
    public int Rating { get; set; }
    public DateTime SubmitDate { get; set; }

    
    public string AttractionId { get; set; }
    public string UserId { get; set; }
    [Required]
    public string? Title { get; set; }
    [Required]
    public string? Partner { get; set; }
    [Required]
    public string? Reason { get; set; }
    [Required]
    public string? Review { get; set; }

    public Comment? commentDetail { get; set; }
}

public class Comment()
{
    public string? Title { get; set; }
    
    public string? Partner { get; set; }
    
    public string? Reason { get; set; }
    
    public string? Review { get; set; }
}



//no validation yet
public class LoginVm
{
    public string Email { get; set; }
    public string Password { get; set; }
}
public class RegisterVM
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "IC Number is required.")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid IC Number. Must be exactly 12 digits.")]
    public string IC { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Invalid Phone Number. Must be 10 or 11 digits.")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(M|F)$", ErrorMessage = "Invalid Gender. Only 'male' or 'female' is allowed.")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    //[Display(Name = "Profile Photo")]
    //public IFormFile Photo { get; set; }

}

public class TicketVM {

    public string Id { get; set; }
    [StringLength(200)]
    [Required(ErrorMessage = "Ticket Name is required.")]
    public string ticketName { get; set; }
    public int stockQty { get; set; }
    [Precision(4, 2)]
    public decimal ticketPrice { get; set; }
    public string ticketStatus { get; set; }
    [StringLength(1000)]
    [Required(ErrorMessage = "Ticket details is required.")]
    public string ticketDetails { get; set; }
    public string ticketType { get; set; }
    //FK
    public string AttractionId { get; set; }
}

public class AdminTicketDetails
{
    public Attraction Attraction { get; set; }
    public List<Ticket> Tickets { get; set; }
}
