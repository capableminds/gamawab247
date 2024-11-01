﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Infrastructure.Sessions;
using Infrastructure.Cache;
using Application.Exceptions;
using Application.Helpers;
using Application.Services.Implementations.FormProcessing;
using Application.Services.Interfaces;
using Application.Services.Interfaces.FormProcessing;
using Domain.Entities;
using Domain.Entities.DialogMessageEntitties;
using Domain.Entities.FormProcessing;
using Domain.Entities.FormProcessing.ValueObjects;
using Domain.Enums;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using ApiCustomization.Common;
using Newtonsoft.Json;

namespace Application.Services.Cron
{
    public class FormProcessorCron : IFormProcessorCron
    {
        private readonly IFormRequestResponseService _formRequesResponseService;
        private readonly IRepository<BusinessForm> _businessFormRepo;
        private readonly IRepository<BusinessMessage> _businessMessageRepo;
        private readonly ISessionManagement _sessionManagement;
        private readonly ILogger<FormProcessorCron> _logger;
        private readonly IOutboundMesageService _outboundMesageService;
        private readonly IDuplicateFIlterHelper _duplicateFIlterHelper;
        private readonly IFormConclusionMgtService _formConclusionMgtService;
        private readonly IApiContentIntegrationManager apiContentIntegrationFactory;
        private readonly IRepository<InboundMessage> inboundMessageRepo;
        private readonly IRepository<WhatsappUser> _whatsappUserRepo;
        private readonly IRepository<MessageLog> _messageLogRepo;

        public FormProcessorCron(IFormRequestResponseService formRequesResponseService,
           IRepository<BusinessForm> businessFormRepo,
           IApiContentIntegrationManager apiContentIntegrationFactory,
           ISessionManagement sessionManagement,
           ILogger<FormProcessorCron> logger,
           IRepository<BusinessMessage> businessMessageRepo,
           IOutboundMesageService outboundMesageService,
           IDuplicateFIlterHelper duplicateFIlterHelper,
           IFormConclusionMgtService formConclusionMgtService,
           IRepository<InboundMessage> inboundMessageRepo,
           IRepository<WhatsappUser> whatsappUserRepo,
           IRepository<MessageLog> messageLogRepo)
        {
            _formRequesResponseService = formRequesResponseService;
            _businessFormRepo = businessFormRepo;
            _sessionManagement = sessionManagement;
            _formConclusionMgtService = formConclusionMgtService;
            _logger = logger;
            _outboundMesageService = outboundMesageService;
            _duplicateFIlterHelper = duplicateFIlterHelper;
            _businessMessageRepo = businessMessageRepo;
            this.apiContentIntegrationFactory = apiContentIntegrationFactory;
            this.inboundMessageRepo = inboundMessageRepo;
            _whatsappUserRepo = whatsappUserRepo;
            _messageLogRepo = messageLogRepo;
        }

