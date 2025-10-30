using FluentValidation;

namespace BaseMiCakeApplication.Dto
{
    public class AddBookDto
    {
        public string BookName { get; set; }

        public string AuthorFirstName { get; set; }
        public string AuthroLastName { get; set; }
    }

    public class AddBookDtoValidator : FluentValidation.AbstractValidator<AddBookDto>
    {
        public AddBookDtoValidator()
        {
            RuleFor(x => x.BookName).NotEmpty().WithMessage("Book name cannot be empty");
            RuleFor(x => x.AuthorFirstName).NotEmpty().WithMessage("Author first name cannot be empty");
            RuleFor(x => x.AuthroLastName).NotEmpty().WithMessage("Author last name cannot be empty");
        }
    }
}
