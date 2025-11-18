using FluentValidation;

namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for user registration requests.
    /// </summary>
    public class RegisterUserDto
    {
        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the user's name (optional).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user's email address (optional).
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's age (optional).
        /// </summary>
        public int? Age { get; set; }
    }

    /// <summary>
    /// Validator for RegisterUserDto using Fluent Validation.
    /// </summary>
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        /// <summary>
        /// Configures validation rules for the DTO.
        /// </summary>
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^\d{10,20}$").WithMessage("Phone number must be 10-20 digits");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email must be a valid email address")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("Age must be greater than 0")
                .LessThan(150).WithMessage("Age must be less than 150")
                .When(x => x.Age.HasValue);
        }
    }
}
