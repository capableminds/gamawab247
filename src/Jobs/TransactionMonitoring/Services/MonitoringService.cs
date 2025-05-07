using BillProcessorAPI.Dtos.Flutterwave;
using BillProcessorAPI.Enums;
using BillProcessorAPI.Helpers.Flutterwave;
using BillProcessorAPI.Repositories.Interfaces;
using Domain.Exceptions;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using System.Net.Mail;
using System.Text;
using TransactionMonitoring.Helpers;
using TransactionMonitoring.Helpers.Https;
using TransactionMonitoring.Helpers.Https.Models;

namespace TransactionMonitoring.Service
{
    public partial class MonitoringService(IBillTransactionRepository billTransactionsRepo, IOptions<FlwaveOptions> options, IOptions<MailSettings> mailSettings, IHttpService httpService)
    {
        private readonly FlwaveOptions _options = options.Value;
        private readonly MailSettings _mailSettings = mailSettings.Value;


        public async Task SendMessage()
        {
            // Retrieve successful transactions with empty receipt url
            var billTransactionsWithoutReceipt = billTransactionsRepo
                .Query(x => x.Status == ETransactionStatus.Successful.ToString() && string.IsNullOrEmpty(x.ReceiptUrl)).OrderBy(x => x.CreatedAt).ToList();
            //Todo, subject to review
            //var billTransactionsWithCheckoutUrl = billTransactionsRepo.Query(x => !string.IsNullOrEmpty(x.PaymentUrl) && x.ReceiptUrl == null && x.Status.ToLower() == ETransactionStatus.Pending.ToString().ToLower());

            // iterate through the list and process message sending as below
            foreach (var transaction in billTransactionsWithoutReceipt)
            {
                var tranRef = transaction.TransactionReference;
                try
                {

                    // Call the flutter wave verify transaction endpoint.
                    var verificationResponse = await VerifyTransaction(tranRef);

                    // If transaction is successful 
                    if (verificationResponse.Status == "successful")
                    {
                        //if status is successful and receipt is null call the resend webhook endpoint
                        // this should return a webhook for the transaction that updates the bill transaction and triggers a broadcast
                        await ResendWebhook(verificationResponse.TransactionId);
                    }
                    else
                    {
                        // If transaction is unsuccessful or otherwise
                        transaction.Status = ETransactionStatus.RECONCILIATION_NEEDED.ToString();

                        //Send support mail
                        var getSupportTemplate = MailTemplatingHelper.GetSupportMailTemplate(_options.SupportMail,transaction.PayerName,"title");

                        await SendSingleMail(transaction.Email, getSupportTemplate,"Transaction reconciliation alert");
                        
                        
                        //Send mail to customer notifying them that transaction is being reconciled
                        var getCustomerMailTemplate = MailTemplatingHelper.GetCustomerMailTemplate(transaction.Email,transaction.PayerName,"title");
                        await SendSingleMail(transaction.Email, getSupportTemplate, "Mail to pacify customer");

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


            //// iterate through the list and process message sending as below
            //foreach (var transaction in billTransactionsWithCheckoutUrl)
            //{
            //    var tranRef = transaction.TransactionReference;
            //    try
            //    {
            //        var response = true;
            //        IDictionary<string, string> param = new Dictionary<string, string>();
            //        var url = options.Value.TransactionVerification.Replace("{tranRef}", tranRef);

            //        // Call the flutter wave verify transaction endpoint.
            //        var verificationResponse = await httpService.Get<bool>(url, null);

            //        // If transaction is unsuccessful 
            //        if (!verificationResponse.Data)
            //        {
            //            //transaction not completed
            //        }

            //        // If transaction is successful 
            //        if (verificationResponse.Data)
            //        {
            //            //update receipt url
            //            //if status is successful and receipt is null call the resend webhook endpoint

            //            // trigger broadcast
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        throw ex;
            //    }
            //    finally
            //    {
            //        await billTransactionsRepo.SaveChangesAsync();
            //    }

            }

        private async Task<SimpleTransactionVerificationResponse> VerifyTransaction(string transactionReference)
        {
            var response = new SimpleTransactionVerificationResponse();
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add(key: "Authorization", _options.SecretKey);

            var headerParam = new RequestHeader(param);
            var billTransactionRecord = await billTransactionsRepo.FirstOrDefault(x => x.TransactionReference == transactionReference);
            var url = $"{_options.BaseUrl}/{_options.VerifyByReference}/?tx_ref={transactionReference}";

            var transactionVerificationResponse = await httpService.Get<FlutterwaveResponse<SimpleFlutterwaveVerificationRes>>(url, headerParam);
            if (transactionVerificationResponse.Data.Status != "success")
                throw new BackgroundException("Unable to fetch transaction for this reference");

            if (transactionVerificationResponse.Data.Data.status != "successful"
                || transactionVerificationResponse.Data.Data.amount != billTransactionRecord.AmountPaid
                || transactionVerificationResponse.Data.Data.currency != "NGN")
            {
                response.Status = transactionVerificationResponse.Data.Status;
                response.StatusMessage = transactionVerificationResponse.Data.Message;
                
                //NOTE
                // the response will be like this when verification fails, just showing this for context should be deleted during your review
                    //{
                    //    "status": "error",
                    //    "message": "No transaction was found for this id",
                    //    "data": null
                    //}
            }
            else
            {
                //to be sure of the status when successful, we want to use the status in the transaction data returned which will be "success"
                response.Status = transactionVerificationResponse.Data.Data.status;
                response.StatusMessage = transactionVerificationResponse.Data.Message;
                response.TransactionId = transactionVerificationResponse.Data.Data.id;
            }

            return response;
        }

        public async Task<FailedWebhookResponseModel> ResendWebhook(int paymentRef)
        {

            var response = new FailedWebhookResponseModel();
            IDictionary<string, string> resendWehookHeader = new Dictionary<string, string>();
            resendWehookHeader.Add(key: "Authorization", _options.SecretKey);
            var headerParamm = new RequestHeader(resendWehookHeader);

            var url = $"{_options.BaseUrl}/{_options.ResendFailedWebhook}/{paymentRef}/resend-hook";

            try
            {
                var notificationResponse = await httpService
                       .Post<FailedWebhookResponseModel, int>(url, headerParamm, paymentRef);
                if (notificationResponse.Data.Status != "success")
                    throw new BackgroundException("Unable to resend webhook notification for the payment reference provided");

                response.Status = notificationResponse.Data.Status;
                response.Message = notificationResponse.Data.Message;
                response.Data = notificationResponse.Data.Data;

                return response;

            }
            catch (Exception ex)
            {
                throw new BackgroundException(ex.Message);
            }

        }

        public async Task<bool> SendSingleMail(string reciepientAddress, string message,
            string subject)
        {
            try
            {
                var x = Encoding.UTF8;
                {

                };
                var email = new MimeMessage()
                {

                    Subject = subject,
                    Body = new TextPart("html", message)
                };

                email.To.Add(MailboxAddress.Parse(reciepientAddress));
                email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));


                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    await client.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.SslOnConnect);

                    await client.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);

                    await client.SendAsync(email);

                    client.Disconnect(true);
                }
                return true;
            }
            catch (SmtpException)
            {
                throw;
            }
        }
    }
}
