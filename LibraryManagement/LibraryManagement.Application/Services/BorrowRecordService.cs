using LibraryManagement.Application.Dtos;
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
    }
}