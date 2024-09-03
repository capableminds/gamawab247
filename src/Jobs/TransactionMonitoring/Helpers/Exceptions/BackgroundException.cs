namespace TransactionMonitoring.Helpers.Exceptions
{
    public class BackgroundException: Exception
	{
		public BackgroundException(string message, Exception innerException = null): base(message, innerException)
		{
		}
	}
}

