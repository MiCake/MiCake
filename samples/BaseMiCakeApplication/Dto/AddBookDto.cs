using FluentValidation;

namespace BaseMiCakeApplication.Dto
{
    /// <summary>
    /// DTO for adding a new book.
    /// </summary>
    public class AddBookDto
    {
        /// <summary>
        /// Gets or sets the book name/title.
        /// </summary>
        public string BookName { get; set; }

        /// <summary>
        /// Gets or sets the author's first name.
        /// </summary>
        public string AuthorFirstName { get; set; }

        /// <summary>
        /// Gets or sets the author's last name.
        /// </summary>
        public string AuthroLastName { get; set; }

        /// <summary>
        /// Gets or sets the ISBN (optional).
        /// </summary>
        public string ISBN { get; set; }

        /// <summary>
        /// Gets or sets the publication year (optional).
        /// </summary>
        public int? PublicationYear { get; set; }

        /// <summary>
        /// Gets or sets the book description (optional).
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Validator for AddBookDto using Fluent Validation.
    /// </summary>
    public class AddBookDtoValidator : AbstractValidator<AddBookDto>
    {
        /// <summary>
        /// Configures validation rules for the DTO.
        /// </summary>
        public AddBookDtoValidator()
        {
            RuleFor(x => x.BookName)
                .NotEmpty().WithMessage("Book name is required")
                .MaximumLength(200).WithMessage("Book name cannot exceed 200 characters");

            RuleFor(x => x.AuthorFirstName)
                .NotEmpty().WithMessage("Author first name is required")
                .MaximumLength(100).WithMessage("Author first name cannot exceed 100 characters");

            RuleFor(x => x.AuthroLastName)
                .NotEmpty().WithMessage("Author last name is required")
                .MaximumLength(100).WithMessage("Author last name cannot exceed 100 characters");

            RuleFor(x => x.ISBN)
                .MaximumLength(20).WithMessage("ISBN cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.ISBN));

            RuleFor(x => x.PublicationYear)
                .GreaterThanOrEqualTo(1000).WithMessage("Publication year must be at least 1000")
                .LessThanOrEqualTo(System.DateTime.Now.Year).WithMessage("Publication year cannot be in the future")
                .When(x => x.PublicationYear.HasValue);
        }
    }
}
