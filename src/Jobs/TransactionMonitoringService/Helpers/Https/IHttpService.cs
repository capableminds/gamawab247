using TransactionMonitoringService.Helpers.Https.Models;

namespace TransactionMonitoringService.Helpers.Https
{
    public interface IHttpService
    {
        Task<HttpMessageResponse<TResponse>> Get<TResponse>(string url, RequestHeader header, IDictionary<string, object> parameters = null);
        Task<HttpMessageResponse<TResponse>> Post<TResponse, TRequest>(string fullUrl, RequestHeader header, TRequest request);
    }
}
