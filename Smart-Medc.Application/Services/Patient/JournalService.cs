using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Journal;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.Patient
{
    public class JournalService : IJournalService
    {
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IFileStorageService _fileStorage;
        private readonly ILogger<JournalService> _logger;

        //private const string JournalPhotosContainer = "journal-photos";
        //private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/jpg" };
        //private const long MaxPhotoSizeBytes = 10 * 1024 * 1024; // 10 MB
        //private const int MaxPhotosPerEntry = 5;
        private const int ExcerptMaxLength = 150;

        public JournalService(
            IUnitOfWork unitOfWork,
            //IFileStorageService fileStorage,
            ILogger<JournalService> logger)
        {
            _unitOfWork = unitOfWork;
            // _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<JournalEntryDto>>> GetEntriesAsync(
            Guid patientId, JournalQueryDto query, CancellationToken ct = default)
        {
            var entriesQuery = _unitOfWork.JournalEntries.QueryNoTracking()
                .Where(e => e.PatientId == patientId && !e.IsDeleted);

            // Text search
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                entriesQuery = entriesQuery.Where(e =>
                    e.Title.ToLower().Contains(term) ||
                    e.Content.ToLower().Contains(term));
            }

            // Mood/Pain filters
            if (query.MinMood.HasValue)
                entriesQuery = entriesQuery.Where(e => e.MoodLevel >= query.MinMood.Value);
            if (query.MaxMood.HasValue)
                entriesQuery = entriesQuery.Where(e => e.MoodLevel <= query.MaxMood.Value);
            if (query.MinPain.HasValue)
                entriesQuery = entriesQuery.Where(e => e.PainLevel >= query.MinPain.Value);
            if (query.MaxPain.HasValue)
                entriesQuery = entriesQuery.Where(e => e.PainLevel <= query.MaxPain.Value);

            // Date range
            if (query.StartDate.HasValue)
                entriesQuery = entriesQuery.Where(e => e.EntryDate >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                entriesQuery = entriesQuery.Where(e => e.EntryDate <= query.EndDate.Value);

            // Tag filter
            if (!string.IsNullOrWhiteSpace(query.Tag))
                entriesQuery = entriesQuery.Where(e => e.Tags.Any(t => t.Tag == query.Tag));

            var totalCount = await entriesQuery.CountAsync(ct);

            // Sorting
            entriesQuery = query.SortBy?.ToLower() switch
            {
                "title" => query.SortDescending
                    ? entriesQuery.OrderByDescending(e => e.Title)
                    : entriesQuery.OrderBy(e => e.Title),
                "mood" => query.SortDescending
                    ? entriesQuery.OrderByDescending(e => e.MoodLevel)
                    : entriesQuery.OrderBy(e => e.MoodLevel),
                _ => query.SortDescending
                    ? entriesQuery.OrderByDescending(e => e.EntryDate)
                    : entriesQuery.OrderBy(e => e.EntryDate),
            };

            var entries = await entriesQuery
                .Include(e => e.Tags)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var items = entries.Select(MapToDto).ToList();

            return ServiceResult<PagedResult<JournalEntryDto>>.Success(new PagedResult<JournalEntryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
            });
        }

        public async Task<ServiceResult<JournalEntryDetailDto>> GetEntryByIdAsync(
            Guid patientId, Guid entryId, CancellationToken ct = default)
        {
            var entry = await _unitOfWork.JournalEntries.GetByIdAsync(entryId, ct);
            if (entry == null || entry.PatientId != patientId || entry.IsDeleted)
                return ServiceResult<JournalEntryDetailDto>.NotFound("Journal entry not found");

            return ServiceResult<JournalEntryDetailDto>.Success(MapToDetailDto(entry));
        }

        public async Task<ServiceResult<JournalEntryDto>> CreateEntryAsync(
            Guid patientId, CreateJournalEntryDto dto, CancellationToken ct = default)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<JournalEntryDto>.NotFound("Patient not found");

            // Validate mood/pain ranges
            if (dto.MoodLevel.HasValue && (dto.MoodLevel < 1 || dto.MoodLevel > 10))
                return ServiceResult<JournalEntryDto>.Failure("Mood level must be between 1 and 10");
            if (dto.PainLevel.HasValue && (dto.PainLevel < 1 || dto.PainLevel > 10))
                return ServiceResult<JournalEntryDto>.Failure("Pain level must be between 1 and 10");

            var entry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = dto.Title,
                Content = dto.Content,
                EntryDate = dto.EntryDate,
                MoodLevel = dto.MoodLevel,
                PainLevel = dto.PainLevel,
                Symptom = dto.Symptoms != null ? string.Join(",", dto.Symptoms) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.JournalEntries.AddAsync(entry, ct);

            // Add tags
            if (dto.Tags != null && dto.Tags.Count > 0)
            {
                foreach (var tagName in dto.Tags.Distinct())
                {
                    var tag = new JournalEntryTag
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = entry.Id,
                        Tag = tagName.Trim()
                    };
                    await _unitOfWork.JournalEntryTags.AddAsync(tag, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // Reload with includes
            var created = await _unitOfWork.JournalEntries.GetByIdAsync(entry.Id, ct);
            return ServiceResult<JournalEntryDto>.Success(MapToDto(created!));
        }

        public async Task<ServiceResult<JournalEntryDto>> UpdateEntryAsync(
            Guid patientId, Guid entryId, UpdateJournalEntryDto dto, CancellationToken ct = default)
        {
            var entry = await _unitOfWork.JournalEntries.GetByIdAsync(entryId, ct);
            if (entry == null || entry.PatientId != patientId || entry.IsDeleted)
                return ServiceResult<JournalEntryDto>.NotFound("Journal entry not found");

            if (dto.MoodLevel.HasValue && (dto.MoodLevel < 1 || dto.MoodLevel > 10))
                return ServiceResult<JournalEntryDto>.Failure("Mood level must be between 1 and 10");
            if (dto.PainLevel.HasValue && (dto.PainLevel < 1 || dto.PainLevel > 10))
                return ServiceResult<JournalEntryDto>.Failure("Pain level must be between 1 and 10");

            entry.Title = dto.Title;
            entry.Content = dto.Content;
            entry.EntryDate = dto.EntryDate;
            entry.MoodLevel = dto.MoodLevel;
            entry.PainLevel = dto.PainLevel;
            entry.Symptom = dto.Symptoms != null ? string.Join(",", dto.Symptoms) : null;
            entry.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.JournalEntries.UpdateAsync(entry, ct);

            // Replace tags: remove old, add new
            var existingTags = await _unitOfWork.JournalEntryTags
                .GetByJournalEntryIdAsync(entryId, ct);

            foreach (var oldTag in existingTags)
                await _unitOfWork.JournalEntryTags.DeleteAsync(oldTag, ct);

            if (dto.Tags != null && dto.Tags.Count > 0)
            {
                foreach (var tagName in dto.Tags.Distinct())
                {
                    var tag = new JournalEntryTag
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = entryId,
                        Tag = tagName.Trim()
                    };
                    await _unitOfWork.JournalEntryTags.AddAsync(tag, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            var updated = await _unitOfWork.JournalEntries.GetByIdAsync(entryId, ct);
            return ServiceResult<JournalEntryDto>.Success(MapToDto(updated!));
        }

        public async Task<ServiceResult> DeleteEntryAsync(
            Guid patientId, Guid entryId, CancellationToken ct = default)
        {
            var entry = await _unitOfWork.JournalEntries.GetByIdAsync(entryId, ct);
            if (entry == null || entry.PatientId != patientId || entry.IsDeleted)
                return ServiceResult.NotFound("Journal entry not found");

            entry.IsDeleted = true;
            entry.DeletedAt = DateTime.UtcNow;


            await _unitOfWork.JournalEntries.UpdateAsync(entry, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }

        //public async Task<ServiceResult<JournalPhotoDto>> UploadPhotoAsync(
        //    Guid patientId, Guid entryId, IFormFile photo, CancellationToken ct = default)
        //{
        //    if (photo == null || photo.Length == 0)
        //        return ServiceResult<JournalPhotoDto>.Failure("No photo provided");

        //    if (photo.Length > MaxPhotoSizeBytes)
        //        return ServiceResult<JournalPhotoDto>.Failure($"Photo exceeds maximum size of {MaxPhotoSizeBytes / (1024 * 1024)}MB");

        //    if (!AllowedImageTypes.Contains(photo.ContentType.ToLower()))
        //        return ServiceResult<JournalPhotoDto>.Failure("Invalid image type. Allowed: JPEG, PNG");

        //    var entry = await _unitOfWork.JournalEntries.GetByIdWithDetailsAsync(entryId, ct);
        //    if (entry == null || entry.PatientId != patientId || entry.IsDeleted)
        //        return ServiceResult<JournalPhotoDto>.NotFound("Journal entry not found");

        //    var activePhotoCount = entry.Photos?.Count(p => !p.IsDeleted) ?? 0;
        //    if (activePhotoCount >= MaxPhotosPerEntry)
        //        return ServiceResult<JournalPhotoDto>.Failure($"Maximum of {MaxPhotosPerEntry} photos per entry");

        //    using var stream = photo.OpenReadStream();
        //    var uploadResult = await _fileStorage.UploadFileAsync(
        //        stream, photo.FileName, photo.ContentType, JournalPhotosContainer, ct);

        //    var journalPhoto = new JournalEntryPhoto
        //    {
        //        Id = Guid.NewGuid(),
        //        JournalEntryId = entryId,
        //        FileName = uploadResult.FileName,
        //        OriginalFileName = photo.FileName,
        //        ContentType = photo.ContentType,
        //        FileSizeBytes = uploadResult.FileSizeBytes,
        //        StoragePath = uploadResult.StoragePath,
        //        UploadedAt = DateTime.UtcNow
        //    };

        //    await _unitOfWork.JournalEntryPhotos.AddAsync(journalPhoto, ct);
        //    await _unitOfWork.SaveChangesAsync(ct);

        //    return ServiceResult<JournalPhotoDto>.Success(new JournalPhotoDto
        //    {
        //        Id = journalPhoto.Id,
        //        FileName = journalPhoto.OriginalFileName,
        //        ContentType = journalPhoto.ContentType,
        //        FileSizeBytes = journalPhoto.FileSizeBytes,
        //        UploadedAt = journalPhoto.UploadedAt
        //    });
        //}

        //public async Task<ServiceResult> DeletePhotoAsync(
        //    Guid patientId, Guid entryId, Guid photoId, CancellationToken ct = default)
        //{
        //    var entry = await _unitOfWork.JournalEntries.GetByIdAsync(entryId, ct);
        //    if (entry == null || entry.PatientId != patientId || entry.IsDeleted)
        //        return ServiceResult.NotFound("Journal entry not found");

        //    var photo = await _unitOfWork.JournalEntryPhotos.GetByIdAsync(photoId, ct);
        //    if (photo == null || photo.JournalEntryId != entryId || photo.IsDeleted)
        //        return ServiceResult.NotFound("Photo not found");

        //    photo.IsDeleted = true;
        //    photo.DeletedAt = DateTime.UtcNow;
        //    await _unitOfWork.JournalEntryPhotos.UpdateAsync(photo, ct);

        //    await _fileStorage.DeleteFileAsync(JournalPhotosContainer, photo.StoragePath, ct);
        //    await _unitOfWork.SaveChangesAsync(ct);

        //    return ServiceResult.Success();
        //}

        public async Task<ServiceResult<JournalStatisticsDto>> GetStatisticsAsync(
            Guid patientId, DateRangeDto? range, CancellationToken ct = default)
        {
            var entriesQuery = _unitOfWork.JournalEntries.QueryNoTracking()
                .Where(e => e.PatientId == patientId && !e.IsDeleted);

            if (range != null)
            {
                entriesQuery = entriesQuery.Where(e =>
                    e.EntryDate >= range.StartDate && e.EntryDate <= range.EndDate);
            }

            var entries = await entriesQuery
                .Include(e => e.Tags)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var allTags = entries.SelectMany(e => e.Tags.Select(t => t.Tag)).ToList();
            var allSymptoms = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Symptom))
                .SelectMany(e => e.Symptom!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

            return ServiceResult<JournalStatisticsDto>.Success(new JournalStatisticsDto
            {
                TotalEntries = entries.Count,
                EntriesThisMonth = entries.Count(e => e.EntryDate >= startOfMonth),
                AverageMood = entries.Where(e => e.MoodLevel.HasValue).Select(e => e.MoodLevel!.Value).DefaultIfEmpty(0).Average(),
                AveragePain = entries.Where(e => e.PainLevel.HasValue).Select(e => e.PainLevel!.Value).DefaultIfEmpty(0).Average(),
                MostCommonSymptoms = allSymptoms
                    .GroupBy(s => s.ToLower())
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList(),
                MostUsedTags = allTags
                    .GroupBy(t => t.ToLower())
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList()
            });
        }

        public async Task<ServiceResult<List<string>>> GetTagsAsync(
            Guid patientId, CancellationToken ct = default)
        {
            var tags = await _unitOfWork.JournalEntries.QueryNoTracking()
                .Where(e => e.PatientId == patientId && !e.IsDeleted)
                .SelectMany(e => e.Tags.Select(t => t.Tag))
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync(ct);

            return ServiceResult<List<string>>.Success(tags);
        }

        public async Task<ServiceResult<byte[]>> ExportJournalAsync(
            Guid patientId, string format, CancellationToken ct = default)
        {
            // Simple text/CSV export — PDF generation would require a library like QuestPDF
            var entries = await _unitOfWork.JournalEntries.QueryNoTracking()
                .Where(e => e.PatientId == patientId && !e.IsDeleted)
                .Include(e => e.Tags)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync(ct);

            if (!entries.Any())
                return ServiceResult<byte[]>.Failure("No entries to export");

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);

            await writer.WriteLineAsync("Title,Date,Mood,Pain,Symptoms,Tags,Content");
            foreach (var entry in entries)
            {
                var symptoms = entry.Symptom ?? "";
                var tags = string.Join(";", entry.Tags.Select(t => t.Tag));
                var content = entry.Content.Replace("\"", "\"\"");
                await writer.WriteLineAsync(
                    $"\"{entry.Title}\",\"{entry.EntryDate:yyyy-MM-dd}\",{entry.MoodLevel},{entry.PainLevel},\"{symptoms}\",\"{tags}\",\"{content}\"");
            }

            await writer.FlushAsync(ct);
            return ServiceResult<byte[]>.Success(ms.ToArray());
        }

        // --- Mapping ---
        private static JournalEntryDto MapToDto(JournalEntry entry) => new()
        {
            Id = entry.Id,
            Title = entry.Title,
            Excerpt = entry.Content.Length > ExcerptMaxLength
                ? entry.Content[..ExcerptMaxLength] + "..."
                : entry.Content,
            EntryDate = entry.EntryDate,
            MoodLevel = entry.MoodLevel,
            PainLevel = entry.PainLevel,
            Symptoms = !string.IsNullOrWhiteSpace(entry.Symptom)
                ? entry.Symptom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new(),
            Tags = entry.Tags?.Select(t => t.Tag).ToList() ?? new(),
            CreatedAt = entry.CreatedAt,
        };

        private static JournalEntryDetailDto MapToDetailDto(JournalEntry entry) => new()
        {
            Id = entry.Id,
            Title = entry.Title,
            Excerpt = entry.Content.Length > ExcerptMaxLength
                ? entry.Content[..ExcerptMaxLength] + "..."
                : entry.Content,
            Content = entry.Content,
            EntryDate = entry.EntryDate,
            MoodLevel = entry.MoodLevel,
            PainLevel = entry.PainLevel,
            Symptoms = !string.IsNullOrWhiteSpace(entry.Symptom)
                ? entry.Symptom.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new(),
            Tags = entry.Tags?.Select(t => t.Tag).ToList() ?? new(),
            CreatedAt = entry.CreatedAt
        };
    }
}