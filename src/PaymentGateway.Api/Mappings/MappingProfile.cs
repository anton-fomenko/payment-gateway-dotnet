using AutoMapper;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Payment, GetPaymentResponse>()
                .ForMember(dest => dest.CardNumberLastFour, opt => opt.MapFrom(src => src.CardNumber.Substring(src.CardNumber.Length - 4)));

            CreateMap<Payment, PostPaymentResponse>()
                .ForMember(dest => dest.CardNumberLastFour, opt => opt.MapFrom(src => src.CardNumber.Substring(src.CardNumber.Length - 4)));

            CreateMap<PostPaymentRequest, Payment>();
        }
    }
}
