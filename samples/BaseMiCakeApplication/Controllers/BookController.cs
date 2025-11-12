using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.Dto;
using MiCake.Core;
using MiCake.Util.LinqFilter;
using MiCake.DDD.Domain;
using MiCake.DDD.Infrastructure.Paging;
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
        private readonly IBookRepository _bookRepositoryPaging;

        public BookController(IRepository<Book, Guid> repository, IBookRepository bookRepositoryPaging)
        {
            _bookRepository = repository;
            _bookRepositoryPaging = bookRepositoryPaging;
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
        public List<int> GetListResult() => [1, 3, 4];

        [HttpGet]
        public IActionResult GetSoftlyMiCakeException() => throw new SlightMiCakeException("This is MiCake softly exception. http code is 200.");

        [HttpGet]
        public IActionResult GetUnauthorized()
        {
            return Unauthorized();
        }

        [HttpPost]
        public async Task AddBook([FromBody] AddBookDto bookDto)
        {
            var book = new Book(bookDto.BookName, bookDto.AuthorFirstName, bookDto.AuthroLastName);

            await _bookRepository.AddAsync(book);
        }

        [HttpPost]
        public async Task<bool> ChangeAuthor([FromBody] ChangeBookAuthorDto bookDto)
        {
            var _bookInfo = await _bookRepository.FindAsync(bookDto.BookID)
                                ?? throw new SlightMiCakeException("未找到对应书籍信息");

            _bookInfo.ChangeAuthor(bookDto.AuthorFirstName, bookDto.AuthorLastName);

            return true;
        }

        [HttpGet]
        public async Task<IActionResult> GetBookList([FromQuery] BookFilterDto filterDto)
        {
            var filterGrp = filterDto.GenerateFilterGroup();
            var books = await _bookRepositoryPaging.CommonFilterPagingQueryAsync(new PagingQueryModel(1, 10), filterGrp);

            return Ok(books);
        }
    }
}
