using System.Text.Json.Nodes;
namespace MobileWebAssignment.Service; 
public class RecaptchaService
{
    public static async Task<bool> verifyRecaptchaV2(string response, string secret)
    {
        try
        {
            using (var client = new HttpClient())
            {
                string url = "https://www.google.com/recaptcha/api/siteverify";
                MultipartFormDataContent content = new();
                content.Add(new StringContent(response), "response");
                content.Add(new StringContent(secret), "secret");

                var result = await client.PostAsync(url, content);

                if (result.IsSuccessStatusCode)
                {
                    var strResponse = await result.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);

                    if (jsonResponse != null)
                    {
                        var success = (bool?)jsonResponse["success"];
                        return success == true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            Console.WriteLine($"reCAPTCHA validation failed: {ex.Message}");
        }
        return false;
    }
}
