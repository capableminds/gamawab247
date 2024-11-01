﻿using AutoMapper;
using BillProcessorAPI.Dtos;
using BillProcessorAPI.Dtos.BroadcastMessage;
using BillProcessorAPI.Dtos.Common;
using BillProcessorAPI.Dtos.Flutterwave;
using BillProcessorAPI.Entities;
using BillProcessorAPI.Enums;
using BillProcessorAPI.Helpers;
using BillProcessorAPI.Helpers.BroadcastMessage;
using BillProcessorAPI.Helpers.Flutterwave;
using BillProcessorAPI.Repositories.Interfaces;
using BillProcessorAPI.Services.Interfaces;
using Domain.Common;
using Domain.Exceptions;
using Infrastructure.Http;
using Infrastructure.ShortLink;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net;

namespace BillProcessorAPI.Services.Implementations
{
    public class FlutterwaveService : IFlutterwaveService
    {
        private readonly IRepository<BillTransaction> _billTransactionsRepo;

        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IRepository<BillPayerInfo> _billPayerRepository;
        private readonly IRepository<WebhookNotification> _oldAppWebhook;
        private readonly ICutlyService _cutlyService;

        private readonly FlutterwaveOptions _flutterOptions;
        private readonly IHttpService _httpService;
        private readonly IConfigurationService _configService;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _context;
        private ILogger<FlutterwaveService> _logger;
        private readonly BusinessesPhoneNumber _phoneNumberOptions;
        private readonly ReceiptBroadcastConfig _receiptBroadcastOptions;

        public FlutterwaveService(
            IRepository<BillTransaction> billTransactionsRepo,
            IOptions<BillTransactionSettings> settings,
            IRepository<BillPayerInfo> billPayerRepository,
            IOptions<FlutterwaveOptions> flutterOptions,
            IHttpService httpService,
            IConfigurationService configService,
            IInvoiceRepository invoiceRepo,
            IMapper mapper,
            ILogger<FlutterwaveService> logger,
            IHttpContextAccessor context,
            IRepository<WebhookNotification> oldAppWebhook,
            IOptions<BusinessesPhoneNumber> phoneNumberOptions,
            IOptions<ReceiptBroadcastConfig> receiptBroadcastOptions,
            ICutlyService cutlyService)
        {
            _billTransactionsRepo = billTransactionsRepo;
            _billPayerRepository = billPayerRepository;
            _flutterOptions = flutterOptions.Value;
            _httpService = httpService;
            _configService = configService;
            _invoiceRepo = invoiceRepo;
            _mapper = mapper;
            _logger = logger;
            _context = context;
            _oldAppWebhook = oldAppWebhook;
            _phoneNumberOptions = phoneNumberOptions.Value;
            _receiptBroadcastOptions = receiptBroadcastOptions.Value;
            _cutlyService = cutlyService;
        }

