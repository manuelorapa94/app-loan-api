using DocumentFormat.OpenXml.InkML;
using LoanEnquiryApi.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LoanEnquiryApi.Service
{
    public class SoraServices
    {
        private readonly DataContext _dataContext;

        public SoraServices(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IActionResult> GetSoraRateAsync()
        {
            try
            {
                HttpClient client = new();
                // Define the base URL of the API
                string baseUrl = "https://eservices.mas.gov.sg/apimg-gw/server/monthly_statistical_bulletin_non610mssql/domestic_interest_rates_daily/views/domestic_interest_rates_daily";

                // Define the API key
                string apiKey = "7bcc481a-dc63-4ae5-b687-3e2307e7a61d";

                DateTime currentDate = DateTime.Now;
                DateTime yesterday = GetPreviousWeekDays(currentDate);

                string formattedYesterday = yesterday.ToString("yyyy-MM-dd");

                string fullUrl = $"{baseUrl}?end_of_day={formattedYesterday}";

                client.DefaultRequestHeaders.Add("KeyId", apiKey);

                HttpResponseMessage response = await client.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();

                    using (JsonDocument document = JsonDocument.Parse(responseData))
                    {
                        JsonElement root = document.RootElement;
                        // Assuming there's only one element in the "elements" array
                        JsonElement element = root.GetProperty("elements")[0];
                        if ((element.TryGetProperty("comp_sora_1m", out JsonElement sora1mElement) && sora1mElement.TryGetDecimal(out decimal comp_sora_1m)) 
                            && element.TryGetProperty("comp_sora_3m", out JsonElement sora3mElement) && sora3mElement.TryGetDecimal(out decimal comp_sora_3m)
                            && element.TryGetProperty("comp_sora_6m", out JsonElement sora6mElement) && sora6mElement.TryGetDecimal(out decimal comp_sora_6m))
                        {
                            // Create a new instance of SoraRateEntity and set its properties
                            var soraRateEntity = new SoraRateEntity
                            {
                                Id = Guid.NewGuid(),
                                SoraRate1M = comp_sora_1m,
                                SoraRate3M = comp_sora_3m,
                                SoraRate6M = comp_sora_6m,
                                CreatedAt = DateTime.UtcNow
                            };

                            _dataContext.Add(soraRateEntity);
                            await _dataContext.SaveChangesAsync();

                            return new OkObjectResult(soraRateEntity);
                        }
                    }
                }

                return new OkObjectResult(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        static DateTime GetPreviousWeekDays(DateTime currentDate)
        {
            DateTime previousDay = currentDate.AddDays(-1);

            while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
            {
                previousDay = previousDay.AddDays(-1);
            }

            return previousDay;
        }
    }
}