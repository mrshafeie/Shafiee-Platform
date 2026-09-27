using FluentValidation;
using shop.Business.DTOs;

namespace shop.Business.Validators;

public class ProductValidator : AbstractValidator<ProductDto>
{
    public ProductValidator()
    {



        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The Name field is required.")
            .MaximumLength(250).WithMessage("The length of Name cannot exceed 250 characters.");


        RuleFor(x => x.Price)
            .NotNull().WithMessage("The Price value must not be null.")
            .GreaterThan(0).WithMessage("The Price value must be greater than zero.");


        RuleFor(x => x.Stock)
            .NotNull().WithMessage("The Stock value must not be null.")
            .GreaterThan(0).WithMessage("The Stock value must be greater than zero.");


    }
}