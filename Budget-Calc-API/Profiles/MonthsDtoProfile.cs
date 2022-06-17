using aspnetcore_auth.Models.DTOs.Responses;
using aspnetcore_auth.Models.UI;
using AutoMapper;

namespace Budget.Profiles;

public class MonthsDtoProfile : Profile
{
    public MonthsDtoProfile()
    {

        CreateMap<Month, MonthDto>()
            .ForMember(dest => dest.UserId,
                opt =>
                    opt.MapFrom(src => src.ApplicationUserId));
        CreateMap<Item, ItemDto>();
    }
}