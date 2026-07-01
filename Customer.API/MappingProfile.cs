using AutoMapper;
using Customer.API.Entities;
using Shared.DTOs.Customer;

namespace Customer.API;

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		CreateMap<CustomerEntity, CustomerDto>();
		CreateMap<CreateCustomerDto, CustomerEntity>();
		CreateMap<UpdateCustomerDto, CustomerEntity>();
	}
}
