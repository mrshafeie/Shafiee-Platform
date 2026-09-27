using System.IO;

namespace Shafiee.CLI.PipelineSteps;

public static class SharedGeneratorPipelineStep
{
    public static void Execute(string projectRootPath)
    {
        string sharedDir = Path.Combine(projectRootPath, "Project.BuildingBlocks.Shared");

        // ایجاد دایرکتوری‌های اصلی
        Directory.CreateDirectory(Path.Combine(sharedDir, "Abstractions"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Results"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Exceptions"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Models"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Pagination"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Extensions"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Helpers", "Persian"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "Helpers", "Numbers"));

        // ۱. ساخت فایل‌های Abstractions
        GenerateAbstractionFiles(sharedDir);

        // ۲. ساخت فایل‌های Results
        GenerateResultFiles(sharedDir);

        // ۳. ساخت فایل‌های Exceptions
        GenerateExceptionFiles(sharedDir);

        // ۴. ساخت فایل‌های Models
        GenerateModelFiles(sharedDir);

        // ۵. ساخت فایل‌های Pagination
        GeneratePaginationFiles(sharedDir);

        // ۶. ساخت فایل‌های Extensions
        GenerateExtensionFiles(sharedDir);

        // ۷. ساخت فایل‌های Helpers (Persian: Text, Date, Calendar)
        GeneratePersianHelperFiles(sharedDir);

        // ۸. ساخت فایل‌های Number Helpers (Persian, NumberToWords)
        GenerateNumberHelperFiles(sharedDir);
    }

