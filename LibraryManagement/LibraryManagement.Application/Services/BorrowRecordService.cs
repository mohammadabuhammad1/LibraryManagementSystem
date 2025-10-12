using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services
{
    public class BorrowRecordService : IBorrowRecordService
    {
        private readonly IBorrowRecordRepository _borrowRecordRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private const decimal FINE_PER_DAY = 0.50m;

        public BorrowRecordService(
            IBorrowRecordRepository borrowRecordRepository,
            IBookRepository bookRepository,
            IMemberRepository memberRepository)
        {
            _borrowRecordRepository = borrowRecordRepository;
            _bookRepository = bookRepository;
            _memberRepository = memberRepository;
        }

        public async Task<BorrowRecordDto> BorrowBookAsync(CreateBorrowRecordDto borrowDto)
        {
            var member = await _memberRepository.GetByIdAsync(borrowDto.MemberId);
            if (member == null)
                throw new Exception($"Member with ID {borrowDto.MemberId} not found.");

            if (!member.IsActive)
                throw new Exception($"Member {member.Name} is not active.");

            var book = await _bookRepository.GetByIdAsync(borrowDto.BookId);
            if (book == null)
                throw new Exception($"Book with ID {borrowDto.BookId} not found.");

            if (book.CopiesAvailable <= 0)
                throw new Exception($"No copies available for '{book.Title}'.");

            var existingBorrow = await _borrowRecordRepository
                .GetActiveBorrowByBookAndMemberAsync(borrowDto.BookId, borrowDto.MemberId);
            if (existingBorrow != null)
                throw new Exception($"Member already has '{book.Title}' borrowed.");

            var borrowRecord = new BorrowRecord
            {
                BookId = borrowDto.BookId,
                MemberId = borrowDto.MemberId,
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
            createdRecord.Member = member;

            return MapToBorrowRecordDto(createdRecord);
        }

        public async Task<BorrowRecordDto> ReturnBookAsync(ReturnBookDto returnDto)
        {
            var borrowRecord = await _borrowRecordRepository
                .GetActiveBorrowByBookAndMemberAsync(returnDto.BookId, returnDto.MemberId);

            if (borrowRecord == null)
                throw new Exception($"No active borrow record found for Book {returnDto.BookId} and Member {returnDto.MemberId}");

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

            var member = await _memberRepository.GetByIdAsync(returnDto.MemberId);
            borrowRecord.Book = book!;
            borrowRecord.Member = member!;

            return MapToBorrowRecordDto(borrowRecord);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetMemberBorrowHistoryAsync(int memberId)
        {
            var records = await _borrowRecordRepository.GetBorrowHistoryByMemberAsync(memberId);
            return records.Select(MapToBorrowRecordDto);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetOverdueBooksAsync()
        {
            var overdueRecords = await _borrowRecordRepository.GetOverdueBorrowsAsync();
            return overdueRecords.Select(MapToBorrowRecordDto);
        }

        public async Task<IEnumerable<BorrowRecordDto>> GetActiveBorrowsByMemberAsync(int memberId)
        {
            var activeBorrows = await _borrowRecordRepository.GetActiveBorrowsByMemberAsync(memberId);
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

        private static BorrowRecordDto MapToBorrowRecordDto(BorrowRecord record)
        {
            return new BorrowRecordDto
            {
                Id = record.Id,
                BookId = record.BookId,
                MemberId = record.MemberId,
                BookTitle = record.Book?.Title ?? string.Empty,
                MemberName = record.Member?.Name ?? string.Empty,
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