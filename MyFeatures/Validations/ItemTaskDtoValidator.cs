using FluentValidation;
using MyFeatures.DTO;

namespace MyFeatures.Validations
{
    //automatski radi bez da manualno ja moram postavit
    public class ItemTaskDtoValidator : AbstractValidator<ItemTaskDto>
    {
        public ItemTaskDtoValidator()
        {
            // Validate the Description in the main DTO
            RuleFor(dto => dto.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            When(dto => dto.Item != null, () =>
            {
                RuleFor(dto => dto.Item.Description)
                    .NotEmpty()
                    .WithMessage("Item description is required.");
            });
        }
    }
}
