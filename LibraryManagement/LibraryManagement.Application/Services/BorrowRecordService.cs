using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Application.Services;

public class BorrowRecordService(
    IBorrowRecordRepository borrowRecordRepository,
    IBookRepository bookRepository,
    UserManager<ApplicationUser> userManager) : IBorrowRecordService
{
    private const decimal FINE_PER_DAY = 0.50m;
    private const int MAX_RENEWAL_DAYS = 30;
    private const int MAX_RENEWAL_COUNT = 3;
    private const int MAX_BORROW_LIMIT = 5;

    public async Task<BorrowRecordDto> BorrowBookAsync(CreateBorrowRecordDto borrowDto)
    {
        ArgumentNullException.ThrowIfNull(borrowDto);

        // ✅ Validate UserId is not null or empty
        if (string.IsNullOrWhiteSpace(borrowDto.UserId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(borrowDto));


        ApplicationUser? user = await userManager.FindByIdAsync(borrowDto.UserId).ConfigureAwait(false);
        if (user == null)
            throw new InvalidOperationException($"User with ID {borrowDto.UserId} not found.");

        if (!user.IsActive)
            throw new InvalidOperationException($"User {user.Name} is not active.");

        Book? book = await bookRepository.GetByIdAsync(borrowDto.BookId).ConfigureAwait(false);

        if (book == null)
            throw new InvalidOperationException($"Book with ID {borrowDto.BookId} not found.");

        if (book.CopiesAvailable <= 0)
            throw new InvalidOperationException($"No copies available for '{book.Title}'.");

        BorrowRecord? existingBorrow = await borrowRecordRepository
            .GetActiveBorrowByBookAndUserAsync(borrowDto.BookId, borrowDto.UserId).ConfigureAwait(false);

        if (existingBorrow != null)
            throw new InvalidOperationException($"User already has '{book.Title}' borrowed.");

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

        BorrowRecord? createdRecord = await borrowRecordRepository.AddAsync(borrowRecord).ConfigureAwait(false);

        book.CopiesAvailable--;
        await bookRepository.UpdateAsync(book).ConfigureAwait(false);

        createdRecord.Book = book;
        createdRecord.User = user;

        return MapToBorrowRecordDto(createdRecord);
    }

    public async Task<BorrowRecordDto> ReturnBookAsync(ReturnBookDto returnDto)
    {
        ArgumentNullException.ThrowIfNull(returnDto);

        if(string.IsNullOrWhiteSpace(returnDto.UserId))
            throw new ArgumentException("User ID cannot be null or empty", returnDto.UserId);

        BorrowRecord? borrowRecord = await
            borrowRecordRepository.GetActiveBorrowByBookAndUserAsync(returnDto.BookId, returnDto.UserId).ConfigureAwait(false);

        if (borrowRecord == null)
            throw new InvalidOperationException($"No active borrow record found for Book {returnDto.BookId} and User {returnDto.UserId}");

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

        await borrowRecordRepository.UpdateAsync(borrowRecord).ConfigureAwait(false);

        Book? book = await bookRepository.GetByIdAsync(returnDto.BookId).ConfigureAwait(false);

        if (book != null)
        {
            book.CopiesAvailable++;
            await bookRepository.UpdateAsync(book).ConfigureAwait(false);
        }

        ApplicationUser? user = await userManager.FindByIdAsync(returnDto.UserId).ConfigureAwait(false);
        borrowRecord.Book = book!;
        borrowRecord.User = user!;

        return MapToBorrowRecordDto(borrowRecord);
    }

    public async Task<IEnumerable<BorrowRecordDto>> GetUserBorrowHistoryAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));


        IEnumerable<BorrowRecord> records = await borrowRecordRepository.GetBorrowHistoryByUserAsync(userId).ConfigureAwait(false);
        return records.Select(MapToBorrowRecordDto);
    }

    public async Task<IEnumerable<BorrowRecordDto>> GetOverdueBooksAsync()
    {
        IEnumerable<BorrowRecord> overdueRecords = await borrowRecordRepository.GetOverdueBorrowsAsync().ConfigureAwait(false);
        return overdueRecords.Select(MapToBorrowRecordDto);
    }

    public async Task<IEnumerable<BorrowRecordDto>> GetActiveBorrowsByUserAsync(string userId)
    {
        IEnumerable<BorrowRecord> activeBorrows = await borrowRecordRepository.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);
        return activeBorrows.Select(MapToBorrowRecordDto);
    }

    public async Task<decimal> CalculateFineAsync(int borrowRecordId)
    {
        BorrowRecord? record = await borrowRecordRepository.GetByIdAsync(borrowRecordId).ConfigureAwait(false);
        if (record == null)
            throw new InvalidOperationException($"Borrow record with ID {borrowRecordId} not found.");

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
        IEnumerable<BorrowRecord> userBorrowHistory = await borrowRecordRepository.GetBorrowHistoryByUserAsync(userId).ConfigureAwait(false);
        return userBorrowHistory.Any(br => br.Id == borrowRecordId);
    }

    public async Task<IEnumerable<BorrowRecordDto>> GetBorrowHistoryByBookAsync(int bookId)
    {
        Book? book = await bookRepository.GetByIdAsync(bookId).ConfigureAwait(false);
        if (book == null)
            throw new InvalidOperationException($"Book with ID {bookId} not found.");

        IEnumerable<BorrowRecord> records = await borrowRecordRepository.GetBorrowHistoryByBookAsync(bookId).ConfigureAwait(false);
        return records.Select(MapToBorrowRecordDto);
    }

    public async Task<BorrowRecordDto> RenewBorrowAsync(int borrowRecordId, int additionalDays, string userId)
    {
        if (additionalDays <= 0)
            throw new InvalidOperationException("Additional days must be greater than zero.");

        if (additionalDays > MAX_RENEWAL_DAYS)
            throw new InvalidOperationException($"Maximum renewal period is {MAX_RENEWAL_DAYS} days.");

        BorrowRecord? borrowRecord = await borrowRecordRepository.GetBorrowRecordWithDetailsAsync(borrowRecordId).ConfigureAwait(false);
        if (borrowRecord == null)
            throw new InvalidOperationException($"Borrow record with ID {borrowRecordId} not found.");

        // Authorization check
        if (borrowRecord.UserId != userId)
            throw new InvalidOperationException("You can only renew your own borrow records.");

        if (borrowRecord.IsReturned)
            throw new InvalidOperationException("Cannot renew a book that has already been returned.");

        if (borrowRecord.DueDate < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot renew an overdue book. Please return it and pay any fines first.");

        // Check if book has been requested by other users
        Book? book = borrowRecord.Book;
        if (book == null)
            throw new InvalidOperationException("Book information not found.");

        if (book.CopiesAvailable <= 0)
            throw new InvalidOperationException("Cannot renew book as all copies are currently borrowed.");

        // Check renewal count (you might want to add a RenewalCount property to BorrowRecord)
        // For now, we'll check if it's already been renewed multiple times

        decimal renewalCount = await GetRenewalCountAsync(borrowRecordId).ConfigureAwait(false);
        if (renewalCount >= MAX_RENEWAL_COUNT)
            throw new InvalidOperationException($"Maximum renewal count ({MAX_RENEWAL_COUNT}) reached for this book.");

        // Calculate new due date
        DateTime newDueDate = borrowRecord.DueDate.AddDays(additionalDays);

        // Update the borrow record
        borrowRecord.DueDate = newDueDate;
        borrowRecord.UpdatedAt = DateTime.UtcNow;

        // Add renewal note
        borrowRecord.Notes = $"{borrowRecord.Notes} | Renewed on {DateTime.UtcNow:yyyy-MM-dd}, new due date: {newDueDate:yyyy-MM-dd}";

        await borrowRecordRepository.UpdateAsync(borrowRecord).ConfigureAwait(false);

        return MapToBorrowRecordDto(borrowRecord);
    }

    private async Task<int> GetRenewalCountAsync(int borrowRecordId)
    {
        // This is a simplified implementation
        // You might want to add a proper RenewalCount property to BorrowRecord entity
        BorrowRecord? record = await borrowRecordRepository.GetByIdAsync(borrowRecordId).ConfigureAwait(false);
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

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        IEnumerable<BorrowRecord> activeBorrows = await borrowRecordRepository.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);

        return activeBorrows
                   .Where(borrowRecord => borrowRecord.Book != null)
                   .Select(borrowRecord => new BookDto
                   {
                       Id = borrowRecord.Book!.Id,
                       Title = borrowRecord.Book.Title,
                       Author = borrowRecord.Book.Author,
                       ISBN = borrowRecord.Book.ISBN,
                       PublishedYear = borrowRecord.Book.PublishedYear,
                       TotalCopies = borrowRecord.Book.TotalCopies,
                       CopiesAvailable = borrowRecord.Book.CopiesAvailable
                   });
    }

    public async Task<bool> CanUserBorrowAsync(string userId)
    {
        // Check if user exists and is active
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null || !user.IsActive)
            return false;

        // Check if user has overdue books
        IEnumerable<BorrowRecord> overdueBooks = await borrowRecordRepository.GetOverdueBorrowsAsync().ConfigureAwait(false);
        IEnumerable<BorrowRecord> userOverdueBooks = overdueBooks.Where(b => b.UserId == userId);
        if (userOverdueBooks.Any())
            return false;

        // Check if user has reached borrowing limit
        IEnumerable<BorrowRecord> activeBorrows = await borrowRecordRepository.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);
        if (activeBorrows.Count() >= MAX_BORROW_LIMIT)
            return false;

        return true;
    }

    public async Task<BorrowRecordDto?> GetActiveBorrowByBookAsync(int bookId)
    {
        // Get all active borrows and find the one for this book
        IEnumerable<BorrowRecord> allActiveBorrows = await borrowRecordRepository.GetAllAsync().ConfigureAwait(false);
        BorrowRecord? activeBorrow = allActiveBorrows
            .FirstOrDefault(br => br.BookId == bookId && !br.IsReturned);

        if (activeBorrow == null)
            return null;

        // Load related data
        activeBorrow.Book = await bookRepository.GetByIdAsync(bookId).ConfigureAwait(false);
        if (activeBorrow.UserId != null)
        {
            activeBorrow.User = await userManager.FindByIdAsync(activeBorrow.UserId).ConfigureAwait(false);
        }

        return MapToBorrowRecordDto(activeBorrow);
    }
}