        public async Task<SuccessResponse<PaymentCreationResponse>> CreateTransaction(string email, decimal amount, string billPaymentCode)
        {
            if (_flutterOptions == null)
                throw new RestException(HttpStatusCode.BadRequest, "please update your application setting file");

            if (amount < _flutterOptions.MinimumPayableAmount)
            {
                throw new RestException(HttpStatusCode.BadRequest, $"The minimum amount payable is {_flutterOptions.MinimumPayableAmount}");
            }
            if (string.IsNullOrEmpty(email) || amount < 0)
                throw new RestException(HttpStatusCode.BadRequest, "All fields are required");

            var billPayer = await _billPayerRepository.Query(x => x.billCode == billPaymentCode).OrderByDescending(c => c.UpdatedAt).FirstOrDefaultAsync()
                        ?? throw new RestException(HttpStatusCode.NotFound, "unable to fetch bill payer for this transaction");

            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add(key: "Authorization", _flutterOptions.SecretKey);
            var headerParam = new RequestHeader(param);
            var trxReference = FlutterConfig.PaymentReference();

            var flutterwaveRequestPayload = new FCreateTransactionRequestDto
            {
                Amount = amount,
                Tx_ref = trxReference,
                Currency = "NGN",
                Redirect_url = _flutterOptions.RedirectUrl,
                Customer = new CustomerDto
                {
                    Email = email,
                },
                Meta = new MetaDto
                {
                    Webguid = billPaymentCode,
                    Creditaccount = billPayer.CreditAccount
                }
            };

            try
            {
                var url = $"{_flutterOptions.BaseUrl}/{_flutterOptions.CreateTransaction}";

                var paymentCreationResponse = await _httpService
                    .Post<FlutterwaveResponse<LinkData>, FCreateTransactionRequestDto>(url, headerParam, flutterwaveRequestPayload);
                if (paymentCreationResponse.Data.Status != "success")
                    throw new RestException(HttpStatusCode.BadRequest, "An error occured while processing your request");

                var charge = new ChargesInputDto
                {
                    Amount = amount,
                    Channel = "Flutterwave"
                };

                var billTransaction = new BillTransaction
                {

                    GatewayType = EGatewayType.Flutterwave,
                    Status = ETransactionStatus.Pending.ToString(),
                    BillPayerInfoId = billPayer.Id,
                    PayerName = billPayer.PayerName,
                    BillNumber = billPayer.billCode,
                    Pid = billPayer.Pid,
                    RevName = billPayer.RevName,
                    PhoneNumber = billPayer.PhoneNumber,
                    DueDate = billPayer.AcctCloseDate,
                    TransactionReference = trxReference,
                    TransactionCharge = _configService.CalculateBillChargesOnAmount(charge).Data.AmountCharge,
                    AmountDue = billPayer.AmountDue,
                    AmountPaid = amount,
                    PaymentUrl = paymentCreationResponse.Data.Data.Link,
                    PaymentInfoResponseData = JsonConvert.SerializeObject(paymentCreationResponse.Data),
                    PaymentInfoRequestData = JsonConvert.SerializeObject(flutterwaveRequestPayload)
                };

                await _billTransactionsRepo.AddAsync(billTransaction);
                await _billTransactionsRepo.SaveChangesAsync();


                var billInvoice = _mapper.Map<Invoice>(billPayer);
                billInvoice.BillTransactionId = billTransaction.Id;
                billInvoice.TransactionReference = trxReference;
                billInvoice.DueDate = billPayer.AcctCloseDate;
                billInvoice.BillNumber = billPaymentCode;
                billInvoice.GatewayType = EGatewayType.Flutterwave;
                billInvoice.PhoneNumber = billPayer.PhoneNumber;
                billInvoice.TransactionCharge = _configService.CalculateBillChargesOnAmount(charge).Data.AmountCharge;


                await _invoiceRepo.AddAsync(billInvoice);
                await _invoiceRepo.SaveChangesAsync();

                var response = new PaymentCreationResponse
                {
                    SystemCharge = billInvoice.TransactionCharge,
                    PayLink = paymentCreationResponse.Data.Data.Link,
                    Status = paymentCreationResponse.Data.Status,
                };

                return new SuccessResponse<PaymentCreationResponse>
                {
                    Data = response,
                    Message = "Payment created successfully",
                };
            }
            catch (HttpException e)
            {
                throw new RestException((HttpStatusCode)e.StatusCode, e.Message);
            }
            catch (Exception ex)
            {
                throw new RestException(HttpStatusCode.InternalServerError, ex.Message);
            }


        }

