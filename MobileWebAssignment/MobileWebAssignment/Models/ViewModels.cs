
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
}
