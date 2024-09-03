using BillProcessorAPI.Repositories.Implementations;
using BillProcessorAPI.Repositories.Interfaces;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using TransactionMonitoring.Helpers;
using TransactionMonitoring.Helpers.Https;

namespace TransactionMonitoring.ServiceExtension
{
    public static class ServiceCollections
    {
        public static void AddHttpClientInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IHttpService, HttpService>();

            services.AddHttpClient("GamaWabsAPI")
                    .AddPolicyHandler(GetPollyPolicy("GamaWabsAPI"));
        }

        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddOptions<MailSettings>().BindConfiguration(nameof(MailSettings));
            services.AddOptions<FlwaveOptions>().BindConfiguration(nameof(FlwaveOptions));
            services.AddScoped<IBillTransactionRepository, BillTransactionRespository>();
        }

        /// <summary>
        /// Polly configuration for resilient http calls
        /// </summary>
        /// <returns></returns>
        private static AsyncRetryPolicy<HttpResponseMessage> GetPollyPolicy(string name)
        {
            // Create the retry policy we want
            return HttpPolicyExtensions
                            .HandleTransientHttpError() // HttpRequestException, 5XX and 408
                            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                            onRetryAsync: (dr, ts) =>
                            {
                                Console.WriteLine($"Retrying call to api for service name {name}");
                                return Task.CompletedTask;
                            });

        }
    }
}
