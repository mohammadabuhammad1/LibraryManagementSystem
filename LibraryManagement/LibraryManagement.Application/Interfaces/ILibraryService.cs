using LibraryManagement.Application.Dtos.Book;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.Application.Interfaces
{
    public interface ILibraryService
    {

        Task<BookDto> BorrowBookAsync(int bookId);
        Task<BookDto> ReturnBookAsync(int bookId);
        Task<IEnumerable<BookDto>> GetBorrowedBooksAsync();
        Task<IEnumerable<BookDto>> GetAvailableBooksAsync();


    }
}
