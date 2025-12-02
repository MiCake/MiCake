using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.Dto;
using MiCake.Core;
using MiCake.DDD.Domain;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    /// <summary>
    /// API controller for book management operations.
    /// </summary>
    /// <remarks>
    /// This controller demonstrates:
    /// 1. Dependency injection of repositories
    /// 2. CRUD operations on aggregate roots
    /// 3. Pagination support
    /// 4. Exception handling (BusinessException, DomainException)
    /// 5. Proper async/await patterns
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the BookController.
    /// </remarks>
    /// <param name="bookRepositoryPaging">The book repository with pagination support</param>
    /// <param name="logger">The logger</param>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class BookController(IBookRepository bookRepositoryPaging, ILogger<BookController> logger) : ControllerBase
    {
        private readonly IBookRepository _bookRepositoryPaging = bookRepositoryPaging;
        private readonly ILogger<BookController> _logger = logger;

        /// <summary>
        /// Gets a book by its ID.
        /// </summary>
        /// <param name="bookId">The book ID</param>
        /// <returns>The book if found; otherwise null</returns>
        [HttpGet("{bookId}")]
        public async Task<ActionResult<Book>> GetBook(Guid bookId)
        {
            _logger.LogInformation($"Getting book with ID: {bookId}");
            var book = await _bookRepositoryPaging.FindAsync(bookId);

            if (book == null)
                return NotFound("Book not found");

            return Ok(book);
        }

        /// <summary>
        /// Retrieves a paginated list of books with optional filtering.
        /// </summary>
        /// <param name="filterDto">The filter criteria</param>
        /// <returns>A paginated list of books</returns>
        [HttpPost]
        public async Task<ActionResult<PagingResponse<Book>>> GetBookList([FromBody] BookFilterDto filterDto)
        {
            _logger.LogInformation("Getting book list with pagination");

            var filterGroup = filterDto.GenerateFilterGroup();
            var books = await _bookRepositoryPaging.FilterPagingQueryAsync(
                new PagingRequest(filterDto.PageNumber ?? 1, filterDto.PageSize ?? 10),
                filterGroup);

            return Ok(books);
        }

        /// <summary>
        /// Creates a new book.
        /// </summary>
        /// <param name="bookDto">The book data</param>
        /// <returns>The created book ID</returns>
        [HttpPost]
        public async Task<ActionResult<Guid>> AddBook([FromBody] AddBookDto bookDto)
        {
            _logger.LogInformation($"Adding new book: {bookDto.BookName}");

            try
            {
                var book = new Book(bookDto.BookName, bookDto.AuthorFirstName, bookDto.AuthroLastName);

                // Optional: Set additional properties if provided
                if (!string.IsNullOrEmpty(bookDto.ISBN))
                    book.SetISBN(bookDto.ISBN);

                if (bookDto.PublicationYear.HasValue)
                    book.SetPublicationYear(bookDto.PublicationYear.Value);

                await _bookRepositoryPaging.AddAsync(book);

                return CreatedAtAction(nameof(GetBook), new { bookId = book.Id }, book.Id);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning($"Book creation failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates a book's author information.
        /// </summary>
        /// <param name="bookDto">The update data</param>
        /// <returns>Success status</returns>
        [HttpPut]
        public async Task<IActionResult> ChangeAuthor([FromBody] ChangeBookAuthorDto bookDto)
        {
            _logger.LogInformation($"Updating author for book: {bookDto.BookID}");

            var bookInfo = await _bookRepositoryPaging.FindAsync(bookDto.BookID)
                ?? throw new BusinessException("Book not found");

            bookInfo.ChangeAuthor(bookDto.AuthorFirstName, bookDto.AuthorLastName);
            await _bookRepositoryPaging.SaveChangesAsync();

            return Ok(true);
        }
    }
}
