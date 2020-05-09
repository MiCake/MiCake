using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Dto;
using MiCake.Core;
using MiCake.DDD.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
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

        [HttpPost]
        public async Task AddBook([FromBody]AddBookDto bookDto)
            => await _bookRepository.AddAsync(new Book(bookDto.BookName, bookDto.AuthorFirstName, bookDto.AuthroLastName));

        [HttpPost]
        public async Task<bool> ChangeAuthor([FromBody]ChangeBookAuthorDto bookDto)
        {
            var _bookInfo = await _bookRepository.FindAsync(bookDto.BookID)
                                ?? throw new SoftlyMiCakeException("未找到对应书籍信息");

            _bookInfo.ChangeAuthor(bookDto.AuthorFirstName, bookDto.AuthorLastName);
            await _bookRepository.UpdateAsync(_bookInfo);

            return true;
        }
    }
}
