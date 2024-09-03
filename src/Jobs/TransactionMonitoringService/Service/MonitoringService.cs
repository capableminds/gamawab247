using BillProcessorAPI.Enums;
using BillProcessorAPI.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using TransactionMonitoringService.Helpers;
using TransactionMonitoringService.Helpers.Https;

namespace TransactionMonitoringService.Service
{
    public class MonitoringService(IBillTransactionRepository billTransactionsRepo, IOptions<FlwaveOptions> options, IHttpService httpService)
    {
        private readonly FlwaveOptions _options = options.Value;

        public async Task SendMessage()
        {
            // Retrieve successful transactions with empty receipt url
            var billTransactionsWithoutReceipt = billTransactionsRepo
                .Query(x => x.Status == ETransactionStatus.Successful.ToString() && string.IsNullOrEmpty(x.ReceiptUrl)).OrderBy(x => x.CreatedAt).ToList();

            var billTransactionsWithCheckoutUrl = billTransactionsRepo.Query(x => !string.IsNullOrEmpty(x.PaymentUrl) && x.ReceiptUrl == null && x.Status.ToLower() == ETransactionStatus.Pending.ToString().ToLower());

            // iterate through the list and process message sending as below
            foreach (var transaction in billTransactionsWithoutReceipt)
            {
                var tranRef = transaction.TransactionReference;
                try
                {
                    var response = true;
                    IDictionary<string, string> param = new Dictionary<string, string>();
                    var url = _options.TransactionVerification.Replace("{tranRef}",tranRef);

                    // Call the flutter wave verify transaction endpoint.
                    var verificationResponse = await httpService.Get<bool>(url, null);

                    // If transaction is unsuccessful 
                    if (!verificationResponse.Data)
                    {
                        transaction.Status = ETransactionStatus.RECONCILIATION_NEEDED.ToString();

                        //Send support mail
                        var getSupportTemplate = MailTemplatingHelper.GetSupportMailTemplate(_options.SupportMail,transaction.PayerName,"title");

                        //Send mail to customer notifying them that transaction is being reconciled
                        var getCustomerMailTemplate = MailTemplatingHelper.GetCustomerMailTemplate(transaction.Email,transaction.PayerName,"title");
                    }

                    // If transaction is successful 
                    if (verificationResponse.Data)
                    {
                        //update receipt url
                        //update value on receipt

                        //if status is successful and receipt is null call the resend webhook endpoint
                        // trigger broadcast
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    await billTransactionsRepo.SaveChangesAsync();
                }

            }


            // iterate through the list and process message sending as below
            foreach (var transaction in billTransactionsWithCheckoutUrl)
            {
                var tranRef = transaction.TransactionReference;
                try
                {
                    var response = true;
                    IDictionary<string, string> param = new Dictionary<string, string>();
                    var url = options.Value.TransactionVerification.Replace("{tranRef}", tranRef);

                    // Call the flutter wave verify transaction endpoint.
                    var verificationResponse = await httpService.Get<bool>(url, null);

                    // If transaction is unsuccessful 
                    if (!verificationResponse.Data)
                    {
                        //transaction not completed
                    }

                    // If transaction is successful 
                    if (verificationResponse.Data)
                    {
                        //update receipt url
                        //if status is successful and receipt is null call the resend webhook endpoint

                        // trigger broadcast
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    await billTransactionsRepo.SaveChangesAsync();
                }

            }
        }
    }
}