        public async Task<SuccessResponse<string>> PaymentNotification(WebHookNotificationWrapper model)
        {

            BillTransaction transaction = null;
            try
            {
                if (model == null)
                    throw new RestException(HttpStatusCode.BadRequest, "invalid transaction, notification content is null and empty");

                transaction = await _billTransactionsRepo.FirstOrDefault(x => x.TransactionReference == model.TransactionReference);


                _logger.LogCritical($"Payment notification from Flutterwave just came in as at: {DateTime.UtcNow}");

                _logger.LogCritical($"Details of notification : {JsonConvert.SerializeObject(model)}");


                if (transaction is null)
                {
                    var webhook = model.ToWebHook();
                    webhook.Data = JsonConvert.SerializeObject(model);
                    webhook.GatewayType = "Flutterwave";
                    webhook.Remark = "This webhook transaction is not found on the billTransaction";
                    //saving the webhook to the database since no transaction was retrieved for the webhook to update
                    await _oldAppWebhook.AddAsync(webhook);
                    await _oldAppWebhook.SaveChangesAsync();
                    return new SuccessResponse<string>
                    {
                        Data = "Transaction Completed"
                    };
                }
                _logger.LogInformation($"No transaction was found for the webhok received, webhook saved to the database");

                //verify transaction with flutterwave using the transactionId from the webhook
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add(key: "Authorization", _flutterOptions.SecretKey);
                var headerParam = new RequestHeader(param);

                var url = $"{_flutterOptions.BaseUrl}/{_flutterOptions.VerifyByReference}/?tx_ref={model.TransactionReference}";
                var verificationReaponse = await _httpService.Get<FlutterwaveResponse<FlutterwaveResponseData>>(url, headerParam);

                if (verificationReaponse.Data.Status != "success")
                    _logger.LogCritical($"Unable to verify payment notification from Flutterwave as at: {DateTime.UtcNow}");

                transaction.AmountPaid = verificationReaponse.Data.Data.amount;
                transaction.PrinciPalAmount = verificationReaponse.Data.Data.amount; // amount paid minus charges
                transaction.Channel = verificationReaponse.Data.Data.payment_type;
                transaction.TransactionCharge = transaction.TransactionCharge;
                transaction.GatewayTransactionCharge = (decimal)verificationReaponse.Data.Data.app_fee;
                transaction.GatewayTransactionReference = verificationReaponse.Data.Data.flw_ref;
                transaction.PaymentReference = model.PaymentRef.ToString();
                transaction.FiName = "N/A";
                transaction.Narration = verificationReaponse.Data.Data.narration;

                if (verificationReaponse?.Data?.Status?.ToUpper()
                    == "SUCCESS")
                {
                    transaction.Status = ETransactionStatus.Successful.ToString();
                }
                else
                {
                    transaction.Status = ETransactionStatus.Failed.ToString();
                }
                
                transaction.DateCompleted = verificationReaponse.Data.Data.created_at.ToString();
                transaction.StatusMessage = verificationReaponse.Data.Data.status;
                transaction.ReceiptUrl = model.ReceiptNumber;
                transaction.SuccessIndicator = verificationReaponse.Data.Data.status;
                transaction.Hash = "N/A";
                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.NotificationResponseData = JsonConvert.SerializeObject(model);
                transaction.Email = verificationReaponse.Data.Data.customer.email;

                await _billTransactionsRepo.SaveChangesAsync();


                //Send customer receipt
                await ReceiptBroadcast.SendReceipt(transaction,_phoneNumberOptions,_cutlyService,
                    _receiptBroadcastOptions,_httpService);

                //add the receipt to the invoice
                var invoice = await _invoiceRepo.FirstOrDefault(x => x.BillTransactionId == transaction.Id);
                if (invoice is null)
                    throw new PaymentVerificationException(HttpStatusCode.NotFound, "No invoice found for this transaction");

                invoice.ReceiptUrl = model.ReceiptNumber;
                invoice.AmountPaid = verificationReaponse.Data.Data.amount;
                invoice.AmountDue = transaction.AmountDue;
                invoice.GatewayTransactionCharge = (decimal)verificationReaponse.Data.Data.app_fee;
                invoice.UpdatedAt = DateTime.UtcNow;
                invoice.GatewayTransactionReference = verificationReaponse.Data.Data.flw_ref;

                // Create a receipt record
                //var receipt = _mapper.Map<Receipt>(transaction);
                //receipt.TransactionId = transaction.Id;
                //receipt.PaymentRef = transaction.TransactionReference;
                //receipt.InvoiceId = invoice.Id;
                //receipt.TransactionDate = transaction.DateCompleted;
                //receipt.GateWay = transaction.GatewayType.ToString();
                //receipt.ReceiptUrl = transaction.ReceiptUrl;

                //await _receipts.AddAsync(receipt);
                //await _receipts.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred on receipt of payment notification from flutterwave: {ex.Message}", ex);

                transaction.ErrorMessage = ex.ToString();
                throw;
            }
            finally
            {
                if (transaction is not null)
                    await _billTransactionsRepo.SaveChangesAsync();
            }


            return new SuccessResponse<string>
            {
                Data = "Transaction Completed"
            };
        }

        public async Task<SuccessResponse<PaymentConfirmationResponse>> PaymentConfirmation(string status, string tx_ref, string transaction_id)
        {
            var trxStatus = new SuccessResponse<string>();
            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(tx_ref))
                throw new RestException(HttpStatusCode.BadGateway, "bad request, status param and transaction reference cannot be null");

            var invoiceResponse = new SuccessResponse<PaymentConfirmationResponse>();

            await Task.Delay(2000);

