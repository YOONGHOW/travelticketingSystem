
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






