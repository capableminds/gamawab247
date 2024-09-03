namespace TransactionMonitoringService.Helpers.Exceptions
{
    public class BadRequestException : HttpException
    {
        public BadRequestException(String message, Exception innerException = null) : base(message, innerException)
        {
            if (String.IsNullOrEmpty(message))
                message = "Bad request error, Verify request data and try again";

            base.StatusCode = 400;
        }

        public BadRequestException(String message, int statusErrorCode, Exception innerException = null) : base(message, innerException)
        {
            if (String.IsNullOrEmpty(message))
                message = "Bad request, Verify request data and try again";

            if (statusErrorCode == 0)
                this.StatusCode = 400;
            else
                this.StatusCode = statusErrorCode;

        }
    }
}

