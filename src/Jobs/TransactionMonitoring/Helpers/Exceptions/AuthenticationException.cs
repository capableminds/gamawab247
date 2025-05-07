using System;

namespace TransactionMonitoring.Helpers.Exceptions
{
    public class AuthenticationException : HttpException
    {
        
        public AuthenticationException(string message) : base(message)
        {
            StatusCode = 401;
        }

        public AuthenticationException(string message, int statusCode) : base(message, statusCode)
        {
        }

        public AuthenticationException(string message,
            Exception innerException = null): base(message, innerException)
        {
            StatusCode = 401;
        }
    }
}

