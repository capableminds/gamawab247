namespace TransactionMonitoring.Helpers
{
    public static class MailTemplatingHelper
    {
        public static string GetSupportMailTemplate(string email, string firstName, string title)
        {
            string body;
            var folderName = Path.Combine("wwwroot", "SupportMailTemplate.html");
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return null;

            string msgBody = body.
                Replace("{email}", email)
                .Replace("{first_name}", firstName)
                .Replace("{title}", title);

            return msgBody;
        }

        public static string GetCustomerMailTemplate(string email, string firstName, string title)
        {
            string body;
            var folderName = Path.Combine("wwwroot", "SupportMailTemplate.html");
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return null;

            string msgBody = body.
                Replace("{email}", email)
                .Replace("{first_name}", firstName)
                .Replace("{title}", title);

            return msgBody;
        }
    }
}
