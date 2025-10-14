using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Application.Services
{
    public class BorrowRecordService : IBorrowRecordService
    {
        private readonly IBorrowRecordRepository _borrowRecordRepository;
        private readonly IBookRepository _bookRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private const decimal FINE_PER_DAY = 0.50m;
        private const int MAX_RENEWAL_DAYS = 30;
        private const int MAX_RENEWAL_COUNT = 3;
        private const int MAX_BORROW_LIMIT = 5;

        public BorrowRecordService(
            IBorrowRecordRepository borrowRecordRepository,
            IBookRepository bookRepository,
            UserManager<ApplicationUser> userManager)
        {
            _borrowRecordRepository = borrowRecordRepository;
            _bookRepository = bookRepository;
            _userManager = userManager;
        }

        public async Task<BorrowRecordDto> BorrowBookAsync(CreateBorrowRecordDto borrowDto)
        {
            var user = await _userManager.FindByIdAsync(borrowDto.UserId);
            if (user == null)
                throw new Exception($"User with ID {borrowDto.UserId} not found.");

            if (!user.IsActive)
                throw new Exception($"User {user.Name} is not active.");

            var book = await _bookRepository.GetByIdAsync(borrowDto.BookId);
            if (book == null)
                throw new Exception($"Book with ID {borrowDto.BookId} not found.");

            if (book.CopiesAvailable <= 0)
                throw new Exception($"No copies available for '{book.Title}'.");

            var existingBorrow = await _borrowRecordRepository
                .GetActiveBorrowByBookAndUserAsync(borrowDto.BookId, borrowDto.UserId);
            if (existingBorrow != null)
                throw new Exception($"User already has '{book.Title}' borrowed.");

            var borrowRecord = new BorrowRecord
            {
                BookId = borrowDto.BookId,
                UserId = borrowDto.UserId,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(borrowDto.BorrowDurationDays),
                IsReturned = false,
                Notes = borrowDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            var createdRecord = await _borrowRecordRepository.AddAsync(borrowRecord);

            book.CopiesAvailable--;
            await _bookRepository.UpdateAsync(book);

            createdRecord.Book = book;
            createdRecord.User = user;

            return MapToBorrowRecordDto(createdRecord);
        }

        public async Task<BorrowRecordDto> ReturnBookAsync(ReturnBookDto returnDto)
        {
            var borrowRecord = await _borrowRecordRepository
                .GetActiveBorrowByBookAndUserAsync(returnDto.BookId, returnDto.UserId);

            if (borrowRecord == null)
                throw new Exception($"No active borrow record found for Book {returnDto.BookId} and User {returnDto.UserId}");

            borrowRecord.ReturnDate = DateTime.UtcNow;
            borrowRecord.IsReturned = true;
            borrowRecord.UpdatedAt = DateTime.UtcNow;

            if (DateTime.UtcNow > borrowRecord.DueDate)
            {
                var daysOverdue = (DateTime.UtcNow - borrowRecord.DueDate).Days;
                borrowRecord.FineAmount = daysOverdue * FINE_PER_DAY;
            }

            if (!string.IsNullOrEmpty(returnDto.Notes))
            {
                borrowRecord.Notes = $"{borrowRecord.Notes} | Return notes: {returnDto.Notes}";
            }

            await _borrowRecordRepository.UpdateAsync(borrowRecord);

            var book = await _bookRepository.GetByIdAsync(returnDto.BookId);
            if (book != null)
            {
                book.CopiesAvailable++;
                await _bookRepository.UpdateAsync(book);
            }

            var user = await _userManager.FindByIdAsync(returnDto.UserId);
            borrowRecord.Book = book!;
            borrowRecord.User = user!;

            return MapToBorrowRecordDto(borrowRecord);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetUserBorrowHistoryAsync(string userId)
        {
            var records = await _borrowRecordRepository.GetBorrowHistoryByUserAsync(userId);
            return records.Select(MapToBorrowRecordDto);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetOverdueBooksAsync()
        {
            var overdueRecords = await _borrowRecordRepository.GetOverdueBorrowsAsync();
            return overdueRecords.Select(MapToBorrowRecordDto);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetActiveBorrowsByUserAsync(string userId)
        {
            var activeBorrows = await _borrowRecordRepository.GetActiveBorrowsByUserAsync(userId);
            return activeBorrows.Select(MapToBorrowRecordDto);
        }

        public async Task<decimal> CalculateFineAsync(int borrowRecordId)
        {
            var record = await _borrowRecordRepository.GetByIdAsync(borrowRecordId);
            if (record == null)
                throw new Exception($"Borrow record with ID {borrowRecordId} not found.");

            if (record.IsReturned && record.FineAmount.HasValue)
            {
                return record.FineAmount.Value;
            }

            if (!record.IsReturned && DateTime.UtcNow > record.DueDate)
            {
                var daysOverdue = (DateTime.UtcNow - record.DueDate).Days;
                return daysOverdue * FINE_PER_DAY;
            }

            return 0;
        }

        public async Task<bool> CanUserViewFineAsync(int borrowRecordId, string userId)
        {
            var userBorrowHistory = await _borrowRecordRepository.GetBorrowHistoryByUserAsync(userId);
            return userBorrowHistory.Any(br => br.Id == borrowRecordId);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetBorrowHistoryByBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                throw new Exception($"Book with ID {bookId} not found.");

            var records = await _borrowRecordRepository.GetBorrowHistoryByBookAsync(bookId);
            return records.Select(MapToBorrowRecordDto);
        }

        public async Task<BorrowRecordDto> RenewBorrowAsync(int borrowRecordId, int additionalDays, string userId)
        {
            if (additionalDays <= 0)
                throw new Exception("Additional days must be greater than zero.");

            if (additionalDays > MAX_RENEWAL_DAYS)
                throw new Exception($"Maximum renewal period is {MAX_RENEWAL_DAYS} days.");

            var borrowRecord = await _borrowRecordRepository.GetBorrowRecordWithDetailsAsync(borrowRecordId);
            if (borrowRecord == null)
                throw new Exception($"Borrow record with ID {borrowRecordId} not found.");

            // Authorization check
            if (borrowRecord.UserId != userId)
                throw new Exception("You can only renew your own borrow records.");

            if (borrowRecord.IsReturned)
                throw new Exception("Cannot renew a book that has already been returned.");

            if (borrowRecord.DueDate < DateTime.UtcNow)
                throw new Exception("Cannot renew an overdue book. Please return it and pay any fines first.");

            // Check if book has been requested by other users
            var book = borrowRecord.Book;
            if (book == null)
                throw new Exception("Book information not found.");

            if (book.CopiesAvailable <= 0)
                throw new Exception("Cannot renew book as all copies are currently borrowed.");

            // Check renewal count (you might want to add a RenewalCount property to BorrowRecord)
            // For now, we'll check if it's already been renewed multiple times
            var renewalCount = await GetRenewalCountAsync(borrowRecordId);
            if (renewalCount >= MAX_RENEWAL_COUNT)
                throw new Exception($"Maximum renewal count ({MAX_RENEWAL_COUNT}) reached for this book.");

            // Calculate new due date
            var newDueDate = borrowRecord.DueDate.AddDays(additionalDays);

            // Update the borrow record
            borrowRecord.DueDate = newDueDate;
            borrowRecord.UpdatedAt = DateTime.UtcNow;

            // Add renewal note
            borrowRecord.Notes = $"{borrowRecord.Notes} | Renewed on {DateTime.UtcNow:yyyy-MM-dd}, new due date: {newDueDate:yyyy-MM-dd}";

            await _borrowRecordRepository.UpdateAsync(borrowRecord);

            return MapToBorrowRecordDto(borrowRecord);
        }

        private async Task<int> GetRenewalCountAsync(int borrowRecordId)
        {
            // This is a simplified implementation
            // You might want to add a proper RenewalCount property to BorrowRecord entity
            var record = await _borrowRecordRepository.GetByIdAsync(borrowRecordId);
            if (record == null) return 0;

            // Count renewals based on notes or create a separate renewal history table
            // For now, return 0 to allow at least one renewal
            return 0;
        }

        private static BorrowRecordDto MapToBorrowRecordDto(BorrowRecord record)
        {
            return new BorrowRecordDto
            {
                Id = record.Id,
                BookId = record.BookId,
                UserId = record.UserId,
                BookTitle = record.Book?.Title ?? string.Empty,
                UserName = record.User?.Name ?? string.Empty,
                BorrowDate = record.BorrowDate,
                DueDate = record.DueDate,
                ReturnDate = record.ReturnDate,
                IsReturned = record.IsReturned,
                FineAmount = record.FineAmount,
                Notes = record.Notes
            };
        }

        public async Task<IEnumerable<BookDto>> GetBorrowedBooksByUserAsync(string userId)
        {
            var activeBorrows = await _borrowRecordRepository.GetActiveBorrowsByUserAsync(userId);

            var borrowedBooks = new List<BookDto>();
            foreach (var borrowRecord in activeBorrows)
            {
                if (borrowRecord.Book != null)
                {
                    borrowedBooks.Add(new BookDto
                    {
                        Id = borrowRecord.Book.Id,
                        Title = borrowRecord.Book.Title,
                        Author = borrowRecord.Book.Author,
                        ISBN = borrowRecord.Book.ISBN,
                        PublishedYear = borrowRecord.Book.PublishedYear,
                        TotalCopies = borrowRecord.Book.TotalCopies,
                        CopiesAvailable = borrowRecord.Book.CopiesAvailable
                    });
                }
            }

            return borrowedBooks;
        }

        public async Task<bool> CanUserBorrowAsync(string userId)
        {
            // Check if user exists and is active
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Check if user has overdue books
            var overdueBooks = await _borrowRecordRepository.GetOverdueBorrowsAsync();
            var userOverdueBooks = overdueBooks.Where(b => b.UserId == userId);
            if (userOverdueBooks.Any())
                return false;

            // Check if user has reached borrowing limit
            var activeBorrows = await _borrowRecordRepository.GetActiveBorrowsByUserAsync(userId);
            if (activeBorrows.Count() >= MAX_BORROW_LIMIT)
                return false;

            return true;
        }

        public async Task<BorrowRecordDto?> GetActiveBorrowByBookAsync(int bookId)
        {
            // Get all active borrows and find the one for this book
            var allActiveBorrows = await _borrowRecordRepository.GetAllAsync();
            var activeBorrow = allActiveBorrows
                .FirstOrDefault(br => br.BookId == bookId && !br.IsReturned);

            if (activeBorrow == null)
                return null;

            // Load related data
            activeBorrow.Book = await _bookRepository.GetByIdAsync(bookId);
            if (activeBorrow.UserId != null)
            {
                activeBorrow.User = await _userManager.FindByIdAsync(activeBorrow.UserId);
            }

            return MapToBorrowRecordDto(activeBorrow);
        }
    }
}