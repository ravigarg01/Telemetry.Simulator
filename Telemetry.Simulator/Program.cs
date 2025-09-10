using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// You'll need to copy the TelemetryData class into this project as well,
// or create a shared class library. For now, copying is fine.
public class TelemetryData
{
    public string DeviceId { get; set; }
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public DateTime Timestamp { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting high-concurrency ship telemetry simulator...");
        var apiUrl = "http://localhost:5172/telemetry/ingest"; // Change port if yours is different
        int shipCount = 2; // Number of ships/devices to simulate
        int minDelayMs = 200; // Minimum delay between sends (ms)
        int maxDelayMs = 2000; // Maximum delay between sends (ms)
        var random = new Random();

        var tasks = new Task[shipCount];
        for (int i = 0; i < shipCount; i++)
        {
            var deviceId = $"ship-{i + 1}-{Guid.NewGuid()}";
            tasks[i] = Task.Run(async () =>
            {
                using var httpClient = new HttpClient();
                while (true)
                {
                    var telemetry = new TelemetryData
                    {
                        DeviceId = deviceId,
                        Temperature = 20 + random.NextDouble() * 10,
                        Pressure = 1000 + random.NextDouble() * 20,
                        Timestamp = DateTime.UtcNow
                    };

                    // Simulate weak connectivity: random delay, random drop
                    int delay = random.Next(minDelayMs, maxDelayMs);
                    await Task.Delay(delay);

                    // Simulate random network failure (10% chance)
                    if (random.NextDouble() < 0.1)
                    {
                        Console.WriteLine($"[WARN] Ship {deviceId} failed to send due to weak connectivity.");
                        continue;
                    }

                    try
                    {
                        var response = await httpClient.PostAsJsonAsync(apiUrl, telemetry);
                        response.EnsureSuccessStatusCode();
                        Console.WriteLine($"[OK] {deviceId}: Temp={telemetry.Temperature:F2}, Pressure={telemetry.Pressure:F2}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERR] {deviceId}: {ex.Message}");
                    }
                }
            });
        }

        await Task.WhenAll(tasks); // Wait for all tasks (will run forever)
    }
}