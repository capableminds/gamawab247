using System;
using System.Net;

namespace TransactionMonitoringService.Helpers.Exceptions
{
    public class HttpException : Exception
    {
        public override string Message { get; }
        public int StatusCode { get; set; }
        public HttpException(string message) : base(message)
        {
            Message = base.Message;
        }
        public HttpException(string message, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        public HttpException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}

