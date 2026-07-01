using FluentValidation;

namespace Ordering.Application.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
	public UpdateOrderCommandValidator()
	{
		RuleFor(x => x.Id).GreaterThan(0);
		RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
		RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
		RuleFor(x => x.TotalPrice).GreaterThan(0);
	}
}
