using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/exchange-rate")]
public class ExchangeRateController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetExchangeRate(string currency)
    {
        string baseCurrency = "MYR"; // Base currency for your prices
        string apiKey = "9092d9fe4856c5ed427dd5d5"; // Replace with your API key
        string apiUrl = $"https://api.exchangerate-api.com/v4/latest/{baseCurrency}";

        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(json);
            if (data["rates"][currency] != null)
            {
                double rate = (double)data["rates"][currency];
                return Ok(new { rate });
            }
            return BadRequest("Currency not supported.");
        }

        return StatusCode((int)response.StatusCode, "Failed to fetch exchange rate.");
    }
}