        public async Task DoWork()
        {
            try
            {
                // get the list of form request that are pending and are within the hour
                var pending = EResponseProcessingStatus.Pending.ToString();
                Expression<Func<FormRequestResponse, bool>> func = (x => x.Status == pending
                && x.Direction == EMessageDirection.Outbound.ToString());

                var formRequests = _formRequesResponseService.GetAll(func, 0, 20, true);

                if (formRequests.Any())
                {
                    foreach (var item in formRequests)
                    {
                        item.Status = EResponseProcessingStatus.Pending.ToString();
                        item.UpdatedAt = DateTime.UtcNow;
                        await _formRequesResponseService.Update(item);
                        DialogSession dialogSession = await _sessionManagement.GetByWaId(item.To.Trim());
                        FormElement newCurrentFormElement = null;
                        FormElement nextFormElement = null;
                        RetrieveContentResponse integrationResponseBody = null;


                        try
                        {
                            // check the session for the state of things
                       
                            if(dialogSession is null)
                            {
                                item.Status = EResponseProcessingStatus.SessionExpired.ToString();
                                item.ErrorMessage = $"Could not retrieve session details for the user with phone: {item.To}";

                                throw new FormBgProcessorException(item.ErrorMessage, item.Status, null);
                            }

                            if(dialogSession.SessionState != ESessionState.FORMCONVOABOUTTOSTART
                                && dialogSession.SessionState != ESessionState.FORMCONVORUNNING)
                            {
                                throw new FormBgProcessorException("The user's dialog is not in a form processing state. Reference dialog session",
                               EResponseProcessingStatus.InValidSessionState.ToString(), null);
                            }

                          
                            newCurrentFormElement = dialogSession.SessionFormDetails
                                                        ?.BusinessForm?.FormElements
                                                        ?.FirstOrDefault(x => x.Key == item.FormElement);

                            dialogSession.SessionState = ESessionState.FORMCONVORUNNING;

                            if (newCurrentFormElement is null)
                            {
                                item.Status = EResponseProcessingStatus.Failed.ToString();
                                item.ErrorMessage = $"Form Configuration Error : Could not retrieve the form request with name {item.FormElement} from the business form record";
                                throw new FormBgProcessorException(item.ErrorMessage, item.Status, ESessionState.PLAINCONVERSATION);
                            }

                            if (newCurrentFormElement.ShouldRetrieveContentExternally)
                            {
                                // use the indentifier with Factory to get the correct api integration

                                integrationResponseBody = await apiContentIntegrationFactory
                                    .RetrieveContent<string>(item.BusinessId, newCurrentFormElement.PartnerContentProcessorKey,
                                    item.To, newCurrentFormElement.Key);

                                if (!integrationResponseBody.IsSuccessful && string.IsNullOrEmpty(integrationResponseBody?.Response))
                                    throw new FormBgProcessorException($"Could not retrieve content from integration processor {newCurrentFormElement.PartnerContentProcessorKey}",
                                        "Unable to retrieve content from partner at the moment, pls try again, send *cancel* to restart menu", ESessionState.PLAINCONVERSATION);

                                // after which set the message body as the result of the partintegration call
                                item.Message = integrationResponseBody?.Response;
                            }

                            bool isSent = false; int count = 1;
                            while (!isSent && count <= 2)
                            {

                                if (!item.IsValidationResponse)
                                {
                                    dialogSession.SessionState = ESessionState.FORMCONVORUNNING;
                                    dialogSession.SessionFormDetails.IsFormQuestionSent = true;

                                    dialogSession.SessionFormDetails.IsFormResponseRecieved = false;

                                    if(newCurrentFormElement.RequireUserInputResponse)
                                        dialogSession.SessionFormDetails.Payload += $"* {newCurrentFormElement.Label}: ";

                                    dialogSession.UpdatedAt = DateTime.Now;

                                }
                                var whatsappUser = await _whatsappUserRepo.FirstOrDefaultNoTracking(x => x.WaId == item.To);
                                if (whatsappUser == null)
                                    throw new FormBgProcessorException($"Could not retrieve whatsapp user while trying to send form message","", ESessionState.PLAINCONVERSATION);

                                var sendMessageResult = await _outboundMesageService.HttpSendTextMessage(model: item, wa_Id: item.To);
                                isSent = sendMessageResult.Data;
                                count++;
                                if (isSent)
                                {
                                   
                                    var messageLog = new MessageLog
                                    {
                                        RequestResponseData = JsonConvert.SerializeObject(sendMessageResult),
                                        MessageType = EMessageType.Text,
                                        MessageDirection = EMessageDirection.Outbound,
                                        MessageBody = item.Message,
                                        From = item.From,
                                        To = item.To,
                                        IsRead = true,
                                        WhatsappUserId = whatsappUser.Id,
                                        BusinessId = item.BusinessId
                                    };

                                    await _messageLogRepo.AddAsync(messageLog);
                                    await _messageLogRepo.SaveChangesAsync();
                                }
                                item.Status = EResponseProcessingStatus.Sent.ToString();
                            }

                            // after message has been sent, confirm that response
                            if(integrationResponseBody is not null && integrationResponseBody.DisContinueProcess)
                                throw new FormBgProcessorException($"Could not retrieve content from integration processor {newCurrentFormElement.PartnerContentProcessorKey}",
                                        "Process has been discontinue from send any folllowup message or conclusive message", ESessionState.PLAINCONVERSATION);

                            if (dialogSession.SessionFormDetails.IsFormResponseRecieved)
                            {
                                // set the new current and next form elements
                                dialogSession.SessionFormDetails.CurrentFormElement = newCurrentFormElement;

                                if (!newCurrentFormElement.IsLastFormElement)
                                {
                                    nextFormElement = dialogSession
                                                        ?.SessionFormDetails
                                                        ?.BusinessForm.FormElements
                                                        ?.FirstOrDefault(x => x.Position == newCurrentFormElement.NextFormElementPosition);

                                    dialogSession.SessionFormDetails.NextFormElement = nextFormElement;
                                }
                                else
                                {
                                    dialogSession.SessionState = ESessionState.PLAINCONVERSATION;
                                }

                            }


                            if (newCurrentFormElement.FollowUpMessageId.HasValue)
                            {
                                // create an inbound message and flag as
                                var inboundMsg = new InboundMessage
                                {
                                    BusinessIdMessageId = newCurrentFormElement.FollowUpMessageId,
                                    BusinessId = item.BusinessId,
                                    Body = item.Message,
                                    From = item.To,
                                    To = item.From,
                                    ResponseProcessingStatus = EResponseProcessingStatus.Pending.ToString(),
                                    IsFirstMessageSent = false,
                                    Type = "text",
                                    Wa_Id = item.From
                                };

                                await inboundMessageRepo.AddAsync(inboundMsg);
                            }
                            // update session details
                            await _sessionManagement.Update(waId: item.To, dialogSession: dialogSession);
                            await inboundMessageRepo.SaveChangesAsync();

                            if (!isSent)
                            {
                                item.Status = EResponseProcessingStatus.Failed.ToString();
                                item.ErrorMessage = $"HttpError: Could not send form request with label : {newCurrentFormElement.Label}";
                                throw new FormBgProcessorException(item.ErrorMessage, item.Status, ESessionState.PLAINCONVERSATION);
                            }

                            if (item.IsSummaryMessage)
                            {
                                item.ErrorMessage = "Form completed. Summary message sent";
                                item.Status = EResponseProcessingStatus.ProcessCompleted.ToString();
                                dialogSession.SessionState = ESessionState.FORMCONVERSATIONCOMPLETED;

                                // save an inbound at this point of the business message that conclude a form.
                                throw new FormBgProcessorException(item.ErrorMessage, item.Status, ESessionState.PLAINCONVERSATION);

                            }

                            //TODO: Verify it does not affect existing flow.
                            if(!item.IsValidationResponse && newCurrentFormElement.IsLastFormElement && !newCurrentFormElement.RequireUserInputResponse)
                            {
                                // update session state back to plain conversation
                                dialogSession.SessionState = ESessionState.PLAINCONVERSATION;
                            }
                            
                        }
                        catch (FormBgProcessorException e)
                        {
                            item.Status = !string.IsNullOrEmpty(e.FormProcessingStatus) ?
                                e.FormProcessingStatus : EResponseProcessingStatus.Failed.ToString();

                            item.ErrorMessage = e.Message;

                            if (e.NewSessionState is not null)
                                dialogSession.SessionState = e.NewSessionState.Value;
                                
                            
                        }
                        catch (Exception e)
                        {
                            item.Status = EResponseProcessingStatus.Failed.ToString();
                            item.ErrorMessage = e.Message;

                            dialogSession.SessionState = ESessionState.PLAINCONVERSATION;

                        }
                        finally
                        {
                            var sessionBusinessForm = dialogSession?.SessionFormDetails?.BusinessForm;

                            item.Status = item.Status;
                            item.ErrorMessage = item.ErrorMessage;
                            await _formRequesResponseService.Update(item);

                            await _sessionManagement.Update(waId: item.To, dialogSession: dialogSession);

                            if(dialogSession?.SessionState == ESessionState.FORMCONVERSATIONCOMPLETED)
                            {
                                // check and queue conclusion inbound message tied to the business form if any.
                                //if(sessionBusinessForm?.ConclusionBusinessMessageId is not null)
                                //{
                                //    var businessMessage = await _businessMessageRepo.GetByIdAsync(sessionBusinessForm.ConclusionBusinessMessageId.Value);

                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        private void getDate()
        {
            DateTime dateTime = new DateTime(2023, 07, 14, 16, 03, 21, 417);
            Console.WriteLine(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));

           // DateTime dateTime = new DateTime(2023, 07, 14, 16, 03, 21, 417);
        }
    }
}

