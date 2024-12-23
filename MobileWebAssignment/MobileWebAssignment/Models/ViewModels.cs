using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public ImageSet Photo { get; set; }

    public List<OperatingHour>? operatingHours { get; set; }

}

public class PromotionInsertVM
{
    public string Id { get; set; } // PM0001
    public string Title { get; set; } // Promotion Name
    public string Code { get; set; } // PROMO2024
    public decimal PriceDeduction { get; set; } // Discount Amount
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PromoStatus { get; set; } // Active, Inactive, Expired
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
    public ImageSet? Photo { get; set; }

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

//get average feedback
public class AttractFeedback
{
    public Attraction attraction { get; set; }

    public List<Feedback> feedbacks { get; set; }

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

//================================== USER Account Features ===========================================================
public class LoginVm
{
    [EmailAddress]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    [Required(ErrorMessage = "Password is required.")]
    public string PasswordCurrent { get; set; }
}
public class RegisterVM
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [Remote("CheckEmail", "Client",ErrorMessage = "This email is already in use.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "IC Number is required.")]
    [RegularExpression(@"^\d{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])\d{2}\d{4}$", ErrorMessage = "Invalid IC Number. Please enter correct IC Number")]
    public string IC { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Invalid Phone Number. Must be 10 or 11 digits.")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(M|F)$", ErrorMessage = "Invalid Gender. Only 'male' or 'female' is allowed.")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long and less than 50 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    public IFormFile Photo { get; set; }

}

public class UpdateProfileVm
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Invalid Phone Number. Must be 10 or 11 digits.")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression(@"^(M|F)$", ErrorMessage = "Invalid Gender. Only 'male' or 'female' is allowed.")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "IC Number is required.")]
    [RegularExpression(@"^\d{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])\d{2}\d{4}$", ErrorMessage = "Invalid IC Number. Please enter correct IC Number")]
    public string IC { get; set; }

    public string? PhotoURL { get; set; }
    public string BirthDate { get; set; }

    public IFormFile? Photo { get; set; }

}

public class ChangePassword
{
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long and less than 50 characters.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long and less than 50 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
}

public class ResetPassword
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email {  get; set; }

    [BindProperty]
    public string? RecaptchaToken { get; set; }

}

//================================== USER Account End ===========================================================


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

public class CartVM
{
    public string Id { get; set; }
    public string TicketId { get; set; }
    public string UserId { get; set; }
    public int quantity { get; set; }
}


public class AdminTicketDetails
{
    public Attraction Attraction { get; set; }
    public List<Ticket> Tickets { get; set; }
}

public class ImageSet
{
    [Required(ErrorMessage = "Please select file.")]
    [Display(Name = "Browse File")]
    public List<IFormFile> images { get; set; }

    public List<string>? imagePaths { get; set; }

}



