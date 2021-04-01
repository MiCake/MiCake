using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Dto;
using MiCake.Core;
using MiCake.DDD.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BookController : ControllerBase
    {
        private readonly IRepository<Book, Guid> _bookRepository;

        public BookController(IRepository<Book, Guid> repository)
        {
            _bookRepository = repository;
        }

        [HttpGet]
        public async Task<Book> GetBook(Guid bookId)
        {
            return await _bookRepository.FindAsync(bookId);
        }

        [HttpGet]
        public IActionResult GetNoFound() => NotFound("No Found Action");

        [HttpGet]
        public IActionResult GetMiCakeException() => throw new MiCakeException("This is MiCake exception. http code is 500.");

        [HttpGet]
        public string GetStringResult() => "MiCake";

        [HttpGet]
        public List<int> GetListResult() => new List<int>() { 1, 3, 4 };

        [HttpGet]
        public IActionResult GetSoftlyMiCakeException() => throw new SoftlyMiCakeException("This is MiCake softly exception. http code is 200.");

        [HttpGet]
        public IActionResult GetUnauthorized()
        {
            return Unauthorized();
        }

        [HttpPost]
        public async Task AddBook([FromBody] AddBookDto bookDto)
        {
            var book = new Book(bookDto.BookName, bookDto.AuthorFirstName, bookDto.AuthroLastName);

            book.ChangeAuthor("xx", "aa");
            await _bookRepository.AddAsync(book);
        }

        [HttpPost]
        public async Task<bool> ChangeAuthor([FromBody] ChangeBookAuthorDto bookDto)
        {
            var _bookInfo = await _bookRepository.FindAsync(bookDto.BookID)
                                ?? throw new SoftlyMiCakeException("未找到对应书籍信息");

            _bookInfo.ChangeAuthor(bookDto.AuthorFirstName, bookDto.AuthorLastName);

            return true;
        }
    }
}