    private static void GenerateAbstractionFiles(string baseDir)
    {
        string absDir = Path.Combine(baseDir, "Abstractions");

        File.WriteAllText(Path.Combine(absDir, "IClock.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }
}");

        File.WriteAllText(Path.Combine(absDir, "ICurrentUser.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
}");

        File.WriteAllText(Path.Combine(absDir, "IExecutionContext.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IExecutionContext
{
    string? CorrelationId { get; }
    string? RequestId { get; }
    string? TraceId { get; }
    string? ClientIp { get; }
    string? UserAgent { get; }
}");

        File.WriteAllText(Path.Combine(absDir, "IIdGenerator.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IIdGenerator
{
    Guid NewGuid();
}");
    }

    private static void GenerateResultFiles(string baseDir)
    {
        string resultsDir = Path.Combine(baseDir, "Results");

        File.WriteAllText(Path.Combine(resultsDir, "ResultStatus.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Results;

public enum ResultStatus
{
    Success = 200,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    Error = 500
}");

        File.WriteAllText(Path.Combine(resultsDir, "ResultError.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Results;

public sealed record ResultError
{
    public ResultError(string code, string message, string? field = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(""Error code cannot be null or whitespace."", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(""Error message cannot be null or whitespace."", nameof(message));

        Code = code.Trim();
        Message = message.Trim();
        Field = string.IsNullOrWhiteSpace(field) ? null : field.Trim();
    }

    public string Code { get; }
    public string Message { get; }
    public string? Field { get; }
}");

        File.WriteAllText(Path.Combine(resultsDir, "Result.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Results;

public class Result
{
    private static readonly IReadOnlyList<ResultError> EmptyErrors = Array.Empty<ResultError>();

    protected Result(bool isSuccess, IReadOnlyList<ResultError> errors)
    {
        if (isSuccess && errors.Count > 0)
            throw new ArgumentException(""A successful result cannot contain errors."", nameof(errors));
        if (!isSuccess && errors.Count == 0)
            throw new ArgumentException(""A failed result must contain at least one error."", nameof(errors));

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<ResultError> Errors { get; }

    public static Result Success() => new(true, EmptyErrors);

    public static Result Failure(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure([error]);
    }

    public static Result Failure(IEnumerable<ResultError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.Where(static e => e is not null).ToArray();
        if (errorList.Length == 0)
            throw new ArgumentException(""At least one error is required."", nameof(errors));

        return new Result(false, errorList);
    }
}");

        File.WriteAllText(Path.Combine(resultsDir, "ResultT.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Results;

public sealed class Result<TValue> : Result
{
    private Result(TValue value) : base(true, Array.Empty<ResultError>())
    {
        Value = value;
    }

    private Result(IReadOnlyList<ResultError> errors) : base(false, errors)
    {
        Value = default;
    }

    public TValue? Value { get; }

    public static Result<TValue> Success(TValue value) => new(value);

    public new static Result<TValue> Failure(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>([error]);
    }

    public new static Result<TValue> Failure(IEnumerable<ResultError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.Where(static e => e is not null).ToArray();
        if (errorList.Length == 0)
            throw new ArgumentException(""At least one error is required."", nameof(errors));

        return new Result<TValue>(errorList);
    }
}");
    }

    private static void GenerateExceptionFiles(string baseDir)
    {
        string exceptionsDir = Path.Combine(baseDir, "Exceptions");

        File.WriteAllText(Path.Combine(exceptionsDir, "BusinessException.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Exceptions;

public class BusinessException : Exception
{
    public BusinessException() { }
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}");

        File.WriteAllText(Path.Combine(exceptionsDir, "ValidationException.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(""One or more validation errors occurred."")
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
            throw new ArgumentException(""At least one validation error is required."", nameof(errors));

        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}");

        File.WriteAllText(Path.Combine(exceptionsDir, "NotFoundException.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object resourceId)
        : base($""{resourceName} with identifier '{resourceId}' was not found."")
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException(""Resource name cannot be null or whitespace."", nameof(resourceName));
        ArgumentNullException.ThrowIfNull(resourceId);

        ResourceName = resourceName.Trim();
        ResourceId = resourceId;
    }

    public string ResourceName { get; }
    public object ResourceId { get; }
}");

        File.WriteAllText(Path.Combine(exceptionsDir, "ConflictException.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(""Conflict message cannot be null or whitespace."", nameof(message));
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(""Conflict message cannot be null or whitespace."", nameof(message));
    }
}");

        File.WriteAllText(Path.Combine(exceptionsDir, "UnauthorizedException.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Exceptions;

public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException() : base(""The current user is not authorized to perform this operation."") { }
    
    public UnauthorizedException(string message) : base(message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(""Authorization message cannot be null or whitespace."", nameof(message));
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(""Authorization message cannot be null or whitespace."", nameof(message));
    }
}");
    }

    private static void GenerateModelFiles(string baseDir)
    {
        string modelsDir = Path.Combine(baseDir, "Models");

        File.WriteAllText(Path.Combine(modelsDir, "KeyValueModel.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Models;

public sealed record KeyValueModel<TKey, TValue>
{
    public KeyValueModel(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public TKey Key { get; init; }
    public TValue Value { get; init; }
}");

        File.WriteAllText(Path.Combine(modelsDir, "SelectItemModel.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Models;

public sealed record SelectItemModel<TValue>
{
    public SelectItemModel(TValue value, string text, bool selected = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(""Display text cannot be null or whitespace."", nameof(text));

        Value = value;
        Text = text.Trim();
        Selected = selected;
    }

    public TValue Value { get; init; }
    public string Text { get; init; }
    public bool Selected { get; init; }
}");

        File.WriteAllText(Path.Combine(modelsDir, "LookupModel.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Models;

public sealed record LookupModel<TId>
{
    public LookupModel(TId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(""Lookup name cannot be null or whitespace."", nameof(name));

        Id = id;
        Name = name.Trim();
    }

    public TId Id { get; init; }
    public string Name { get; init; }
}");
    }

    private static void GeneratePaginationFiles(string baseDir)
    {
        string paginationDir = Path.Combine(baseDir, "Pagination");

        File.WriteAllText(Path.Combine(paginationDir, "PageRequest.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public PageRequest(int page = DefaultPage, int pageSize = DefaultPageSize)
    {
        Page = NormalizePage(page);
        PageSize = NormalizePageSize(pageSize);
    }

    public int Page { get; init; }
    public int PageSize { get; init; }

    public int Skip => checked((Page - 1) * PageSize);

    public static PageRequest Create(int page = DefaultPage, int pageSize = DefaultPageSize) =>
        new(page, pageSize);

    private static int NormalizePage(int page) => page < 1 ? DefaultPage : page;

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1) return DefaultPageSize;
        return Math.Min(pageSize, MaxPageSize);
    }
}");

        File.WriteAllText(Path.Combine(paginationDir, "PaginationMetadata.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PaginationMetadata
{
    public PaginationMetadata(int page, int pageSize, long totalCount)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), page, ""Page must be greater than zero."");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, ""Page size must be greater than zero."");
        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, ""Total count cannot be negative."");

        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = CalculateTotalPages(totalCount, pageSize);
    }

    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public int TotalPages { get; init; }

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    private static int CalculateTotalPages(long totalCount, int pageSize)
    {
        if (totalCount == 0) return 0;
        return checked((int)Math.Ceiling(totalCount / (double)pageSize));
    }
}");

        File.WriteAllText(Path.Combine(paginationDir, "PageResponse.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PageResponse<T>
{
    private PageResponse(IReadOnlyList<T> items, PaginationMetadata metadata)
    {
        Items = items;
        Metadata = metadata;
    }

    public IReadOnlyList<T> Items { get; }
    public PaginationMetadata Metadata { get; }

    public static PageResponse<T> Create(IEnumerable<T> items, int page, int pageSize, long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        var itemList = items.ToArray();
        var metadata = new PaginationMetadata(page, pageSize, totalCount);

        return new PageResponse<T>(itemList, metadata);
    }

    public static PageResponse<T> Create(IEnumerable<T> items, PageRequest request, long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);

        return Create(items, request.Page, request.PageSize, totalCount);
    }
}");
    }

    private static void GenerateExtensionFiles(string baseDir)
    {
        string extensionsDir = Path.Combine(baseDir, "Extensions");

        File.WriteAllText(Path.Combine(extensionsDir, "StringExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string? NullIfWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string TrimSafe(this string? value) => value?.Trim() ?? string.Empty;

    public static bool Contains(this string? source, string? value, StringComparison comparison)
    {
        if (source is null || value is null) return false;
        return source.Contains(value, comparison);
    }
}");

        File.WriteAllText(Path.Combine(extensionsDir, "DateTimeExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime StartOfDay(this DateTime value) => value.Date;

    public static DateTime EndOfDay(this DateTime value) => value.Date.AddDays(1).AddTicks(-1);

    public static DateTime StartOfMonth(this DateTime value) =>
        new DateTime(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    public static DateTime EndOfMonth(this DateTime value) =>
        value.StartOfMonth().AddMonths(1).AddTicks(-1);

    public static bool IsBetween(this DateTime value, DateTime start, DateTime end, bool inclusive = true)
    {
        if (start > end)
            throw new ArgumentException(""The start date cannot be greater than the end date."", nameof(start));

        return inclusive ? value >= start && value <= end : value > start && value < end;
    }
}");

        File.WriteAllText(Path.Combine(extensionsDir, "EnumerableExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) =>
        source ?? Enumerable.Empty<T>();

    public static void ForEach<T>(this IEnumerable<T>? source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (source is null) return;

        foreach (var item in source)
        {
            action(item);
        }
    }
}");

        File.WriteAllText(Path.Combine(extensionsDir, "EnumExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class EnumExtensions
{
    public static string? GetName<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        Enum.GetName(value);

    public static bool IsDefined<TEnum>(this TEnum value) where TEnum : struct, Enum =>
        Enum.IsDefined(value);
}");

        File.WriteAllText(Path.Combine(extensionsDir, "ObjectExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class ObjectExtensions
{
    public static bool IsNull<T>(this T? value) => value is null;

    public static bool IsNotNull<T>(this T? value) => value is not null;
}");

        File.WriteAllText(Path.Combine(extensionsDir, "CollectionExtensions.cs"), @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class CollectionExtensions
{
    public static bool AddIfNotNull<T>(this ICollection<T> collection, T? item) where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (item is null) return false;

        collection.Add(item);
        return true;
    }

    public static int AddRangeIfNotNull<T>(this ICollection<T> collection, IEnumerable<T?>? items) where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (items is null) return 0;

        var count = 0;
        foreach (var item in items)
        {
            if (item is null) continue;
            collection.Add(item);
            count++;
        }

        return count;
    }
}");
    }

    private static void GeneratePersianHelperFiles(string baseDir)
    {
        string persianHelpersDir = Path.Combine(baseDir, "Helpers", "Persian");

        File.WriteAllText(Path.Combine(persianHelpersDir, "PersianTextHelper.cs"), @"using System.Text;

namespace Shafiee.BuildingBlocks.Shared.Helpers.Persian;

public static class PersianTextHelper
{
    private const char PersianYe = 'ی';
    private const char PersianKaf = 'ک';
    private const char PersianHe = 'ه';
    private const char ZeroWidthNonJoiner = '\u200C';

    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            builder.Append(NormalizeCharacter(character));
        }

        return NormalizeSpaces(builder.ToString());
    }

    public static string NormalizeForSearch(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = Normalize(text);
        normalized = normalized.Replace(ZeroWidthNonJoiner.ToString(), "" "");

        return NormalizeSpaces(normalized);
    }

    private static char NormalizeCharacter(char character)
    {
        return character switch
        {
            'ي' => PersianYe,
            'ى' => PersianYe,
            'ك' => PersianKaf,
            'ة' => PersianHe,
            'ۀ' => PersianHe,
            'ۂ' => PersianHe,
            '\u200E' => ' ',
            '\u200F' => ' ',
            '\u061C' => ' ',
            '\uFEFF' => ' ',
            _ => character
        };
    }

    private static string NormalizeSpaces(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        var previousWasWhitespace = false;

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                if (previousWasWhitespace)
                    continue;

                builder.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    public static bool ContainsPersian(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var character in text)
        {
            if (IsPersianCharacter(character))
                return true;
        }

        return false;
    }

    public static bool IsPersianCharacter(char character)
    {
        return character switch
        {
            >= '\u0600' and <= '\u06FF' => true,
            >= '\u0750' and <= '\u077F' => true,
            >= '\u08A0' and <= '\u08FF' => true,
            _ => false
        };
    }

    public static string RemoveInvisibleCharacters(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            if (IsInvisibleCharacter(character))
                continue;

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsInvisibleCharacter(char character)
    {
        return character switch
        {
            '\u200B' => true,
            '\u200D' => true,
            '\u200E' => true,
            '\u200F' => true,
            '\u061C' => true,
            '\uFEFF' => true,
            _ => false
        };
    }
}");

        File.WriteAllText(Path.Combine(persianHelpersDir, "PersianDateHelper.cs"), @"using System.Globalization;

namespace Shafiee.BuildingBlocks.Shared.Helpers.Persian;

public static class PersianDateHelper
{
    private static readonly PersianCalendar Calendar = new();

    public static (int Year, int Month, int Day) ToPersianDate(DateTime date)
    {
        return (
            Calendar.GetYear(date),
            Calendar.GetMonth(date),
            Calendar.GetDayOfMonth(date));
    }

    public static (int Year, int Month, int Day) ToPersianDate(DateTimeOffset date)
    {
        return (
            Calendar.GetYear(date.DateTime),
            Calendar.GetMonth(date.DateTime),
            Calendar.GetDayOfMonth(date.DateTime));
    }

    public static DateTime FromPersianDate(int year, int month, int day)
    {
        ValidateDate(year, month, day);
        return Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }

    public static DateTime FromPersianDate(int year, int month, int day, int hour, int minute = 0, int second = 0, int millisecond = 0)
    {
        ValidateDate(year, month, day);
        return Calendar.ToDateTime(year, month, day, hour, minute, second, millisecond);
    }

    public static int GetYear(DateTime date) => Calendar.GetYear(date);

    public static int GetMonth(DateTime date) => Calendar.GetMonth(date);

    public static int GetDay(DateTime date) => Calendar.GetDayOfMonth(date);

    public static DayOfWeek GetDayOfWeek(DateTime date) => date.DayOfWeek;

    public static bool IsValidDate(int year, int month, int day)
    {
        if (year < 1 || month < 1 || day < 1)
            return false;

        try
        {
            Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static int GetDaysInMonth(int year, int month)
    {
        ValidateMonth(year, month);
        return Calendar.GetDaysInMonth(year, month);
    }

    public static bool IsLeapYear(int year)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), year, ""Persian year must be greater than zero."");

        return Calendar.IsLeapYear(year);
    }

    public static DateTime GetStartOfYear(int year) => FromPersianDate(year, 1, 1);

    public static DateTime GetEndOfYear(int year)
    {
        var lastMonth = 12;
        var lastDay = Calendar.GetDaysInMonth(year, lastMonth);

        return FromPersianDate(year, lastMonth, lastDay)
            .Date
            .AddDays(1)
            .AddTicks(-1);
    }

    public static DateTime GetStartOfMonth(int year, int month)
    {
        ValidateMonth(year, month);
        return FromPersianDate(year, month, 1);
    }

    public static DateTime GetEndOfMonth(int year, int month)
    {
        ValidateMonth(year, month);
        var lastDay = Calendar.GetDaysInMonth(year, month);

        return FromPersianDate(year, month, lastDay)
            .Date
            .AddDays(1)
            .AddTicks(-1);
    }

    public static (int Year, int Month, int Day) Today() => ToPersianDate(DateTime.Now);

    public static string Format(DateTime date, string format = ""yyyy/MM/dd"")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        return date.ToString(
            format,
            CultureInfo.InvariantCulture
                .WithCalendar(Calendar));
    }

    public static DateTime Parse(string value, string format = ""yyyy/MM/dd"")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        return DateTime.ParseExact(
            value.Trim(),
            format,
            CultureInfo.InvariantCulture
                .WithCalendar(Calendar),
            DateTimeStyles.None);
    }

    private static void ValidateDate(int year, int month, int day)
    {
        if (!IsValidDate(year, month, day))
        {
            throw new ArgumentOutOfRangeException(
                nameof(day),
                $""Invalid Persian date: {year:0000}/{month:00}/{day:00}."");
        }
    }

    private static void ValidateMonth(int year, int month)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), year, ""Persian year must be greater than zero."");

        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), month, ""Persian month must be between 1 and 12."");
    }

    private static CultureInfo WithCalendar(this CultureInfo culture, Calendar calendar)
    {
        var clone = (CultureInfo)culture.Clone();
        clone.DateTimeFormat.Calendar = calendar;
        return clone;
    }
}");

        File.WriteAllText(Path.Combine(persianHelpersDir, "PersianCalendarHelper.cs"), @"using System.Globalization;

namespace Shafiee.BuildingBlocks.Shared.Helpers.Persian;

public static class PersianCalendarHelper
{
    private static readonly PersianCalendar Calendar = new();

    public static (int Year, int Month, int Day) GetDateParts(DateTime date)
    {
        return (
            Calendar.GetYear(date),
            Calendar.GetMonth(date),
            Calendar.GetDayOfMonth(date));
    }

    public static int GetMonth(DateTime date) => Calendar.GetMonth(date);

    public static int GetYear(DateTime date) => Calendar.GetYear(date);

    public static int GetDay(DateTime date) => Calendar.GetDayOfMonth(date);

    public static int GetQuarter(DateTime date)
    {
        var month = GetMonth(date);
        return ((month - 1) / 3) + 1;
    }

    public static int GetQuarterStartMonth(int quarter)
    {
        ValidateQuarter(quarter);
        return ((quarter - 1) * 3) + 1;
    }

    public static int GetQuarterEndMonth(int quarter)
    {
        ValidateQuarter(quarter);
        return quarter * 3;
    }

    public static DateTime GetStartOfQuarter(DateTime date)
    {
        var year = GetYear(date);
        var quarter = GetQuarter(date);
        var month = GetQuarterStartMonth(quarter);

        return PersianDateHelper.FromPersianDate(year, month, 1);
    }

    public static DateTime GetEndOfQuarter(DateTime date)
    {
        var year = GetYear(date);
        var quarter = GetQuarter(date);
        var month = GetQuarterEndMonth(quarter);
        var day = Calendar.GetDaysInMonth(year, month);

        return PersianDateHelper
            .FromPersianDate(year, month, day)
            .Date
            .AddDays(1)
            .AddTicks(-1);
    }

    public static DateTime GetStartOfYear(DateTime date)
    {
        var year = GetYear(date);
        return PersianDateHelper.FromPersianDate(year, 1, 1);
    }

    public static DateTime GetEndOfYear(DateTime date)
    {
        var year = GetYear(date);
        var lastDay = Calendar.GetDaysInMonth(year, 12);

        return PersianDateHelper
            .FromPersianDate(year, 12, lastDay)
            .Date
            .AddDays(1)
            .AddTicks(-1);
    }

    public static DateTime GetStartOfMonth(DateTime date)
    {
        var year = GetYear(date);
        var month = GetMonth(date);

        return PersianDateHelper.FromPersianDate(year, month, 1);
    }

    public static DateTime GetEndOfMonth(DateTime date)
    {
        var year = GetYear(date);
        var month = GetMonth(date);
        var lastDay = Calendar.GetDaysInMonth(year, month);

        return PersianDateHelper
            .FromPersianDate(year, month, lastDay)
            .Date
            .AddDays(1)
            .AddTicks(-1);
    }

    public static int GetDaysInYear(int year)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), year, ""Persian year must be greater than zero."");

        return Calendar.GetDaysInYear(year);
    }

    public static bool IsLeapYear(int year)
    {
        if (year < 1)
            throw new ArgumentOutOfRangeException(nameof(year), year, ""Persian year must be greater than zero."");

        return Calendar.IsLeapYear(year);
    }

    public static (DateTime Start, DateTime End) GetMonthRange(DateTime date) =>
        (GetStartOfMonth(date), GetEndOfMonth(date));

    public static (DateTime Start, DateTime End) GetQuarterRange(DateTime date) =>
        (GetStartOfQuarter(date), GetEndOfQuarter(date));

    public static (DateTime Start, DateTime End) GetYearRange(DateTime date) =>
        (GetStartOfYear(date), GetEndOfYear(date));

    public static bool IsSameYear(DateTime first, DateTime second) =>
        GetYear(first) == GetYear(second);

    public static bool IsSameMonth(DateTime first, DateTime second) =>
        IsSameYear(first, second) && GetMonth(first) == GetMonth(second);

    public static bool IsSameQuarter(DateTime first, DateTime second) =>
        IsSameYear(first, second) && GetQuarter(first) == GetQuarter(second);

    public static int GetDayOfYear(DateTime date) => Calendar.GetDayOfYear(date);

    public static string GetMonthName(int month)
    {
        return month switch
        {
            1 => ""فروردین"",
            2 => ""اردیبهشت"",
            3 => ""خرداد"",
            4 => ""تیر"",
            5 => ""مرداد"",
            6 => ""شهریور"",
            7 => ""مهر"",
            8 => ""آبان"",
            9 => ""آذر"",
            10 => ""دی"",
            11 => ""بهمن"",
            12 => ""اسفند"",
            _ => throw new ArgumentOutOfRangeException(nameof(month), month, ""Persian month must be between 1 and 12."")
        };
    }

    public static string GetQuarterName(int quarter)
    {
        ValidateQuarter(quarter);
        return $""سه‌ماهه {quarter}"";
    }

    private static void ValidateQuarter(int quarter)
    {
        if (quarter is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(quarter), quarter, ""Persian quarter must be between 1 and 4."");
        }
    }
}");
    }

    private static void GenerateNumberHelperFiles(string baseDir)
    {
        string numbersDir = Path.Combine(baseDir, "Helpers", "Numbers");

        // ۱. PersianNumberHelper.cs
        File.WriteAllText(Path.Combine(numbersDir, "PersianNumberHelper.cs"), @"using System.Globalization;
using System.Text;

namespace Shafiee.BuildingBlocks.Shared.Helpers.Numbers;

public static class PersianNumberHelper
{
    private const char PersianZero = '۰';
    private const char ArabicZero = '٠';
    private const char LatinZero = '0';

    public static string ToLatinDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return ConvertDigits(value, LatinZero);
    }

    public static string ToPersianDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return ConvertDigits(value, PersianZero);
    }

    public static string ToArabicDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return ConvertDigits(value, ArabicZero);
    }

    public static string Normalize(string? value)
    {
        return ToLatinDigits(value);
    }

    public static bool IsDigit(char character)
    {
        return IsLatinDigit(character)
            || IsPersianDigit(character)
            || IsArabicDigit(character);
    }

    public static bool IsLatinDigit(char character)
    {
        return character is >= '0' and <= '9';
    }

    public static bool IsPersianDigit(char character)
    {
        return character is >= '۰' and <= '۹';
    }

    public static bool IsArabicDigit(char character)
    {
        return character is >= '٠' and <= '٩';
    }

    public static int ToInt32(char character)
    {
        if (IsLatinDigit(character))
            return character - LatinZero;

        if (IsPersianDigit(character))
            return character - PersianZero;

        if (IsArabicDigit(character))
            return character - ArabicZero;

        throw new ArgumentException(
            $""The character '{character}' is not a supported digit."",
            nameof(character));
    }

    public static bool ContainsDigit(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var character in value)
        {
            if (IsDigit(character))
                return true;
        }

        return false;
    }

    public static string ExtractDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var character in value)
        {
            if (!IsDigit(character))
                continue;

            builder.Append(ToInt32(character));
        }

        return builder.ToString();
    }

    public static string ToPersianDigits<T>(T value)
        where T : IFormattable
    {
        return ToPersianDigits(
            value.ToString(null, CultureInfo.InvariantCulture));
    }

    public static string ToLatinDigits<T>(T value)
        where T : IFormattable
    {
        return ToLatinDigits(
            value.ToString(null, CultureInfo.InvariantCulture));
    }

    public static string ToArabicDigits<T>(T value)
        where T : IFormattable
    {
        return ToArabicDigits(
            value.ToString(null, CultureInfo.InvariantCulture));
    }

    public static bool TryParseInt(string? value, out int result)
    {
        var normalized = ToLatinDigits(value);

        return int.TryParse(
            normalized,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result);
    }

    public static bool TryParseLong(string? value, out long result)
    {
        var normalized = ToLatinDigits(value);

        return long.TryParse(
            normalized,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result);
    }

    public static bool TryParseDecimal(string? value, out decimal result)
    {
        var normalized = ToLatinDigits(value);

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static string ConvertDigits(string value, char targetZero)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (!IsDigit(character))
            {
                builder.Append(character);
                continue;
            }

            var digit = ToInt32(character);

            builder.Append((char)(targetZero + digit));
        }

        return builder.ToString();
    }
}");

        // ۲. NumberToWordsHelper.cs
        File.WriteAllText(Path.Combine(numbersDir, "NumberToWordsHelper.cs"), @"using System.Globalization;

namespace Shafiee.BuildingBlocks.Shared.Helpers.Numbers;

/// <summary>
/// Provides conversion of numeric values to Persian words.
/// </summary>
public static class NumberToWordsHelper
{
    private static readonly string[] Units =
    [
        ""صفر"",
        ""یک"",
        ""دو"",
        ""سه"",
        ""چهار"",
        ""پنج"",
        ""شش"",
        ""هفت"",
        ""هشت"",
        ""نه""
    ];

    private static readonly string[] Teens =
    [
        ""ده"",
        ""یازده"",
        ""دوازده"",
        ""سیزده"",
        ""چهارده"",
        ""پانزده"",
        ""شانزده"",
        ""هفده"",
        ""هجده"",
        ""نوزده""
    ];

    private static readonly string[] Tens =
    [
        """",
        """",
        ""بیست"",
        ""سی"",
        ""چهل"",
        ""پنجاه"",
        ""شصت"",
        ""هفتاد"",
        ""هشتاد"",
        ""نود""
    ];

    private static readonly string[] Hundreds =
    [
        """",
        ""صد"",
        ""دویست"",
        ""سیصد"",
        ""چهارصد"",
        ""پانصد"",
        ""ششصد"",
        ""هفتصد"",
        ""هشتصد"",
        ""نهصد""
    ];

    private static readonly string[] Scales =
    [
        """",
        ""هزار"",
        ""میلیون"",
        ""میلیارد"",
        ""تریلیون"",
        ""کوادریلیون"",
        ""کوینتیلیون""
    ];

    /// <summary>
    /// Converts a signed 64-bit integer to Persian words.
    /// </summary>
    public static string ToWords(long value)
    {
        if (value == 0)
            return Units[0];

        if (value < 0)
            return $""منفی {ToWordsPositive(value)}"";

        return ToWordsPositive(value);
    }

    /// <summary>
    /// Converts an integer to Persian words.
    /// </summary>
    public static string ToWords(int value)
    {
        return ToWords((long)value);
    }

    /// <summary>
    /// Converts an unsigned 64-bit integer to Persian words.
    /// </summary>
    public static string ToWords(ulong value)
    {
        if (value == 0)
            return Units[0];

        return ToWordsPositive(value);
    }

    /// <summary>
    /// Converts a decimal value to Persian words.
    /// </summary>
    /// <remarks>
    /// The integer and fractional parts are returned separately.
    /// </remarks>
    public static string ToWords(
        decimal value,
        string fractionalSeparator = "" و "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            fractionalSeparator);

        var negative = value < 0;
        var absolute = decimal.Abs(value);

        var integerPart = decimal.Truncate(absolute);
        var fractionalPart = absolute - integerPart;

        var integerText = ToWords(
            decimal.ToInt64(integerPart));

        if (fractionalPart == 0)
            return negative
                ? $""منفی {integerText}""
                : integerText;

        var fractionalText =
            FractionToWords(fractionalPart);

        var result =
            $""{integerText}{fractionalSeparator}{fractionalText}"";

        return negative
            ? $""منفی {result}""
            : result;
    }

    private static string ToWordsPositive(long value)
    {
        return ToWordsPositive((ulong)value);
    }

    private static string ToWordsPositive(ulong value)
    {
        var parts = new List<string>();

        var scaleIndex = 0;

        while (value > 0)
        {
            var group = value % 1000;

            if (group > 0)
            {
                var groupText = ThreeDigitToWords(
                    (int)group);

                var scale = Scales[scaleIndex];

                if (!string.IsNullOrEmpty(scale))
                {
                    groupText =
                        $""{groupText} {scale}"";
                }

                parts.Insert(
                    0,
                    groupText);
            }

            value /= 1000;
            scaleIndex++;
        }

        return string.Join(
            "" و "",
            parts);
    }

    private static string ThreeDigitToWords(
        int value)
    {
        if (value is < 0 or > 999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                ""Value must be between 0 and 999."");
        }

        if (value == 0)
            return string.Empty;

        var parts = new List<string>();

        var hundreds = value / 100;
        var remainder = value % 100;

        if (hundreds > 0)
        {
            parts.Add(Hundreds[hundreds]);
        }

        if (remainder > 0)
        {
            if (remainder < 10)
            {
                parts.Add(Units[remainder]);
            }
            else if (remainder < 20)
            {
                parts.Add(Teens[remainder - 10]);
            }
            else
            {
                var tens = remainder / 10;
                var units = remainder % 10;

                parts.Add(Tens[tens]);

                if (units > 0)
                {
                    parts.Add(Units[units]);
                }
            }
        }

        return string.Join(
            "" و "",
            parts);
    }

    private static string FractionToWords(
        decimal fractionalPart)
    {
        var text = fractionalPart
            .ToString(
                ""0.##################"",
                CultureInfo.InvariantCulture)
            .TrimStart('0')
            .TrimStart('.');

        if (string.IsNullOrEmpty(text))
            return Units[0];

        var digits = text
            .Select(character => character - '0')
            .ToArray();

        var parts = digits
            .Select(digit => Units[digit])
            .ToArray();

        return string.Join(
            "" "",
            parts);
    }
}");
    }
}