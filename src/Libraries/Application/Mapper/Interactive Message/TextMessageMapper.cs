﻿using Application.DTOs.InboundMessageDto;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.CreateDialogDtos;
using Domain.Entities.DialogMessageEntitties;
using Application.Helpers;

namespace Application.Mapper.Interactive_Message
{
    public class TextMessageMapper: Profile
    {
        public TextMessageMapper()
        {
            CreateMap<InboundMessage, TextNotificationDto>().ReverseMap();
            CreateMap<TextMessageDto, TextMessage>().ReverseMap().AfterMap((src, dest) =>
            {
                dest.IsResponsePermitted = src.IsResponsePermitted;
                dest.KeyResponses = src.KeyResponses;
            });
            
            CreateMap<CreateBusinessMessageDto<CreateTextMessageDto>,
                CreateBusinessMessageDto<BaseCreateMessageDto>>().ReverseMap();
            CreateMap<CreateTextMessageDto, BaseCreateMessageDto>();

            CreateMap<CreateBusinessMessageDto<CreateTextMessageDto>,
                TextMessage>().AfterMap((src,
                dest) =>
            {
                dest.Body = src?.MessageTypeObject?.Body?.WordWrapContent();
                dest.Footer = src?.MessageTypeObject?.Footer?.WordWrapContent();
                dest.Header = src?.MessageTypeObject?.Header;
                dest.NextMessagePosition = src?.MessageTypeObject?.NextMessagePosition ?? 0;
            });
            
            CreateMap<CreateBusinessMessageDto<BaseCreateMessageDto>,
                TextMessage>().AfterMap((src,
                dest) =>
            {
                dest.Body = src?.MessageTypeObject?.Body.WordWrapContent();
                dest.Footer = src?.MessageTypeObject?.Footer.WordWrapContent();
                dest.Header = src?.MessageTypeObject?.Header;
            });
            
            CreateMap<BaseCreateMessageDto,
                TextMessage>().AfterMap((src,
                dest) =>
            {
                dest.Body = src?.Body.WordWrapContent();
                dest.Footer = src?.Footer;
                dest.Header = src?.Header;
            });
        }
    }
}
