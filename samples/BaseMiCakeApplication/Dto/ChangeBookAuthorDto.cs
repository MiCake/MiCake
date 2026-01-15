using FluentValidation;
using System;

namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for changing a book's author information.
    /// </summary>
    public class ChangeBookAuthorDto
    {
        /// <summary>
        /// Gets or sets the book ID.
        /// </summary>
        public Guid BookID { get; set; }

        /// <summary>
        /// Gets or sets the author's first name.
        /// </summary>
        public string AuthorFirstName { get; set; }

        /// <summary>
        /// Gets or sets the author's last name.
        /// </summary>
        public string AuthorLastName { get; set; }
    }

    /// <summary>
    /// Validator for ChangeBookAuthorDto using Fluent Validation.
    /// </summary>
    public class ChangeBookAuthorDtoValidator : AbstractValidator<ChangeBookAuthorDto>
    {
        /// <summary>
        /// Configures validation rules for the DTO.
        /// </summary>
        public ChangeBookAuthorDtoValidator()
        {
            RuleFor(x => x.BookID)
                .NotEmpty().WithMessage("Book ID is required");

            RuleFor(x => x.AuthorFirstName)
                .NotEmpty().WithMessage("Author first name is required")
                .MaximumLength(100).WithMessage("Author first name cannot exceed 100 characters");

            RuleFor(x => x.AuthorLastName)
                .NotEmpty().WithMessage("Author last name is required")
                .MaximumLength(100).WithMessage("Author last name cannot exceed 100 characters");
        }
    }
}
