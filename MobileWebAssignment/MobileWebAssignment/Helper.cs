using MobileWebAssignment.Models;

﻿using Microsoft.AspNetCore.Identity;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MobileWebAssignment;

public class Helper
{
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;

    public Helper(IWebHostEnvironment en, IHttpContextAccessor ct)
    {
        this.en = en;
        this.ct = ct;
    }

    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(800, 700),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }


    public List<OperatingHour> ParseOperatingHours(string operatingHoursText)
    {
        // Initialize the list to hold the operating hours
        List<OperatingHour> operatingHoursList = new List<OperatingHour>();

        // Split the input string by '|' to separate each day and status
        string[] daysStatus = operatingHoursText.Split('|');

        // Iterate through each day and status pair
        foreach (var dayStatus in daysStatus)
        {
            // Trim the leading and trailing spaces
            var dayStatusTrimmed = dayStatus.Trim();

            // Split the "Day Status" pair by spaces (splitting only the first two parts)
            var parts = dayStatusTrimmed.Split(new[] { ' ' }, 3);

            if (parts.Length >= 2)
            {
                var day = parts[0];   // First part is the day (e.g., "Monday")
                var status = parts[1].Trim();  // The second part is the status (e.g., "open" or "closed")

                // Initialize StartTime and EndTime as null
                TimeSpan? startTime = null;
                TimeSpan? endTime = null;

                // Handle the "open" status by parsing the times if available
                if (status.Equals("open", StringComparison.OrdinalIgnoreCase) && parts.Length == 3)
                {
                    var timeRange = parts[2].Trim();
                    var timeParts = timeRange.Split(' '); // Split the time range like "22:58:00 13:02:00"

                    if (timeParts.Length == 2)
                    {
                        startTime = ParseTime(timeParts[0].Trim());  // First part is start time
                        endTime = ParseTime(timeParts[1].Trim());    // Second part is end time
                    }
                }

                // If status is not "open", we assume it's "closed" and set times to null
                if (status.Equals("closed", StringComparison.OrdinalIgnoreCase))
                {
                    startTime = null;
                    endTime = null;
                }

                // Create an OperatingHour object and add it to the list
                var operatingHour = new OperatingHour
                {
                    Day = day,
                    Status = status,
                    StartTime = startTime,
                    EndTime = endTime
                };

                operatingHoursList.Add(operatingHour);
            }
        }

        // Now the list has been populated, assign it to the model property
        var model = new AttractionInsertVM
        {
            operatingHours = operatingHoursList
        };

        return model.operatingHours;
    }


    public List<OperatingTime> ConvertOperatingTimes(string operatingHoursText)
    {
        // Initialize the list to hold the operating hours
        List<OperatingTime> operatingHoursList = new List<OperatingTime>();

        // Split the input string by '|' to separate each day and status
        string[] daysStatus = operatingHoursText.Split('|');

        // Iterate through each day and status pair
        foreach (var dayStatus in daysStatus)
        {
            // Trim the leading and trailing spaces
            var dayStatusTrimmed = dayStatus.Trim();

            // Split the "Day Status" pair by spaces (splitting only the first two parts)
            var parts = dayStatusTrimmed.Split(new[] { ' ' }, 3);

            if (parts.Length >= 2)
            {
                var day = parts[0];   // First part is the day (e.g., "Monday")
                var status = parts[1].Trim();  // The second part is the status (e.g., "open" or "closed")

                // Initialize StartTime and EndTime as null
                string? startTime = null;
                string? endTime = null;

                // Handle the "open" status by parsing the times if available
                if (status.Equals("open", StringComparison.OrdinalIgnoreCase) && parts.Length == 3)
                {
                    var timeRange = parts[2].Trim();
                    var timeParts = timeRange.Split(' '); // Split the time range like "22:58:00 13:02:00"

                    if (timeParts.Length == 2)
                    {
                        startTime = ConvertToAmPmFormat(timeParts[0].Trim());  // First part is start time
                        endTime = ConvertToAmPmFormat(timeParts[1].Trim());    // Second part is end time
                    }
                }

                // If status is not "open", we assume it's "closed" and set times to null
                if (status.Equals("closed", StringComparison.OrdinalIgnoreCase))
                {
                    startTime = null;
                    endTime = null;
                }

                // Create an OperatingHour object and add it to the list
                var operatingTime = new OperatingTime
                {
                    Day = day,
                    Status = status,
                    StartTime = startTime,
                    EndTime = endTime
                };

                operatingHoursList.Add(operatingTime);
            }
        }

        // Now the list has been populated, assign it to the model property
        var model = new AttractionUpdateVM
        {
            operatingTimes = operatingHoursList,
        };

        return model.operatingTimes;
    }

    // Helper method to parse time string into TimeSpan ()
    private TimeSpan? ParseTime(string time)
    {
        // Try to parse time in the format "hh:mm:ss" or "h:mm:ss"
        TimeSpan parsedTime;
        if (TimeSpan.TryParseExact(time, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out parsedTime))
        {
            return parsedTime;
        }
        return null; // Return null if parsing fails
    }


    //Helper method transfer time span to am/pm format
    private string ConvertToAmPmFormat(string time)
    {
        try
        {
            // Parse the time string in "HH:mm:ss" (24-hour format)
            DateTime parsedTime = DateTime.ParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture);

            // Format the parsed time into "h:mm tt" (12-hour format with AM/PM)
            return parsedTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return null; // Return null if parsing fails
        }
    }


    public Comment ConvertComment(string comment)
    {
        //create a new comment to store the converted comment
        Comment commentDetails = new Comment();

        //trim the blank space
        comment = comment.Trim();

        //retieve part by part
        var parts = comment.Split('|');

        commentDetails.Title = parts[0];
        commentDetails.Reason = parts[1];
        commentDetails.Partner = parts[2];
        commentDetails.Review = parts[3];

        return commentDetails;
    }

    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();
    public string HashPassword(string password)
    {

        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;
    }


}