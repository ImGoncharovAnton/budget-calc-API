using aspnetcore_auth.Models.DTOs.Responses;
using AutoMapper;

namespace Budget.Profiles;

public class ItemsDtoProfile: Profile
{
    public ItemsDtoProfile()
    {
        CreateMap<Item, ItemDto>();
    }
}