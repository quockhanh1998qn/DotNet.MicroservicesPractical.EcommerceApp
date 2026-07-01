using FluentValidation;

namespace Ordering.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
	public CreateOrderCommandValidator()
	{
		RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
		RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
		RuleFor(x => x.TotalPrice).GreaterThan(0);
		RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500);
	}
}
