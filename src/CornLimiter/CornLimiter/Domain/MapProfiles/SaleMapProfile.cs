using AutoMapper;
using CornLimiter.Domain.Models;
using CornLimiter.Domain.ValueObjects;

namespace CornLimiter.Domain.MapProfiles;

public class SaleMapProfile : Profile
{
    public SaleMapProfile()
    {
        CreateMap<Sale, SaleDto>();
    }
}
