
using Microsoft.AspNetCore.Mvc;
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
    public string OperatingHours { get; set; }
    [StringLength(200)]
    public string ImagePath { get; set; }

    public string AttractionTypeId { get; set; }

    public IFormFile Photo { get; set; }

    public List<OperatingHour> OperatingHoursArray { get; set; }
}

public class OperatingHour
{
    public string Day { get; set; }
    public string Status { get; set; } 
    public TimeSpan? StartTime { get; set; } 
    public TimeSpan? EndTime { get; set; }   
}

public class RegisterVM
{
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string Name { get; set; }

    public string Email { get; set; }

    [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid IC Number. Must be 12 digits.")]
    public string IC { get; set; }

    public string PhoneNumber { get; set; }

    public string Gender { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    //[Display(Name = "Profile Photo")]
    //public IFormFile Photo { get; set; }

}