using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.ComponentModel;
using System.Resources;

namespace WebAppPollyRetries
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //BrokenCircuitException: When the circuit is open, any call to WeatherApiClient will immediately throw a BrokenCircuitException
            //Combining Strategies: In production systems, Retry and Circuit Breaker are frequently used together(with the Retry nested inside the Circuit Breaker) so that transient blips get retried a couple of times, but persistent outages trigger the circuit breaker to protect the system.BrokenCircuitException: When the circuit is open, any call to WeatherApiClient will immediately throw a BrokenCircuitException without even attempting to call the network, saving resources.Combining Strategies: In production systems, Retry and Circuit Breaker
            //are frequently used together(with the Retry nested inside the Circuit Breaker).

            builder.Services.AddHttpClient<WeatherApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.weather.com");
            })
            .AddResilienceHandler("WeatherApiPolicy", resilienceBuilder =>
            {
                var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

                resilienceBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    // Break the circuit if 50% or more of the requests fail
                    FailureRatio = 0.5,

                    // The rolling time window to evaluate failures
                    SamplingDuration = TimeSpan.FromSeconds(30),

                    // Minimum number of requests required within the sampling window before the breaker can trip
                    MinimumThroughput = 4,

                    // How long the circuit stays open (blocking all calls) before attempting a test request (Half-Open state)
                    BreakDuration = TimeSpan.FromSeconds(30),

                    // Optional hooks for logging state changes
                    OnOpened = args =>
                    {
                        logger.LogWarning("[WeatherApi] Circuit breaker OPENED! Subordinate service is failing. Further requests will fail fast for {BreakDuration}s.", args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        logger.LogInformation("[WeatherApi] Circuit breaker CLOSED. Service has recovered.");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        logger.LogInformation("[WeatherApi] Circuit breaker HALF-OPEN. Testing downstream health with next request.");
                        return ValueTask.CompletedTask;
                    }
                });

                // 2. Add a Timeout strategy so a single hanging request doesn't block indefinitely
                resilienceBuilder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(5)
                });
            });

            builder.Services.AddControllers();
            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
