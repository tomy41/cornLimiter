using CornLimiter.Application.Queries;
using FluentValidation;

namespace CornLimiter.Application.Validators;

public class SalesByFarmerQueryValidator : AbstractValidator<SalesByFarmerQuery>
{
    public SalesByFarmerQueryValidator()
    {
        RuleFor(x => x.FarmerCode)
            .NotEmpty().WithMessage("Farmer code is required.")
            .NotEqual(Guid.Empty).WithMessage("Farmer code cannot be Guid.Empty.");
    }
}