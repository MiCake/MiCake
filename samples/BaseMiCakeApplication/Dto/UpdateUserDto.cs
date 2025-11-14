using FluentValidation;

namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for updating user information.
    /// </summary>
    public class UpdateUserDto
    {
        /// <summary>
        /// Gets or sets the user's name (optional).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number (optional).
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the user's email address (optional).
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's age (optional).
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// Gets or sets the user's avatar URL (optional).
        /// </summary>
        public string Avatar { get; set; }
    }

    /// <summary>
    /// Validator for UpdateUserDto using Fluent Validation.
    /// </summary>
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        /// <summary>
        /// Configures validation rules for the DTO.
        /// </summary>
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Phone)
                .Matches(@"^\d{10,20}$").WithMessage("Phone number must be 10-20 digits")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email must be a valid email address")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("Age must be greater than 0")
                .LessThan(150).WithMessage("Age must be less than 150")
                .When(x => x.Age.HasValue);

            RuleFor(x => x.Avatar)
                .MaximumLength(500).WithMessage("Avatar URL cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Avatar));
        }
    }
}
