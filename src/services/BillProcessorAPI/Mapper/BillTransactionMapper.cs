﻿using AutoMapper;
using BillProcessorAPI.Dtos;
using BillProcessorAPI.Dtos.Paythru;
using BillProcessorAPI.Entities;

namespace BillProcessorAPI.Mapper
{
    public class BillTransactionMapper :Profile
	{
		public BillTransactionMapper()
		{
			CreateMap<BillTransaction, BillPaymentVerificationResponseDto>().ReverseMap();
			CreateMap<BillTransaction, BillPaymentVerificationRequestDto>().ReverseMap();
			CreateMap<BillTransaction, PayThruPaymentRequestDto>().ReverseMap();
			CreateMap<BillTransaction, BankTransferInstruction>();
			CreateMap<BillTransaction, Receipt>()
				.ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Empty));

		}
	}
}
