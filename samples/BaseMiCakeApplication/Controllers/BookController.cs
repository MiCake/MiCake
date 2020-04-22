using BaseMiCakeApplication.Domain.Aggregates;
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

        public BookController(IRepository<Book, Guid> bookRepository)
        {
            _bookRepository = bookRepository;
        }

        [HttpPost]
        public async Task AddBookAsync(string name, string author)
        {
            await _bookRepository.AddAsync(new Book(name, author));
        }

        [HttpPost]
        public async Task DeleteBookAsync(Guid bookId)
        {
            var currentBook = await _bookRepository.FindAsync(bookId);
            await _bookRepository.DeleteAsync(currentBook);
        }

        [HttpPost]
        public async Task ChangeBookNameAsync(Guid bookId, string bookName)
        {
            var currentBook = await _bookRepository.FindAsync(bookId);
            currentBook.ChangeName(bookName);

            await _bookRepository.UpdateAsync(currentBook);
        }
    }
}