            try
            {
                var billTransaction = await _billTransactionsRepo.FirstOrDefault(x => x.TransactionReference == tx_ref);
                if (billTransaction == null)
                    throw new RestException(HttpStatusCode.NotFound, "Unable to fetch transaction: transaction failed");

                // check the transaction for the bill
                // if it's pending dont perform verification..
                if (billTransaction.Status.Equals(ETransactionStatus.Pending.ToString())
                    || billTransaction.Status.Equals(ETransactionStatus.Created.ToString()))
                {
                    var response = _mapper.Map<PaymentConfirmationResponse>(billTransaction);

                    return new SuccessResponse<PaymentConfirmationResponse>
                    {
                        Data = response,
                        Success = false,
                        Message = "Transaction not completed",
                    };
                }

                var verifyPayment = await VerifyTransaction(tx_ref);
                if (!verifyPayment)
                {
                    invoiceResponse.Success = false;
                    invoiceResponse.Message = "Unable to fetch transaction: transaction failed";
                    invoiceResponse.Data = null;
                    return invoiceResponse;
                }

                var invoice = await _invoiceRepo.Query(x => x.BillTransactionId == billTransaction.Id)
                    .Include(x => x.Receipts).FirstOrDefaultAsync();
                if (invoice is null)
                    throw new RestException(HttpStatusCode.NotFound, "Unable to retrieve invoice for this transaction");

                var invoiceDto = _mapper.Map<PaymentConfirmationResponse>(invoice);
                invoiceDto.DateCompleted = billTransaction.DateCompleted;

                invoiceResponse.Data = invoiceDto;
                invoiceResponse.Success = true;
                invoiceResponse.Message = "Transaction Successful";

                return invoiceResponse;
            }
            catch (Exception ex)
            {

                throw new RestException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public async Task<bool> VerifyTransaction(string transactionReference)
        {
            var response = false;
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add(key: "Authorization", _flutterOptions.SecretKey);

            var headerParam = new RequestHeader(param);
            var billTransationRecord = await _billTransactionsRepo.FirstOrDefault(x => x.TransactionReference == transactionReference);
            var url = $"{_flutterOptions.BaseUrl}/{_flutterOptions.VerifyByReference}/?tx_ref={transactionReference}";

            var transaction = await _httpService.Get<FlutterwaveResponse<FlutterwaveResponseData>>(url, headerParam);
            if (transaction.Data.Status != "success")
                throw new RestException(HttpStatusCode.BadRequest, "Unable to fetch transaction for this reference");

            if (transaction.Data.Data.status != "successful"
                || transaction.Data.Data.amount != billTransationRecord.AmountPaid
                || transaction.Data.Data.currency != "NGN")
            {
                return response;
            }

            return response = true;
        }

        public async Task<FailedWebhookResponseModel> ResendWebhook(FailedWebhookRequest model)
        {
            var request = _context.HttpContext.Request;
            if (!request.Headers.ContainsKey(_flutterOptions.ResendWebhookHeader)
                || request.Headers[_flutterOptions.ResendWebhookHeader] != _flutterOptions.ResendWebhookHeaderValue)
            {
                throw new RestException(HttpStatusCode.Unauthorized, "Authorization failed");
            }

            var response = new FailedWebhookResponseModel();
            IDictionary<string, string> resendWehookUrl = new Dictionary<string, string>();
            resendWehookUrl.Add(key: "Authorization", _flutterOptions.SecretKey);
            var headerParamm = new RequestHeader(resendWehookUrl);

            //var billTransationRecord = await _billTransactionsRepo.FirstOrDefault(x => x.PaymentReference == model.PaymentReference.ToString());
            var url = $"{_flutterOptions.BaseUrl}/{_flutterOptions.ResendFailedWebhook}/{model.PaymentReference}/resend-hook";

            try
            {
                var notificationResponse = await _httpService
                       .Post<FailedWebhookResponseModel, FailedWebhookRequest>(url, headerParamm, model);
                if (notificationResponse.Data.Status != "success")
                    throw new RestException(HttpStatusCode.BadRequest, "Unable to resend webhook notification for the payment reference provided");

                response.Status = notificationResponse.Data.Status;
                response.Message = notificationResponse.Data.Message;
                response.Data = notificationResponse.Data.Data;

                return response;

            }
            catch (Exception ex)
            {

                throw new RestException(HttpStatusCode.InternalServerError, ex.Message);
            }

        }

    }
}
