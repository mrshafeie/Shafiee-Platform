namespace Shafiee.CLI.PipelineSteps;

using Shafiee.SDK.Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SharedGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        string? solutionName = context.Command.GetOption("solution");
        if (string.IsNullOrEmpty(solutionName) || solutionName.Equals("shared", StringComparison.OrdinalIgnoreCase))
        {
            solutionName = "Shop";
        }

        string desktopRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionRoot = Path.Combine(desktopRoot, solutionName);
        string projectDir = Path.Combine(solutionRoot, "src", "BuildingBlocks", $"{solutionName}.BuildingBlocks.Shared", $"{solutionName}.BuildingBlocks.Shared");

        var files = GetSharedSourceFiles(projectDir);

        int successCount = 0;
        foreach (var file in files)
        {
            string targetPath = file.Key;
            string? fileDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            File.WriteAllText(targetPath, file.Value);
            Console.WriteLine($"✅ [Written] {targetPath}");
            successCount++;
        }

        Console.WriteLine($"\n📊 [Disk Summary] Successfully wrote {successCount} files directly to destination.");

        return Task.CompletedTask;
    }

    public static Dictionary<string, string> GetSharedSourceFiles(string projectDir)
    {
        return new Dictionary<string, string>
        {
             // ==========================================
            // GlobalUsings
            // ==========================================
            {
                Path.Combine(projectDir, "GlobalUsings.cs"),
                """
                global using System;
                global using System.Collections.Generic;
                global using System.Collections.ObjectModel;
                global using System.Globalization;
                global using System.Linq;
                global using System.Numerics;
                global using System.Text;
                """
            },  

            // ==========================================
            // ABSTRACTIONS
            // ==========================================
            {
                Path.Combine(projectDir, "Abstractions", "IClock.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Abstractions;

                public interface IClock
                {
                    DateTime UtcNow { get; }
                    DateTime Now { get; }
                    DateTimeOffset UtcNowOffset { get; }
                    DateTimeOffset NowOffset { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Abstractions", "ICurrentUser.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Abstractions;

                public interface ICurrentUser
                {
                    string? UserId { get; }
                    string? UserName { get; }
                    string? DisplayName { get; }
                    bool IsAuthenticated { get; }
                    IReadOnlyCollection<string> Roles { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Abstractions", "IExecutionContext.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Abstractions;

                public interface IExecutionContext
                {
                    string? CorrelationId { get; }
                    string? RequestId { get; }
                    string? TraceId { get; }
                    string? ClientIp { get; }
                    string? UserAgent { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Abstractions", "IIdGenerator.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Abstractions;

                public interface IIdGenerator
                {
                    Guid NewGuid();
                }
                """
            },

            // ==========================================
            // RESULTS
            // ==========================================
            {
                Path.Combine(projectDir, "Results", "ResultError.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Results;

                /// <summary>
                /// Represents a structured error returned by an operation.
                /// </summary>
                public sealed record ResultError
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="ResultError"/> record.
                    /// </summary>
                    /// <param name="code">A stable machine-readable error code.</param>
                    /// <param name="message">A human-readable error message.</param>
                    /// <param name="field">The related field name, when applicable.</param>
                    public ResultError(
                        string code,
                        string message,
                        string? field = null)
                    {
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            throw new ArgumentException(
                                "Error code cannot be null or whitespace.",
                                nameof(code));
                        }

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new ArgumentException(
                                "Error message cannot be null or whitespace.",
                                nameof(message));
                        }

                        Code = code.Trim();
                        Message = message.Trim();
                        Field = string.IsNullOrWhiteSpace(field)
                            ? null
                            : field.Trim();
                    }

                    /// <summary>
                    /// Gets the stable machine-readable error code.
                    /// </summary>
                    public string Code { get; }

                    /// <summary>
                    /// Gets the human-readable error message.
                    /// </summary>
                    public string Message { get; }

                    /// <summary>
                    /// Gets the related field name, when the error belongs to a specific field.
                    /// </summary>
                    public string? Field { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Results", "Result.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Results;

                /// <summary>
                /// Represents the outcome of an operation that does not return a value.
                /// </summary>
                public class Result
                {
                    private static readonly IReadOnlyList<ResultError> EmptyErrors =
                        Array.Empty<ResultError>();

                    protected Result(
                        bool isSuccess,
                        IReadOnlyList<ResultError> errors)
                    {
                        if (isSuccess && errors.Count > 0)
                        {
                            throw new ArgumentException(
                                "A successful result cannot contain errors.",
                                nameof(errors));
                        }

                        if (!isSuccess && errors.Count == 0)
                        {
                            throw new ArgumentException(
                                "A failed result must contain at least one error.",
                                nameof(errors));
                        }

                        IsSuccess = isSuccess;
                        Errors = errors;
                    }

                    /// <summary>
                    /// Gets a value indicating whether the operation was successful.
                    /// </summary>
                    public bool IsSuccess { get; }

                    /// <summary>
                    /// Gets a value indicating whether the operation failed.
                    /// </summary>
                    public bool IsFailure => !IsSuccess;

                    /// <summary>
                    /// Gets the errors associated with the result.
                    /// </summary>
                    public IReadOnlyList<ResultError> Errors { get; }

                    /// <summary>
                    /// Creates a successful result.
                    /// </summary>
                    public static Result Success()
                    {
                        return new Result(
                            isSuccess: true,
                            errors: EmptyErrors);
                    }

                    /// <summary>
                    /// Creates a failed result containing a single error.
                    /// </summary>
                    public static Result Failure(ResultError error)
                    {
                        ArgumentNullException.ThrowIfNull(error);

                        return Failure([error]);
                    }

                    /// <summary>
                    /// Creates a failed result containing multiple errors.
                    /// </summary>
                    public static Result Failure(
                        IEnumerable<ResultError> errors)
                    {
                        ArgumentNullException.ThrowIfNull(errors);

                        var errorList = errors
                            .Where(static error => error is not null)
                            .ToArray();

                        if (errorList.Length == 0)
                        {
                            throw new ArgumentException(
                                "At least one error is required.",
                                nameof(errors));
                        }

                        return new Result(
                            isSuccess: false,
                            errors: errorList);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Results", "ResultT.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Results;

                public sealed class Result<TValue> : Result
                {
                    public TValue? Value { get; }

                    private Result(TValue value) : base(true, Array.Empty<ResultError>()) => Value = value;
                    private Result(IReadOnlyList<ResultError> errors) : base(false, errors) => Value = default;

                    public static Result<TValue> Success(TValue value) => new(value);
                    public new static Result<TValue> Failure(ResultError error) => new(new[] { error });
                    public new static Result<TValue> Failure(IEnumerable<ResultError> errors) => new(errors.ToArray());
                }
                """
            },
            
            // ==========================================
            // Exceptions
            // ==========================================
            {
                Path.Combine(projectDir, "Exceptions", "BusinessException.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Exceptions;

                /// <summary>
                /// Represents an exception caused by a business rule violation.
                /// </summary>
                public class BusinessException : Exception
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="BusinessException"/> class.
                    /// </summary>
                    public BusinessException()
                    {
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="BusinessException"/> class.
                    /// </summary>
                    /// <param name="message">The business error message.</param>
                    public BusinessException(string message)
                        : base(message)
                    {
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="BusinessException"/> class.
                    /// </summary>
                    /// <param name="message">The business error message.</param>
                    /// <param name="innerException">The inner exception.</param>
                    public BusinessException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                    }
                }
                """
            },   
            {
                Path.Combine(projectDir, "Exceptions", "ValidationException.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Exceptions;

                /// <summary>
                /// Represents one or more validation failures.
                /// </summary>
                public sealed class ValidationException : Exception
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="ValidationException"/> class.
                    /// </summary>
                    /// <param name="errors">The validation errors grouped by field name.</param>
                    public ValidationException(
                        IReadOnlyDictionary<string, string[]> errors)
                        : base("One or more validation errors occurred.")
                    {
                        ArgumentNullException.ThrowIfNull(errors);

                        if (errors.Count == 0)
                        {
                            throw new ArgumentException(
                                "At least one validation error is required.",
                                nameof(errors));
                        }

                        Errors = errors;
                    }

                    /// <summary>
                    /// Gets the validation errors grouped by field name.
                    /// </summary>
                    public IReadOnlyDictionary<string, string[]> Errors { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Exceptions", "NotFoundException.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Exceptions;

                /// <summary>
                /// Represents an exception raised when a requested resource cannot be found.
                /// </summary>
                public sealed class NotFoundException : Exception
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
                    /// </summary>
                    /// <param name="resourceName">The name of the resource that was not found.</param>
                    /// <param name="resourceId">The identifier of the resource.</param>
                    public NotFoundException(
                        string resourceName,
                        object resourceId)
                        : base(
                            $"{resourceName} with identifier '{resourceId}' was not found.")
                    {
                        if (string.IsNullOrWhiteSpace(resourceName))
                        {
                            throw new ArgumentException(
                                "Resource name cannot be null or whitespace.",
                                nameof(resourceName));
                        }

                        ArgumentNullException.ThrowIfNull(resourceId);

                        ResourceName = resourceName.Trim();
                        ResourceId = resourceId;
                    }

                    /// <summary>
                    /// Gets the resource name.
                    /// </summary>
                    public string ResourceName { get; }

                    /// <summary>
                    /// Gets the resource identifier.
                    /// </summary>
                    public object ResourceId { get; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Exceptions", "ConflictException.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Exceptions;

                /// <summary>
                /// Represents an exception caused by a conflict with the current state of a resource.
                /// </summary>
                public sealed class ConflictException : Exception
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConflictException"/> class.
                    /// </summary>
                    /// <param name="message">The conflict description.</param>
                    public ConflictException(string message)
                        : base(message)
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new ArgumentException(
                                "Conflict message cannot be null or whitespace.",
                                nameof(message));
                        }
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ConflictException"/> class.
                    /// </summary>
                    /// <param name="message">The conflict description.</param>
                    /// <param name="innerException">The inner exception.</param>
                    public ConflictException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new ArgumentException(
                                "Conflict message cannot be null or whitespace.",
                                nameof(message));
                        }
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Exceptions", "UnauthorizedException.cs"),
                """
                                namespace Project.BuildingBlocks.Shared.Exceptions;

                /// <summary>
                /// Represents an authorization failure.
                /// </summary>
                public sealed class UnauthorizedException : Exception
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
                    /// </summary>
                    public UnauthorizedException()
                        : base("The current user is not authorized to perform this operation.")
                    {
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
                    /// </summary>
                    /// <param name="message">The authorization error message.</param>
                    public UnauthorizedException(string message)
                        : base(message)
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new ArgumentException(
                                "Authorization message cannot be null or whitespace.",
                                nameof(message));
                        }
                    }

                    /// <summary>
                    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
                    /// </summary>
                    /// <param name="message">The authorization error message.</param>
                    /// <param name="innerException">The inner exception.</param>
                    public UnauthorizedException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new ArgumentException(
                                "Authorization message cannot be null or whitespace.",
                                nameof(message));
                        }
                    }
                }
                """
            },
                  
            // ==========================================
            // Models
            // ==========================================
            {
                Path.Combine(projectDir, "Models", "KeyValueModel.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Models;

                /// <summary>
                /// Represents a generic key-value pair model.
                /// </summary>
                /// <typeparam name="TKey">The type of the key.</typeparam>
                /// <typeparam name="TValue">The type of the value.</typeparam>
                public sealed record KeyValueModel<TKey, TValue>
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="KeyValueModel{TKey, TValue}"/> record.
                    /// </summary>
                    /// <param name="key">The key.</param>
                    /// <param name="value">The value.</param>
                    public KeyValueModel(
                        TKey key,
                        TValue value)
                    {
                        Key = key;
                        Value = value;
                    }

                    /// <summary>
                    /// Gets the key.
                    /// </summary>
                    public TKey Key { get; init; }

                    /// <summary>
                    /// Gets the value.
                    /// </summary>
                    public TValue Value { get; init; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Models", "SelectItemModel.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Models;

                /// <summary>
                /// Represents an item that can be displayed and selected by a consumer.
                /// </summary>
                /// <typeparam name="TValue">The type of the item's value.</typeparam>
                public sealed record SelectItemModel<TValue>
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="SelectItemModel{TValue}"/> record.
                    /// </summary>
                    /// <param name="value">The underlying value.</param>
                    /// <param name="text">The display text.</param>
                    /// <param name="selected">Indicates whether the item is selected.</param>
                    public SelectItemModel(
                        TValue value,
                        string text,
                        bool selected = false)
                    {
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            throw new ArgumentException(
                                "Display text cannot be null or whitespace.",
                                nameof(text));
                        }

                        Value = value;
                        Text = text.Trim();
                        Selected = selected;
                    }

                    /// <summary>
                    /// Gets the underlying value.
                    /// </summary>
                    public TValue Value { get; init; }

                    /// <summary>
                    /// Gets the display text.
                    /// </summary>
                    public string Text { get; init; }

                    /// <summary>
                    /// Gets a value indicating whether the item is selected.
                    /// </summary>
                    public bool Selected { get; init; }
                }
                """
            },
            {
                Path.Combine(projectDir, "Models", "LookupModel.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Models;

                /// <summary>
                /// Represents a lightweight lookup model containing an identifier and display name.
                /// </summary>
                /// <typeparam name="TId">The type of the identifier.</typeparam>
                public sealed record LookupModel<TId>
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="LookupModel{TId}"/> record.
                    /// </summary>
                    /// <param name="id">The identifier.</param>
                    /// <param name="name">The display name.</param>
                    public LookupModel(
                        TId id,
                        string name)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            throw new ArgumentException(
                                "Lookup name cannot be null or whitespace.",
                                nameof(name));
                        }

                        Id = id;
                        Name = name.Trim();
                    }

                    /// <summary>
                    /// Gets the identifier.
                    /// </summary>
                    public TId Id { get; init; }

                    /// <summary>
                    /// Gets the display name.
                    /// </summary>
                    public string Name { get; init; }
                }
                """
            },


            // ==========================================
            // PAGINATION
            // ==========================================
            {
                Path.Combine(projectDir, "Pagination", "PageRequest.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Pagination;

                /// <summary>
                /// Represents pagination parameters for a collection query.
                /// </summary>
                public sealed record PageRequest
                {
                    /// <summary>
                    /// Gets the default page number.
                    /// </summary>
                    public const int DefaultPage = 1;

                    /// <summary>
                    /// Gets the default page size.
                    /// </summary>
                    public const int DefaultPageSize = 20;

                    /// <summary>
                    /// Gets the maximum allowed page size.
                    /// </summary>
                    public const int MaxPageSize = 200;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="PageRequest"/> record.
                    /// </summary>
                    /// <param name="page">The requested page number.</param>
                    /// <param name="pageSize">The requested number of items per page.</param>
                    public PageRequest(
                        int page = DefaultPage,
                        int pageSize = DefaultPageSize)
                    {
                        Page = NormalizePage(page);
                        PageSize = NormalizePageSize(pageSize);
                    }

                    /// <summary>
                    /// Gets the requested page number.
                    /// </summary>
                    public int Page { get; init; }

                    /// <summary>
                    /// Gets the requested number of items per page.
                    /// </summary>
                    public int PageSize { get; init; }

                    /// <summary>
                    /// Gets the number of items to skip.
                    /// </summary>
                    public int Skip
                    {
                        get
                        {
                            return checked((Page - 1) * PageSize);
                        }
                    }

                    /// <summary>
                    /// Creates a normalized pagination request.
                    /// </summary>
                    public static PageRequest Create(
                        int page = DefaultPage,
                        int pageSize = DefaultPageSize)
                    {
                        return new PageRequest(page, pageSize);
                    }

                    private static int NormalizePage(int page)
                    {
                        return page < 1
                            ? DefaultPage
                            : page;
                    }

                    private static int NormalizePageSize(int pageSize)
                    {
                        if (pageSize < 1)
                        {
                            return DefaultPageSize;
                        }

                        return Math.Min(pageSize, MaxPageSize);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Pagination", "PaginationMetadata.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Pagination;

                /// <summary>
                /// Contains metadata describing a paginated result.
                /// </summary>
                public sealed record PaginationMetadata
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="PaginationMetadata"/> record.
                    /// </summary>
                    /// <param name="page">The current page number.</param>
                    /// <param name="pageSize">The number of items per page.</param>
                    /// <param name="totalCount">The total number of available items.</param>
                    public PaginationMetadata(
                        int page,
                        int pageSize,
                        long totalCount)
                    {
                        if (page < 1)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(page),
                                page,
                                "Page must be greater than zero.");
                        }

                        if (pageSize < 1)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(pageSize),
                                pageSize,
                                "Page size must be greater than zero.");
                        }

                        if (totalCount < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(totalCount),
                                totalCount,
                                "Total count cannot be negative.");
                        }

                        Page = page;
                        PageSize = pageSize;
                        TotalCount = totalCount;

                        TotalPages = CalculateTotalPages(
                            totalCount,
                            pageSize);
                    }

                    /// <summary>
                    /// Gets the current page number.
                    /// </summary>
                    public int Page { get; init; }

                    /// <summary>
                    /// Gets the number of items per page.
                    /// </summary>
                    public int PageSize { get; init; }

                    /// <summary>
                    /// Gets the total number of items across all pages.
                    /// </summary>
                    public long TotalCount { get; init; }

                    /// <summary>
                    /// Gets the total number of available pages.
                    /// </summary>
                    public int TotalPages { get; init; }

                    /// <summary>
                    /// Gets a value indicating whether a previous page exists.
                    /// </summary>
                    public bool HasPreviousPage =>
                        Page > 1;

                    /// <summary>
                    /// Gets a value indicating whether a next page exists.
                    /// </summary>
                    public bool HasNextPage =>
                        Page < TotalPages;

                    private static int CalculateTotalPages(
                        long totalCount,
                        int pageSize)
                    {
                        if (totalCount == 0)
                        {
                            return 0;
                        }

                        return checked(
                            (int)Math.Ceiling(
                                totalCount / (double)pageSize));
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Pagination", "PageResponse.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Pagination;

                /// <summary>
                /// Represents a paginated collection response.
                /// </summary>
                /// <typeparam name="T">The type of items contained in the response.</typeparam>
                public sealed record PageResponse<T>
                {
                    private PageResponse(
                        IReadOnlyList<T> items,
                        PaginationMetadata metadata)
                    {
                        Items = items;
                        Metadata = metadata;
                    }

                    /// <summary>
                    /// Gets the items contained in the current page.
                    /// </summary>
                    public IReadOnlyList<T> Items { get; }

                    /// <summary>
                    /// Gets the pagination metadata.
                    /// </summary>
                    public PaginationMetadata Metadata { get; }

                    /// <summary>
                    /// Creates a paginated response.
                    /// </summary>
                    /// <param name="items">The items in the current page.</param>
                    /// <param name="page">The current page number.</param>
                    /// <param name="pageSize">The number of items per page.</param>
                    /// <param name="totalCount">The total number of items.</param>
                    public static PageResponse<T> Create(
                        IEnumerable<T> items,
                        int page,
                        int pageSize,
                        long totalCount)
                    {
                        ArgumentNullException.ThrowIfNull(items);

                        var itemList = items.ToArray();

                        var metadata = new PaginationMetadata(
                            page,
                            pageSize,
                            totalCount);

                        return new PageResponse<T>(
                            itemList,
                            metadata);
                    }

                    /// <summary>
                    /// Creates a paginated response from an existing page request.
                    /// </summary>
                    /// <param name="items">The items in the current page.</param>
                    /// <param name="request">The original pagination request.</param>
                    /// <param name="totalCount">The total number of items.</param>
                    public static PageResponse<T> Create(
                        IEnumerable<T> items,
                        PageRequest request,
                        long totalCount)
                    {
                        ArgumentNullException.ThrowIfNull(items);
                        ArgumentNullException.ThrowIfNull(request);

                        return Create(
                            items,
                            request.Page,
                            request.PageSize,
                            totalCount);
                    }
                }
                """
            },

            // ==========================================
            // EXTENSIONS
            // ==========================================
            {
                Path.Combine(projectDir, "Extensions", "StringExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for string values.
                /// </summary>
                public static class StringExtensions
                {
                    /// <summary>
                    /// Determines whether the specified string is null or empty.
                    /// </summary>
                    public static bool IsNullOrEmpty(
                        this string? value)
                    {
                        return string.IsNullOrEmpty(value);
                    }

                    /// <summary>
                    /// Determines whether the specified string is null, empty,
                    /// or consists only of white-space characters.
                    /// </summary>
                    public static bool IsNullOrWhiteSpace(
                        this string? value)
                    {
                        return string.IsNullOrWhiteSpace(value);
                    }

                    /// <summary>
                    /// Returns null when the value is null, empty,
                    /// or consists only of white-space characters;
                    /// otherwise returns a trimmed value.
                    /// </summary>
                    public static string? NullIfWhiteSpace(
                        this string? value)
                    {
                        return string.IsNullOrWhiteSpace(value)
                            ? null
                            : value.Trim();
                    }

                    /// <summary>
                    /// Returns a trimmed string or an empty string when the value is null.
                    /// </summary>
                    public static string TrimSafe(
                        this string? value)
                    {
                        return value?.Trim() ?? string.Empty;
                    }

                    /// <summary>
                    /// Determines whether the string contains the specified value
                    /// using the specified comparison.
                    /// </summary>
                    public static bool Contains(
                        this string? source,
                        string? value,
                        StringComparison comparison)
                    {
                        if (source is null || value is null)
                        {
                            return false;
                        }

                        return source.Contains(value, comparison);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Extensions", "DateTimeExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for date and time values.
                /// </summary>
                public static class DateTimeExtensions
                {
                    /// <summary>
                    /// Gets the beginning of the day.
                    /// </summary>
                    public static DateTime StartOfDay(
                        this DateTime value)
                    {
                        return value.Date;
                    }

                    /// <summary>
                    /// Gets the end of the day with millisecond precision.
                    /// </summary>
                    public static DateTime EndOfDay(
                        this DateTime value)
                    {
                        return value.Date
                            .AddDays(1)
                            .AddTicks(-1);
                    }

                    /// <summary>
                    /// Gets the beginning of the month.
                    /// </summary>
                    public static DateTime StartOfMonth(
                        this DateTime value)
                    {
                        return new DateTime(
                            value.Year,
                            value.Month,
                            1,
                            0,
                            0,
                            0,
                            value.Kind);
                    }

                    /// <summary>
                    /// Gets the end of the month.
                    /// </summary>
                    public static DateTime EndOfMonth(
                        this DateTime value)
                    {
                        return value
                            .StartOfMonth()
                            .AddMonths(1)
                            .AddTicks(-1);
                    }

                    /// <summary>
                    /// Determines whether the value is within the specified range.
                    /// </summary>
                    /// <param name="value">The value to check.</param>
                    /// <param name="start">The range start.</param>
                    /// <param name="end">The range end.</param>
                    /// <param name="inclusive">Determines whether the boundaries are included.</param>
                    public static bool IsBetween(
                        this DateTime value,
                        DateTime start,
                        DateTime end,
                        bool inclusive = true)
                    {
                        if (start > end)
                        {
                            throw new ArgumentException(
                                "The start date cannot be greater than the end date.",
                                nameof(start));
                        }

                        return inclusive
                            ? value >= start && value <= end
                            : value > start && value < end;
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Extensions", "EnumerableExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for enumerable collections.
                /// </summary>
                public static class EnumerableExtensions
                {
                    /// <summary>
                    /// Determines whether the sequence is null or contains no elements.
                    /// </summary>
                    public static bool IsNullOrEmpty<T>(
                        this IEnumerable<T>? source)
                    {
                        return source is null || !source.Any();
                    }

                    /// <summary>
                    /// Returns an empty sequence when the source is null.
                    /// </summary>
                    public static IEnumerable<T> EmptyIfNull<T>(
                        this IEnumerable<T>? source)
                    {
                        return source ?? Enumerable.Empty<T>();
                    }

                    /// <summary>
                    /// Executes an action for each element in the sequence.
                    /// </summary>
                    public static void ForEach<T>(
                        this IEnumerable<T>? source,
                        Action<T> action)
                    {
                        ArgumentNullException.ThrowIfNull(action);

                        if (source is null)
                        {
                            return;
                        }

                        foreach (var item in source)
                        {
                            action(item);
                        }
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Extensions", "EnumExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for enumeration values.
                /// </summary>
                public static class EnumExtensions
                {
                    /// <summary>
                    /// Gets the name of the enumeration value.
                    /// </summary>
                    public static string? GetName<TEnum>(
                        this TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Enum.GetName(value);
                    }

                    /// <summary>
                    /// Determines whether the specified value is defined in the enumeration.
                    /// </summary>
                    public static bool IsDefined<TEnum>(
                        this TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Enum.IsDefined(value);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Extensions", "ObjectExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for object values.
                /// </summary>
                public static class ObjectExtensions
                {
                    /// <summary>
                    /// Determines whether the specified value is null.
                    /// </summary>
                    public static bool IsNull<T>(
                        this T? value)
                    {
                        return value is null;
                    }

                    /// <summary>
                    /// Determines whether the specified value is not null.
                    /// </summary>
                    public static bool IsNotNull<T>(
                        this T? value)
                    {
                        return value is not null;
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Extensions", "CollectionExtensions.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Extensions;

                /// <summary>
                /// Provides extension methods for mutable collections.
                /// </summary>
                public static class CollectionExtensions
                {
                    /// <summary>
                    /// Adds the specified item to the collection when the item is not null.
                    /// </summary>
                    public static bool AddIfNotNull<T>(
                        this ICollection<T> collection,
                        T? item)
                        where T : class
                    {
                        ArgumentNullException.ThrowIfNull(collection);

                        if (item is null)
                        {
                            return false;
                        }

                        collection.Add(item);

                        return true;
                    }

                    /// <summary>
                    /// Adds all non-null items from the specified source to the collection.
                    /// </summary>
                    public static int AddRangeIfNotNull<T>(
                        this ICollection<T> collection,
                        IEnumerable<T?>? items)
                        where T : class
                    {
                        ArgumentNullException.ThrowIfNull(collection);

                        if (items is null)
                        {
                            return 0;
                        }

                        var count = 0;

                        foreach (var item in items)
                        {
                            if (item is null)
                            {
                                continue;
                            }

                            collection.Add(item);
                            count++;
                        }

                        return count;
                    }
                }
                """
            },
             // ==========================================
            // HELPERS - Persian
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Persian", "PersianTextHelper.cs"),
                """
                ```csharp
                using System.Text;

                namespace Project.BuildingBlocks.Shared.Helpers.Persian;

                /// <summary>
                /// Provides normalization and cleanup operations for Persian text.
                /// </summary>
                public static class PersianTextHelper
                {
                    private const char PersianYe = 'ی';
                    private const char PersianKaf = 'ک';
                    private const char PersianHe = 'ه';
                    private const char ZeroWidthNonJoiner = '\u200C';

                    /// <summary>
                    /// Normalizes Persian and Arabic character variants into a consistent form.
                    /// </summary>
                    /// <param name="text">The source text.</param>
                    /// <returns>Normalized Persian text.</returns>
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

                    /// <summary>
                    /// Normalizes text for search and comparison.
                    /// </summary>
                    /// <param name="text">The source text.</param>
                    /// <returns>Search-friendly normalized text.</returns>
                    public static string NormalizeForSearch(string? text)
                    {
                        if (string.IsNullOrWhiteSpace(text))
                            return string.Empty;

                        var normalized = Normalize(text);

                        normalized = normalized
                            .Replace(ZeroWidthNonJoiner.ToString(), " ");

                        return NormalizeSpaces(normalized);
                    }

                    /// <summary>
                    /// Normalizes a single Persian or Arabic character.
                    /// </summary>
                    private static char NormalizeCharacter(char character)
                    {
                        return character switch
                        {
                            // Arabic Yeh variants
                            'ي' => PersianYe,
                            'ى' => PersianYe,

                            // Arabic Kaf
                            'ك' => PersianKaf,

                            // Arabic Teh Marbuta
                            'ة' => PersianHe,

                            // Persian Heh with Yeh Above
                            'ۀ' => PersianHe,

                            // Arabic Heh with Yeh Above
                            'ۂ' => PersianHe,

                            // Left-to-right mark
                            '\u200E' => ' ',

                            // Right-to-left mark
                            '\u200F' => ' ',

                            // Arabic Letter Mark
                            '\u061C' => ' ',

                            // Byte Order Mark
                            '\uFEFF' => ' ',

                            _ => character
                        };
                    }

                    /// <summary>
                    /// Normalizes consecutive whitespace characters.
                    /// </summary>
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

                    /// <summary>
                    /// Determines whether the specified text contains Persian characters.
                    /// </summary>
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

                    /// <summary>
                    /// Determines whether the specified character belongs to the Persian/Arabic
                    /// script range commonly used by Persian text.
                    /// </summary>
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

                    /// <summary>
                    /// Removes zero-width formatting characters that can interfere with
                    /// comparison and search.
                    /// </summary>
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

                    /// <summary>
                    /// Determines whether a character is a known invisible formatting character.
                    /// </summary>
                    private static bool IsInvisibleCharacter(char character)
                    {
                        return character switch
                        {
                            '\u200B' => true, // Zero Width Space
                            '\u200D' => true, // Zero Width Joiner
                            '\u200E' => true, // Left-to-Right Mark
                            '\u200F' => true, // Right-to-Left Mark
                            '\u061C' => true, // Arabic Letter Mark
                            '\uFEFF' => true, // Zero Width No-Break Space / BOM
                            _ => false
                        };
                    }
                }
                ```
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Persian", "PersianDateHelper.cs"),
                """
                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Persian;

                /// <summary>
                /// Provides helper methods for working with the Persian calendar.
                /// </summary>
                public static class PersianDateHelper
                {
                    private static readonly PersianCalendar Calendar = new();

                    /// <summary>
                    /// Converts a Gregorian DateTime to Persian calendar components.
                    /// </summary>
                    public static (int Year, int Month, int Day) ToPersianDate(
                        DateTime date)
                    {
                        return (
                            Calendar.GetYear(date),
                            Calendar.GetMonth(date),
                            Calendar.GetDayOfMonth(date));
                    }

                    /// <summary>
                    /// Converts a DateTimeOffset to Persian calendar components.
                    /// </summary>
                    public static (int Year, int Month, int Day) ToPersianDate(
                        DateTimeOffset date)
                    {
                        return (
                            Calendar.GetYear(date.DateTime),
                            Calendar.GetMonth(date.DateTime),
                            Calendar.GetDayOfMonth(date.DateTime));
                    }

                    /// <summary>
                    /// Creates a Gregorian DateTime from Persian calendar components.
                    /// </summary>
                    public static DateTime FromPersianDate(
                        int year,
                        int month,
                        int day)
                    {
                        ValidateDate(year, month, day);

                        return Calendar.ToDateTime(
                            year,
                            month,
                            day,
                            0,
                            0,
                            0,
                            0);
                    }

                    /// <summary>
                    /// Converts a Persian date to a Gregorian DateTime while preserving
                    /// the specified time components.
                    /// </summary>
                    public static DateTime FromPersianDate(
                        int year,
                        int month,
                        int day,
                        int hour,
                        int minute = 0,
                        int second = 0,
                        int millisecond = 0)
                    {
                        ValidateDate(year, month, day);

                        return Calendar.ToDateTime(
                            year,
                            month,
                            day,
                            hour,
                            minute,
                            second,
                            millisecond);
                    }

                    /// <summary>
                    /// Gets the Persian year of the specified Gregorian date.
                    /// </summary>
                    public static int GetYear(DateTime date)
                    {
                        return Calendar.GetYear(date);
                    }

                    /// <summary>
                    /// Gets the Persian month of the specified Gregorian date.
                    /// </summary>
                    public static int GetMonth(DateTime date)
                    {
                        return Calendar.GetMonth(date);
                    }

                    /// <summary>
                    /// Gets the Persian day of month of the specified Gregorian date.
                    /// </summary>
                    public static int GetDay(DateTime date)
                    {
                        return Calendar.GetDayOfMonth(date);
                    }

                    /// <summary>
                    /// Gets the day of week of the specified Gregorian date.
                    /// </summary>
                    public static DayOfWeek GetDayOfWeek(DateTime date)
                    {
                        return date.DayOfWeek;
                    }

                    /// <summary>
                    /// Determines whether the specified Persian date is valid.
                    /// </summary>
                    public static bool IsValidDate(
                        int year,
                        int month,
                        int day)
                    {
                        if (year < 1 || month < 1 || day < 1)
                            return false;

                        try
                        {
                            Calendar.ToDateTime(
                                year,
                                month,
                                day,
                                0,
                                0,
                                0,
                                0);

                            return true;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return false;
                        }
                    }

                    /// <summary>
                    /// Gets the number of days in the specified Persian month.
                    /// </summary>
                    public static int GetDaysInMonth(
                        int year,
                        int month)
                    {
                        ValidateMonth(year, month);

                        return Calendar.GetDaysInMonth(year, month);
                    }

                    /// <summary>
                    /// Determines whether the specified Persian year is a leap year.
                    /// </summary>
                    public static bool IsLeapYear(int year)
                    {
                        if (year < 1)
                            throw new ArgumentOutOfRangeException(
                                nameof(year),
                                year,
                                "Persian year must be greater than zero.");

                        return Calendar.IsLeapYear(year);
                    }

                    /// <summary>
                    /// Gets the first day of the specified Persian year as Gregorian DateTime.
                    /// </summary>
                    public static DateTime GetStartOfYear(int year)
                    {
                        return FromPersianDate(year, 1, 1);
                    }

                    /// <summary>
                    /// Gets the last day of the specified Persian year as Gregorian DateTime.
                    /// </summary>
                    public static DateTime GetEndOfYear(int year)
                    {
                        var lastMonth = 12;
                        var lastDay = Calendar.GetDaysInMonth(year, lastMonth);

                        return FromPersianDate(year, lastMonth, lastDay)
                            .Date
                            .AddDays(1)
                            .AddTicks(-1);
                    }

                    /// <summary>
                    /// Gets the first day of the specified Persian month.
                    /// </summary>
                    public static DateTime GetStartOfMonth(
                        int year,
                        int month)
                    {
                        ValidateMonth(year, month);

                        return FromPersianDate(year, month, 1);
                    }

                    /// <summary>
                    /// Gets the last moment of the specified Persian month.
                    /// </summary>
                    public static DateTime GetEndOfMonth(
                        int year,
                        int month)
                    {
                        ValidateMonth(year, month);

                        var lastDay = Calendar.GetDaysInMonth(year, month);

                        return FromPersianDate(year, month, lastDay)
                            .Date
                            .AddDays(1)
                            .AddTicks(-1);
                    }

                    /// <summary>
                    /// Gets today's Persian date.
                    /// </summary>
                    public static (int Year, int Month, int Day) Today()
                    {
                        return ToPersianDate(DateTime.Now);
                    }

                    /// <summary>
                    /// Formats a Gregorian DateTime using the Persian calendar.
                    /// </summary>
                    public static string Format(
                        DateTime date,
                        string format = "yyyy/MM/dd")
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(format);

                        return date.ToString(
                            format,
                            CultureInfo.InvariantCulture
                                .WithCalendar(Calendar));
                    }

                    /// <summary>
                    /// Parses a Persian calendar date using the specified format.
                    /// </summary>
                    public static DateTime Parse(
                        string value,
                        string format = "yyyy/MM/dd")
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

                    private static void ValidateDate(
                        int year,
                        int month,
                        int day)
                    {
                        if (!IsValidDate(year, month, day))
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(day),
                                $"Invalid Persian date: {year:0000}/{month:00}/{day:00}.");
                        }
                    }

                    private static void ValidateMonth(
                        int year,
                        int month)
                    {
                        if (year < 1)
                            throw new ArgumentOutOfRangeException(
                                nameof(year),
                                year,
                                "Persian year must be greater than zero.");

                        if (month is < 1 or > 12)
                            throw new ArgumentOutOfRangeException(
                                nameof(month),
                                month,
                                "Persian month must be between 1 and 12.");
                    }

                    private static CultureInfo WithCalendar(
                        this CultureInfo culture,
                        Calendar calendar)
                    {
                        var clone = (CultureInfo)culture.Clone();
                        clone.DateTimeFormat.Calendar = calendar;
                        return clone;
                    }
                }
                ```

                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Persian", "PersianCalendarHelper.cs"),
                """
                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Persian;

                /// <summary>
                /// Provides higher-level calendar operations for the Persian calendar.
                /// </summary>
                public static class PersianCalendarHelper
                {
                    private static readonly PersianCalendar Calendar = new();

                    /// <summary>
                    /// Gets the Persian year, month and day for the specified date.
                    /// </summary>
                    public static (int Year, int Month, int Day) GetDateParts(
                        DateTime date)
                    {
                        return (
                            Calendar.GetYear(date),
                            Calendar.GetMonth(date),
                            Calendar.GetDayOfMonth(date));
                    }

                    /// <summary>
                    /// Gets the Persian month number for the specified date.
                    /// </summary>
                    public static int GetMonth(DateTime date)
                    {
                        return Calendar.GetMonth(date);
                    }

                    /// <summary>
                    /// Gets the Persian year for the specified date.
                    /// </summary>
                    public static int GetYear(DateTime date)
                    {
                        return Calendar.GetYear(date);
                    }

                    /// <summary>
                    /// Gets the Persian day of month for the specified date.
                    /// </summary>
                    public static int GetDay(DateTime date)
                    {
                        return Calendar.GetDayOfMonth(date);
                    }

                    /// <summary>
                    /// Gets the Persian quarter of the specified date.
                    /// </summary>
                    public static int GetQuarter(DateTime date)
                    {
                        var month = GetMonth(date);

                        return ((month - 1) / 3) + 1;
                    }

                    /// <summary>
                    /// Gets the first month of the specified Persian quarter.
                    /// </summary>
                    public static int GetQuarterStartMonth(int quarter)
                    {
                        ValidateQuarter(quarter);

                        return ((quarter - 1) * 3) + 1;
                    }

                    /// <summary>
                    /// Gets the last month of the specified Persian quarter.
                    /// </summary>
                    public static int GetQuarterEndMonth(int quarter)
                    {
                        ValidateQuarter(quarter);

                        return quarter * 3;
                    }

                    /// <summary>
                    /// Gets the first day of the Persian quarter containing the specified date.
                    /// </summary>
                    public static DateTime GetStartOfQuarter(DateTime date)
                    {
                        var year = GetYear(date);
                        var quarter = GetQuarter(date);
                        var month = GetQuarterStartMonth(quarter);

                        return PersianDateHelper.FromPersianDate(
                            year,
                            month,
                            1);
                    }

                    /// <summary>
                    /// Gets the last moment of the Persian quarter containing the specified date.
                    /// </summary>
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

                    /// <summary>
                    /// Gets the first day of the Persian year containing the specified date.
                    /// </summary>
                    public static DateTime GetStartOfYear(DateTime date)
                    {
                        var year = GetYear(date);

                        return PersianDateHelper.FromPersianDate(
                            year,
                            1,
                            1);
                    }

                    /// <summary>
                    /// Gets the last moment of the Persian year containing the specified date.
                    /// </summary>
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

                    /// <summary>
                    /// Gets the first day of the Persian month containing the specified date.
                    /// </summary>
                    public static DateTime GetStartOfMonth(DateTime date)
                    {
                        var year = GetYear(date);
                        var month = GetMonth(date);

                        return PersianDateHelper.FromPersianDate(
                            year,
                            month,
                            1);
                    }

                    /// <summary>
                    /// Gets the last moment of the Persian month containing the specified date.
                    /// </summary>
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

                    /// <summary>
                    /// Gets the number of days in the Persian year.
                    /// </summary>
                    public static int GetDaysInYear(int year)
                    {
                        if (year < 1)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(year),
                                year,
                                "Persian year must be greater than zero.");
                        }

                        return Calendar.GetDaysInYear(year);
                    }

                    /// <summary>
                    /// Determines whether the specified Persian year is a leap year.
                    /// </summary>
                    public static bool IsLeapYear(int year)
                    {
                        if (year < 1)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(year),
                                year,
                                "Persian year must be greater than zero.");
                        }

                        return Calendar.IsLeapYear(year);
                    }

                    /// <summary>
                    /// Gets the Persian month containing the specified date as a range.
                    /// </summary>
                    public static (DateTime Start, DateTime End) GetMonthRange(
                        DateTime date)
                    {
                        return (
                            GetStartOfMonth(date),
                            GetEndOfMonth(date));
                    }

                    /// <summary>
                    /// Gets the Persian quarter containing the specified date as a range.
                    /// </summary>
                    public static (DateTime Start, DateTime End) GetQuarterRange(
                        DateTime date)
                    {
                        return (
                            GetStartOfQuarter(date),
                            GetEndOfQuarter(date));
                    }

                    /// <summary>
                    /// Gets the Persian year containing the specified date as a range.
                    /// </summary>
                    public static (DateTime Start, DateTime End) GetYearRange(
                        DateTime date)
                    {
                        return (
                            GetStartOfYear(date),
                            GetEndOfYear(date));
                    }

                    /// <summary>
                    /// Determines whether two Gregorian dates belong to the same Persian year.
                    /// </summary>
                    public static bool IsSameYear(
                        DateTime first,
                        DateTime second)
                    {
                        return GetYear(first) == GetYear(second);
                    }

                    /// <summary>
                    /// Determines whether two Gregorian dates belong to the same Persian month.
                    /// </summary>
                    public static bool IsSameMonth(
                        DateTime first,
                        DateTime second)
                    {
                        return IsSameYear(first, second)
                            && GetMonth(first) == GetMonth(second);
                    }

                    /// <summary>
                    /// Determines whether two Gregorian dates belong to the same Persian quarter.
                    /// </summary>
                    public static bool IsSameQuarter(
                        DateTime first,
                        DateTime second)
                    {
                        return IsSameYear(first, second)
                            && GetQuarter(first) == GetQuarter(second);
                    }

                    /// <summary>
                    /// Gets the Persian day number within the year.
                    /// </summary>
                    public static int GetDayOfYear(DateTime date)
                    {
                        return Calendar.GetDayOfYear(date);
                    }

                    /// <summary>
                    /// Gets the Persian month name used for the specified month number.
                    /// </summary>
                    public static string GetMonthName(int month)
                    {
                        return month switch
                        {
                            1 => "فروردین",
                            2 => "اردیبهشت",
                            3 => "خرداد",
                            4 => "تیر",
                            5 => "مرداد",
                            6 => "شهریور",
                            7 => "مهر",
                            8 => "آبان",
                            9 => "آذر",
                            10 => "دی",
                            11 => "بهمن",
                            12 => "اسفند",
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(month),
                                month,
                                "Persian month must be between 1 and 12.")
                        };
                    }

                    /// <summary>
                    /// Gets the Persian quarter name.
                    /// </summary>
                    public static string GetQuarterName(int quarter)
                    {
                        ValidateQuarter(quarter);

                        return $"سه‌ماهه {quarter}";
                    }

                    private static void ValidateQuarter(int quarter)
                    {
                        if (quarter is < 1 or > 4)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(quarter),
                                quarter,
                                "Persian quarter must be between 1 and 4.");
                        }
                    }
                }
                ```
                """
            },
            // ==========================================
            // HELPERS - STRINGS
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Strings", "StringHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Strings;

                using System;
                using System.Collections.Generic;
                using System.Globalization;
                using System.Linq;
                using System.Text;

                public static class StringHelper
                {
                    public static string EmptyIfNull(string? value) => value ?? string.Empty;

                    public static string? NullIfWhiteSpace(string? value) =>
                        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

                    public static string TrimSafe(this string? value) => value?.Trim() ?? string.Empty;

                    public static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

                    public static string Truncate(string? value, int maxLength, string suffix = "...")
                    {
                        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
                        ArgumentNullException.ThrowIfNull(suffix);

                        if (value is null) return string.Empty;
                        if (value.Length <= maxLength) return value;
                        if (suffix.Length >= maxLength) return suffix[..maxLength];

                        return value[..(maxLength - suffix.Length)] + suffix;
                    }

                    public static string NormalizeWhitespace(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

                        var builder = new StringBuilder(value.Length);
                        var previousWasWhitespace = false;

                        foreach (var character in value)
                        {
                            if (char.IsWhiteSpace(character))
                            {
                                if (previousWasWhitespace) continue;
                                builder.Append(' ');
                                previousWasWhitespace = true;
                                continue;
                            }

                            builder.Append(character);
                            previousWasWhitespace = false;
                        }

                        return builder.ToString().Trim();
                    }

                    public static string NormalizeLines(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

                        var normalized = value
                            .Replace("\r\n", " ")
                            .Replace('\r', ' ')
                            .Replace('\n', ' ');

                        return NormalizeWhitespace(normalized);
                    }

                    public static string FirstNonEmpty(params string?[] values)
                    {
                        ArgumentNullException.ThrowIfNull(values);

                        foreach (var value in values)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                                return value.Trim();
                        }

                        return string.Empty;
                    }

                    public static string JoinNonEmpty(string separator, IEnumerable<string?> values)
                    {
                        ArgumentNullException.ThrowIfNull(separator);
                        ArgumentNullException.ThrowIfNull(values);

                        return string.Join(
                            separator,
                            values
                                .Where(static v => !string.IsNullOrWhiteSpace(v))
                                .Select(static v => v!.Trim()));
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Strings", "SlugHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Strings;

                using System;
                using System.Text;
                using System.Text.RegularExpressions;

                public static partial class SlugHelper
                {
                    private const char Separator = '-';

                    public static string Create(string? value, bool preservePersian = true)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return string.Empty;

                        var text = value.Trim();

                        if (!preservePersian)
                        {
                            text = RemoveNonAsciiCharacters(text);
                        }

                        text = ReplacePersianCharacters(text);
                        text = ReplaceSeparators(text);
                        text = RemoveInvalidCharacters(text);
                        text = CollapseSeparators(text);

                        return text.Trim(Separator);
                    }

                    public static string CreatePersian(string? value) => Create(value, preservePersian: true);

                    public static string CreateAscii(string? value) => Create(value, preservePersian: false);

                    public static bool IsValid(string? value, bool allowPersian = true)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        if (value != value.Trim())
                            return false;

                        if (value.StartsWith(Separator) || value.EndsWith(Separator))
                            return false;

                        if (value.Contains("--", StringComparison.Ordinal))
                            return false;

                        foreach (var character in value)
                        {
                            if (character == Separator)
                                continue;

                            if (char.IsLetterOrDigit(character))
                            {
                                if (allowPersian || character <= 127)
                                    continue;
                            }

                            return false;
                        }

                        return true;
                    }

                    public static string CreateOrFallback(string? value, string fallback, bool preservePersian = true)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

                        var slug = Create(value, preservePersian);

                        return string.IsNullOrEmpty(slug)
                            ? Create(fallback, preservePersian)
                            : slug;
                    }

                    private static string ReplacePersianCharacters(string value)
                    {
                        return value
                            .Replace('ي', 'ی')
                            .Replace('ى', 'ی')
                            .Replace('ك', 'ک')
                            .Replace('ة', 'ه')
                            .Replace('ۀ', 'ه');
                    }

                    private static string ReplaceSeparators(string value) => SeparatorRegex().Replace(value, "-");

                    private static string RemoveInvalidCharacters(string value)
                    {
                        var builder = new StringBuilder(value.Length);

                        foreach (var character in value)
                        {
                            if (char.IsLetterOrDigit(character) || character == Separator)
                            {
                                builder.Append(character);
                            }
                        }

                        return builder.ToString();
                    }

                    private static string CollapseSeparators(string value) => MultipleSeparatorRegex().Replace(value, "-");

                    private static string RemoveNonAsciiCharacters(string value)
                    {
                        return new string(value.Where(character => character <= 127).ToArray());
                    }

                    [GeneratedRegex(@"[\s_./\\|+&?=:;,]+", RegexOptions.CultureInvariant)]
                    private static partial Regex SeparatorRegex();

                    [GeneratedRegex(@"-{2,}", RegexOptions.CultureInvariant)]
                    private static partial Regex MultipleSeparatorRegex();
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Strings", "TextNormalizer.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Strings;

                using System;
                using System.Text;

                public static class TextNormalizer
                {
                    public static string Normalize(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return string.Empty;

                        var normalized = value.Normalize(NormalizationForm.FormC);

                        return StringHelper.NormalizeWhitespace(normalized);
                    }

                    public static string NormalizeForSearch(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return string.Empty;

                        var normalized = Normalize(value);

                        normalized = normalized.Replace(
                            "\u200C",
                            " ",
                            StringComparison.Ordinal);

                        normalized = RemoveTatweel(normalized);
                        normalized = RemoveArabicDiacritics(normalized);

                        return StringHelper.NormalizeWhitespace(normalized);
                    }

                    public static bool AreEqual(string? first, string? second)
                    {
                        return string.Equals(
                            NormalizeForSearch(first),
                            NormalizeForSearch(second),
                            StringComparison.Ordinal);
                    }

                    public static bool Contains(string? source, string? value)
                    {
                        var normalizedSource = NormalizeForSearch(source);
                        var normalizedValue = NormalizeForSearch(value);

                        if (normalizedSource.Length == 0 || normalizedValue.Length == 0)
                        {
                            return false;
                        }

                        return normalizedSource.Contains(
                            normalizedValue,
                            StringComparison.Ordinal);
                    }

                    private static string RemoveTatweel(string value)
                    {
                        return value.Replace(
                            "\u0640",
                            string.Empty,
                            StringComparison.Ordinal);
                    }

                    private static string RemoveArabicDiacritics(string value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        var builder = new StringBuilder(value.Length);

                        foreach (var character in value)
                        {
                            if (IsArabicDiacritic(character))
                                continue;

                            builder.Append(character);
                        }

                        return builder.ToString();
                    }

                    private static bool IsArabicDiacritic(char character)
                    {
                        return character switch
                        {
                            '\u0610' or '\u0611' or '\u0612' or '\u0613' or '\u0614' or '\u0615' or '\u0616' or '\u0617' or '\u0618' or '\u0619' or '\u061A' or
                            '\u064B' or '\u064C' or '\u064D' or '\u064E' or '\u064F' or '\u0650' or '\u0651' or '\u0652' or '\u0653' or '\u0654' or '\u0655' or
                            '\u0656' or '\u0657' or '\u0658' or '\u0659' or '\u065A' or '\u065B' or '\u065C' or '\u065D' or '\u065E' or '\u065F' or '\u0670' or
                            '\u06D6' or '\u06D7' or '\u06D8' or '\u06D9' or '\u06DA' or '\u06DB' or '\u06DC' or '\u06DD' or '\u06DE' or '\u06DF' or '\u06E0' or
                            '\u06E1' or '\u06E2' or '\u06E3' or '\u06E4' or '\u06E5' or '\u06E6' or '\u06E7' or '\u06E8' or '\u06E9' or '\u06EA' or '\u06EB' or
                            '\u06EC' or '\u06ED' => true,

                            _ => false
                        };
                    }
                }
                """
            },
                     
            // ==========================================
            // HELPERS - Numbers
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Numbers", "NumberHelper.cs"),
                """
                                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Numbers;

                /// <summary>
                /// Provides general-purpose helper methods for working with numeric values.
                /// </summary>
                public static class NumberHelper
                {
                    /// <summary>
                    /// Determines whether the specified value is zero.
                    /// </summary>
                    public static bool IsZero<T>(T value)
                        where T : INumber<T>
                    {
                        return value == T.Zero;
                    }

                    /// <summary>
                    /// Determines whether the specified value is positive.
                    /// </summary>
                    public static bool IsPositive<T>(T value)
                        where T : INumber<T>
                    {
                        return value > T.Zero;
                    }

                    /// <summary>
                    /// Determines whether the specified value is negative.
                    /// </summary>
                    public static bool IsNegative<T>(T value)
                        where T : INumber<T>
                    {
                        return value < T.Zero;
                    }

                    /// <summary>
                    /// Determines whether the specified value is non-negative.
                    /// </summary>
                    public static bool IsNonNegative<T>(T value)
                        where T : INumber<T>
                    {
                        return value >= T.Zero;
                    }

                    /// <summary>
                    /// Determines whether the specified value is within the specified range.
                    /// </summary>
                    public static bool IsBetween<T>(
                        T value,
                        T minimum,
                        T maximum,
                        bool inclusive = true)
                        where T : INumber<T>
                    {
                        if (minimum > maximum)
                        {
                            throw new ArgumentException(
                                "Minimum cannot be greater than maximum.",
                                nameof(minimum));
                        }

                        return inclusive
                            ? value >= minimum && value <= maximum
                            : value > minimum && value < maximum;
                    }

                    /// <summary>
                    /// Clamps a numeric value to the specified range.
                    /// </summary>
                    public static T Clamp<T>(
                        T value,
                        T minimum,
                        T maximum)
                        where T : INumber<T>
                    {
                        if (minimum > maximum)
                        {
                            throw new ArgumentException(
                                "Minimum cannot be greater than maximum.",
                                nameof(minimum));
                        }

                        if (value < minimum)
                            return minimum;

                        if (value > maximum)
                            return maximum;

                        return value;
                    }

                    /// <summary>
                    /// Returns the absolute value of the specified number.
                    /// </summary>
                    public static T Abs<T>(T value)
                        where T : INumber<T>
                    {
                        return T.Abs(value);
                    }

                    /// <summary>
                    /// Returns the minimum of two values.
                    /// </summary>
                    public static T Min<T>(
                        T first,
                        T second)
                        where T : INumber<T>
                    {
                        return T.Min(first, second);
                    }

                    /// <summary>
                    /// Returns the maximum of two values.
                    /// </summary>
                    public static T Max<T>(
                        T first,
                        T second)
                        where T : INumber<T>
                    {
                        return T.Max(first, second);
                    }

                    /// <summary>
                    /// Safely converts a value to the specified numeric type.
                    /// </summary>
                    public static bool TryConvert<T>(
                        object? value,
                        out T result)
                        where T : INumberBase<T>
                    {
                        if (value is null)
                        {
                            result = T.Zero;
                            return false;
                        }

                        if (value is T typedValue)
                        {
                            result = typedValue;
                            return true;
                        }

                        return T.TryParse(
                            Convert.ToString(
                                value,
                                CultureInfo.InvariantCulture),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out result);
                    }

                    /// <summary>
                    /// Determines whether the specified string represents a valid number.
                    /// </summary>
                    public static bool IsNumber<T>(
                        string? value)
                        where T : INumberBase<T>
                    {
                        return !string.IsNullOrWhiteSpace(value)
                            && T.TryParse(
                                value.Trim(),
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out _);
                    }

                    /// <summary>
                    /// Parses a number or returns the specified fallback value.
                    /// </summary>
                    public static T ParseOrDefault<T>(
                        string? value,
                        T defaultValue = default)
                        where T : INumberBase<T>
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return defaultValue;

                        return T.TryParse(
                            value.Trim(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out var result)
                            ? result
                            : defaultValue;
                    }

                    /// <summary>
                    /// Calculates a percentage of a numeric value.
                    /// </summary>
                    public static decimal Percentage(
                        decimal value,
                        decimal percentage)
                    {
                        return value * percentage / 100m;
                    }

                    /// <summary>
                    /// Calculates what percentage the part represents from the whole.
                    /// </summary>
                    public static decimal PercentageOf(
                        decimal part,
                        decimal whole)
                    {
                        if (whole == 0)
                            throw new DivideByZeroException(
                                "The whole value cannot be zero.");

                        return part / whole * 100m;
                    }

                    /// <summary>
                    /// Rounds a decimal value using the specified precision and rounding mode.
                    /// </summary>
                    public static decimal Round(
                        decimal value,
                        int decimals = 2,
                        MidpointRounding rounding = MidpointRounding.ToEven)
                    {
                        if (decimals < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(decimals),
                                decimals,
                                "Decimal places cannot be negative.");
                        }

                        return decimal.Round(
                            value,
                            decimals,
                            rounding);
                    }

                    /// <summary>
                    /// Determines whether a decimal value is effectively zero
                    /// using the specified tolerance.
                    /// </summary>
                    public static bool IsApproximatelyZero(
                        decimal value,
                        decimal tolerance = 0.0000001m)
                    {
                        if (tolerance < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(tolerance),
                                tolerance,
                                "Tolerance cannot be negative.");
                        }

                        return Math.Abs(value) <= tolerance;
                    }
                }
                ```
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Numbers", "PersianNumberHelper.cs"),
                """
                                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Numbers;

                /// <summary>
                /// Provides normalization and conversion helpers for Persian, Arabic,
                /// and Latin numeric characters.
                /// </summary>
                public static class PersianNumberHelper
                {
                    private const char PersianZero = '۰';
                    private const char ArabicZero = '٠';
                    private const char LatinZero = '0';

                    /// <summary>
                    /// Converts Persian and Arabic digits to Latin digits.
                    /// </summary>
                    /// <example>
                    /// ۱۲۳۴۵ → 12345
                    /// ١٢٣٤٥ → 12345
                    /// </example>
                    public static string ToLatinDigits(string? value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        return ConvertDigits(
                            value,
                            LatinZero);
                    }

                    /// <summary>
                    /// Converts Latin and Arabic digits to Persian digits.
                    /// </summary>
                    /// <example>
                    /// 12345 → ۱۲۳۴۵
                    /// ١٢٣٤٥ → ۱۲۳۴۵
                    /// </example>
                    public static string ToPersianDigits(string? value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        return ConvertDigits(
                            value,
                            PersianZero);
                    }

                    /// <summary>
                    /// Converts Latin and Persian digits to Arabic digits.
                    /// </summary>
                    /// <example>
                    /// 12345 → ١٢٣٤٥
                    /// ۱۲۳۴۵ → ١٢٣٤٥
                    /// </example>
                    public static string ToArabicDigits(string? value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        return ConvertDigits(
                            value,
                            ArabicZero);
                    }

                    /// <summary>
                    /// Normalizes all supported digit systems to Latin digits.
                    /// </summary>
                    public static string Normalize(string? value)
                    {
                        return ToLatinDigits(value);
                    }

                    /// <summary>
                    /// Determines whether the specified character is a numeric digit
                    /// in Persian, Arabic, or Latin representation.
                    /// </summary>
                    public static bool IsDigit(char character)
                    {
                        return IsLatinDigit(character)
                            || IsPersianDigit(character)
                            || IsArabicDigit(character);
                    }

                    /// <summary>
                    /// Determines whether the specified character is a Latin digit.
                    /// </summary>
                    public static bool IsLatinDigit(char character)
                    {
                        return character is >= '0' and <= '9';
                    }

                    /// <summary>
                    /// Determines whether the specified character is a Persian digit.
                    /// </summary>
                    public static bool IsPersianDigit(char character)
                    {
                        return character is >= '۰' and <= '۹';
                    }

                    /// <summary>
                    /// Determines whether the specified character is an Arabic digit.
                    /// </summary>
                    public static bool IsArabicDigit(char character)
                    {
                        return character is >= '٠' and <= '٩';
                    }

                    /// <summary>
                    /// Converts a single supported digit to its numeric value.
                    /// </summary>
                    public static int ToInt32(char character)
                    {
                        if (IsLatinDigit(character))
                            return character - LatinZero;

                        if (IsPersianDigit(character))
                            return character - PersianZero;

                        if (IsArabicDigit(character))
                            return character - ArabicZero;

                        throw new ArgumentException(
                            $"The character '{character}' is not a supported digit.",
                            nameof(character));
                    }

                    /// <summary>
                    /// Determines whether the specified value contains at least one
                    /// Persian, Arabic, or Latin digit.
                    /// </summary>
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

                    /// <summary>
                    /// Extracts all numeric digits from a string and returns them
                    /// as Latin digits.
                    /// </summary>
                    /// <example>
                    /// "فاکتور ۱۲۴۰۰۵" → "124005"
                    /// </example>
                    public static string ExtractDigits(string? value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        var builder = new StringBuilder();

                        foreach (var character in value)
                        {
                            if (!IsDigit(character))
                                continue;

                            builder.Append(
                                ToInt32(character));
                        }

                        return builder.ToString();
                    }

                    /// <summary>
                    /// Converts a numeric value to Persian digits.
                    /// </summary>
                    public static string ToPersianDigits<T>(T value)
                        where T : IFormattable
                    {
                        return ToPersianDigits(
                            value.ToString(
                                null,
                                CultureInfo.InvariantCulture));
                    }

                    /// <summary>
                    /// Converts a numeric value to Latin digits.
                    /// </summary>
                    public static string ToLatinDigits<T>(T value)
                        where T : IFormattable
                    {
                        return ToLatinDigits(
                            value.ToString(
                                null,
                                CultureInfo.InvariantCulture));
                    }

                    /// <summary>
                    /// Converts a numeric value to Arabic digits.
                    /// </summary>
                    public static string ToArabicDigits<T>(T value)
                        where T : IFormattable
                    {
                        return ToArabicDigits(
                            value.ToString(
                                null,
                                CultureInfo.InvariantCulture));
                    }

                    /// <summary>
                    /// Tries to parse a string containing Persian, Arabic, or Latin digits.
                    /// </summary>
                    public static bool TryParseInt(
                        string? value,
                        out int result)
                    {
                        var normalized = ToLatinDigits(value);

                        return int.TryParse(
                            normalized,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out result);
                    }

                    /// <summary>
                    /// Tries to parse a string containing Persian, Arabic, or Latin digits.
                    /// </summary>
                    public static bool TryParseLong(
                        string? value,
                        out long result)
                    {
                        var normalized = ToLatinDigits(value);

                        return long.TryParse(
                            normalized,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out result);
                    }

                    /// <summary>
                    /// Tries to parse a decimal containing Persian, Arabic, or Latin digits.
                    /// </summary>
                    public static bool TryParseDecimal(
                        string? value,
                        out decimal result)
                    {
                        var normalized = ToLatinDigits(value);

                        return decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out result);
                    }

                    private static string ConvertDigits(
                        string value,
                        char targetZero)
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

                            builder.Append(
                                (char)(targetZero + digit));
                        }

                        return builder.ToString();
                    }
                }
                ```
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Numbers", "NumberToWordsHelper.cs"),
                """
                                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Numbers;

                /// <summary>
                /// Provides conversion of numeric values to Persian words.
                /// </summary>
                public static class NumberToWordsHelper
                {
                    private static readonly string[] Units =
                    [
                        "صفر",
                        "یک",
                        "دو",
                        "سه",
                        "چهار",
                        "پنج",
                        "شش",
                        "هفت",
                        "هشت",
                        "نه"
                    ];

                    private static readonly string[] Teens =
                    [
                        "ده",
                        "یازده",
                        "دوازده",
                        "سیزده",
                        "چهارده",
                        "پانزده",
                        "شانزده",
                        "هفده",
                        "هجده",
                        "نوزده"
                    ];

                    private static readonly string[] Tens =
                    [
                        "",
                        "",
                        "بیست",
                        "سی",
                        "چهل",
                        "پنجاه",
                        "شصت",
                        "هفتاد",
                        "هشتاد",
                        "نود"
                    ];

                    private static readonly string[] Hundreds =
                    [
                        "",
                        "صد",
                        "دویست",
                        "سیصد",
                        "چهارصد",
                        "پانصد",
                        "ششصد",
                        "هفتصد",
                        "هشتصد",
                        "نهصد"
                    ];

                    private static readonly string[] Scales =
                    [
                        "",
                        "هزار",
                        "میلیون",
                        "میلیارد",
                        "تریلیون",
                        "کوادریلیون",
                        "کوینتیلیون"
                    ];

                    /// <summary>
                    /// Converts a signed 64-bit integer to Persian words.
                    /// </summary>
                    public static string ToWords(long value)
                    {
                        if (value == 0)
                            return Units[0];

                        if (value < 0)
                            return $"منفی {ToWordsPositive(value)}";

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
                        string fractionalSeparator = " و ")
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
                                ? $"منفی {integerText}"
                                : integerText;

                        var fractionalText =
                            FractionToWords(fractionalPart);

                        var result =
                            $"{integerText}{fractionalSeparator}{fractionalText}";

                        return negative
                            ? $"منفی {result}"
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
                                        $"{groupText} {scale}";
                                }

                                parts.Insert(
                                    0,
                                    groupText);
                            }

                            value /= 1000;
                            scaleIndex++;
                        }

                        return string.Join(
                            " و ",
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
                                "Value must be between 0 and 999.");
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
                            " و ",
                            parts);
                    }

                    private static string FractionToWords(
                        decimal fractionalPart)
                    {
                        var text = fractionalPart
                            .ToString(
                                "0.##################",
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
                            " ",
                            parts);
                    }
                }
                ```
                """
            },
               
            // ==========================================
            // HELPERS - Files
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Files", "FileHelper.cs"),
                """
                                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Files;

                /// <summary>
                /// Provides general-purpose helpers for working with file paths,
                /// names, extensions, and file metadata.
                /// </summary>
                public static class FileHelper
                {
                    /// <summary>
                    /// Determines whether a file exists at the specified path.
                    /// </summary>
                    public static bool Exists(string? path)
                    {
                        return !string.IsNullOrWhiteSpace(path)
                            && File.Exists(path);
                    }

                    /// <summary>
                    /// Returns the file name from a path.
                    /// </summary>
                    public static string GetFileName(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return string.Empty;

                        return Path.GetFileName(path);
                    }

                    /// <summary>
                    /// Returns the file name without its extension.
                    /// </summary>
                    public static string GetFileNameWithoutExtension(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return string.Empty;

                        return Path.GetFileNameWithoutExtension(path);
                    }

                    /// <summary>
                    /// Returns the extension of a file without the leading dot.
                    /// </summary>
                    public static string GetExtension(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return string.Empty;

                        return Path
                            .GetExtension(path)
                            .TrimStart('.')
                            .ToLowerInvariant();
                    }

                    /// <summary>
                    /// Returns the extension including the leading dot.
                    /// </summary>
                    public static string GetExtensionWithDot(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return string.Empty;

                        return Path
                            .GetExtension(path)
                            .ToLowerInvariant();
                    }

                    /// <summary>
                    /// Determines whether a file has one of the specified extensions.
                    /// </summary>
                    public static bool HasExtension(
                        string? path,
                        params string[] extensions)
                    {
                        ArgumentNullException.ThrowIfNull(extensions);

                        if (string.IsNullOrWhiteSpace(path))
                            return false;

                        var fileExtension = GetExtension(path);

                        if (fileExtension.Length == 0)
                            return false;

                        foreach (var extension in extensions)
                        {
                            if (string.IsNullOrWhiteSpace(extension))
                                continue;

                            var normalizedExtension = NormalizeExtension(extension);

                            if (string.Equals(
                                    fileExtension,
                                    normalizedExtension,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    /// <summary>
                    /// Determines whether a file path has a valid file name.
                    /// </summary>
                    public static bool HasFileName(string? path)
                    {
                        return !string.IsNullOrWhiteSpace(GetFileName(path));
                    }

                    /// <summary>
                    /// Returns the directory portion of a file path.
                    /// </summary>
                    public static string GetDirectory(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return string.Empty;

                        return Path.GetDirectoryName(path) ?? string.Empty;
                    }

                    /// <summary>
                    /// Combines path segments into a single path.
                    /// </summary>
                    public static string CombinePath(params string[] parts)
                    {
                        ArgumentNullException.ThrowIfNull(parts);

                        if (parts.Length == 0)
                            return string.Empty;

                        var validParts = parts
                            .Where(static part => !string.IsNullOrWhiteSpace(part))
                            .Select(static part => part.Trim())
                            .ToArray();

                        return validParts.Length == 0
                            ? string.Empty
                            : Path.Combine(validParts);
                    }

                    /// <summary>
                    /// Returns the size of a file in bytes.
                    /// </summary>
                    public static long GetSize(string? path)
                    {
                        if (!Exists(path))
                            return 0;

                        return new FileInfo(path!).Length;
                    }

                    /// <summary>
                    /// Returns file information for an existing file.
                    /// </summary>
                    public static FileInfo? GetInfo(string? path)
                    {
                        if (!Exists(path))
                            return null;

                        return new FileInfo(path!);
                    }

                    /// <summary>
                    /// Returns the last modification time in UTC.
                    /// </summary>
                    public static DateTime? GetLastWriteTimeUtc(string? path)
                    {
                        if (!Exists(path))
                            return null;

                        return File.GetLastWriteTimeUtc(path!);
                    }

                    /// <summary>
                    /// Determines whether a file exceeds the specified maximum size.
                    /// </summary>
                    public static bool ExceedsSize(
                        string? path,
                        long maximumBytes)
                    {
                        ArgumentOutOfRangeException.ThrowIfNegative(
                            maximumBytes);

                        return GetSize(path) > maximumBytes;
                    }

                    /// <summary>
                    /// Determines whether a file is within the specified maximum size.
                    /// </summary>
                    public static bool IsWithinSize(
                        string? path,
                        long maximumBytes)
                    {
                        ArgumentOutOfRangeException.ThrowIfNegative(
                            maximumBytes);

                        return Exists(path)
                            && GetSize(path) <= maximumBytes;
                    }

                    /// <summary>
                    /// Normalizes a file extension by removing a leading dot
                    /// and converting it to lowercase.
                    /// </summary>
                    public static string NormalizeExtension(string? extension)
                    {
                        if (string.IsNullOrWhiteSpace(extension))
                            return string.Empty;

                        return extension
                            .Trim()
                            .TrimStart('.')
                            .ToLowerInvariant();
                    }

                    /// <summary>
                    /// Builds a file name from a base name and extension.
                    /// </summary>
                    public static string BuildFileName(
                        string baseName,
                        string? extension = null)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);

                        var normalizedBaseName = Path.GetFileNameWithoutExtension(
                            baseName.Trim());

                        if (string.IsNullOrWhiteSpace(extension))
                            return normalizedBaseName;

                        var normalizedExtension = NormalizeExtension(extension);

                        return string.IsNullOrEmpty(normalizedExtension)
                            ? normalizedBaseName
                            : $"{normalizedBaseName}.{normalizedExtension}";
                    }

                    /// <summary>
                    /// Determines whether the specified file name is valid
                    /// for the current operating system.
                    /// </summary>
                    public static bool IsValidFileName(string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return false;

                        var name = Path.GetFileName(fileName);

                        if (!string.Equals(
                                name,
                                fileName,
                                StringComparison.Ordinal))
                        {
                            return false;
                        }

                        if (name.IndexOfAny(
                                Path.GetInvalidFileNameChars()) >= 0)
                        {
                            return false;
                        }

                        return true;
                    }

                    /// <summary>
                    /// Determines whether the specified path contains invalid
                    /// path characters.
                    /// </summary>
                    public static bool IsValidPath(string? path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return false;

                        try
                        {
                            return path.IndexOfAny(
                                       Path.GetInvalidPathChars()) < 0;
                        }
                        catch (ArgumentException)
                        {
                            return false;
                        }
                    }

                    /// <summary>
                    /// Creates a directory if it does not already exist.
                    /// </summary>
                    public static string EnsureDirectory(string path)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(path);

                        return Directory
                            .CreateDirectory(path)
                            .FullName;
                    }

                    /// <summary>
                    /// Deletes a file when it exists.
                    /// </summary>
                    public static bool TryDelete(string? path)
                    {
                        if (!Exists(path))
                            return false;

                        try
                        {
                            File.Delete(path!);
                            return true;
                        }
                        catch (IOException)
                        {
                            return false;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return false;
                        }
                    }

                    /// <summary>
                    /// Reads the entire text content of an existing file.
                    /// </summary>
                    public static string ReadAllText(
                        string path,
                        Encoding? encoding = null)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(path);

                        return encoding is null
                            ? File.ReadAllText(path)
                            : File.ReadAllText(path, encoding);
                    }

                    /// <summary>
                    /// Writes text content to a file.
                    /// </summary>
                    public static void WriteAllText(
                        string path,
                        string content,
                        Encoding? encoding = null)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(path);
                        ArgumentNullException.ThrowIfNull(content);

                        if (encoding is null)
                        {
                            File.WriteAllText(path, content);
                            return;
                        }

                        File.WriteAllText(path, content, encoding);
                    }
                }
                ```
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Files", "FileNameHelper.cs"),
                """
                                ```csharp id="x7k2qm"
                using System.Security.Cryptography;
                using Project.BuildingBlocks.Shared.Helpers.Strings;

                namespace Project.BuildingBlocks.Shared.Helpers.Files;

                /// <summary>
                /// Provides helpers for sanitizing, generating, and validating file names.
                /// </summary>
                public static class FileNameHelper
                {
                    private const int DefaultMaxFileNameLength = 180;

                    /// <summary>
                    /// Removes invalid characters from a file name.
                    /// </summary>
                    public static string Sanitize(
                        string? fileName,
                        string replacement = "_")
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return string.Empty;

                        ArgumentNullException.ThrowIfNull(replacement);

                        var name = Path.GetFileName(fileName.Trim());

                        if (string.IsNullOrEmpty(name))
                            return string.Empty;

                        var invalidCharacters =
                            Path.GetInvalidFileNameChars();

                        var builder = new StringBuilder(name.Length);

                        foreach (var character in name)
                        {
                            if (invalidCharacters.Contains(character))
                            {
                                builder.Append(replacement);
                            }
                            else
                            {
                                builder.Append(character);
                            }
                        }

                        return RemoveRepeatedReplacement(
                            builder.ToString(),
                            replacement);
                    }

                    /// <summary>
                    /// Sanitizes a file name and removes leading/trailing whitespace
                    /// and dots.
                    /// </summary>
                    public static string Normalize(
                        string? fileName,
                        int maxLength = DefaultMaxFileNameLength)
                    {
                        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                            maxLength);

                        if (string.IsNullOrWhiteSpace(fileName))
                            return string.Empty;

                        var sanitized = Sanitize(fileName);

                        if (string.IsNullOrWhiteSpace(sanitized))
                            return string.Empty;

                        sanitized = sanitized.Trim();

                        sanitized = sanitized.Trim('.');

                        if (sanitized.Length == 0)
                            return string.Empty;

                        return TruncatePreservingExtension(
                            sanitized,
                            maxLength);
                    }

                    /// <summary>
                    /// Generates a unique file name using a GUID.
                    /// </summary>
                    public static string GenerateUnique(
                        string? extension = null)
                    {
                        var identifier = Guid.NewGuid()
                            .ToString("N");

                        var normalizedExtension =
                            FileHelper.NormalizeExtension(extension);

                        return string.IsNullOrEmpty(normalizedExtension)
                            ? identifier
                            : $"{identifier}.{normalizedExtension}";
                    }

                    /// <summary>
                    /// Generates a unique file name while preserving the specified
                    /// original extension.
                    /// </summary>
                    public static string GenerateUnique(
                        string originalFileName,
                        bool preserveExtension)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            originalFileName);

                        var extension = preserveExtension
                            ? FileHelper.GetExtension(originalFileName)
                            : string.Empty;

                        return GenerateUnique(extension);
                    }

                    /// <summary>
                    /// Generates a deterministic file name from a prefix and unique identifier.
                    /// </summary>
                    public static string Generate(
                        string? prefix,
                        string? extension = null)
                    {
                        var normalizedPrefix = Normalize(prefix);

                        if (string.IsNullOrWhiteSpace(normalizedPrefix))
                            normalizedPrefix = "file";

                        var uniquePart = Guid.NewGuid()
                            .ToString("N");

                        var normalizedExtension =
                            FileHelper.NormalizeExtension(extension);

                        var fileName = $"{normalizedPrefix}-{uniquePart}";

                        return string.IsNullOrEmpty(normalizedExtension)
                            ? fileName
                            : $"{fileName}.{normalizedExtension}";
                    }

                    /// <summary>
                    /// Generates a file name using the current UTC timestamp
                    /// and a cryptographically strong random suffix.
                    /// </summary>
                    public static string GenerateTimestamped(
                        string? prefix,
                        string? extension = null)
                    {
                        var normalizedPrefix = Normalize(prefix);

                        if (string.IsNullOrWhiteSpace(normalizedPrefix))
                            normalizedPrefix = "file";

                        var timestamp = DateTime.UtcNow
                            .ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);

                        Span<byte> randomBytes = stackalloc byte[6];
                        RandomNumberGenerator.Fill(randomBytes);

                        var randomPart =
                            Convert.ToHexString(randomBytes)
                                .ToLowerInvariant();

                        var normalizedExtension =
                            FileHelper.NormalizeExtension(extension);

                        var fileName =
                            $"{normalizedPrefix}-{timestamp}-{randomPart}";

                        return string.IsNullOrEmpty(normalizedExtension)
                            ? fileName
                            : $"{fileName}.{normalizedExtension}";
                    }

                    /// <summary>
                    /// Replaces whitespace with the specified separator.
                    /// </summary>
                    public static string ReplaceWhitespace(
                        string? fileName,
                        char separator = '_')
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return string.Empty;

                        var builder = new StringBuilder(fileName.Length);
                        var previousWasSeparator = false;

                        foreach (var character in fileName.Trim())
                        {
                            if (char.IsWhiteSpace(character))
                            {
                                if (previousWasSeparator)
                                    continue;

                                builder.Append(separator);
                                previousWasSeparator = true;
                                continue;
                            }

                            builder.Append(character);
                            previousWasSeparator =
                                character == separator;
                        }

                        return builder.ToString().Trim(separator);
                    }

                    /// <summary>
                    /// Removes the extension from a file name.
                    /// </summary>
                    public static string WithoutExtension(
                        string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return string.Empty;

                        return Path.GetFileNameWithoutExtension(
                            fileName.Trim());
                    }

                    /// <summary>
                    /// Changes or adds the extension of a file name.
                    /// </summary>
                    public static string ChangeExtension(
                        string fileName,
                        string? extension)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            fileName);

                        return Path.ChangeExtension(
                            fileName,
                            FileHelper.NormalizeExtension(extension));
                    }

                    /// <summary>
                    /// Determines whether a file name is safe for normal file-system use.
                    /// </summary>
                    public static bool IsSafe(string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return false;

                        var normalized = Normalize(fileName);

                        if (string.IsNullOrWhiteSpace(normalized))
                            return false;

                        if (!FileHelper.IsValidFileName(normalized))
                            return false;

                        if (normalized is "." or "..")
                            return false;

                        return true;
                    }

                    /// <summary>
                    /// Prevents Windows reserved device names from being used as file names.
                    /// </summary>
                    public static bool IsReservedName(string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return false;

                        var name = Path
                            .GetFileNameWithoutExtension(fileName)
                            .Trim()
                            .TrimEnd('.')
                            .ToUpperInvariant();

                        return name is
                            "CON" or
                            "PRN" or
                            "AUX" or
                            "NUL" or
                            "COM1" or
                            "COM2" or
                            "COM3" or
                            "COM4" or
                            "COM5" or
                            "COM6" or
                            "COM7" or
                            "COM8" or
                            "COM9" or
                            "LPT1" or
                            "LPT2" or
                            "LPT3" or
                            "LPT4" or
                            "LPT5" or
                            "LPT6" or
                            "LPT7" or
                            "LPT8" or
                            "LPT9";
                    }

                    /// <summary>
                    /// Creates a safe file name from arbitrary input.
                    /// </summary>
                    public static string CreateSafe(
                        string? fileName,
                        string fallback = "file")
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            fallback);

                        var normalized = Normalize(fileName);

                        if (string.IsNullOrWhiteSpace(normalized) ||
                            IsReservedName(normalized))
                        {
                            normalized = Normalize(fallback);
                        }

                        if (string.IsNullOrWhiteSpace(normalized))
                            normalized = "file";

                        return normalized;
                    }

                    /// <summary>
                    /// Creates a safe unique file name while preserving an extension.
                    /// </summary>
                    public static string CreateSafeUnique(
                        string? fileName,
                        string fallback = "file")
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            fallback);

                        var extension =
                            FileHelper.GetExtension(fileName);

                        var baseName =
                            FileHelper.GetFileNameWithoutExtension(fileName);

                        var safeBaseName =
                            CreateSafe(baseName, fallback);

                        var uniquePart =
                            Guid.NewGuid().ToString("N");

                        var result =
                            $"{safeBaseName}-{uniquePart}";

                        return string.IsNullOrEmpty(extension)
                            ? result
                            : $"{result}.{extension}";
                    }

                    private static string RemoveRepeatedReplacement(
                        string value,
                        string replacement)
                    {
                        if (string.IsNullOrEmpty(value) ||
                            string.IsNullOrEmpty(replacement))
                        {
                            return value;
                        }

                        while (value.Contains(
                                   replacement + replacement,
                                   StringComparison.Ordinal))
                        {
                            value = value.Replace(
                                replacement + replacement,
                                replacement,
                                StringComparison.Ordinal);
                        }

                        return value;
                    }

                    private static string TruncatePreservingExtension(
                        string fileName,
                        int maxLength)
                    {
                        if (fileName.Length <= maxLength)
                            return fileName;

                        var extension =
                            Path.GetExtension(fileName);

                        if (string.IsNullOrEmpty(extension))
                            return fileName[..maxLength];

                        var baseName =
                            Path.GetFileNameWithoutExtension(fileName);

                        var availableLength =
                            maxLength - extension.Length;

                        if (availableLength <= 0)
                            return extension.Length > maxLength
                                ? extension[..maxLength]
                                : extension;

                        if (baseName.Length > availableLength)
                            baseName = baseName[..availableLength];

                        return baseName + extension;
                    }
                }
                ```
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Files", "FileSizeHelper.cs"),
                """
                                ```csharp
                namespace Project.BuildingBlocks.Shared.Helpers.Files;

                /// <summary>
                /// Provides helpers for converting, formatting, and comparing file sizes.
                /// </summary>
                public static class FileSizeHelper
                {
                    /// <summary>
                    /// Number of bytes in one kilobyte using the binary convention.
                    /// </summary>
                    public const long BytesPerKilobyte = 1024;

                    /// <summary>
                    /// Number of bytes in one megabyte using the binary convention.
                    /// </summary>
                    public const long BytesPerMegabyte =
                        BytesPerKilobyte * 1024;

                    /// <summary>
                    /// Number of bytes in one gigabyte using the binary convention.
                    /// </summary>
                    public const long BytesPerGigabyte =
                        BytesPerMegabyte * 1024;

                    /// <summary>
                    /// Number of bytes in one terabyte using the binary convention.
                    /// </summary>
                    public const long BytesPerTerabyte =
                        BytesPerGigabyte * 1024;

                    /// <summary>
                    /// Converts bytes to kilobytes.
                    /// </summary>
                    public static decimal ToKilobytes(long bytes)
                    {
                        ValidateBytes(bytes);

                        return bytes / (decimal)BytesPerKilobyte;
                    }

                    /// <summary>
                    /// Converts bytes to megabytes.
                    /// </summary>
                    public static decimal ToMegabytes(long bytes)
                    {
                        ValidateBytes(bytes);

                        return bytes / (decimal)BytesPerMegabyte;
                    }

                    /// <summary>
                    /// Converts bytes to gigabytes.
                    /// </summary>
                    public static decimal ToGigabytes(long bytes)
                    {
                        ValidateBytes(bytes);

                        return bytes / (decimal)BytesPerGigabyte;
                    }

                    /// <summary>
                    /// Converts bytes to terabytes.
                    /// </summary>
                    public static decimal ToTerabytes(long bytes)
                    {
                        ValidateBytes(bytes);

                        return bytes / (decimal)BytesPerTerabyte;
                    }

                    /// <summary>
                    /// Converts kilobytes to bytes.
                    /// </summary>
                    public static long FromKilobytes(decimal kilobytes)
                    {
                        ValidateNonNegative(kilobytes, nameof(kilobytes));

                        return checked(
                            decimal.ToInt64(
                                decimal.Round(
                                    kilobytes * BytesPerKilobyte,
                                    0,
                                    MidpointRounding.AwayFromZero)));
                    }

                    /// <summary>
                    /// Converts megabytes to bytes.
                    /// </summary>
                    public static long FromMegabytes(decimal megabytes)
                    {
                        ValidateNonNegative(megabytes, nameof(megabytes));

                        return checked(
                            decimal.ToInt64(
                                decimal.Round(
                                    megabytes * BytesPerMegabyte,
                                    0,
                                    MidpointRounding.AwayFromZero)));
                    }

                    /// <summary>
                    /// Converts gigabytes to bytes.
                    /// </summary>
                    public static long FromGigabytes(decimal gigabytes)
                    {
                        ValidateNonNegative(gigabytes, nameof(gigabytes));

                        return checked(
                            decimal.ToInt64(
                                decimal.Round(
                                    gigabytes * BytesPerGigabyte,
                                    0,
                                    MidpointRounding.AwayFromZero)));
                    }

                    /// <summary>
                    /// Converts terabytes to bytes.
                    /// </summary>
                    public static long FromTerabytes(decimal terabytes)
                    {
                        ValidateNonNegative(terabytes, nameof(terabytes));

                        return checked(
                            decimal.ToInt64(
                                decimal.Round(
                                    terabytes * BytesPerTerabyte,
                                    0,
                                    MidpointRounding.AwayFromZero)));
                    }

                    /// <summary>
                    /// Converts a numeric value and unit into bytes.
                    /// </summary>
                    public static long ToBytes(
                        decimal value,
                        FileSizeUnit unit)
                    {
                        ValidateNonNegative(value, nameof(value));

                        var multiplier = unit switch
                        {
                            FileSizeUnit.Byte => 1m,
                            FileSizeUnit.Kilobyte => BytesPerKilobyte,
                            FileSizeUnit.Megabyte => BytesPerMegabyte,
                            FileSizeUnit.Gigabyte => BytesPerGigabyte,
                            FileSizeUnit.Terabyte => BytesPerTerabyte,
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(unit),
                                unit,
                                "Unsupported file size unit.")
                        };

                        return checked(
                            decimal.ToInt64(
                                decimal.Round(
                                    value * multiplier,
                                    0,
                                    MidpointRounding.AwayFromZero)));
                    }

                    /// <summary>
                    /// Determines whether a file size is within the specified limit.
                    /// </summary>
                    public static bool IsWithinLimit(
                        long bytes,
                        long maximumBytes)
                    {
                        ValidateBytes(bytes);
                        ValidateBytes(maximumBytes);

                        return bytes <= maximumBytes;
                    }

                    /// <summary>
                    /// Determines whether a file size exceeds the specified limit.
                    /// </summary>
                    public static bool ExceedsLimit(
                        long bytes,
                        long maximumBytes)
                    {
                        ValidateBytes(bytes);
                        ValidateBytes(maximumBytes);

                        return bytes > maximumBytes;
                    }

                    /// <summary>
                    /// Returns the difference between a maximum size and the current size.
                    /// </summary>
                    public static long Remaining(
                        long bytes,
                        long maximumBytes)
                    {
                        ValidateBytes(bytes);
                        ValidateBytes(maximumBytes);

                        return Math.Max(
                            0,
                            maximumBytes - bytes);
                    }

                    /// <summary>
                    /// Formats a byte count using the most appropriate binary unit.
                    /// </summary>
                    public static string Format(
                        long bytes,
                        int decimals = 2)
                    {
                        ValidateBytes(bytes);

                        if (decimals < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(decimals),
                                decimals,
                                "Decimal places cannot be negative.");
                        }

                        if (bytes == 0)
                            return "0 B";

                        var size = (decimal)bytes;
                        var unit = "B";

                        if (size >= BytesPerTerabyte)
                        {
                            size /= BytesPerTerabyte;
                            unit = "TB";
                        }
                        else if (size >= BytesPerGigabyte)
                        {
                            size /= BytesPerGigabyte;
                            unit = "GB";
                        }
                        else if (size >= BytesPerMegabyte)
                        {
                            size /= BytesPerMegabyte;
                            unit = "MB";
                        }
                        else if (size >= BytesPerKilobyte)
                        {
                            size /= BytesPerKilobyte;
                            unit = "KB";
                        }

                        size = decimal.Round(
                            size,
                            decimals,
                            MidpointRounding.ToEven);

                        return $"{size.ToString(CultureInfo.InvariantCulture)} {unit}";
                    }

                    /// <summary>
                    /// Formats a byte count using Persian unit names.
                    /// </summary>
                    public static string FormatPersian(
                        long bytes,
                        int decimals = 2)
                    {
                        ValidateBytes(bytes);

                        if (decimals < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(decimals),
                                decimals,
                                "Decimal places cannot be negative.");
                        }

                        if (bytes == 0)
                            return "۰ بایت";

                        var size = (decimal)bytes;
                        var unit = "بایت";

                        if (size >= BytesPerTerabyte)
                        {
                            size /= BytesPerTerabyte;
                            unit = "ترابایت";
                        }
                        else if (size >= BytesPerGigabyte)
                        {
                            size /= BytesPerGigabyte;
                            unit = "گیگابایت";
                        }
                        else if (size >= BytesPerMegabyte)
                        {
                            size /= BytesPerMegabyte;
                            unit = "مگابایت";
                        }
                        else if (size >= BytesPerKilobyte)
                        {
                            size /= BytesPerKilobyte;
                            unit = "کیلوبایت";
                        }

                        size = decimal.Round(
                            size,
                            decimals,
                            MidpointRounding.ToEven);

                        var formatted =
                            size.ToString(
                                $"F{decimals}",
                                CultureInfo.InvariantCulture);

                        formatted = Helpers.Numbers.PersianNumberHelper
                            .ToPersianDigits(formatted);

                        return $"{formatted} {unit}";
                    }

                    /// <summary>
                    /// Returns the most appropriate unit for the specified byte count.
                    /// </summary>
                    public static FileSizeUnit GetUnit(long bytes)
                    {
                        ValidateBytes(bytes);

                        if (bytes >= BytesPerTerabyte)
                            return FileSizeUnit.Terabyte;

                        if (bytes >= BytesPerGigabyte)
                            return FileSizeUnit.Gigabyte;

                        if (bytes >= BytesPerMegabyte)
                            return FileSizeUnit.Megabyte;

                        if (bytes >= BytesPerKilobyte)
                            return FileSizeUnit.Kilobyte;

                        return FileSizeUnit.Byte;
                    }

                    private static void ValidateBytes(long bytes)
                    {
                        if (bytes < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(bytes),
                                bytes,
                                "File size cannot be negative.");
                        }
                    }

                    private static void ValidateNonNegative(
                        decimal value,
                        string parameterName)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "File size cannot be negative.");
                        }
                    }
                }

                /// <summary>
                /// Represents binary file size units.
                /// </summary>
                public enum FileSizeUnit
                {
                    Byte = 0,
                    Kilobyte = 1,
                    Megabyte = 2,
                    Gigabyte = 3,
                    Terabyte = 4
                }
                ```
                """
            },
           
             // ==========================================
            // HELPERS - Images
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Images", "ImageHelper.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Helpers.Images;

                /// <summary>
                /// Provides lightweight helpers for identifying and validating common
                /// image file formats without depending on an external image-processing library.
                /// </summary>
                public static class ImageHelper
                {
                    private static readonly IReadOnlyDictionary<string, string> MimeTypes =
                        new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase)
                        {
                            [".jpg"] = "image/jpeg",
                            [".jpeg"] = "image/jpeg",
                            [".png"] = "image/png",
                            [".gif"] = "image/gif",
                            [".bmp"] = "image/bmp",
                            [".webp"] = "image/webp",
                            [".tif"] = "image/tiff",
                            [".tiff"] = "image/tiff",
                            [".ico"] = "image/x-icon"
                        };

                    /// <summary>
                    /// Determines whether the specified file extension represents
                    /// a supported common image format.
                    /// </summary>
                    public static bool IsImageExtension(string? extension)
                    {
                        var normalized = NormalizeExtension(extension);

                        return MimeTypes.ContainsKey(normalized);
                    }

                    /// <summary>
                    /// Determines whether the specified file name represents
                    /// a supported common image format.
                    /// </summary>
                    public static bool IsImageFileName(string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return false;

                        return IsImageExtension(
                            Path.GetExtension(fileName));
                    }

                    /// <summary>
                    /// Returns the MIME type associated with an image extension.
                    /// </summary>
                    public static string? GetMimeType(string? extension)
                    {
                        var normalized = NormalizeExtension(extension);

                        return MimeTypes.TryGetValue(
                            normalized,
                            out var mimeType)
                            ? mimeType
                            : null;
                    }

                    /// <summary>
                    /// Returns the MIME type associated with a file name.
                    /// </summary>
                    public static string? GetMimeTypeFromFileName(
                        string? fileName)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            return null;

                        return GetMimeType(
                            Path.GetExtension(fileName));
                    }

                    /// <summary>
                    /// Returns the normalized extension associated with a MIME type.
                    /// </summary>
                    public static string? GetExtensionFromMimeType(
                        string? mimeType)
                    {
                        if (string.IsNullOrWhiteSpace(mimeType))
                            return null;

                        var normalizedMimeType =
                            mimeType.Trim();

                        foreach (var pair in MimeTypes)
                        {
                            if (string.Equals(
                                    pair.Value,
                                    normalizedMimeType,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return pair.Key;
                            }
                        }

                        return null;
                    }

                    /// <summary>
                    /// Determines whether the specified MIME type represents
                    /// a supported image format.
                    /// </summary>
                    public static bool IsImageMimeType(
                        string? mimeType)
                    {
                        return GetExtensionFromMimeType(mimeType) is not null;
                    }

                    /// <summary>
                    /// Detects an image format from its file signature.
                    /// </summary>
                    /// <remarks>
                    /// This method does not trust the file extension and instead
                    /// examines the binary signature at the beginning of the file.
                    /// </remarks>
                    public static ImageFormat? DetectFormat(
                        ReadOnlySpan<byte> data)
                    {
                        if (data.Length >= 3 &&
                            data[0] == 0xFF &&
                            data[1] == 0xD8 &&
                            data[2] == 0xFF)
                        {
                            return ImageFormat.Jpeg;
                        }

                        if (data.Length >= 8 &&
                            data[0] == 0x89 &&
                            data[1] == 0x50 &&
                            data[2] == 0x4E &&
                            data[3] == 0x47 &&
                            data[4] == 0x0D &&
                            data[5] == 0x0A &&
                            data[6] == 0x1A &&
                            data[7] == 0x0A)
                        {
                            return ImageFormat.Png;
                        }

                        if (data.Length >= 6 &&
                            data[0] == (byte)'G' &&
                            data[1] == (byte)'I' &&
                            data[2] == (byte)'F' &&
                            data[3] == (byte)'8' &&
                            (data[4] == (byte)'7' ||
                             data[4] == (byte)'9') &&
                            data[5] == (byte)'a')
                        {
                            return ImageFormat.Gif;
                        }

                        if (data.Length >= 2 &&
                            data[0] == (byte)'B' &&
                            data[1] == (byte)'M')
                        {
                            return ImageFormat.Bmp;
                        }

                        if (data.Length >= 12 &&
                            data[0] == (byte)'R' &&
                            data[1] == (byte)'I' &&
                            data[2] == (byte)'F' &&
                            data[3] == (byte)'F' &&
                            data[8] == (byte)'W' &&
                            data[9] == (byte)'E' &&
                            data[10] == (byte)'B' &&
                            data[11] == (byte)'P')
                        {
                            return ImageFormat.WebP;
                        }

                        if (data.Length >= 4 &&
                            data[0] == 0x00 &&
                            data[1] == 0x00 &&
                            data[2] == 0x01 &&
                            data[3] == 0x00)
                        {
                            return ImageFormat.Ico;
                        }

                        if (data.Length >= 4 &&
                            data[0] == (byte)'I' &&
                            data[1] == (byte)'I' &&
                            data[2] == 0x2A &&
                            data[3] == 0x00)
                        {
                            return ImageFormat.Tiff;
                        }

                        if (data.Length >= 4 &&
                            data[0] == (byte)'M' &&
                            data[1] == (byte)'M' &&
                            data[2] == 0x00 &&
                            data[3] == 0x2A)
                        {
                            return ImageFormat.Tiff;
                        }

                        return null;
                    }

                    /// <summary>
                    /// Determines whether the supplied binary data appears to contain
                    /// a supported image format.
                    /// </summary>
                    public static bool IsImageData(
                        ReadOnlySpan<byte> data)
                    {
                        return DetectFormat(data) is not null;
                    }

                    /// <summary>
                    /// Determines whether the specified extension matches
                    /// the detected image format.
                    /// </summary>
                    public static bool MatchesExtension(
                        ReadOnlySpan<byte> data,
                        string? extension)
                    {
                        var format = DetectFormat(data);

                        if (format is null)
                            return false;

                        return format.Value switch
                        {
                            ImageFormat.Jpeg =>
                                IsExtension(extension, ".jpg", ".jpeg"),

                            ImageFormat.Png =>
                                IsExtension(extension, ".png"),

                            ImageFormat.Gif =>
                                IsExtension(extension, ".gif"),

                            ImageFormat.Bmp =>
                                IsExtension(extension, ".bmp"),

                            ImageFormat.WebP =>
                                IsExtension(extension, ".webp"),

                            ImageFormat.Tiff =>
                                IsExtension(extension, ".tif", ".tiff"),

                            ImageFormat.Ico =>
                                IsExtension(extension, ".ico"),

                            _ => false
                        };
                    }

                    /// <summary>
                    /// Returns the canonical extension for the specified image format.
                    /// </summary>
                    public static string GetCanonicalExtension(
                        ImageFormat format)
                    {
                        return format switch
                        {
                            ImageFormat.Jpeg => ".jpg",
                            ImageFormat.Png => ".png",
                            ImageFormat.Gif => ".gif",
                            ImageFormat.Bmp => ".bmp",
                            ImageFormat.WebP => ".webp",
                            ImageFormat.Tiff => ".tiff",
                            ImageFormat.Ico => ".ico",
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(format),
                                format,
                                "Unsupported image format.")
                        };
                    }

                    /// <summary>
                    /// Returns the MIME type for the specified image format.
                    /// </summary>
                    public static string GetMimeType(
                        ImageFormat format)
                    {
                        return format switch
                        {
                            ImageFormat.Jpeg => "image/jpeg",
                            ImageFormat.Png => "image/png",
                            ImageFormat.Gif => "image/gif",
                            ImageFormat.Bmp => "image/bmp",
                            ImageFormat.WebP => "image/webp",
                            ImageFormat.Tiff => "image/tiff",
                            ImageFormat.Ico => "image/x-icon",
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(format),
                                format,
                                "Unsupported image format.")
                        };
                    }

                    /// <summary>
                    /// Returns the image format represented by the specified extension.
                    /// </summary>
                    public static ImageFormat? GetFormatFromExtension(
                        string? extension)
                    {
                        var normalized = NormalizeExtension(extension);

                        return normalized switch
                        {
                            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                            ".png" => ImageFormat.Png,
                            ".gif" => ImageFormat.Gif,
                            ".bmp" => ImageFormat.Bmp,
                            ".webp" => ImageFormat.WebP,
                            ".tif" or ".tiff" => ImageFormat.Tiff,
                            ".ico" => ImageFormat.Ico,
                            _ => null
                        };
                    }

                    /// <summary>
                    /// Normalizes an image extension.
                    /// </summary>
                    public static string NormalizeExtension(
                        string? extension)
                    {
                        if (string.IsNullOrWhiteSpace(extension))
                            return string.Empty;

                        return extension
                            .Trim()
                            .TrimStart('.')
                            .Insert(0, ".")
                            .ToLowerInvariant();
                    }

                    private static bool IsExtension(
                        string? extension,
                        params string[] allowed)
                    {
                        var normalized =
                            NormalizeExtension(extension);

                        foreach (var item in allowed)
                        {
                            if (string.Equals(
                                    normalized,
                                    NormalizeExtension(item),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                /// <summary>
                /// Represents a supported image format.
                /// </summary>
                public enum ImageFormat
                {
                    Jpeg = 1,
                    Png = 2,
                    Gif = 3,
                    Bmp = 4,
                    WebP = 5,
                    Tiff = 6,
                    Ico = 7
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Images", "ImageMetadataHelper.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Helpers.Images;

                /// <summary>
                /// Provides lightweight metadata extraction for common image formats.
                /// </summary>
                /// <remarks>
                /// This helper intentionally does not depend on external imaging libraries.
                /// It reads only the image headers required to determine basic metadata
                /// such as format and dimensions.
                /// </remarks>
                public static class ImageMetadataHelper
                {
                    /// <summary>
                    /// Attempts to extract basic metadata from image data.
                    /// </summary>
                    public static bool TryGetMetadata(
                        ReadOnlySpan<byte> data,
                        out ImageMetadata metadata)
                    {
                        metadata = default!;

                        if (data.IsEmpty)
                            return false;

                        var format = ImageHelper.DetectFormat(data);

                        if (format is null)
                            return false;

                        if (!TryGetDimensions(
                                data,
                                format.Value,
                                out var width,
                                out var height))
                        {
                            return false;
                        }

                        metadata = new ImageMetadata(
                            format.Value,
                            width,
                            height,
                            data.Length);

                        return true;
                    }

                    /// <summary>
                    /// Attempts to extract image dimensions.
                    /// </summary>
                    public static bool TryGetDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.IsEmpty)
                            return false;

                        var format = ImageHelper.DetectFormat(data);

                        return format is not null &&
                               TryGetDimensions(
                                   data,
                                   format.Value,
                                   out width,
                                   out height);
                    }

                    /// <summary>
                    /// Gets the aspect ratio of an image.
                    /// </summary>
                    public static double GetAspectRatio(
                        int width,
                        int height)
                    {
                        ValidateDimensions(width, height);

                        return (double)width / height;
                    }

                    /// <summary>
                    /// Determines whether the image is landscape.
                    /// </summary>
                    public static bool IsLandscape(
                        int width,
                        int height)
                    {
                        ValidateDimensions(width, height);
                        return width > height;
                    }

                    /// <summary>
                    /// Determines whether the image is portrait.
                    /// </summary>
                    public static bool IsPortrait(
                        int width,
                        int height)
                    {
                        ValidateDimensions(width, height);
                        return height > width;
                    }

                    /// <summary>
                    /// Determines whether the image is square.
                    /// </summary>
                    public static bool IsSquare(
                        int width,
                        int height)
                    {
                        ValidateDimensions(width, height);
                        return width == height;
                    }

                    /// <summary>
                    /// Gets the number of pixels in the image.
                    /// </summary>
                    public static long GetPixelCount(
                        int width,
                        int height)
                    {
                        ValidateDimensions(width, height);

                        return checked((long)width * height);
                    }

                    /// <summary>
                    /// Attempts to extract dimensions for a known image format.
                    /// </summary>
                    private static bool TryGetDimensions(
                        ReadOnlySpan<byte> data,
                        ImageFormat format,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        return format switch
                        {
                            ImageFormat.Jpeg =>
                                TryGetJpegDimensions(data, out width, out height),

                            ImageFormat.Png =>
                                TryGetPngDimensions(data, out width, out height),

                            ImageFormat.Gif =>
                                TryGetGifDimensions(data, out width, out height),

                            ImageFormat.Bmp =>
                                TryGetBmpDimensions(data, out width, out height),

                            ImageFormat.WebP =>
                                TryGetWebPDimensions(data, out width, out height),

                            _ => false
                        };
                    }

                    private static bool TryGetPngDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.Length < 24)
                            return false;

                        // PNG signature + IHDR chunk.
                        if (data[12] != (byte)'I' ||
                            data[13] != (byte)'H' ||
                            data[14] != (byte)'D' ||
                            data[15] != (byte)'R')
                        {
                            return false;
                        }

                        var unsignedWidth = ReadUInt32BigEndian(data[16..20]);
                        var unsignedHeight = ReadUInt32BigEndian(data[20..24]);

                        return TryConvertDimensions(
                            unsignedWidth,
                            unsignedHeight,
                            out width,
                            out height);
                    }

                    private static bool TryGetGifDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.Length < 10)
                            return false;

                        if (data[0] != (byte)'G' ||
                            data[1] != (byte)'I' ||
                            data[2] != (byte)'F')
                        {
                            return false;
                        }

                        var gifWidth = ReadUInt16LittleEndian(data[6..8]);
                        var gifHeight = ReadUInt16LittleEndian(data[8..10]);

                        return TryConvertDimensions(
                            gifWidth,
                            gifHeight,
                            out width,
                            out height);
                    }

                    private static bool TryGetBmpDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.Length < 26)
                            return false;

                        if (data[0] != (byte)'B' ||
                            data[1] != (byte)'M')
                        {
                            return false;
                        }

                        var dibHeaderSize = ReadUInt32LittleEndian(data[14..18]);

                        if (dibHeaderSize < 12)
                            return false;

                        if (dibHeaderSize == 12)
                        {
                            var bmpWidth = ReadUInt16LittleEndian(data[18..20]);
                            var bmpHeight = ReadUInt16LittleEndian(data[20..22]);

                            return TryConvertDimensions(
                                bmpWidth,
                                bmpHeight,
                                out width,
                                out height);
                        }

                        if (data.Length < 26)
                            return false;

                        var signedWidth = ReadInt32LittleEndian(data[18..22]);
                        var signedHeight = ReadInt32LittleEndian(data[22..26]);

                        if (signedWidth <= 0 ||
                            signedHeight == 0 ||
                            signedHeight == int.MinValue)
                        {
                            return false;
                        }

                        width = signedWidth;
                        height = Math.Abs(signedHeight);

                        return true;
                    }

                    private static bool TryGetWebPDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.Length < 30)
                            return false;

                        if (data[0] != (byte)'R' ||
                            data[1] != (byte)'I' ||
                            data[2] != (byte)'F' ||
                            data[3] != (byte)'F' ||
                            data[8] != (byte)'W' ||
                            data[9] != (byte)'E' ||
                            data[10] != (byte)'B' ||
                            data[11] != (byte)'P')
                        {
                            return false;
                        }

                        var chunk = data[12..];

                        if (chunk.Length < 8)
                            return false;

                        if (chunk[0] == (byte)'V' &&
                            chunk[1] == (byte)'P' &&
                            chunk[2] == (byte)'8' &&
                            chunk[3] == (byte)' ')
                        {
                            return TryGetWebpVp8Dimensions(
                                chunk,
                                out width,
                                out height);
                        }

                        if (chunk[0] == (byte)'V' &&
                            chunk[1] == (byte)'P' &&
                            chunk[2] == (byte)'8' &&
                            chunk[3] == (byte)'L')
                        {
                            return TryGetWebpVp8LDimensions(
                                chunk,
                                out width,
                                out height);
                        }

                        if (chunk[0] == (byte)'V' &&
                            chunk[1] == (byte)'P' &&
                            chunk[2] == (byte)'8' &&
                            chunk[3] == (byte)'X')
                        {
                            return TryGetWebpVp8XDimensions(
                                chunk,
                                out width,
                                out height);
                        }

                        return false;
                    }

                    private static bool TryGetWebpVp8Dimensions(
                        ReadOnlySpan<byte> chunk,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        // VP8 lossy frame header:
                        // start code: 9D 01 2A
                        if (chunk.Length < 30)
                            return false;

                        var data = chunk[8..];

                        if (data.Length < 10)
                            return false;

                        var startCodeIndex = FindSequence(
                            data,
                            0x9D,
                            0x01,
                            0x2A);

                        if (startCodeIndex < 0 ||
                            data.Length < startCodeIndex + 7)
                        {
                            return false;
                        }

                        var dimensionOffset = startCodeIndex + 3;

                        var webpWidth = ReadUInt16LittleEndian(
                            data[dimensionOffset..]);

                        var webpHeight = ReadUInt16LittleEndian(
                            data[(dimensionOffset + 2)..]);

                        return TryConvertDimensions(
                            webpWidth & 0x3FFF,
                            webpHeight & 0x3FFF,
                            out width,
                            out height);
                    }

                    private static bool TryGetWebpVp8LDimensions(
                        ReadOnlySpan<byte> chunk,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (chunk.Length < 25)
                            return false;

                        // VP8L signature byte after chunk header.
                        if (chunk[8] != 0x2F)
                            return false;

                        var bits = chunk[9..];

                        if (bits.Length < 5)
                            return false;

                        var b0 = bits[0];
                        var b1 = bits[1];
                        var b2 = bits[2];
                        var b3 = bits[3];

                        var webpWidth =
                            1 +
                            ((b0 | ((b1 & 0x3F) << 8)) & 0x3FFF);

                        var webpHeight =
                            1 +
                            (((b1 >> 6) |
                              (b2 << 2) |
                              ((b3 & 0x0F) << 10)) & 0x3FFF);

                        return TryConvertDimensions(
                            webpWidth,
                            webpHeight,
                            out width,
                            out height);
                    }

                    private static bool TryGetWebpVp8XDimensions(
                        ReadOnlySpan<byte> chunk,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (chunk.Length < 30)
                            return false;

                        // VP8X dimensions are stored as 24-bit little-endian
                        // values representing width - 1 and height - 1.
                        var widthMinusOne =
                            chunk[24] |
                            (chunk[25] << 8) |
                            (chunk[26] << 16);

                        var heightMinusOne =
                            chunk[27] |
                            (chunk[28] << 8) |
                            (chunk[29] << 16);

                        return TryConvertDimensions(
                            (uint)(widthMinusOne + 1),
                            (uint)(heightMinusOne + 1),
                            out width,
                            out height);
                    }

                    private static bool TryGetJpegDimensions(
                        ReadOnlySpan<byte> data,
                        out int width,
                        out int height)
                    {
                        width = 0;
                        height = 0;

                        if (data.Length < 4 ||
                            data[0] != 0xFF ||
                            data[1] != 0xD8)
                        {
                            return false;
                        }

                        var offset = 2;

                        while (offset + 1 < data.Length)
                        {
                            while (offset < data.Length &&
                                   data[offset] == 0xFF)
                            {
                                offset++;
                            }

                            if (offset >= data.Length)
                                return false;

                            var marker = data[offset++];

                            if (marker == 0xD8 ||
                                marker == 0xD9)
                            {
                                continue;
                            }

                            if (marker == 0xDA)
                            {
                                // Start of scan. No more frame headers are expected.
                                return false;
                            }

                            if (offset + 1 >= data.Length)
                                return false;

                            var segmentLength =
                                ReadUInt16BigEndian(data[offset..]);

                            if (segmentLength < 2 ||
                                offset + segmentLength > data.Length)
                            {
                                return false;
                            }

                            if (IsJpegStartOfFrame(marker))
                            {
                                if (segmentLength < 7 ||
                                    offset + 6 > data.Length)
                                {
                                    return false;
                                }

                                var frameHeight =
                                    ReadUInt16BigEndian(data[(offset + 3)..]);

                                var frameWidth =
                                    ReadUInt16BigEndian(data[(offset + 5)..]);

                                return TryConvertDimensions(
                                    frameWidth,
                                    frameHeight,
                                    out width,
                                    out height);
                            }

                            offset += segmentLength;
                        }

                        return false;
                    }

                    private static bool IsJpegStartOfFrame(byte marker)
                    {
                        return marker switch
                        {
                            0xC0 or
                            0xC1 or
                            0xC2 or
                            0xC3 or
                            0xC5 or
                            0xC6 or
                            0xC7 or
                            0xC9 or
                            0xCA or
                            0xCB or
                            0xCD or
                            0xCE or
                            0xCF => true,

                            _ => false
                        };
                    }

                    private static int FindSequence(
                        ReadOnlySpan<byte> data,
                        byte first,
                        byte second,
                        byte third)
                    {
                        for (var index = 0; index <= data.Length - 3; index++)
                        {
                            if (data[index] == first &&
                                data[index + 1] == second &&
                                data[index + 2] == third)
                            {
                                return index;
                            }
                        }

                        return -1;
                    }

                    private static ushort ReadUInt16LittleEndian(
                        ReadOnlySpan<byte> data)
                    {
                        return (ushort)(
                            data[0] |
                            (data[1] << 8));
                    }

                    private static ushort ReadUInt16BigEndian(
                        ReadOnlySpan<byte> data)
                    {
                        return (ushort)(
                            (data[0] << 8) |
                            data[1]);
                    }

                    private static uint ReadUInt32LittleEndian(
                        ReadOnlySpan<byte> data)
                    {
                        return
                            (uint)data[0] |
                            ((uint)data[1] << 8) |
                            ((uint)data[2] << 16) |
                            ((uint)data[3] << 24);
                    }

                    private static uint ReadUInt32BigEndian(
                        ReadOnlySpan<byte> data)
                    {
                        return
                            ((uint)data[0] << 24) |
                            ((uint)data[1] << 16) |
                            ((uint)data[2] << 8) |
                            data[3];
                    }

                    private static int ReadInt32LittleEndian(
                        ReadOnlySpan<byte> data)
                    {
                        return unchecked((int)ReadUInt32LittleEndian(data));
                    }

                    private static bool TryConvertDimensions(
                        uint width,
                        uint height,
                        out int convertedWidth,
                        out int convertedHeight)
                    {
                        convertedWidth = 0;
                        convertedHeight = 0;

                        if (width == 0 ||
                            height == 0 ||
                            width > int.MaxValue ||
                            height > int.MaxValue)
                        {
                            return false;
                        }

                        convertedWidth = (int)width;
                        convertedHeight = (int)height;

                        return true;
                    }

                    private static void ValidateDimensions(
                        int width,
                        int height)
                    {
                        if (width <= 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(width),
                                width,
                                "Image width must be greater than zero.");
                        }

                        if (height <= 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(height),
                                height,
                                "Image height must be greater than zero.");
                        }
                    }
                }

                /// <summary>
                /// Represents lightweight metadata extracted from an image.
                /// </summary>
                public sealed record ImageMetadata
                {
                    public ImageMetadata(
                        ImageFormat format,
                        int width,
                        int height,
                        long fileSize)
                    {
                        if (width <= 0)
                            throw new ArgumentOutOfRangeException(nameof(width));

                        if (height <= 0)
                            throw new ArgumentOutOfRangeException(nameof(height));

                        if (fileSize < 0)
                            throw new ArgumentOutOfRangeException(nameof(fileSize));

                        Format = format;
                        Width = width;
                        Height = height;
                        FileSize = fileSize;
                    }

                    public ImageFormat Format { get; }

                    public int Width { get; }

                    public int Height { get; }

                    public long FileSize { get; }

                    public double AspectRatio =>
                        (double)Width / Height;

                    public long PixelCount =>
                        checked((long)Width * Height);

                    public bool IsLandscape =>
                        Width > Height;

                    public bool IsPortrait =>
                        Height > Width;

                    public bool IsSquare =>
                        Width == Height;

                    public string MimeType =>
                        ImageHelper.GetMimeType(Format);

                    public string Extension =>
                        ImageHelper.GetCanonicalExtension(Format);
                }
                """
            },
          
             // ==========================================
            // HELPERS - Urls
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Urls", "UrlHelper.cs"),
                """
                namespace Project.BuildingBlocks.Shared.Helpers.Urls;

                /// <summary>
                /// Provides framework-independent helpers for working with URLs.
                /// </summary>
                /// <remarks>
                /// This helper does not depend on ASP.NET Core, HttpContext, routing,
                /// or application-specific URL generation.
                /// </remarks>
                public static class UrlHelper
                {
                    /// <summary>
                    /// Determines whether the specified value is an absolute URI.
                    /// </summary>
                    public static bool IsAbsolute(string? value)
                    {
                        return Uri.TryCreate(
                            value,
                            UriKind.Absolute,
                            out _);
                    }

                    /// <summary>
                    /// Determines whether the specified value is a relative URI.
                    /// </summary>
                    public static bool IsRelative(string? value)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        return Uri.TryCreate(
                            value.Trim(),
                            UriKind.Relative,
                            out _);
                    }

                    /// <summary>
                    /// Determines whether the specified value is a valid absolute HTTP or HTTPS URL.
                    /// </summary>
                    public static bool IsHttpUrl(string? value)
                    {
                        if (!TryCreateAbsolute(value, out var uri))
                            return false;

                        return uri.Scheme.Equals(
                                   Uri.UriSchemeHttp,
                                   StringComparison.OrdinalIgnoreCase)
                               ||
                               uri.Scheme.Equals(
                                   Uri.UriSchemeHttps,
                                   StringComparison.OrdinalIgnoreCase);
                    }

                    /// <summary>
                    /// Determines whether the specified value is an HTTPS URL.
                    /// </summary>
                    public static bool IsHttps(string? value)
                    {
                        return TryCreateAbsolute(value, out var uri)
                            && uri.Scheme.Equals(
                                Uri.UriSchemeHttps,
                                StringComparison.OrdinalIgnoreCase);
                    }

                    /// <summary>
                    /// Determines whether the specified value is an HTTP URL.
                    /// </summary>
                    public static bool IsHttp(string? value)
                    {
                        return TryCreateAbsolute(value, out var uri)
                            && uri.Scheme.Equals(
                                Uri.UriSchemeHttp,
                                StringComparison.OrdinalIgnoreCase);
                    }

                    /// <summary>
                    /// Attempts to create an absolute URI.
                    /// </summary>
                    public static bool TryCreateAbsolute(
                        string? value,
                        out Uri uri)
                    {
                        return Uri.TryCreate(
                            value?.Trim(),
                            UriKind.Absolute,
                            out uri!);
                    }

                    /// <summary>
                    /// Attempts to create a URI using the specified URI kind.
                    /// </summary>
                    public static bool TryCreate(
                        string? value,
                        UriKind uriKind,
                        out Uri uri)
                    {
                        return Uri.TryCreate(
                            value?.Trim(),
                            uriKind,
                            out uri!);
                    }

                    /// <summary>
                    /// Gets the scheme of an absolute URL.
                    /// </summary>
                    public static string GetScheme(string? url)
                    {
                        return TryCreateAbsolute(url, out var uri)
                            ? uri.Scheme
                            : string.Empty;
                    }

                    /// <summary>
                    /// Gets the host of an absolute URL.
                    /// </summary>
                    public static string GetHost(string? url)
                    {
                        return TryCreateAbsolute(url, out var uri)
                            ? uri.Host
                            : string.Empty;
                    }

                    /// <summary>
                    /// Gets the port of an absolute URL.
                    /// </summary>
                    public static int? GetPort(string? url)
                    {
                        return TryCreateAbsolute(url, out var uri)
                            ? uri.Port
                            : null;
                    }

                    /// <summary>
                    /// Gets the absolute path component of a URL.
                    /// </summary>
                    public static string GetPath(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url))
                            return string.Empty;

                        if (TryCreateAbsolute(url, out var absoluteUri))
                            return absoluteUri.AbsolutePath;

                        return Uri.TryCreate(
                                url.Trim(),
                                UriKind.Relative,
                                out var relativeUri)
                            ? relativeUri.OriginalString.Split(
                                '?',
                                '#')[0]
                            : string.Empty;
                    }

                    /// <summary>
                    /// Gets the query component without the leading question mark.
                    /// </summary>
                    public static string GetQuery(string? url)
                    {
                        if (!TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                            return string.Empty;

                        return uri.Query.TrimStart('?');
                    }

                    /// <summary>
                    /// Gets the fragment component without the leading hash.
                    /// </summary>
                    public static string GetFragment(string? url)
                    {
                        if (!TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                            return string.Empty;

                        return uri.Fragment.TrimStart('#');
                    }

                    /// <summary>
                    /// Gets the complete authority component of an absolute URL.
                    /// </summary>
                    public static string GetAuthority(string? url)
                    {
                        return TryCreateAbsolute(url, out var uri)
                            ? uri.Authority
                            : string.Empty;
                    }

                    /// <summary>
                    /// Gets the base URL consisting of scheme, host and optional port.
                    /// </summary>
                    public static string GetBaseUrl(string? url)
                    {
                        if (!TryCreateAbsolute(url, out var uri))
                            return string.Empty;

                        return $"{uri.Scheme}://{uri.Authority}";
                    }

                    /// <summary>
                    /// Removes the query string and fragment from a URL.
                    /// </summary>
                    public static string RemoveQueryAndFragment(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url))
                            return string.Empty;

                        if (!TryCreate(
                                url,
                                UriKind.RelativeOrAbsolute,
                                out var uri))
                        {
                            return string.Empty;
                        }

                        if (uri.IsAbsoluteUri)
                        {
                            return $"{uri.GetLeftPart(UriPartial.Path)}";
                        }

                        var value = uri.OriginalString;

                        var queryIndex = value.IndexOf('?');
                        var fragmentIndex = value.IndexOf('#');

                        var endIndex = value.Length;

                        if (queryIndex >= 0)
                            endIndex = Math.Min(endIndex, queryIndex);

                        if (fragmentIndex >= 0)
                            endIndex = Math.Min(endIndex, fragmentIndex);

                        return value[..endIndex];
                    }

                    /// <summary>
                    /// Removes a trailing slash from a URL.
                    /// </summary>
                    public static string TrimTrailingSlash(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url))
                            return string.Empty;

                        var value = url.Trim();

                        while (value.Length > 1 &&
                               value.EndsWith(
                                   '/',
                                   StringComparison.Ordinal))
                        {
                            value = value[..^1];
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a URL ends with exactly one slash.
                    /// </summary>
                    public static string EnsureTrailingSlash(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url))
                            return string.Empty;

                        return TrimTrailingSlash(url) + "/";
                    }

                    /// <summary>
                    /// Combines URL path segments while preserving the base URL.
                    /// </summary>
                    public static string Combine(
                        string? baseUrl,
                        params string?[] segments)
                    {
                        ArgumentNullException.ThrowIfNull(segments);

                        if (string.IsNullOrWhiteSpace(baseUrl))
                            return string.Empty;

                        var result = TrimTrailingSlash(baseUrl);

                        foreach (var segment in segments)
                        {
                            if (string.IsNullOrWhiteSpace(segment))
                                continue;

                            result += "/" + segment
                                .Trim()
                                .Trim('/');
                        }

                        return result;
                    }

                    /// <summary>
                    /// Adds or replaces a query parameter.
                    /// </summary>
                    public static string AddQueryParameter(
                        string url,
                        string key,
                        string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(url);
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        var uriBuilder = new UriBuilder(url);

                        var query = ParseQuery(uriBuilder.Query);

                        query[key.Trim()] = value;

                        uriBuilder.Query = BuildQuery(query);

                        return uriBuilder.Uri.IsAbsoluteUri
                            ? uriBuilder.Uri.AbsoluteUri
                            : uriBuilder.Uri.OriginalString;
                    }

                    /// <summary>
                    /// Adds or replaces multiple query parameters.
                    /// </summary>
                    public static string AddQueryParameters(
                        string url,
                        IEnumerable<KeyValuePair<string, string?>> parameters)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(url);
                        ArgumentNullException.ThrowIfNull(parameters);

                        var uriBuilder = new UriBuilder(url);
                        var query = ParseQuery(uriBuilder.Query);

                        foreach (var parameter in parameters)
                        {
                            if (string.IsNullOrWhiteSpace(parameter.Key))
                                continue;

                            query[parameter.Key.Trim()] = parameter.Value;
                        }

                        uriBuilder.Query = BuildQuery(query);

                        return uriBuilder.Uri.IsAbsoluteUri
                            ? uriBuilder.Uri.AbsoluteUri
                            : uriBuilder.Uri.OriginalString;
                    }

                    /// <summary>
                    /// Removes a query parameter from a URL.
                    /// </summary>
                    public static string RemoveQueryParameter(
                        string url,
                        string key)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(url);
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        var uriBuilder = new UriBuilder(url);
                        var query = ParseQuery(uriBuilder.Query);

                        query.Remove(key.Trim());

                        uriBuilder.Query = BuildQuery(query);

                        return uriBuilder.Uri.IsAbsoluteUri
                            ? uriBuilder.Uri.AbsoluteUri
                            : uriBuilder.Uri.OriginalString;
                    }

                    /// <summary>
                    /// Determines whether a URL contains the specified query parameter.
                    /// </summary>
                    public static bool HasQueryParameter(
                        string? url,
                        string key)
                    {
                        if (string.IsNullOrWhiteSpace(url) ||
                            string.IsNullOrWhiteSpace(key))
                        {
                            return false;
                        }

                        var query = ParseQuery(GetQuery(url));

                        return query.ContainsKey(key.Trim());
                    }

                    /// <summary>
                    /// Gets a query parameter value.
                    /// </summary>
                    public static string? GetQueryParameter(
                        string? url,
                        string key)
                    {
                        if (string.IsNullOrWhiteSpace(url) ||
                            string.IsNullOrWhiteSpace(key))
                        {
                            return null;
                        }

                        var query = ParseQuery(GetQuery(url));

                        return query.TryGetValue(
                            key.Trim(),
                            out var value)
                            ? value
                            : null;
                    }

                    /// <summary>
                    /// Parses a query string into a dictionary.
                    /// </summary>
                    public static IReadOnlyDictionary<string, string?> ParseQuery(
                        string? query)
                    {
                        if (string.IsNullOrWhiteSpace(query))
                            return new Dictionary<string, string?>(
                                StringComparer.OrdinalIgnoreCase);

                        var value = query.Trim();

                        if (value.StartsWith('?'))
                            value = value[1..];

                        var result = new Dictionary<string, string?>(
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var part in value.Split(
                                     '&',
                                     StringSplitOptions.RemoveEmptyEntries))
                        {
                            var separatorIndex = part.IndexOf('=');

                            if (separatorIndex < 0)
                            {
                                var keyOnly = Uri.UnescapeDataString(part);

                                if (!string.IsNullOrWhiteSpace(keyOnly))
                                    result[keyOnly] = null;

                                continue;
                            }

                            var key = Uri.UnescapeDataString(
                                part[..separatorIndex]);

                            if (string.IsNullOrWhiteSpace(key))
                                continue;

                            var encodedValue = part[(separatorIndex + 1)..];

                            result[key] = Uri.UnescapeDataString(
                                encodedValue);
                        }

                        return result;
                    }

                    /// <summary>
                    /// Builds a query string from key/value pairs.
                    /// </summary>
                    public static string BuildQuery(
                        IEnumerable<KeyValuePair<string, string?>> parameters)
                    {
                        ArgumentNullException.ThrowIfNull(parameters);

                        var parts = new List<string>();

                        foreach (var parameter in parameters)
                        {
                            if (string.IsNullOrWhiteSpace(parameter.Key))
                                continue;

                            var key = Uri.EscapeDataString(
                                parameter.Key.Trim());

                            if (parameter.Value is null)
                            {
                                parts.Add(key);
                                continue;
                            }

                            var value = Uri.EscapeDataString(
                                parameter.Value);

                            parts.Add($"{key}={value}");
                        }

                        return string.Join('&', parts);
                    }

                    /// <summary>
                    /// Normalizes a URL by trimming surrounding whitespace,
                    /// removing duplicate trailing slashes and normalizing the scheme/host.
                    /// </summary>
                    public static string Normalize(string? url)
                    {
                        if (string.IsNullOrWhiteSpace(url))
                            return string.Empty;

                        var value = url.Trim();

                        if (!TryCreate(
                                value,
                                UriKind.RelativeOrAbsolute,
                                out var uri))
                        {
                            return string.Empty;
                        }

                        if (!uri.IsAbsoluteUri)
                            return TrimTrailingSlash(value);

                        var builder = new UriBuilder(uri)
                        {
                            Scheme = uri.Scheme.ToLowerInvariant(),
                            Host = uri.Host.ToLowerInvariant()
                        };

                        return builder.Uri.AbsoluteUri;
                    }

                    /// <summary>
                    /// Gets the normalized domain/host from an absolute URL.
                    /// </summary>
                    public static string GetDomain(string? url)
                    {
                        return GetHost(url).ToLowerInvariant();
                    }

                    /// <summary>
                    /// Determines whether the URL belongs to the specified host.
                    /// </summary>
                    public static bool IsHost(
                        string? url,
                        string? host)
                    {
                        if (string.IsNullOrWhiteSpace(host))
                            return false;

                        var actualHost = GetHost(url);

                        return string.Equals(
                            actualHost,
                            host.Trim(),
                            StringComparison.OrdinalIgnoreCase);
                    }

                    /// <summary>
                    /// Determines whether the URL belongs to the specified host
                    /// or one of its subdomains.
                    /// </summary>
                    public static bool IsHostOrSubdomain(
                        string? url,
                        string? host)
                    {
                        if (string.IsNullOrWhiteSpace(host))
                            return false;

                        var actualHost = GetHost(url);

                        if (string.IsNullOrWhiteSpace(actualHost))
                            return false;

                        var normalizedHost = host.Trim().TrimEnd('.');

                        return string.Equals(
                                   actualHost,
                                   normalizedHost,
                                   StringComparison.OrdinalIgnoreCase)
                               ||
                               actualHost.EndsWith(
                                   "." + normalizedHost,
                                   StringComparison.OrdinalIgnoreCase);
                    }

                    /// <summary>
                    /// Creates an absolute URL from a base URL and a relative path.
                    /// </summary>
                    public static string Resolve(
                        string baseUrl,
                        string relativePath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
                        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

                        if (!TryCreateAbsolute(
                                baseUrl,
                                out var baseUri))
                        {
                            throw new ArgumentException(
                                "Base URL must be an absolute URL.",
                                nameof(baseUrl));
                        }

                        if (!Uri.TryCreate(
                                baseUri,
                                relativePath.Trim(),
                                out var result))
                        {
                            throw new ArgumentException(
                                "The relative path is not a valid URI.",
                                nameof(relativePath));
                        }

                        return result.AbsoluteUri;
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Images", "QueryStringHelper.cs"),
                """
                                
                namespace Project.BuildingBlocks.Shared.Helpers.Urls;

                /// <summary>
                /// Provides framework-independent helpers for reading, building,
                /// and manipulating query strings.
                /// </summary>
                public static class QueryStringHelper
                {
                    /// <summary>
                    /// Determines whether the specified query string contains
                    /// at least one parameter.
                    /// </summary>
                    public static bool HasValue(string? queryString)
                    {
                        return Parse(queryString).Count > 0;
                    }

                    /// <summary>
                    /// Determines whether the query string contains the specified key.
                    /// </summary>
                    public static bool ContainsKey(
                        string? queryString,
                        string key)
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            return false;

                        return Parse(queryString).ContainsKey(key.Trim());
                    }

                    /// <summary>
                    /// Gets a query parameter value.
                    /// </summary>
                    public static string? Get(
                        string? queryString,
                        string key)
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            return null;

                        var values = Parse(queryString);

                        return values.TryGetValue(
                            key.Trim(),
                            out var value)
                            ? value
                            : null;
                    }

                    /// <summary>
                    /// Gets all values associated with the specified key.
                    /// </summary>
                    public static IReadOnlyList<string?> GetAll(
                        string? queryString,
                        string key)
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            return Array.Empty<string?>();

                        var normalizedKey = key.Trim();

                        var result = new List<string?>();

                        foreach (var parameter in ParsePairs(queryString))
                        {
                            if (string.Equals(
                                    parameter.Key,
                                    normalizedKey,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(parameter.Value);
                            }
                        }

                        return result;
                    }

                    /// <summary>
                    /// Adds a query parameter.
                    /// </summary>
                    public static string Add(
                        string? queryString,
                        string key,
                        string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        var parameters = ParsePairs(queryString).ToList();

                        parameters.Add(
                            new KeyValuePair<string, string?>(
                                key.Trim(),
                                value));

                        return Build(parameters);
                    }

                    /// <summary>
                    /// Adds multiple query parameters.
                    /// </summary>
                    public static string AddRange(
                        string? queryString,
                        IEnumerable<KeyValuePair<string, string?>> parameters)
                    {
                        ArgumentNullException.ThrowIfNull(parameters);

                        var result = ParsePairs(queryString).ToList();

                        foreach (var parameter in parameters)
                        {
                            if (string.IsNullOrWhiteSpace(parameter.Key))
                                continue;

                            result.Add(
                                new KeyValuePair<string, string?>(
                                    parameter.Key.Trim(),
                                    parameter.Value));
                        }

                        return Build(result);
                    }

                    /// <summary>
                    /// Sets a query parameter.
                    /// Existing values for the same key are removed.
                    /// </summary>
                    public static string Set(
                        string? queryString,
                        string key,
                        string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        var normalizedKey = key.Trim();

                        var parameters = ParsePairs(queryString)
                            .Where(parameter =>
                                !string.Equals(
                                    parameter.Key,
                                    normalizedKey,
                                    StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        parameters.Add(
                            new KeyValuePair<string, string?>(
                                normalizedKey,
                                value));

                        return Build(parameters);
                    }

                    /// <summary>
                    /// Sets multiple query parameters.
                    /// Existing values for matching keys are removed.
                    /// </summary>
                    public static string SetRange(
                        string? queryString,
                        IEnumerable<KeyValuePair<string, string?>> parameters)
                    {
                        ArgumentNullException.ThrowIfNull(parameters);

                        var result = ParsePairs(queryString).ToList();

                        foreach (var parameter in parameters)
                        {
                            if (string.IsNullOrWhiteSpace(parameter.Key))
                                continue;

                            var normalizedKey = parameter.Key.Trim();

                            result.RemoveAll(existing =>
                                string.Equals(
                                    existing.Key,
                                    normalizedKey,
                                    StringComparison.OrdinalIgnoreCase));

                            result.Add(
                                new KeyValuePair<string, string?>(
                                    normalizedKey,
                                    parameter.Value));
                        }

                        return Build(result);
                    }

                    /// <summary>
                    /// Removes all parameters with the specified key.
                    /// </summary>
                    public static string Remove(
                        string? queryString,
                        string key)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        var normalizedKey = key.Trim();

                        return Build(
                            ParsePairs(queryString)
                                .Where(parameter =>
                                    !string.Equals(
                                        parameter.Key,
                                        normalizedKey,
                                        StringComparison.OrdinalIgnoreCase)));
                    }

                    /// <summary>
                    /// Removes multiple query parameters.
                    /// </summary>
                    public static string RemoveRange(
                        string? queryString,
                        IEnumerable<string> keys)
                    {
                        ArgumentNullException.ThrowIfNull(keys);

                        var normalizedKeys = new HashSet<string>(
                            keys.Where(static key =>
                                    !string.IsNullOrWhiteSpace(key))
                                .Select(static key => key.Trim()),
                            StringComparer.OrdinalIgnoreCase);

                        if (normalizedKeys.Count == 0)
                            return Build(ParsePairs(queryString));

                        return Build(
                            ParsePairs(queryString)
                                .Where(parameter =>
                                    !normalizedKeys.Contains(parameter.Key)));
                    }

                    /// <summary>
                    /// Removes all query parameters and returns an empty query string.
                    /// </summary>
                    public static string Clear()
                    {
                        return string.Empty;
                    }

                    /// <summary>
                    /// Parses a query string into a dictionary.
                    /// Duplicate keys are represented by the last value.
                    /// </summary>
                    public static IReadOnlyDictionary<string, string?> Parse(
                        string? queryString)
                    {
                        var result = new Dictionary<string, string?>(
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var parameter in ParsePairs(queryString))
                        {
                            result[parameter.Key] = parameter.Value;
                        }

                        return result;
                    }

                    /// <summary>
                    /// Parses a query string while preserving duplicate keys.
                    /// </summary>
                    public static IReadOnlyList<KeyValuePair<string, string?>> ParsePairs(
                        string? queryString)
                    {
                        if (string.IsNullOrWhiteSpace(queryString))
                            return Array.Empty<KeyValuePair<string, string?>>();

                        var value = queryString.Trim();

                        if (value.StartsWith('?'))
                            value = value[1..];

                        if (value.Length == 0)
                            return Array.Empty<KeyValuePair<string, string?>>();

                        var result =
                            new List<KeyValuePair<string, string?>>();

                        foreach (var part in value.Split(
                                     '&',
                                     StringSplitOptions.RemoveEmptyEntries))
                        {
                            var separatorIndex = part.IndexOf('=');

                            if (separatorIndex < 0)
                            {
                                var key = Decode(part);

                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    result.Add(
                                        new KeyValuePair<string, string?>(
                                            key,
                                            null));
                                }

                                continue;
                            }

                            var encodedKey =
                                part[..separatorIndex];

                            var encodedValue =
                                part[(separatorIndex + 1)..];

                            var decodedKey = Decode(encodedKey);

                            if (string.IsNullOrWhiteSpace(decodedKey))
                                continue;

                            result.Add(
                                new KeyValuePair<string, string?>(
                                    decodedKey,
                                    Decode(encodedValue)));
                        }

                        return result;
                    }

                    /// <summary>
                    /// Builds a query string from key/value pairs.
                    /// </summary>
                    public static string Build(
                        IEnumerable<KeyValuePair<string, string?>> parameters)
                    {
                        ArgumentNullException.ThrowIfNull(parameters);

                        var parts = new List<string>();

                        foreach (var parameter in parameters)
                        {
                            if (string.IsNullOrWhiteSpace(parameter.Key))
                                continue;

                            var key = Encode(parameter.Key.Trim());

                            if (parameter.Value is null)
                            {
                                parts.Add(key);
                                continue;
                            }

                            var value = Encode(parameter.Value);

                            parts.Add($"{key}={value}");
                        }

                        return string.Join('&', parts);
                    }

                    /// <summary>
                    /// Builds a query string from a dictionary.
                    /// </summary>
                    public static string Build(
                        IReadOnlyDictionary<string, string?> parameters)
                    {
                        ArgumentNullException.ThrowIfNull(parameters);

                        return Build(
                            parameters.Select(
                                static parameter =>
                                    new KeyValuePair<string, string?>(
                                        parameter.Key,
                                        parameter.Value)));
                    }

                    /// <summary>
                    /// Builds a query string containing a single parameter.
                    /// </summary>
                    public static string Build(
                        string key,
                        string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(key);

                        return Build(
                        [
                            new KeyValuePair<string, string?>(
                                key.Trim(),
                                value)
                        ]);
                    }

                    /// <summary>
                    /// Converts a query string into a canonical representation.
                    /// Keys are ordered alphabetically and duplicate keys are preserved.
                    /// </summary>
                    public static string Normalize(
                        string? queryString)
                    {
                        var parameters = ParsePairs(queryString)
                            .OrderBy(
                                static parameter => parameter.Key,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                static parameter => parameter.Value,
                                StringComparer.Ordinal)
                            .ToList();

                        return Build(parameters);
                    }

                    /// <summary>
                    /// Determines whether two query strings contain equivalent parameters.
                    /// Parameter order does not matter.
                    /// </summary>
                    public static bool AreEquivalent(
                        string? first,
                        string? second)
                    {
                        return string.Equals(
                            Normalize(first),
                            Normalize(second),
                            StringComparison.Ordinal);
                    }

                    /// <summary>
                    /// Gets the number of query parameters.
                    /// </summary>
                    public static int Count(string? queryString)
                    {
                        return ParsePairs(queryString).Count;
                    }

                    /// <summary>
                    /// Encodes a query-string component.
                    /// </summary>
                    public static string Encode(string? value)
                    {
                        return Uri.EscapeDataString(value ?? string.Empty);
                    }

                    /// <summary>
                    /// Decodes a query-string component.
                    /// </summary>
                    public static string Decode(string? value)
                    {
                        if (string.IsNullOrEmpty(value))
                            return string.Empty;

                        return Uri.UnescapeDataString(value.Replace(
                            "+",
                            " ",
                            StringComparison.Ordinal));
                    }

                    /// <summary>
                    /// Converts a query string into a URL-ready representation.
                    /// </summary>
                    public static string ToQueryString(
                        string? queryString)
                    {
                        var normalized = queryString?.Trim() ?? string.Empty;

                        if (normalized.StartsWith('?'))
                            normalized = normalized[1..];

                        return string.IsNullOrWhiteSpace(normalized)
                            ? string.Empty
                            : "?" + normalized;
                    }
                }
                
                """
            },
           

           
            // ==========================================
            // HELPERS - ENUMS
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Enums", "EnumHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Enums;

                using System;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.Globalization;
                using System.Linq;
                using System.Reflection;

                /// <summary>
                /// Provides generic, framework-independent helpers for working with enum types.
                /// </summary>
                public static class EnumHelper
                {
                    public static IReadOnlyList<TEnum> GetValues<TEnum>()
                        where TEnum : struct, Enum
                    {
                        return Enum.GetValues<TEnum>().ToArray();
                    }

                    public static IReadOnlyList<string> GetNames<TEnum>()
                        where TEnum : struct, Enum
                    {
                        return Enum.GetNames<TEnum>().ToArray();
                    }

                    public static bool IsDefined<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Enum.IsDefined(value);
                    }

                    public static bool TryParse<TEnum>(string? value, out TEnum result, bool ignoreCase = true)
                        where TEnum : struct, Enum
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            result = default;
                            return false;
                        }

                        return Enum.TryParse(value.Trim(), ignoreCase, out result);
                    }

                    public static TEnum Parse<TEnum>(string value, bool ignoreCase = true)
                        where TEnum : struct, Enum
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);

                        if (!Enum.TryParse(value.Trim(), ignoreCase, out TEnum result))
                        {
                            throw new ArgumentException(
                                $"'{value}' is not a valid value of enum '{typeof(TEnum).Name}'.",
                                nameof(value));
                        }

                        return result;
                    }

                    public static bool TryParseDefined<TEnum>(string? value, out TEnum result, bool ignoreCase = true)
                        where TEnum : struct, Enum
                    {
                        if (!TryParse(value, out result, ignoreCase))
                        {
                            return false;
                        }

                        return IsDefined(result);
                    }

                    public static string GetName<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Enum.GetName(value) ?? string.Empty;
                    }

                    public static long GetNumericValue<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    }

                    public static ulong GetUnsignedNumericValue<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                    }

                    public static bool TryFromNumericValue<TEnum>(long value, out TEnum result)
                        where TEnum : struct, Enum
                    {
                        try
                        {
                            result = (TEnum)Enum.ToObject(typeof(TEnum), value);
                            return true;
                        }
                        catch
                        {
                            result = default;
                            return false;
                        }
                    }

                    public static string GetDescription<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        var name = GetName(value);
                        if (string.IsNullOrWhiteSpace(name))
                            return string.Empty;

                        var field = typeof(TEnum).GetField(name);
                        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();

                        return string.IsNullOrWhiteSpace(attribute?.Description)
                            ? name
                            : attribute.Description;
                    }

                    public static string? GetDescriptionOrNull<TEnum>(TEnum value)
                        where TEnum : struct, Enum
                    {
                        var name = GetName(value);
                        if (string.IsNullOrWhiteSpace(name))
                            return null;

                        var field = typeof(TEnum).GetField(name);
                        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    }

                    public static bool TryFromDescription<TEnum>(string? description, out TEnum result, bool ignoreCase = true)
                        where TEnum : struct, Enum
                    {
                        result = default;

                        if (string.IsNullOrWhiteSpace(description))
                            return false;

                        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                        foreach (var value in GetValues<TEnum>())
                        {
                            var currentDescription = GetDescription(value);

                            if (string.Equals(currentDescription, description.Trim(), comparison))
                            {
                                result = value;
                                return true;
                            }
                        }

                        return false;
                    }

                    public static IReadOnlyList<(TEnum Value, string Name, string Description)> GetDescriptions<TEnum>()
                        where TEnum : struct, Enum
                    {
                        return GetValues<TEnum>()
                            .Select(value => (
                                Value: value,
                                Name: GetName(value),
                                Description: GetDescription(value)))
                            .ToArray();
                    }
                }
                """
            },

            // ==========================================
            // HELPERS - REFLECTION
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Reflection", "ReflectionHelper.cs"),
                """
                                
                using System.Reflection;

                namespace Project.BuildingBlocks.Shared.Helpers.Reflection;

                /// <summary>
                /// Provides lightweight, framework-independent helpers for working with
                /// .NET reflection metadata.
                /// </summary>
                public static class ReflectionHelper
                {
                    /// <summary>
                    /// Determines whether the specified type implements the given interface.
                    /// </summary>
                    public static bool ImplementsInterface(
                        Type type,
                        Type interfaceType)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentNullException.ThrowIfNull(interfaceType);

                        if (!interfaceType.IsInterface)
                            throw new ArgumentException(
                                "The specified type must be an interface.",
                                nameof(interfaceType));

                        return interfaceType.IsAssignableFrom(type);
                    }

                    /// <summary>
                    /// Determines whether the specified type implements TInterface.
                    /// </summary>
                    public static bool ImplementsInterface<TInterface>(
                        Type type)
                    {
                        return ImplementsInterface(
                            type,
                            typeof(TInterface));
                    }

                    /// <summary>
                    /// Determines whether the specified type inherits from the given base type.
                    /// </summary>
                    public static bool InheritsFrom(
                        Type type,
                        Type baseType)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentNullException.ThrowIfNull(baseType);

                        if (type == baseType)
                            return false;

                        return baseType.IsAssignableFrom(type);
                    }

                    /// <summary>
                    /// Determines whether the specified type inherits from TBaseType.
                    /// </summary>
                    public static bool InheritsFrom<TBaseType>(
                        Type type)
                    {
                        return InheritsFrom(
                            type,
                            typeof(TBaseType));
                    }

                    /// <summary>
                    /// Gets all interfaces implemented directly or indirectly by a type.
                    /// </summary>
                    public static IReadOnlyList<Type> GetInterfaces(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type
                            .GetInterfaces()
                            .OrderBy(static x => x.FullName)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets the interfaces implemented by the specified type
                    /// that match the supplied generic type definition.
                    /// </summary>
                    public static IReadOnlyList<Type> GetGenericInterfaces(
                        Type type,
                        Type genericTypeDefinition)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentNullException.ThrowIfNull(genericTypeDefinition);

                        if (!genericTypeDefinition.IsGenericTypeDefinition)
                            throw new ArgumentException(
                                "The specified type must be a generic type definition.",
                                nameof(genericTypeDefinition));

                        return type
                            .GetInterfaces()
                            .Where(interfaceType =>
                                interfaceType.IsGenericType &&
                                interfaceType.GetGenericTypeDefinition() ==
                                genericTypeDefinition)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets the generic interface implemented by the specified type.
                    /// </summary>
                    public static Type? GetGenericInterface(
                        Type type,
                        Type genericTypeDefinition)
                    {
                        return GetGenericInterfaces(
                                type,
                                genericTypeDefinition)
                            .FirstOrDefault();
                    }

                    /// <summary>
                    /// Gets all properties declared by the specified type.
                    /// </summary>
                    public static IReadOnlyList<PropertyInfo> GetProperties(
                        Type type,
                        bool includeInherited = true)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        var flags =
                            BindingFlags.Instance |
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic;

                        if (!includeInherited)
                            flags |= BindingFlags.DeclaredOnly;

                        return type
                            .GetProperties(flags)
                            .OrderBy(static property => property.MetadataToken)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets a property by name.
                    /// </summary>
                    public static PropertyInfo? GetProperty(
                        Type type,
                        string propertyName,
                        bool ignoreCase = false)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

                        var comparison = ignoreCase
                            ? StringComparison.OrdinalIgnoreCase
                            : StringComparison.Ordinal;

                        return GetProperties(type)
                            .FirstOrDefault(property =>
                                string.Equals(
                                    property.Name,
                                    propertyName.Trim(),
                                    comparison));
                    }

                    /// <summary>
                    /// Determines whether a property exists on the specified type.
                    /// </summary>
                    public static bool HasProperty(
                        Type type,
                        string propertyName,
                        bool ignoreCase = false)
                    {
                        return GetProperty(
                            type,
                            propertyName,
                            ignoreCase) is not null;
                    }

                    /// <summary>
                    /// Gets all fields declared by the specified type.
                    /// </summary>
                    public static IReadOnlyList<FieldInfo> GetFields(
                        Type type,
                        bool includeInherited = true)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        var flags =
                            BindingFlags.Instance |
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic;

                        if (!includeInherited)
                            flags |= BindingFlags.DeclaredOnly;

                        return type
                            .GetFields(flags)
                            .OrderBy(static field => field.MetadataToken)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets all methods declared by the specified type.
                    /// </summary>
                    public static IReadOnlyList<MethodInfo> GetMethods(
                        Type type,
                        bool includeInherited = true)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        var flags =
                            BindingFlags.Instance |
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic;

                        if (!includeInherited)
                            flags |= BindingFlags.DeclaredOnly;

                        return type
                            .GetMethods(flags)
                            .OrderBy(static method => method.MetadataToken)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets constructors declared by the specified type.
                    /// </summary>
                    public static IReadOnlyList<ConstructorInfo> GetConstructors(
                        Type type,
                        bool includeNonPublic = true)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        var flags =
                            BindingFlags.Instance |
                            BindingFlags.Public;

                        if (includeNonPublic)
                            flags |= BindingFlags.NonPublic;

                        return type
                            .GetConstructors(flags)
                            .OrderBy(static constructor => constructor.MetadataToken)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets the first constructor matching the supplied parameter types.
                    /// </summary>
                    public static ConstructorInfo? GetConstructor(
                        Type type,
                        params Type[] parameterTypes)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentNullException.ThrowIfNull(parameterTypes);

                        return type.GetConstructor(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic,
                            binder: null,
                            types: parameterTypes,
                            modifiers: null);
                    }

                    /// <summary>
                    /// Gets a custom attribute from a type.
                    /// </summary>
                    public static TAttribute? GetAttribute<TAttribute>(
                        Type type,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.GetCustomAttribute<TAttribute>(
                            inherit);
                    }

                    /// <summary>
                    /// Gets all custom attributes of the specified type from a type.
                    /// </summary>
                    public static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(
                        Type type,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type
                            .GetCustomAttributes<TAttribute>(inherit)
                            .ToArray();
                    }

                    /// <summary>
                    /// Determines whether a type has the specified attribute.
                    /// </summary>
                    public static bool HasAttribute<TAttribute>(
                        Type type,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsDefined(
                            typeof(TAttribute),
                            inherit);
                    }

                    /// <summary>
                    /// Gets a custom attribute from a member.
                    /// </summary>
                    public static TAttribute? GetAttribute<TAttribute>(
                        MemberInfo member,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(member);

                        return member.GetCustomAttribute<TAttribute>(
                            inherit);
                    }

                    /// <summary>
                    /// Gets all custom attributes of the specified type from a member.
                    /// </summary>
                    public static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(
                        MemberInfo member,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(member);

                        return member
                            .GetCustomAttributes<TAttribute>(inherit)
                            .ToArray();
                    }

                    /// <summary>
                    /// Determines whether a member has the specified attribute.
                    /// </summary>
                    public static bool HasAttribute<TAttribute>(
                        MemberInfo member,
                        bool inherit = true)
                        where TAttribute : Attribute
                    {
                        ArgumentNullException.ThrowIfNull(member);

                        return member.IsDefined(
                            typeof(TAttribute),
                            inherit);
                    }

                    /// <summary>
                    /// Gets the underlying type of a nullable value type.
                    /// </summary>
                    public static Type GetUnderlyingType(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return Nullable.GetUnderlyingType(type)
                               ?? type;
                    }

                    /// <summary>
                    /// Determines whether a type is nullable at the type-system level.
                    /// </summary>
                    public static bool IsNullableValueType(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return Nullable.GetUnderlyingType(type) is not null;
                    }

                    /// <summary>
                    /// Determines whether a type is a generic type definition.
                    /// </summary>
                    public static bool IsGenericTypeDefinition(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsGenericTypeDefinition;
                    }

                    /// <summary>
                    /// Determines whether a type is a constructed generic type.
                    /// </summary>
                    public static bool IsConstructedGenericType(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsGenericType &&
                               !type.IsGenericTypeDefinition;
                    }

                    /// <summary>
                    /// Gets the generic arguments of a type.
                    /// </summary>
                    public static IReadOnlyList<Type> GetGenericArguments(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type
                            .GetGenericArguments()
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets the element type of an array, enumerable, or generic collection
                    /// when it can be determined.
                    /// </summary>
                    public static Type? GetElementType(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        if (type.IsArray)
                            return type.GetElementType();

                        if (type == typeof(string))
                            return null;

                        if (type.IsGenericType)
                        {
                            var genericDefinition =
                                type.GetGenericTypeDefinition();

                            if (genericDefinition == typeof(IEnumerable<>) ||
                                genericDefinition == typeof(ICollection<>) ||
                                genericDefinition == typeof(IList<>) ||
                                genericDefinition == typeof(IReadOnlyCollection<>) ||
                                genericDefinition == typeof(IReadOnlyList<>))
                            {
                                return type.GetGenericArguments()[0];
                            }
                        }

                        var enumerableInterface =
                            type.GetInterfaces()
                                .FirstOrDefault(interfaceType =>
                                    interfaceType.IsGenericType &&
                                    interfaceType.GetGenericTypeDefinition() ==
                                    typeof(IEnumerable<>));

                        return enumerableInterface?.GetGenericArguments()[0];
                    }

                    /// <summary>
                    /// Determines whether a type is an enum.
                    /// </summary>
                    public static bool IsEnum(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsEnum;
                    }

                    /// <summary>
                    /// Determines whether a type is a class.
                    /// </summary>
                    public static bool IsClass(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsClass;
                    }

                    /// <summary>
                    /// Determines whether a type is an interface.
                    /// </summary>
                    public static bool IsInterface(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsInterface;
                    }

                    /// <summary>
                    /// Determines whether a type is abstract.
                    /// </summary>
                    public static bool IsAbstract(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsAbstract;
                    }

                    /// <summary>
                    /// Determines whether a type is sealed.
                    /// </summary>
                    public static bool IsSealed(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsSealed;
                    }

                    /// <summary>
                    /// Determines whether a type is static.
                    /// </summary>
                    public static bool IsStatic(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.IsAbstract && type.IsSealed;
                    }

                    /// <summary>
                    /// Gets the type name without its generic arity suffix.
                    /// </summary>
                    public static string GetSimpleName(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        var name = type.Name;
                        var genericIndex = name.IndexOf('`');

                        return genericIndex >= 0
                            ? name[..genericIndex]
                            : name;
                    }

                    /// <summary>
                    /// Gets the full name of a type when available.
                    /// </summary>
                    public static string GetFullName(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.FullName ?? type.Name;
                    }

                    /// <summary>
                    /// Gets the namespace of a type.
                    /// </summary>
                    public static string GetNamespace(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.Namespace ?? string.Empty;
                    }

                    /// <summary>
                    /// Gets the assembly containing the specified type.
                    /// </summary>
                    public static Assembly GetAssembly(
                        Type type)
                    {
                        ArgumentNullException.ThrowIfNull(type);

                        return type.Assembly;
                    }

                    /// <summary>
                    /// Gets all loadable types from an assembly.
                    /// </summary>
                    public static IReadOnlyList<Type> GetTypes(
                        Assembly assembly)
                    {
                        ArgumentNullException.ThrowIfNull(assembly);

                        try
                        {
                            return assembly
                                .GetTypes()
                                .ToArray();
                        }
                        catch (ReflectionTypeLoadException exception)
                        {
                            return exception.Types
                                .Where(static type => type is not null)
                                .Cast<Type>()
                                .ToArray();
                        }
                    }

                    /// <summary>
                    /// Gets types from an assembly that satisfy the specified predicate.
                    /// </summary>
                    public static IReadOnlyList<Type> GetTypes(
                        Assembly assembly,
                        Func<Type, bool> predicate)
                    {
                        ArgumentNullException.ThrowIfNull(assembly);
                        ArgumentNullException.ThrowIfNull(predicate);

                        return GetTypes(assembly)
                            .Where(predicate)
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets concrete, non-abstract classes from an assembly.
                    /// </summary>
                    public static IReadOnlyList<Type> GetConcreteClasses(
                        Assembly assembly)
                    {
                        return GetTypes(
                            assembly,
                            static type =>
                                type.IsClass &&
                                !type.IsAbstract);
                    }

                    /// <summary>
                    /// Gets types implementing the specified interface from an assembly.
                    /// </summary>
                    public static IReadOnlyList<Type> GetImplementations(
                        Assembly assembly,
                        Type interfaceType)
                    {
                        ArgumentNullException.ThrowIfNull(interfaceType);

                        if (!interfaceType.IsInterface)
                            throw new ArgumentException(
                                "The specified type must be an interface.",
                                nameof(interfaceType));

                        return GetTypes(
                                assembly,
                                type =>
                                    type.IsClass &&
                                    !type.IsAbstract &&
                                    interfaceType.IsAssignableFrom(type))
                            .ToArray();
                    }

                    /// <summary>
                    /// Gets types deriving from TBaseType from an assembly.
                    /// </summary>
                    public static IReadOnlyList<Type> GetDerivedTypes<TBaseType>(
                        Assembly assembly)
                    {
                        return GetTypes(
                            assembly,
                            type =>
                                type.IsClass &&
                                !type.IsAbstract &&
                                typeof(TBaseType).IsAssignableFrom(type));
                    }

                    /// <summary>
                    /// Gets a static property value from an object type.
                    /// </summary>
                    public static object? GetStaticPropertyValue(
                        Type type,
                        string propertyName)
                    {
                        ArgumentNullException.ThrowIfNull(type);
                        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

                        var property = type.GetProperty(
                            propertyName.Trim(),
                            BindingFlags.Static |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                        if (property is null)
                            return null;

                        return property.GetValue(null);
                    }

                    /// <summary>
                    /// Attempts to read a property value from an object.
                    /// </summary>
                    public static bool TryGetPropertyValue(
                        object instance,
                        string propertyName,
                        out object? value)
                    {
                        ArgumentNullException.ThrowIfNull(instance);
                        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

                        var property = GetProperty(
                            instance.GetType(),
                            propertyName);

                        if (property is null ||
                            !property.CanRead)
                        {
                            value = null;
                            return false;
                        }

                        value = property.GetValue(instance);
                        return true;
                    }

                    /// <summary>
                    /// Determines whether the specified member is static.
                    /// </summary>
                    public static bool IsStatic(
                        MemberInfo member)
                    {
                        ArgumentNullException.ThrowIfNull(member);

                        return member switch
                        {
                            MethodBase method => method.IsStatic,
                            FieldInfo field => field.IsStatic,
                            PropertyInfo property =>
                                (property.GetMethod?.IsStatic ?? false) ||
                                (property.SetMethod?.IsStatic ?? false),

                            EventInfo @event =>
                                (@event.AddMethod?.IsStatic ?? false) ||
                                (@event.RemoveMethod?.IsStatic ?? false),

                            _ => false
                        };
                    }
                }
                
                """
            },
                

            // ==========================================
            // HELPERS - SERIALIZATION
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Serialization", "JsonHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Serialization;

                using System;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                public static class JsonHelper
                {
                    private static readonly JsonSerializerOptions DefaultOptions =
                        new(JsonSerializerDefaults.General);

                    public static string Serialize<T>(
                        T value,
                        JsonSerializerOptions? options = null)
                    {
                        return JsonSerializer.Serialize(
                            value,
                            options ?? DefaultOptions);
                    }

                    public static string SerializeIndented<T>(
                        T value,
                        JsonSerializerOptions? options = null)
                    {
                        var serializerOptions = CreateIndentedOptions(options);

                        return JsonSerializer.Serialize(
                            value,
                            serializerOptions);
                    }

                    public static bool TrySerialize<T>(
                        T value,
                        out string json,
                        JsonSerializerOptions? options = null)
                    {
                        try
                        {
                            json = Serialize(value, options);
                            return true;
                        }
                        catch (JsonException)
                        {
                            json = string.Empty;
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            json = string.Empty;
                            return false;
                        }
                    }

                    public static byte[] SerializeToUtf8Bytes<T>(
                        T value,
                        JsonSerializerOptions? options = null)
                    {
                        return JsonSerializer.SerializeToUtf8Bytes(
                            value,
                            options ?? DefaultOptions);
                    }

                    public static T? Deserialize<T>(
                        string json,
                        JsonSerializerOptions? options = null)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(json);

                        return JsonSerializer.Deserialize<T>(
                            json,
                            options ?? DefaultOptions);
                    }

                    public static object? Deserialize(
                        string json,
                        Type returnType,
                        JsonSerializerOptions? options = null)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(json);
                        ArgumentNullException.ThrowIfNull(returnType);

                        return JsonSerializer.Deserialize(
                            json,
                            returnType,
                            options ?? DefaultOptions);
                    }

                    public static bool TryDeserialize<T>(
                        string? json,
                        out T? value,
                        JsonSerializerOptions? options = null)
                    {
                        value = default;

                        if (string.IsNullOrWhiteSpace(json))
                            return false;

                        try
                        {
                            value = Deserialize<T>(
                                json,
                                options);

                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static T? Deserialize<T>(
                        ReadOnlySpan<byte> utf8Json,
                        JsonSerializerOptions? options = null)
                    {
                        if (utf8Json.IsEmpty)
                            throw new ArgumentException(
                                "JSON data cannot be empty.",
                                nameof(utf8Json));

                        return JsonSerializer.Deserialize<T>(
                            utf8Json,
                            options ?? DefaultOptions);
                    }

                    public static bool TryDeserialize<T>(
                        ReadOnlySpan<byte> utf8Json,
                        out T? value,
                        JsonSerializerOptions? options = null)
                    {
                        value = default;

                        if (utf8Json.IsEmpty)
                            return false;

                        try
                        {
                            value = Deserialize<T>(
                                utf8Json,
                                options);

                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static bool IsValid(
                        string? json)
                    {
                        if (string.IsNullOrWhiteSpace(json))
                            return false;

                        try
                        {
                            using var document = JsonDocument.Parse(json);
                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                    }

                    public static JsonElement ToJsonElement<T>(
                        T value,
                        JsonSerializerOptions? options = null)
                    {
                        return JsonSerializer.SerializeToElement(
                            value,
                            options ?? DefaultOptions);
                    }

                    public static T? FromJsonElement<T>(
                        JsonElement element,
                        JsonSerializerOptions? options = null)
                    {
                        return element.Deserialize<T>(
                            options ?? DefaultOptions);
                    }

                    public static JsonSerializerOptions CreateIndentedOptions(
                        JsonSerializerOptions? options = null)
                    {
                        var result = options is null
                            ? new JsonSerializerOptions(DefaultOptions)
                            : new JsonSerializerOptions(options);

                        result.WriteIndented = true;

                        return result;
                    }

                    public static JsonSerializerOptions CreateDefaultOptions()
                    {
                        return new JsonSerializerOptions(DefaultOptions);
                    }

                    public static JsonSerializerOptions CreateCamelCaseOptions(
                        bool writeIndented = false)
                    {
                        return new JsonSerializerOptions(DefaultOptions)
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = writeIndented
                        };
                    }

                    public static JsonSerializerOptions CreateStringEnumOptions(
                        bool camelCase = false,
                        bool writeIndented = false)
                    {
                        var options = new JsonSerializerOptions(DefaultOptions)
                        {
                            WriteIndented = writeIndented
                        };

                        if (camelCase)
                        {
                            options.PropertyNamingPolicy =
                                JsonNamingPolicy.CamelCase;

                            options.DictionaryKeyPolicy =
                                JsonNamingPolicy.CamelCase;
                        }

                        options.Converters.Add(
                            new JsonStringEnumConverter(
                                camelCase
                                    ? JsonNamingPolicy.CamelCase
                                    : null));

                        return options;
                    }

                    public static JsonSerializerOptions CreateApplicationOptions(
                        bool writeIndented = false)
                    {
                        return CreateStringEnumOptions(
                            camelCase: true,
                            writeIndented: writeIndented);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Serialization", "SerializationHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Serialization;

                using System;
                using System.Text;
                using System.Text.Json;

                public static class SerializationHelper
                {
                    public static byte[] Serialize<T>(
                        T value,
                        Encoding? encoding = null)
                    {
                        var json = JsonHelper.Serialize(value);
                        return (encoding ?? Encoding.UTF8).GetBytes(json);
                    }

                    public static string SerializeToString<T>(T value)
                    {
                        return JsonHelper.Serialize(value);
                    }

                    public static string SerializeIndented<T>(T value)
                    {
                        return JsonHelper.SerializeIndented(value);
                    }

                    public static T? Deserialize<T>(
                        byte[] data,
                        Encoding? encoding = null)
                    {
                        ArgumentNullException.ThrowIfNull(data);

                        if (data.Length == 0)
                            throw new ArgumentException(
                                "Serialized data cannot be empty.",
                                nameof(data));

                        var selectedEncoding = encoding ?? Encoding.UTF8;
                        var json = selectedEncoding.GetString(data);

                        return JsonHelper.Deserialize<T>(json);
                    }

                    public static T? DeserializeFromString<T>(
                        string serializedData)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(serializedData);

                        return JsonHelper.Deserialize<T>(serializedData);
                    }

                    public static bool TrySerialize<T>(
                        T value,
                        out byte[] data,
                        Encoding? encoding = null)
                    {
                        data = Array.Empty<byte>();

                        try
                        {
                            var json = JsonHelper.Serialize(value);
                            data = (encoding ?? Encoding.UTF8).GetBytes(json);

                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static bool TrySerializeToString<T>(
                        T value,
                        out string serializedData)
                    {
                        serializedData = string.Empty;

                        try
                        {
                            serializedData = JsonHelper.Serialize(value);
                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static bool TryDeserialize<T>(
                        byte[]? data,
                        out T? value,
                        Encoding? encoding = null)
                    {
                        value = default;

                        if (data is null || data.Length == 0)
                            return false;

                        try
                        {
                            var selectedEncoding = encoding ?? Encoding.UTF8;
                            var json = selectedEncoding.GetString(data);

                            value = JsonHelper.Deserialize<T>(json);

                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static bool TryDeserializeFromString<T>(
                        string? serializedData,
                        out T? value)
                    {
                        value = default;

                        if (string.IsNullOrWhiteSpace(serializedData))
                            return false;

                        try
                        {
                            value = JsonHelper.Deserialize<T>(serializedData);
                            return true;
                        }
                        catch (JsonException)
                        {
                            return false;
                        }
                        catch (NotSupportedException)
                        {
                            return false;
                        }
                    }

                    public static T? DeepClone<T>(T value)
                    {
                        var json = JsonHelper.Serialize(value);
                        return JsonHelper.Deserialize<T>(json);
                    }

                    public static bool IsValid(string? serializedData)
                    {
                        return JsonHelper.IsValid(serializedData);
                    }

                    public static byte[] ToUtf8Bytes<T>(T value)
                    {
                        return JsonHelper.SerializeToUtf8Bytes(value);
                    }

                    public static T? FromUtf8Bytes<T>(
                        ReadOnlySpan<byte> data)
                    {
                        return JsonHelper.Deserialize<T>(data);
                    }

                    public static bool TryFromUtf8Bytes<T>(
                        ReadOnlySpan<byte> data,
                        out T? value)
                    {
                        return JsonHelper.TryDeserialize(
                            data,
                            out value);
                    }

                    public static byte[] ToUtf8Bytes(string value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return Encoding.UTF8.GetBytes(value);
                    }

                    public static string FromUtf8Bytes(
                        ReadOnlySpan<byte> data)
                    {
                        return Encoding.UTF8.GetString(data);
                    }

                    public static byte[] GetBytes(
                        string value,
                        Encoding? encoding = null)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return (encoding ?? Encoding.UTF8).GetBytes(value);
                    }

                    public static string GetString(
                        ReadOnlySpan<byte> data,
                        Encoding? encoding = null)
                    {
                        return (encoding ?? Encoding.UTF8).GetString(data);
                    }
                }
                """
            },

            // ==========================================
            // HELPERS - IDENTIFIERS
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Identifiers", "GuidHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Identifiers;

                using System;

                public static class GuidHelper
                {
                    public static Guid Empty => Guid.Empty;

                    public static Guid New()
                    {
                        return Guid.NewGuid();
                    }

                    public static Guid NewNonEmpty()
                    {
                        Guid value;
                        do
                        {
                            value = Guid.NewGuid();
                        }
                        while (value == Guid.Empty);

                        return value;
                    }

                    public static bool IsEmpty(Guid value)
                    {
                        return value == Guid.Empty;
                    }

                    public static bool IsNotEmpty(Guid value)
                    {
                        return value != Guid.Empty;
                    }

                    public static Guid Parse(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                        return Guid.Parse(value.Trim());
                    }

                    public static bool TryParse(string? value, out Guid result)
                    {
                        return Guid.TryParse(value?.Trim(), out result);
                    }

                    public static Guid ParseExact(string value, string format)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                        ArgumentException.ThrowIfNullOrWhiteSpace(format);
                        return Guid.ParseExact(value.Trim(), format.Trim());
                    }

                    public static bool TryParseExact(string? value, string? format, out Guid result)
                    {
                        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(format))
                        {
                            result = Guid.Empty;
                            return false;
                        }

                        return Guid.TryParseExact(value.Trim(), format.Trim(), out result);
                    }

                    public static string ToString(Guid value)
                    {
                        return value.ToString();
                    }

                    public static string ToStringN(Guid value)
                    {
                        return value.ToString("N");
                    }

                    public static string ToStringD(Guid value)
                    {
                        return value.ToString("D");
                    }

                    public static string ToStringB(Guid value)
                    {
                        return value.ToString("B");
                    }

                    public static string ToStringP(Guid value)
                    {
                        return value.ToString("P");
                    }

                    public static string ToStringX(Guid value)
                    {
                        return value.ToString("X");
                    }

                    public static string Format(Guid value, string format = "D")
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(format);
                        return value.ToString(format.Trim());
                    }

                    public static byte[] ToByteArray(Guid value)
                    {
                        return value.ToByteArray();
                    }

                    public static Guid FromByteArray(byte[] bytes)
                    {
                        ArgumentNullException.ThrowIfNull(bytes);

                        if (bytes.Length != 16)
                        {
                            throw new ArgumentException("A Guid requires exactly 16 bytes.", nameof(bytes));
                        }

                        return new Guid(bytes);
                    }

                    public static bool TryFromByteArray(byte[]? bytes, out Guid result)
                    {
                        if (bytes is null || bytes.Length != 16)
                        {
                            result = Guid.Empty;
                            return false;
                        }

                        result = new Guid(bytes);
                        return true;
                    }

                    public static bool IsValid(string? value)
                    {
                        return TryParse(value, out var result) && result != Guid.Empty;
                    }

                    public static Guid Ensure(Guid value)
                    {
                        return value == Guid.Empty ? New() : value;
                    }

                    public static Guid Ensure(Guid? value)
                    {
                        return !value.HasValue || value.Value == Guid.Empty ? New() : value.Value;
                    }

                    public static Guid ParseOrNew(string? value)
                    {
                        return IsValid(value) ? Guid.Parse(value!.Trim()) : New();
                    }

                    public static int GetVersion(Guid value)
                    {
                        if (value == Guid.Empty)
                            return 0;

                        var bytes = value.ToByteArray();
                        return (bytes[7] >> 4) & 0x0F;
                    }

                    public static int GetVariant(Guid value)
                    {
                        if (value == Guid.Empty)
                            return 0;

                        var bytes = value.ToByteArray();
                        var variantByte = bytes[8];

                        if ((variantByte & 0x80) == 0)
                            return 0;

                        if ((variantByte & 0xC0) == 0x80)
                            return 2;

                        if ((variantByte & 0xE0) == 0xC0)
                            return 6;

                        return 7;
                    }

                    public static bool IsRfcVariant(Guid value)
                    {
                        return GetVariant(value) == 2;
                    }

                    public static bool AreEqual(Guid first, Guid second)
                    {
                        return first == second;
                    }

                    public static int Compare(Guid first, Guid second)
                    {
                        return first.CompareTo(second);
                    }

                    public static Guid? TryParseNullable(string? value)
                    {
                        return TryParse(value, out var result) ? result : null;
                    }

                    public static string? ToNullableString(Guid? value, string format = "D")
                    {
                        if (!value.HasValue)
                            return null;

                        return Format(value.Value, format);
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Identifiers", "IdentifierHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Identifiers;

                using System;
                using System.Linq;
                using System.Security.Cryptography;

                public static class IdentifierHelper
                {
                    private const string DefaultAlphabet =
                        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

                    public static bool HasValue(Guid? value)
                    {
                        return value.HasValue &&
                               value.Value != Guid.Empty;
                    }

                    public static bool HasValue(Guid value)
                    {
                        return value != Guid.Empty;
                    }

                    public static bool HasValue(string? value)
                    {
                        return !string.IsNullOrWhiteSpace(value);
                    }

                    public static string Normalize(string? value)
                    {
                        return value?.Trim() ?? string.Empty;
                    }

                    public static string NormalizeInsensitive(string? value)
                    {
                        return Normalize(value).ToUpperInvariant();
                    }

                    public static bool AreEqual(
                        string? first,
                        string? second)
                    {
                        return string.Equals(
                            Normalize(first),
                            Normalize(second),
                            StringComparison.Ordinal);
                    }

                    public static bool AreEqualInsensitive(
                        string? first,
                        string? second)
                    {
                        return string.Equals(
                            Normalize(first),
                            Normalize(second),
                            StringComparison.OrdinalIgnoreCase);
                    }

                    public static bool IsGuid(string? value)
                    {
                        return GuidHelper.IsValid(value);
                    }

                    public static bool TryGetGuid(
                        string? value,
                        out Guid result)
                    {
                        return GuidHelper.TryParse(
                            value,
                            out result)
                            && result != Guid.Empty;
                    }

                    public static Guid GetGuidOrNew(string? value)
                    {
                        return TryGetGuid(
                            value,
                            out var result)
                            ? result
                            : GuidHelper.New();
                    }

                    public static string ToCompact(
                        Guid value)
                    {
                        if (value == Guid.Empty)
                            throw new ArgumentException(
                                "Identifier cannot be empty.",
                                nameof(value));

                        return Convert.ToBase64String(
                                value.ToByteArray())
                            .TrimEnd('=')
                            .Replace('+', '-')
                            .Replace('/', '_');
                    }

                    public static bool TryFromCompact(
                        string? value,
                        out Guid result)
                    {
                        result = Guid.Empty;

                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        var normalized = value
                            .Trim()
                            .Replace('-', '+')
                            .Replace('_', '/');

                        var padding = normalized.Length % 4;

                        if (padding != 0)
                            normalized = normalized.PadRight(
                                normalized.Length + (4 - padding),
                                '=');

                        try
                        {
                            var bytes = Convert.FromBase64String(normalized);

                            return GuidHelper.TryFromByteArray(
                                bytes,
                                out result);
                        }
                        catch (FormatException)
                        {
                            return false;
                        }
                    }

                    public static string GenerateReference(
                        int length = 12,
                        string? alphabet = null)
                    {
                        if (length <= 0)
                            throw new ArgumentOutOfRangeException(
                                nameof(length),
                                length,
                                "Identifier length must be greater than zero.");

                        var selectedAlphabet =
                            string.IsNullOrEmpty(alphabet)
                                ? DefaultAlphabet
                                : alphabet;

                        ValidateAlphabet(selectedAlphabet);

                        var result = new char[length];

                        for (var index = 0; index < result.Length; index++)
                        {
                            result[index] =
                                selectedAlphabet[
                                    RandomNumberGenerator.GetInt32(
                                        selectedAlphabet.Length)];
                        }

                        return new string(result);
                    }

                    public static string GenerateNumericReference(
                        int length = 10)
                    {
                        return GenerateReference(
                            length,
                            "0123456789");
                    }

                    public static string GenerateHumanReadableReference(
                        int length = 10)
                    {
                        return GenerateReference(
                            length,
                            "23456789ABCDEFGHJKLMNPQRSTUVWXYZ");
                    }

                    public static bool IsSafeAsciiIdentifier(
                        string? value,
                        int minimumLength = 1,
                        int maximumLength = 128)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        if (minimumLength < 0)
                            throw new ArgumentOutOfRangeException(
                                nameof(minimumLength));

                        if (maximumLength < minimumLength)
                            throw new ArgumentOutOfRangeException(
                                nameof(maximumLength));

                        var normalized = value.Trim();

                        if (normalized.Length < minimumLength ||
                            normalized.Length > maximumLength)
                        {
                            return false;
                        }

                        foreach (var character in normalized)
                        {
                            if (character is >= 'A' and <= 'Z')
                                continue;

                            if (character is >= 'a' and <= 'z')
                                continue;

                            if (character is >= '0' and <= '9')
                                continue;

                            if (character is '_' or '-')
                                continue;

                            return false;
                        }

                        return true;
                    }

                    public static bool IsFromAlphabet(
                        string? value,
                        string alphabet)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(alphabet);

                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        foreach (var character in value)
                        {
                            if (alphabet.IndexOf(
                                    character,
                                    StringComparison.Ordinal) < 0)
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    public static string Require(
                        string? value,
                        string parameterName = "identifier")
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            throw new ArgumentException(
                                "Identifier cannot be null or whitespace.",
                                parameterName);
                        }

                        return value.Trim();
                    }

                    public static Guid Require(
                        Guid value,
                        string parameterName = "identifier")
                    {
                        if (value == Guid.Empty)
                        {
                            throw new ArgumentException(
                                "Identifier cannot be empty.",
                                parameterName);
                        }

                        return value;
                    }

                    public static Guid Ensure(
                        Guid value)
                    {
                        return value == Guid.Empty
                            ? GuidHelper.New()
                            : value;
                    }

                    public static string Ensure(
                        string? value,
                        int referenceLength = 12)
                    {
                        return string.IsNullOrWhiteSpace(value)
                            ? GenerateReference(referenceLength)
                            : value.Trim();
                    }

                    public static string ToHex(
                        Guid value)
                    {
                        if (value == Guid.Empty)
                            throw new ArgumentException(
                                "Identifier cannot be empty.",
                                nameof(value));

                        return Convert.ToHexString(
                                value.ToByteArray())
                            .ToLowerInvariant();
                    }

                    public static bool TryFromHex(
                        string? value,
                        out Guid result)
                    {
                        result = Guid.Empty;

                        if (string.IsNullOrWhiteSpace(value))
                            return false;

                        var normalized = value.Trim();

                        if (normalized.Length != 32)
                            return false;

                        try
                        {
                            var bytes = Convert.FromHexString(normalized);

                            return GuidHelper.TryFromByteArray(
                                bytes,
                                out result);
                        }
                        catch (FormatException)
                        {
                            return false;
                        }
                    }

                    private static void ValidateAlphabet(
                        string alphabet)
                    {
                        if (string.IsNullOrEmpty(alphabet))
                        {
                            throw new ArgumentException(
                                "Alphabet cannot be empty.",
                                nameof(alphabet));
                        }

                        var distinctCharacters =
                            alphabet.Distinct().Count();

                        if (distinctCharacters != alphabet.Length)
                        {
                            throw new ArgumentException(
                                "Alphabet cannot contain duplicate characters.",
                                nameof(alphabet));
                        }
                    }
                }
                """
            },

            // ==========================================
            // HELPERS - MONEY & CURRENCY
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Money", "MoneyHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Money;

                using System;

                public static class MoneyHelper
                {
                    public static bool IsZero(decimal amount) => amount == 0m;
                    public static bool IsPositive(decimal amount) => amount > 0m;
                    public static bool IsNegative(decimal amount) => amount < 0m;
                    public static bool IsNonNegative(decimal amount) => amount >= 0m;
                    public static decimal Percentage(decimal amount, decimal percentage) => amount * percentage / 100m;

                    public static decimal ApplyDiscount(decimal amount, decimal discountPercentage)
                    {
                        ValidatePercentage(discountPercentage);
                        var discount = Percentage(amount, discountPercentage);
                        return amount - discount;
                    }

                    public static decimal ApplyTax(decimal amount, decimal taxPercentage)
                    {
                        ValidatePercentage(taxPercentage);
                        var tax = Percentage(amount, taxPercentage);
                        return amount + tax;
                    }

                    public static decimal CalculateDiscount(decimal originalAmount, decimal finalAmount)
                    {
                        if (originalAmount < 0) throw new ArgumentOutOfRangeException(nameof(originalAmount), originalAmount, "Original amount cannot be negative.");
                        if (finalAmount < 0) throw new ArgumentOutOfRangeException(nameof(finalAmount), finalAmount, "Final amount cannot be negative.");
                        if (finalAmount > originalAmount) throw new ArgumentException("Final amount cannot be greater than original amount.", nameof(finalAmount));
                        return originalAmount - finalAmount;
                    }

                    public static decimal CalculateDiscountPercentage(decimal originalAmount, decimal finalAmount)
                    {
                        if (originalAmount <= 0) throw new ArgumentOutOfRangeException(nameof(originalAmount), originalAmount, "Original amount must be greater than zero.");
                        var discount = CalculateDiscount(originalAmount, finalAmount);
                        return discount / originalAmount * 100m;
                    }

                    public static decimal CalculateTax(decimal amount, decimal taxPercentage)
                    {
                        ValidatePercentage(taxPercentage);
                        return Percentage(amount, taxPercentage);
                    }

                    public static decimal Add(decimal first, decimal second) => first + second;
                    public static decimal Subtract(decimal first, decimal second) => first - second;
                    public static decimal Multiply(decimal amount, decimal quantity) => amount * quantity;

                    public static decimal Total(decimal unitPrice, decimal quantity)
                    {
                        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");
                        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity cannot be negative.");
                        return unitPrice * quantity;
                    }

                    public static decimal Round(decimal amount, int decimals = 0, MidpointRounding rounding = MidpointRounding.ToEven)
                    {
                        if (decimals < 0) throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Decimal places cannot be negative.");
                        return decimal.Round(amount, decimals, rounding);
                    }

                    public static decimal RoundCurrency(decimal amount, int decimalPlaces = 0) => Round(amount, decimalPlaces, MidpointRounding.ToEven);

                    public static decimal CalculateNetAmount(decimal amount, decimal discountPercentage)
                    {
                        ValidateAmount(amount);
                        ValidatePercentage(discountPercentage);
                        return ApplyDiscount(amount, discountPercentage);
                    }

                    public static decimal CalculateFinalAmount(decimal amount, decimal discountPercentage, decimal taxPercentage)
                    {
                        ValidateAmount(amount);
                        ValidatePercentage(discountPercentage);
                        ValidatePercentage(taxPercentage);
                        var netAmount = ApplyDiscount(amount, discountPercentage);
                        return ApplyTax(netAmount, taxPercentage);
                    }

                    public static decimal PercentageOf(decimal part, decimal whole)
                    {
                        if (whole == 0) throw new DivideByZeroException("The whole amount cannot be zero.");
                        return part / whole * 100m;
                    }

                    public static decimal ZeroIfNegative(decimal amount) => amount < 0m ? 0m : amount;
                    public static decimal Abs(decimal amount) => decimal.Abs(amount);

                    private static void ValidateAmount(decimal amount)
                    {
                        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
                    }

                    private static void ValidatePercentage(decimal percentage)
                    {
                        if (percentage < 0m || percentage > 100m) throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "Percentage must be between 0 and 100.");
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Money", "CurrencyHelper.cs"),
                """
                namespace Shafiee.BuildingBlocks.Shared.Helpers.Money;

                using System;

                public static class CurrencyHelper
                {
                    public const string IranianRial = "IRR";
                    public const string IranianToman = "IRT";
                    public const string UnitedStatesDollar = "USD";
                    public const string Euro = "EUR";
                    public const string BritishPound = "GBP";
                    public const string UnitedArabEmiratesDirham = "AED";
                    public const string TurkishLira = "TRY";
                    public const string JapaneseYen = "JPY";
                    public const string ChineseYuan = "CNY";
                    public const string CanadianDollar = "CAD";
                    public const string AustralianDollar = "AUD";
                    public const string SwissFranc = "CHF";

                    public static bool IsSupported(string? currencyCode)
                    {
                        if (string.IsNullOrWhiteSpace(currencyCode))
                            return false;

                        return currencyCode.Trim().ToUpperInvariant() switch
                        {
                            IranianRial or
                            IranianToman or
                            UnitedStatesDollar or
                            Euro or
                            BritishPound or
                            UnitedArabEmiratesDirham or
                            TurkishLira or
                            JapaneseYen or
                            ChineseYuan or
                            CanadianDollar or
                            AustralianDollar or
                            SwissFranc => true,

                            _ => false
                        };
                    }

                    public static string NormalizeCode(string? currencyCode)
                    {
                        if (string.IsNullOrWhiteSpace(currencyCode))
                            return string.Empty;

                        return currencyCode.Trim().ToUpperInvariant();
                    }

                    public static string GetPersianName(string? currencyCode)
                    {
                        return NormalizeCode(currencyCode) switch
                        {
                            IranianRial => "ریال ایران",
                            IranianToman => "تومان",
                            UnitedStatesDollar => "دلار آمریکا",
                            Euro => "یورو",
                            BritishPound => "پوند انگلیس",
                            UnitedArabEmiratesDirham => "درهم امارات",
                            TurkishLira => "لیر ترکیه",
                            JapaneseYen => "ین ژاپن",
                            ChineseYuan => "یوان چین",
                            CanadianDollar => "دلار کانادا",
                            AustralianDollar => "دلار استرالیا",
                            SwissFranc => "فرانک سوئیس",
                            _ => string.Empty
                        };
                    }

                    public static string GetSymbol(string? currencyCode)
                    {
                        return NormalizeCode(currencyCode) switch
                        {
                            IranianRial => "﷼",
                            IranianToman => "تومان",
                            UnitedStatesDollar => "$",
                            Euro => "€",
                            BritishPound => "£",
                            UnitedArabEmiratesDirham => "د.إ",
                            TurkishLira => "₺",
                            JapaneseYen => "¥",
                            ChineseYuan => "¥",
                            CanadianDollar => "CA$",
                            AustralianDollar => "A$",
                            SwissFranc => "CHF",
                            _ => string.Empty
                        };
                    }

                    public static int? GetNumericCode(string? currencyCode)
                    {
                        return NormalizeCode(currencyCode) switch
                        {
                            IranianRial => 364,
                            IranianToman => null,
                            UnitedStatesDollar => 840,
                            Euro => 978,
                            BritishPound => 826,
                            UnitedArabEmiratesDirham => 784,
                            TurkishLira => 949,
                            JapaneseYen => 392,
                            ChineseYuan => 156,
                            CanadianDollar => 124,
                            AustralianDollar => 36,
                            SwissFranc => 756,
                            _ => null
                        };
                    }

                    public static int GetDecimalPlaces(string? currencyCode)
                    {
                        return NormalizeCode(currencyCode) switch
                        {
                            JapaneseYen => 0,
                            IranianRial or
                            IranianToman or
                            UnitedStatesDollar or
                            Euro or
                            BritishPound or
                            UnitedArabEmiratesDirham or
                            TurkishLira or
                            ChineseYuan or
                            CanadianDollar or
                            AustralianDollar or
                            SwissFranc => 2,
                            _ => 2
                        };
                    }

                    public static bool IsZeroDecimal(string? currencyCode) => GetDecimalPlaces(currencyCode) == 0;

                    public static string GetOrDefault(string? currencyCode, string fallback = IranianRial)
                    {
                        var normalized = NormalizeCode(currencyCode);
                        if (IsSupported(normalized))
                            return normalized;

                        var normalizedFallback = NormalizeCode(fallback);
                        return IsSupported(normalizedFallback) ? normalizedFallback : IranianRial;
                    }

                    public static bool AreEqual(string? first, string? second)
                    {
                        return string.Equals(NormalizeCode(first), NormalizeCode(second), StringComparison.OrdinalIgnoreCase);
                    }

                    public static bool IsIranianCurrency(string? currencyCode)
                    {
                        return NormalizeCode(currencyCode) is IranianRial or IranianToman;
                    }

                    public static decimal RialToToman(decimal rialAmount) => rialAmount / 10m;

                    public static decimal TomanToRial(decimal tomanAmount) => tomanAmount * 10m;
                }
                """
            },

            // ==========================================
            // HELPERS - NETWORKING
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Networking", "UserAgentHelper.cs"),
                """
                using System;
                using System.Text.RegularExpressions;

                namespace Shafiee.BuildingBlocks.Shared.Helpers.Networking;

                public static partial class UserAgentHelper
                {
                    #region Normalization
                    public static string Normalize(string? userAgent)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return string.Empty;

                        return CollapseWhitespaceRegex()
                            .Replace(userAgent.Trim(), " ");
                    }

                    public static bool IsEmpty(string? userAgent)
                        => string.IsNullOrWhiteSpace(userAgent);

                    public static bool Contains(string? userAgent, string value)
                    {
                        if (string.IsNullOrEmpty(userAgent) || string.IsNullOrEmpty(value))
                        {
                            return false;
                        }

                        return userAgent.Contains(value, StringComparison.OrdinalIgnoreCase);
                    }
                    #endregion

                    #region Parsing
                    public static UserAgentInfo Parse(string? userAgent)
                    {
                        var normalized = Normalize(userAgent);

                        if (normalized.Length == 0)
                            return UserAgentInfo.Empty;

                        var browser = GetBrowserFamily(normalized);
                        var browserVersion = GetBrowserVersion(normalized, browser);
                        var operatingSystem = GetOperatingSystemFamily(normalized);
                        var operatingSystemVersion = GetOperatingSystemVersion(normalized, operatingSystem);
                        var isBot = IsBot(normalized);
                        var deviceType = GetDeviceType(normalized, operatingSystem, isBot);

                        return new UserAgentInfo(
                            Raw: userAgent ?? string.Empty,
                            Normalized: normalized,
                            Browser: browser,
                            BrowserVersion: browserVersion,
                            OperatingSystem: operatingSystem,
                            OperatingSystemVersion: operatingSystemVersion,
                            Device: deviceType,
                            IsBot: isBot);
                    }
                    #endregion

                    #region Browser
                    public static BrowserFamily GetBrowserFamily(string? userAgent)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return BrowserFamily.Unknown;

                        var ua = userAgent;

                        if (GoogleBotRegex().IsMatch(ua)) return BrowserFamily.Googlebot;
                        if (BingBotRegex().IsMatch(ua)) return BrowserFamily.Bingbot;
                        if (OtherBotRegex().IsMatch(ua)) return BrowserFamily.Bot;
                        if (Contains(ua, "PostmanRuntime/")) return BrowserFamily.Postman;
                        if (Contains(ua, "curl/")) return BrowserFamily.Curl;
                        if (Contains(ua, "Edg/")) return BrowserFamily.Edge;
                        if (Contains(ua, "OPR/") || Contains(ua, "Opera/")) return BrowserFamily.Opera;
                        if (Contains(ua, "SamsungBrowser/")) return BrowserFamily.SamsungInternet;
                        if (Contains(ua, "Firefox/") || Contains(ua, "FxiOS/")) return BrowserFamily.Firefox;
                        if (Contains(ua, "Chrome/") || Contains(ua, "CriOS/")) return BrowserFamily.Chrome;
                        if (Contains(ua, "Chromium/")) return BrowserFamily.Chromium;
                        if (Contains(ua, "Safari/") && Contains(ua, "Version/")) return BrowserFamily.Safari;
                        if (Contains(ua, "HeadlessChrome")) return BrowserFamily.HeadlessChrome;

                        return BrowserFamily.Unknown;
                    }

                    public static string GetBrowserVersion(string? userAgent, BrowserFamily? browser = null)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return string.Empty;

                        var detectedBrowser = browser ?? GetBrowserFamily(userAgent);

                        return detectedBrowser switch
                        {
                            BrowserFamily.Edge => MatchVersion(EdgeRegex(), userAgent),
                            BrowserFamily.Opera => MatchVersion(OperaRegex(), userAgent),
                            BrowserFamily.SamsungInternet => MatchVersion(SamsungBrowserRegex(), userAgent),
                            BrowserFamily.Firefox => MatchVersion(FirefoxRegex(), userAgent),
                            BrowserFamily.Chrome => MatchVersion(ChromeRegex(), userAgent),
                            BrowserFamily.Chromium => MatchVersion(ChromiumRegex(), userAgent),
                            BrowserFamily.Safari => MatchVersion(SafariVersionRegex(), userAgent),
                            BrowserFamily.Curl => MatchVersion(CurlRegex(), userAgent),
                            BrowserFamily.Postman => MatchVersion(PostmanRegex(), userAgent),
                            BrowserFamily.Googlebot => MatchVersion(GoogleBotRegex(), userAgent),
                            BrowserFamily.Bingbot => MatchVersion(BingBotRegex(), userAgent),
                            BrowserFamily.HeadlessChrome => MatchVersion(HeadlessChromeRegex(), userAgent),
                            _ => string.Empty
                        };
                    }
                    #endregion

                    #region Operating System
                    public static OperatingSystemFamily GetOperatingSystemFamily(string? userAgent)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return OperatingSystemFamily.Unknown;

                        var ua = userAgent;

                        if (Contains(ua, "Windows Phone")) return OperatingSystemFamily.WindowsPhone;
                        if (Contains(ua, "Android")) return OperatingSystemFamily.Android;
                        if (Contains(ua, "iPhone") || Contains(ua, "iPad") || Contains(ua, "iPod")) return OperatingSystemFamily.iOS;
                        if (Contains(ua, "Windows NT")) return OperatingSystemFamily.Windows;
                        if (Contains(ua, "CrOS")) return OperatingSystemFamily.ChromeOS;
                        if (Contains(ua, "Mac OS X")) return OperatingSystemFamily.macOS;
                        if (Contains(ua, "Linux")) return OperatingSystemFamily.Linux;

                        return OperatingSystemFamily.Unknown;
                    }

                    public static string GetOperatingSystemVersion(string? userAgent, OperatingSystemFamily? operatingSystem = null)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return string.Empty;

                        var detectedOs = operatingSystem ?? GetOperatingSystemFamily(userAgent);

                        return detectedOs switch
                        {
                            OperatingSystemFamily.Windows => MatchVersion(WindowsRegex(), userAgent),
                            OperatingSystemFamily.Android => MatchVersion(AndroidRegex(), userAgent),
                            OperatingSystemFamily.iOS => NormalizeOsVersion(MatchVersion(IosRegex(), userAgent)),
                            OperatingSystemFamily.macOS => NormalizeOsVersion(MatchVersion(MacOsRegex(), userAgent)),
                            OperatingSystemFamily.ChromeOS => MatchVersion(ChromeOsRegex(), userAgent),
                            OperatingSystemFamily.WindowsPhone => MatchVersion(WindowsPhoneRegex(), userAgent),
                            _ => string.Empty
                        };
                    }
                    #endregion

                    #region Device
                    public static DeviceType GetDeviceType(string? userAgent)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return DeviceType.Unknown;

                        var normalized = Normalize(userAgent);
                        var os = GetOperatingSystemFamily(normalized);
                        var bot = IsBot(normalized);

                        return GetDeviceType(normalized, os, bot);
                    }

                    private static DeviceType GetDeviceType(string userAgent, OperatingSystemFamily operatingSystem, bool isBot)
                    {
                        if (isBot) return DeviceType.Bot;

                        if (operatingSystem == OperatingSystemFamily.iOS)
                        {
                            return Contains(userAgent, "iPad") ? DeviceType.Tablet : DeviceType.Mobile;
                        }

                        if (operatingSystem == OperatingSystemFamily.Android)
                        {
                            return Contains(userAgent, "Mobile") ? DeviceType.Mobile : DeviceType.Tablet;
                        }

                        if (operatingSystem == OperatingSystemFamily.WindowsPhone)
                            return DeviceType.Mobile;

                        if (Contains(userAgent, "Mobile"))
                            return DeviceType.Mobile;

                        return operatingSystem switch
                        {
                            OperatingSystemFamily.Windows or OperatingSystemFamily.macOS or OperatingSystemFamily.Linux or OperatingSystemFamily.ChromeOS => DeviceType.Desktop,
                            _ => DeviceType.Unknown
                        };
                    }

                    public static bool IsMobile(string? userAgent) => GetDeviceType(userAgent) == DeviceType.Mobile;
                    public static bool IsTablet(string? userAgent) => GetDeviceType(userAgent) == DeviceType.Tablet;
                    public static bool IsDesktop(string? userAgent) => GetDeviceType(userAgent) == DeviceType.Desktop;
                    #endregion

                    #region Bot Detection
                    public static bool IsBot(string? userAgent)
                    {
                        if (string.IsNullOrWhiteSpace(userAgent))
                            return false;

                        return GoogleBotRegex().IsMatch(userAgent) ||
                               BingBotRegex().IsMatch(userAgent) ||
                               OtherBotRegex().IsMatch(userAgent) ||
                               Contains(userAgent, "HeadlessChrome");
                    }
                    #endregion

                    #region Helpers
                    private static string MatchVersion(Regex regex, string userAgent)
                    {
                        var match = regex.Match(userAgent);
                        if (!match.Success || match.Groups.Count < 2)
                            return string.Empty;

                        return match.Groups[1].Value;
                    }

                    private static string NormalizeOsVersion(string version)
                        => string.IsNullOrWhiteSpace(version) ? string.Empty : version.Replace('_', '.');
                    #endregion

                    #region Regex
                    [GeneratedRegex(@"\s+")] private static partial Regex CollapseWhitespaceRegex();
                    [GeneratedRegex(@"Edg/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex EdgeRegex();
                    [GeneratedRegex(@"OPR/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex OperaRegex();
                    [GeneratedRegex(@"SamsungBrowser/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex SamsungBrowserRegex();
                    [GeneratedRegex(@"(?:Firefox|FxiOS)/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex FirefoxRegex();
                    [GeneratedRegex(@"(?:Chrome|CriOS)/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex ChromeRegex();
                    [GeneratedRegex(@"Chromium/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex ChromiumRegex();
                    [GeneratedRegex(@"Version/([\d.]+).*Safari/", RegexOptions.IgnoreCase)] private static partial Regex SafariVersionRegex();
                    [GeneratedRegex(@"curl/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex CurlRegex();
                    [GeneratedRegex(@"PostmanRuntime/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex PostmanRegex();
                    [GeneratedRegex(@"Googlebot/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex GoogleBotRegex();
                    [GeneratedRegex(@"bingbot/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex BingBotRegex();
                    [GeneratedRegex(@"HeadlessChrome/([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex HeadlessChromeRegex();
                    [GeneratedRegex(@"Windows NT ([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex WindowsRegex();
                    [GeneratedRegex(@"Android ([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex AndroidRegex();
                    [GeneratedRegex(@"OS ([\d_]+).*like Mac OS X", RegexOptions.IgnoreCase)] private static partial Regex IosRegex();
                    [GeneratedRegex(@"Mac OS X ([\d_\.]+)", RegexOptions.IgnoreCase)] private static partial Regex MacOsRegex();
                    [GeneratedRegex(@"CrOS [^ ;]+ ([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex ChromeOsRegex();
                    [GeneratedRegex(@"Windows Phone(?: OS)? ([\d.]+)", RegexOptions.IgnoreCase)] private static partial Regex WindowsPhoneRegex();
                    [GeneratedRegex(@"(?:bot|crawler|spider|slurp|facebookexternalhit|facebookbot|twitterbot|linkedinbot|yandexbot|duckduckbot|baiduspider)", RegexOptions.IgnoreCase)] private static partial Regex OtherBotRegex();
                    #endregion
                }

                public sealed record UserAgentInfo(
                    string Raw,
                    string Normalized,
                    BrowserFamily Browser,
                    string BrowserVersion,
                    OperatingSystemFamily OperatingSystem,
                    string OperatingSystemVersion,
                    DeviceType Device,
                    bool IsBot)
                {
                    public static UserAgentInfo Empty { get; } = new(string.Empty, string.Empty, BrowserFamily.Unknown, string.Empty, OperatingSystemFamily.Unknown, string.Empty, DeviceType.Unknown, false);
                }

                public enum BrowserFamily { Unknown = 0, Chrome = 1, Chromium = 2, Edge = 3, Firefox = 4, Safari = 5, Opera = 6, SamsungInternet = 7, Googlebot = 8, Bingbot = 9, Bot = 10, Curl = 11, Postman = 12, HeadlessChrome = 13 }
                public enum OperatingSystemFamily { Unknown = 0, Windows = 1, macOS = 2, Linux = 3, Android = 4, iOS = 5, ChromeOS = 6, WindowsPhone = 7 }
                public enum DeviceType { Unknown = 0, Desktop = 1, Mobile = 2, Tablet = 3, Bot = 4 }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Networking", "IpAddressHelper.cs"),
                """
                using System.Net;
                using System.Net.Sockets;

                namespace Shafiee.BuildingBlocks.Shared.Helpers.Networking;

                public static class IpAddressHelper
                {
                    public static IPAddress Parse(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                        if (!IPAddress.TryParse(value.Trim(), out var address))
                        {
                            throw new FormatException($"'{value}' is not a valid IP address.");
                        }
                        return address;
                    }

                    public static bool TryParse(string? value, out IPAddress? address)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            address = null;
                            return false;
                        }
                        return IPAddress.TryParse(value.Trim(), out address);
                    }

                    public static string Normalize(string? value)
                    {
                        if (!TryParse(value, out var address) || address is null)
                            return string.Empty;
                        return address.ToString();
                    }

                    public static bool IsValid(string? value) => TryParse(value, out _);
                    public static bool IsIPv4(IPAddress? address) => address?.AddressFamily == AddressFamily.InterNetwork;
                    public static bool IsIPv6(IPAddress? address) => address?.AddressFamily == AddressFamily.InterNetworkV6;
                    public static bool IsIPv4(string? value) => TryParse(value, out var address) && IsIPv4(address);
                    public static bool IsIPv6(string? value) => TryParse(value, out var address) && IsIPv6(address);
                    public static bool IsLoopback(IPAddress? address) => address is not null && IPAddress.IsLoopback(address);
                    public static bool IsLoopback(string? value) => TryParse(value, out var address) && IsLoopback(address);
                    public static bool IsUnspecified(IPAddress? address) => address is not null && (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any));

                    public static bool IsPrivateIPv4(IPAddress? address)
                    {
                        if (!IsIPv4(address)) return false;
                        var bytes = address!.GetAddressBytes();
                        return bytes[0] switch
                        {
                            10 => true,
                            172 => bytes[1] >= 16 && bytes[1] <= 31,
                            192 => bytes[1] == 168,
                            _ => false
                        };
                    }

                    public static bool IsPrivateIPv4(string? value) => TryParse(value, out var address) && IsPrivateIPv4(address);
                    public static bool IsLinkLocalIPv4(IPAddress? address) => IsIPv4(address) && address!.GetAddressBytes()[0] == 169 && address.GetAddressBytes()[1] == 254;
                    public static bool IsMulticast(IPAddress? address) => IsIPv6(address) && address!.GetAddressBytes()[0] == 0xFF;
                    public static bool IsBroadcast(IPAddress? address) => address?.AddressFamily == AddressFamily.InterNetwork && address.Equals(IPAddress.Broadcast);

                    public static IPAddress NormalizeMappedAddress(IPAddress address)
                    {
                        ArgumentNullException.ThrowIfNull(address);
                        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
                    }

                    public static string ToStorageString(IPAddress? address) => address is null ? string.Empty : NormalizeMappedAddress(address).ToString();
                    public static string ToDisplayString(IPAddress? address) => address?.ToString() ?? string.Empty;
                }
                """
            },

            // ==========================================
            // HELPERS - HASHING & CHECKSUM
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Hashing", "ChecksumHelper.cs"),
                """
                using System;
                using System.Globalization;
                using System.IO;
                using System.Text;

                namespace Shafiee.BuildingBlocks.Shared.Helpers.Hashing;

                public static class ChecksumHelper
                {
                    private const uint Crc32Polynomial = 0xEDB88320;
                    private static readonly uint[] Crc32Table = CreateCrc32Table();

                    public static uint Crc32(ReadOnlySpan<byte> data)
                    {
                        var checksum = uint.MaxValue;
                        foreach (var value in data)
                        {
                            var index = (byte)((checksum ^ value) & 0xFF);
                            checksum = (checksum >> 8) ^ Crc32Table[index];
                        }
                        return ~checksum;
                    }

                    public static uint Crc32(string value, Encoding? encoding = null)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return Crc32((encoding ?? Encoding.UTF8).GetBytes(value));
                    }

                    public static string Crc32Hex(ReadOnlySpan<byte> data) => Crc32(data).ToString("X8", CultureInfo.InvariantCulture);
                    public static string Crc32Hex(string value, Encoding? encoding = null) => Crc32Hex((encoding ?? Encoding.UTF8).GetBytes(value));

                    private static uint[] CreateCrc32Table()
                    {
                        var table = new uint[256];
                        for (uint index = 0; index < table.Length; index++)
                        {
                            var value = index;
                            for (var bit = 0; bit < 8; bit++)
                            {
                                value = (value & 1) != 0 ? (value >> 1) ^ Crc32Polynomial : value >> 1;
                            }
                            table[index] = value;
                        }
                        return table;
                    }
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Hashing", "HashHelper.cs"),
                """
                using System;
                using System.Security.Cryptography;
                using System.Text;

                namespace Shafiee.BuildingBlocks.Shared.Helpers.Hashing;

                public static class HashHelper
                {
                    public static byte[] Sha256(ReadOnlySpan<byte> data) => SHA256.HashData(data);
                    public static byte[] Sha512(ReadOnlySpan<byte> data) => SHA512.HashData(data);

                    public static string Sha256Hex(ReadOnlySpan<byte> data) => Convert.ToHexString(Sha256(data)).ToLowerInvariant();
                    public static string Sha512Hex(ReadOnlySpan<byte> data) => Convert.ToHexString(Sha512(data)).ToLowerInvariant();

                    public static string Sha256Hex(string value, Encoding? encoding = null)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return Sha256Hex((encoding ?? Encoding.UTF8).GetBytes(value));
                    }

                    public static bool FixedTimeEquals(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
                    {
                        return CryptographicOperations.FixedTimeEquals(first, second);
                    }
                }
                """
            },

              // ==========================================
            // HELPERS - Environment
            // ==========================================
            {
                Path.Combine(projectDir, "Helpers", "Environment", "EnvironmentHelper.cs"),
                """
                                using System;
                using System.Collections.Generic;
                using System.Globalization;
                using System.Linq;

                namespace Project.BuildingBlocks.Shared.Helpers.Environment;

                /// <summary>
                /// Provides framework-independent utilities for accessing process and
                /// application environment information.
                /// </summary>
                public static class EnvironmentHelper
                {
                    #region Application

                    /// <summary>
                    /// Gets the current application/process name.
                    /// </summary>
                    public static string ApplicationName
                        => System.Environment.ProcessPath is { Length: > 0 } path
                            ? System.IO.Path.GetFileNameWithoutExtension(path)
                            : AppDomain.CurrentDomain.FriendlyName;

                    /// <summary>
                    /// Gets the current process executable path.
                    /// </summary>
                    public static string ProcessPath
                        => System.Environment.ProcessPath ?? string.Empty;

                    /// <summary>
                    /// Gets the current application domain friendly name.
                    /// </summary>
                    public static string ApplicationDomainName
                        => AppDomain.CurrentDomain.FriendlyName;

                    /// <summary>
                    /// Gets the current application base directory.
                    /// </summary>
                    public static string ApplicationBaseDirectory
                        => AppContext.BaseDirectory;

                    /// <summary>
                    /// Gets the current working directory.
                    /// </summary>
                    public static string CurrentDirectory
                        => System.Environment.CurrentDirectory;

                    #endregion

                    #region Machine

                    /// <summary>
                    /// Gets the current machine/computer name.
                    /// </summary>
                    public static string MachineName
                        => System.Environment.MachineName;

                    /// <summary>
                    /// Gets the current user name.
                    /// </summary>
                    public static string UserName
                        => System.Environment.UserName;

                    /// <summary>
                    /// Gets the current user domain name.
                    /// </summary>
                    public static string UserDomainName
                        => System.Environment.UserDomainName;

                    /// <summary>
                    /// Gets the number of logical processors available to the process.
                    /// </summary>
                    public static int ProcessorCount
                        => System.Environment.ProcessorCount;

                    #endregion

                    #region Runtime

                    /// <summary>
                    /// Gets the current .NET runtime version.
                    /// </summary>
                    public static string RuntimeVersion
                        => System.Environment.Version.ToString();

                    /// <summary>
                    /// Gets the current .NET runtime identifier.
                    /// </summary>
                    public static string RuntimeIdentifier
                        => System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

                    /// <summary>
                    /// Gets the framework description.
                    /// </summary>
                    public static string FrameworkDescription
                        => System.Runtime.InteropServices.RuntimeInformation
                            .FrameworkDescription;

                    /// <summary>
                    /// Gets the operating system description.
                    /// </summary>
                    public static string OperatingSystemDescription
                        => System.Runtime.InteropServices.RuntimeInformation
                            .OSDescription;

                    /// <summary>
                    /// Gets the current process architecture.
                    /// </summary>
                    public static string ProcessArchitecture
                        => System.Runtime.InteropServices.RuntimeInformation
                            .ProcessArchitecture
                            .ToString();

                    /// <summary>
                    /// Gets the operating system architecture.
                    /// </summary>
                    public static string OperatingSystemArchitecture
                        => System.Runtime.InteropServices.RuntimeInformation
                            .OSArchitecture
                            .ToString();

                    #endregion

                    #region Environment Variables

                    /// <summary>
                    /// Gets an environment variable.
                    /// </summary>
                    public static string? GetVariable(string name)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return null;

                        return System.Environment.GetEnvironmentVariable(name);
                    }

                    /// <summary>
                    /// Gets an environment variable or returns the supplied default value.
                    /// </summary>
                    public static string GetVariable(
                        string name,
                        string defaultValue)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return defaultValue;

                        return System.Environment.GetEnvironmentVariable(name)
                               ?? defaultValue;
                    }

                    /// <summary>
                    /// Determines whether an environment variable exists.
                    /// </summary>
                    public static bool HasVariable(string name)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return false;

                        return System.Environment.GetEnvironmentVariable(name) is not null;
                    }

                    /// <summary>
                    /// Sets an environment variable for the current process.
                    /// </summary>
                    public static void SetVariable(
                        string name,
                        string? value)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            throw new ArgumentException(
                                "Environment variable name cannot be empty.",
                                nameof(name));

                        System.Environment.SetEnvironmentVariable(
                            name,
                            value,
                            EnvironmentVariableTarget.Process);
                    }

                    /// <summary>
                    /// Removes an environment variable from the current process.
                    /// </summary>
                    public static void RemoveVariable(string name)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        System.Environment.SetEnvironmentVariable(
                            name,
                            null,
                            EnvironmentVariableTarget.Process);
                    }

                    /// <summary>
                    /// Gets all environment variables available to the current process.
                    /// </summary>
                    public static IReadOnlyDictionary<string, string> GetVariables()
                    {
                        var variables = new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase);

                        foreach (System.Collections.DictionaryEntry item
                                 in System.Environment.GetEnvironmentVariables(
                                     EnvironmentVariableTarget.Process))
                        {
                            if (item.Key is null || item.Value is null)
                                continue;

                            variables[item.Key.ToString() ?? string.Empty] =
                                item.Value.ToString() ?? string.Empty;
                        }

                        return variables;
                    }

                    #endregion

                    #region Process

                    /// <summary>
                    /// Gets the current process identifier.
                    /// </summary>
                    public static int ProcessId
                        => System.Environment.ProcessId;

                    /// <summary>
                    /// Gets the current process working set in bytes.
                    /// </summary>
                    public static long WorkingSet
                        => System.Environment.WorkingSet;

                    /// <summary>
                    /// Gets the current process uptime.
                    /// </summary>
                    public static TimeSpan Uptime
                        => TimeSpan.FromMilliseconds(
                            System.Environment.TickCount64);

                    #endregion

                    #region Special Folders

                    /// <summary>
                    /// Gets the specified special folder path.
                    /// </summary>
                    public static string GetFolderPath(
                        Environment.SpecialFolder folder)
                    {
                        return System.Environment.GetFolderPath(folder);
                    }

                    /// <summary>
                    /// Gets the user's home directory.
                    /// </summary>
                    public static string UserHomeDirectory
                        => System.Environment.GetFolderPath(
                            Environment.SpecialFolder.UserProfile);

                    /// <summary>
                    /// Gets the temporary directory.
                    /// </summary>
                    public static string TemporaryDirectory
                        => System.IO.Path.GetTempPath();

                    #endregion

                    #region Culture

                    /// <summary>
                    /// Gets the current process culture name.
                    /// </summary>
                    public static string CurrentCultureName
                        => CultureInfo.CurrentCulture.Name;

                    /// <summary>
                    /// Gets the current UI culture name.
                    /// </summary>
                    public static string CurrentUICultureName
                        => CultureInfo.CurrentUICulture.Name;

                    #endregion

                    #region Runtime Detection

                    /// <summary>
                    /// Determines whether the current process is running in a 64-bit process.
                    /// </summary>
                    public static bool Is64BitProcess
                        => System.Environment.Is64BitProcess;

                    /// <summary>
                    /// Determines whether the operating system is 64-bit.
                    /// </summary>
                    public static bool Is64BitOperatingSystem
                        => System.Environment.Is64BitOperatingSystem;

                    /// <summary>
                    /// Determines whether the current process is interactive.
                    /// </summary>
                    public static bool IsInteractive
                        => System.Environment.UserInteractive;

                    #endregion

                    #region System Information

                    /// <summary>
                    /// Gets a snapshot of common environment information.
                    /// </summary>
                    public static EnvironmentInfo GetInformation()
                    {
                        return new EnvironmentInfo
                        {
                            ApplicationName = ApplicationName,
                            ProcessPath = ProcessPath,
                            ApplicationDomainName = ApplicationDomainName,
                            ApplicationBaseDirectory = ApplicationBaseDirectory,
                            CurrentDirectory = CurrentDirectory,
                            MachineName = MachineName,
                            UserName = UserName,
                            ProcessorCount = ProcessorCount,
                            RuntimeVersion = RuntimeVersion,
                            RuntimeIdentifier = RuntimeIdentifier,
                            FrameworkDescription = FrameworkDescription,
                            OperatingSystemDescription = OperatingSystemDescription,
                            ProcessArchitecture = ProcessArchitecture,
                            OperatingSystemArchitecture = OperatingSystemArchitecture,
                            ProcessId = ProcessId,
                            WorkingSet = WorkingSet,
                            Is64BitProcess = Is64BitProcess,
                            Is64BitOperatingSystem = Is64BitOperatingSystem,
                            CurrentCultureName = CurrentCultureName,
                            CurrentUICultureName = CurrentUICultureName
                        };
                    }

                    #endregion
                }

                /// <summary>
                /// Represents a snapshot of general application and runtime environment information.
                /// </summary>
                public sealed class EnvironmentInfo
                {
                    public string ApplicationName { get; init; } = string.Empty;

                    public string ProcessPath { get; init; } = string.Empty;

                    public string ApplicationDomainName { get; init; } = string.Empty;

                    public string ApplicationBaseDirectory { get; init; } = string.Empty;

                    public string CurrentDirectory { get; init; } = string.Empty;

                    public string MachineName { get; init; } = string.Empty;

                    public string UserName { get; init; } = string.Empty;

                    public int ProcessorCount { get; init; }

                    public string RuntimeVersion { get; init; } = string.Empty;

                    public string RuntimeIdentifier { get; init; } = string.Empty;

                    public string FrameworkDescription { get; init; } = string.Empty;

                    public string OperatingSystemDescription { get; init; } = string.Empty;

                    public string ProcessArchitecture { get; init; } = string.Empty;

                    public string OperatingSystemArchitecture { get; init; } = string.Empty;

                    public int ProcessId { get; init; }

                    public long WorkingSet { get; init; }

                    public bool Is64BitProcess { get; init; }

                    public bool Is64BitOperatingSystem { get; init; }

                    public string CurrentCultureName { get; init; } = string.Empty;

                    public string CurrentUICultureName { get; init; } = string.Empty;
                }
                """
            },
            {
                Path.Combine(projectDir, "Helpers", "Environment", "OperatingSystemHelper.cs"),
                """
                                using System;
                using System.Runtime.InteropServices;

                namespace Project.BuildingBlocks.Shared.Helpers.Environment;

                /// <summary>
                /// Provides framework-independent utilities for detecting
                /// the current operating system and process architecture.
                /// </summary>
                public static class OperatingSystemHelper
                {
                    #region Operating System

                    /// <summary>
                    /// Gets the current operating system family.
                    /// </summary>
                    public static OperatingSystemFamily GetFamily()
                    {
                        if (OperatingSystem.IsWindows())
                            return OperatingSystemFamily.Windows;

                        if (OperatingSystem.IsLinux())
                            return OperatingSystemFamily.Linux;

                        if (OperatingSystem.IsMacOS())
                            return OperatingSystemFamily.macOS;

                        if (OperatingSystem.IsFreeBSD())
                            return OperatingSystemFamily.FreeBSD;

                        return OperatingSystemFamily.Unknown;
                    }

                    public static bool IsWindows()
                        => OperatingSystem.IsWindows();

                    public static bool IsLinux()
                        => OperatingSystem.IsLinux();

                    public static bool IsMacOS()
                        => OperatingSystem.IsMacOS();

                    public static bool IsFreeBSD()
                        => OperatingSystem.IsFreeBSD();

                    public static bool IsUnix()
                        => OperatingSystem.IsLinux() ||
                           OperatingSystem.IsMacOS() ||
                           OperatingSystem.IsFreeBSD();

                    #endregion

                    #region Windows Versions

                    public static bool IsWindows7()
                        => OperatingSystem.IsWindowsVersionAtLeast(6, 1);

                    public static bool IsWindows8()
                        => OperatingSystem.IsWindowsVersionAtLeast(6, 2);

                    public static bool IsWindows10()
                        => OperatingSystem.IsWindowsVersionAtLeast(10, 0);

                    public static bool IsWindows11()
                        => OperatingSystem.IsWindowsVersionAtLeast(10, 0);

                    #endregion

                    #region Architecture

                    /// <summary>
                    /// Gets the current process architecture.
                    /// </summary>
                    public static Architecture ProcessArchitecture
                        => RuntimeInformation.ProcessArchitecture;

                    /// <summary>
                    /// Gets the operating system architecture.
                    /// </summary>
                    public static Architecture OperatingSystemArchitecture
                        => RuntimeInformation.OSArchitecture;

                    public static bool IsX86()
                        => RuntimeInformation.ProcessArchitecture == Architecture.X86;

                    public static bool IsX64()
                        => RuntimeInformation.ProcessArchitecture == Architecture.X64;

                    public static bool IsArm()
                        => RuntimeInformation.ProcessArchitecture == Architecture.Arm;

                    public static bool IsArm64()
                        => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

                    public static bool Is64Bit()
                        => Environment.Is64BitProcess;

                    public static bool IsOperatingSystem64Bit()
                        => Environment.Is64BitOperatingSystem;

                    #endregion

                    #region Runtime

                    /// <summary>
                    /// Gets the .NET runtime version.
                    /// </summary>
                    public static Version RuntimeVersion
                        => Environment.Version;

                    /// <summary>
                    /// Gets the runtime identifier.
                    /// </summary>
                    public static string RuntimeIdentifier
                        => RuntimeInformation.RuntimeIdentifier;

                    /// <summary>
                    /// Gets the framework description.
                    /// </summary>
                    public static string FrameworkDescription
                        => RuntimeInformation.FrameworkDescription;

                    /// <summary>
                    /// Gets the operating system description.
                    /// </summary>
                    public static string Description
                        => RuntimeInformation.OSDescription;

                    #endregion

                    #region System Information

                    /// <summary>
                    /// Gets a snapshot of operating system information.
                    /// </summary>
                    public static OperatingSystemInfo GetInformation()
                    {
                        return new OperatingSystemInfo
                        {
                            Family = GetFamily(),
                            Description = Description,
                            ProcessArchitecture = ProcessArchitecture,
                            OperatingSystemArchitecture = OperatingSystemArchitecture,
                            RuntimeVersion = RuntimeVersion,
                            RuntimeIdentifier = RuntimeIdentifier,
                            FrameworkDescription = FrameworkDescription,
                            Is64BitProcess = Is64Bit(),
                            Is64BitOperatingSystem = IsOperatingSystem64Bit()
                        };
                    }

                    #endregion
                }

                /// <summary>
                /// Represents a general operating system family.
                /// </summary>
                public enum OperatingSystemFamily
                {
                    Unknown = 0,
                    Windows = 1,
                    Linux = 2,
                    macOS = 3,
                    FreeBSD = 4
                }

                /// <summary>
                /// Represents operating system and runtime information.
                /// </summary>
                public sealed class OperatingSystemInfo
                {
                    public OperatingSystemFamily Family { get; init; }

                    public string Description { get; init; } = string.Empty;

                    public Architecture ProcessArchitecture { get; init; }

                    public Architecture OperatingSystemArchitecture { get; init; }

                    public Version RuntimeVersion { get; init; } = Environment.Version;

                    public string RuntimeIdentifier { get; init; } = string.Empty;

                    public string FrameworkDescription { get; init; } = string.Empty;

                    public bool Is64BitProcess { get; init; }

                    public bool Is64BitOperatingSystem { get; init; }
                }
                """
            },
            
              // ==========================================
            // Patterns - Guard
            // ==========================================
            {
                Path.Combine(projectDir, "Patterns", "Guard", "Guard.cs"),
                """
                                using System;
                using System.Collections.Generic;
                using System.Globalization;

                namespace Project.BuildingBlocks.Shared.Patterns.Guard;

                /// <summary>
                /// Provides common guard clauses for validating method arguments,
                /// constructor arguments and invariants at the boundary of an operation.
                /// </summary>
                public static class Guard
                {
                    #region Null

                    /// <summary>
                    /// Ensures that a reference value is not null.
                    /// </summary>
                    public static T NotNull<T>(
                        T? value,
                        string parameterName)
                        where T : class
                    {
                        return value
                               ?? throw new ArgumentNullException(parameterName);
                    }

                    /// <summary>
                    /// Ensures that a nullable value type has a value.
                    /// </summary>
                    public static T NotNull<T>(
                        T? value,
                        string parameterName)
                        where T : struct
                    {
                        return value
                               ?? throw new ArgumentNullException(parameterName);
                    }

                    #endregion

                    #region String

                    /// <summary>
                    /// Ensures that a string is not null or empty.
                    /// </summary>
                    public static string NotNullOrEmpty(
                        string? value,
                        string parameterName)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            throw new ArgumentException(
                                "Value cannot be null or empty.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a string is not null, empty or whitespace.
                    /// </summary>
                    public static string NotNullOrWhiteSpace(
                        string? value,
                        string parameterName)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            throw new ArgumentException(
                                "Value cannot be null, empty or whitespace.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a string does not exceed the specified maximum length.
                    /// </summary>
                    public static string MaxLength(
                        string value,
                        int maxLength,
                        string parameterName)
                    {
                        NotNull(value, parameterName);

                        if (maxLength < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(maxLength),
                                maxLength,
                                "Maximum length cannot be negative.");
                        }

                        if (value.Length > maxLength)
                        {
                            throw new ArgumentException(
                                $"Value cannot contain more than {maxLength} characters.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a string has at least the specified minimum length.
                    /// </summary>
                    public static string MinLength(
                        string value,
                        int minLength,
                        string parameterName)
                    {
                        NotNull(value, parameterName);

                        if (minLength < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(minLength),
                                minLength,
                                "Minimum length cannot be negative.");
                        }

                        if (value.Length < minLength)
                        {
                            throw new ArgumentException(
                                $"Value must contain at least {minLength} characters.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a string length is within the specified range.
                    /// </summary>
                    public static string LengthBetween(
                        string value,
                        int minLength,
                        int maxLength,
                        string parameterName)
                    {
                        NotNull(value, parameterName);

                        if (minLength < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(minLength),
                                minLength,
                                "Minimum length cannot be negative.");
                        }

                        if (maxLength < minLength)
                        {
                            throw new ArgumentException(
                                "Maximum length cannot be smaller than minimum length.",
                                nameof(maxLength));
                        }

                        if (value.Length < minLength ||
                            value.Length > maxLength)
                        {
                            throw new ArgumentException(
                                $"Value length must be between {minLength} and {maxLength}.",
                                parameterName);
                        }

                        return value;
                    }

                    #endregion

                    #region Collections

                    /// <summary>
                    /// Ensures that a collection is not null.
                    /// </summary>
                    public static IReadOnlyCollection<T> NotNull<T>(
                        IReadOnlyCollection<T>? value,
                        string parameterName)
                    {
                        return value
                               ?? throw new ArgumentNullException(parameterName);
                    }

                    /// <summary>
                    /// Ensures that a collection is not null or empty.
                    /// </summary>
                    public static IReadOnlyCollection<T> NotNullOrEmpty<T>(
                        IReadOnlyCollection<T>? value,
                        string parameterName)
                    {
                        NotNull(value, parameterName);

                        if (value.Count == 0)
                        {
                            throw new ArgumentException(
                                "Collection cannot be empty.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that an enumerable is not null.
                    /// </summary>
                    public static IEnumerable<T> NotNull<T>(
                        IEnumerable<T>? value,
                        string parameterName)
                    {
                        return value
                               ?? throw new ArgumentNullException(parameterName);
                    }

                    #endregion

                    #region Boolean

                    /// <summary>
                    /// Ensures that a condition is true.
                    /// </summary>
                    public static void True(
                        bool condition,
                        string message)
                    {
                        if (!condition)
                        {
                            throw new ArgumentException(message);
                        }
                    }

                    /// <summary>
                    /// Ensures that a condition is false.
                    /// </summary>
                    public static void False(
                        bool condition,
                        string message)
                    {
                        if (condition)
                        {
                            throw new ArgumentException(message);
                        }
                    }

                    #endregion

                    #region Numeric

                    /// <summary>
                    /// Ensures that an integer is greater than zero.
                    /// </summary>
                    public static int Positive(
                        int value,
                        string parameterName)
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value must be greater than zero.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a long value is greater than zero.
                    /// </summary>
                    public static long Positive(
                        long value,
                        string parameterName)
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value must be greater than zero.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a decimal value is greater than zero.
                    /// </summary>
                    public static decimal Positive(
                        decimal value,
                        string parameterName)
                    {
                        if (value <= 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value must be greater than zero.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a numeric value is greater than or equal to zero.
                    /// </summary>
                    public static int NonNegative(
                        int value,
                        string parameterName)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value cannot be negative.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a long value is greater than or equal to zero.
                    /// </summary>
                    public static long NonNegative(
                        long value,
                        string parameterName)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value cannot be negative.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a decimal value is greater than or equal to zero.
                    /// </summary>
                    public static decimal NonNegative(
                        decimal value,
                        string parameterName)
                    {
                        if (value < 0)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "Value cannot be negative.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a value is within the specified inclusive range.
                    /// </summary>
                    public static int InRange(
                        int value,
                        int minimum,
                        int maximum,
                        string parameterName)
                    {
                        if (minimum > maximum)
                        {
                            throw new ArgumentException(
                                "Minimum cannot be greater than maximum.");
                        }

                        if (value < minimum || value > maximum)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                $"Value must be between {minimum} and {maximum}.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a long value is within the specified inclusive range.
                    /// </summary>
                    public static long InRange(
                        long value,
                        long minimum,
                        long maximum,
                        string parameterName)
                    {
                        if (minimum > maximum)
                        {
                            throw new ArgumentException(
                                "Minimum cannot be greater than maximum.");
                        }

                        if (value < minimum || value > maximum)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                $"Value must be between {minimum} and {maximum}.");
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a decimal value is within the specified inclusive range.
                    /// </summary>
                    public static decimal InRange(
                        decimal value,
                        decimal minimum,
                        decimal maximum,
                        string parameterName)
                    {
                        if (minimum > maximum)
                        {
                            throw new ArgumentException(
                                "Minimum cannot be greater than maximum.");
                        }

                        if (value < minimum || value > maximum)
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                $"Value must be between {minimum} and {maximum}.");
                        }

                        return value;
                    }

                    #endregion

                    #region DateTime

                    /// <summary>
                    /// Ensures that a DateTime value is not the default value.
                    /// </summary>
                    public static DateTime NotDefault(
                        DateTime value,
                        string parameterName)
                    {
                        if (value == default)
                        {
                            throw new ArgumentException(
                                "DateTime value cannot be the default value.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a DateTimeOffset value is not the default value.
                    /// </summary>
                    public static DateTimeOffset NotDefault(
                        DateTimeOffset value,
                        string parameterName)
                    {
                        if (value == default)
                        {
                            throw new ArgumentException(
                                "DateTimeOffset value cannot be the default value.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a DateOnly value is not the default value.
                    /// </summary>
                    public static DateOnly NotDefault(
                        DateOnly value,
                        string parameterName)
                    {
                        if (value == default)
                        {
                            throw new ArgumentException(
                                "DateOnly value cannot be the default value.",
                                parameterName);
                        }

                        return value;
                    }

                    #endregion

                    #region Guid

                    /// <summary>
                    /// Ensures that a Guid is not empty.
                    /// </summary>
                    public static Guid NotEmpty(
                        Guid value,
                        string parameterName)
                    {
                        if (value == Guid.Empty)
                        {
                            throw new ArgumentException(
                                "Guid cannot be empty.",
                                parameterName);
                        }

                        return value;
                    }

                    /// <summary>
                    /// Ensures that a nullable Guid has a non-empty value.
                    /// </summary>
                    public static Guid NotEmpty(
                        Guid? value,
                        string parameterName)
                    {
                        if (!value.HasValue || value.Value == Guid.Empty)
                        {
                            throw new ArgumentException(
                                "Guid cannot be null or empty.",
                                parameterName);
                        }

                        return value.Value;
                    }

                    #endregion

                    #region Enum

                    /// <summary>
                    /// Ensures that an enum value is defined.
                    /// </summary>
                    public static TEnum Defined<TEnum>(
                        TEnum value,
                        string parameterName)
                        where TEnum : struct, Enum
                    {
                        if (!Enum.IsDefined(value))
                        {
                            throw new ArgumentOutOfRangeException(
                                parameterName,
                                value,
                                "The specified enum value is not defined.");
                        }

                        return value;
                    }

                    #endregion

                    #region Equality

                    /// <summary>
                    /// Ensures that two values are not equal.
                    /// </summary>
                    public static T NotEqual<T>(
                        T value,
                        T other,
                        string parameterName)
                    {
                        if (EqualityComparer<T>.Default.Equals(value, other))
                        {
                            throw new ArgumentException(
                                "Value cannot be equal to the specified value.",
                                parameterName);
                        }

                        return value;
                    }

                    #endregion

                    #region Custom

                    /// <summary>
                    /// Throws an ArgumentException when the specified condition is true.
                    /// </summary>
                    public static void Against(
                        bool condition,
                        string message,
                        string? parameterName = null)
                    {
                        if (!condition)
                            return;

                        throw new ArgumentException(
                            message,
                            parameterName);
                    }

                    /// <summary>
                    /// Throws the supplied exception when the specified condition is true.
                    /// </summary>
                    public static void Against<TException>(
                        bool condition,
                        Func<TException> exceptionFactory)
                        where TException : Exception
                    {
                        if (!condition)
                            return;

                        throw exceptionFactory();
                    }

                    #endregion
                }
                """
            },
           


             // ==========================================
            // Patterns - Result
            // ==========================================
            {
                Path.Combine(projectDir, "Patterns", "Result", "ResultExtensions.cs"),
                """
                                using System;
                using System.Collections.Generic;
                using System.Linq;

                namespace Project.BuildingBlocks.Shared.Patterns.Result;

                public static class ResultExtensions
                {
                    // =========================================================
                    // Map
                    // =========================================================

                    public static Result<TDestination> Map<TSource, TDestination>(
                        this Result<TSource> result,
                        Func<TSource, TDestination> map)
                    {
                        ArgumentNullException.ThrowIfNull(map);

                        if (result.IsFailure)
                            return Result<TDestination>.Failure(result.Errors);

                        return Result<TDestination>.Success(map(result.Value));
                    }

                    public static Result<TDestination> Map<TSource, TDestination>(
                        this Result<TSource> result,
                        Func<TSource, Result<TDestination>> map)
                    {
                        ArgumentNullException.ThrowIfNull(map);

                        if (result.IsFailure)
                            return Result<TDestination>.Failure(result.Errors);

                        return map(result.Value);
                    }


                    // =========================================================
                    // Bind
                    // =========================================================

                    public static Result<TDestination> Bind<TSource, TDestination>(
                        this Result<TSource> result,
                        Func<TSource, Result<TDestination>> bind)
                    {
                        ArgumentNullException.ThrowIfNull(bind);

                        if (result.IsFailure)
                            return Result<TDestination>.Failure(result.Errors);

                        return bind(result.Value);
                    }

                    public static Result Bind<TSource>(
                        this Result<TSource> result,
                        Func<TSource, Result> bind)
                    {
                        ArgumentNullException.ThrowIfNull(bind);

                        if (result.IsFailure)
                            return Result.Failure(result.Errors);

                        return bind(result.Value);
                    }


                    // =========================================================
                    // Ensure
                    // =========================================================

                    public static Result<T> Ensure<T>(
                        this Result<T> result,
                        Func<T, bool> predicate,
                        ResultError error)
                    {
                        ArgumentNullException.ThrowIfNull(predicate);

                        if (result.IsFailure)
                            return result;

                        return predicate(result.Value)
                            ? result
                            : Result<T>.Failure(error);
                    }

                    public static Result<T> Ensure<T>(
                        this Result<T> result,
                        Func<T, bool> predicate,
                        Func<ResultError> errorFactory)
                    {
                        ArgumentNullException.ThrowIfNull(predicate);
                        ArgumentNullException.ThrowIfNull(errorFactory);

                        if (result.IsFailure)
                            return result;

                        return predicate(result.Value)
                            ? result
                            : Result<T>.Failure(errorFactory());
                    }

                    public static Result Ensure(
                        this Result result,
                        Func<bool> predicate,
                        ResultError error)
                    {
                        ArgumentNullException.ThrowIfNull(predicate);

                        if (result.IsFailure)
                            return result;

                        return predicate()
                            ? result
                            : Result.Failure(error);
                    }

                    public static Result Ensure(
                        this Result result,
                        Func<bool> predicate,
                        Func<ResultError> errorFactory)
                    {
                        ArgumentNullException.ThrowIfNull(predicate);
                        ArgumentNullException.ThrowIfNull(errorFactory);

                        if (result.IsFailure)
                            return result;

                        return predicate()
                            ? result
                            : Result.Failure(errorFactory());
                    }


                    // =========================================================
                    // Match
                    // =========================================================

                    public static TResult Match<T, TResult>(
                        this Result<T> result,
                        Func<T, TResult> onSuccess,
                        Func<IReadOnlyCollection<ResultError>, TResult> onFailure)
                    {
                        ArgumentNullException.ThrowIfNull(onSuccess);
                        ArgumentNullException.ThrowIfNull(onFailure);

                        return result.IsSuccess
                            ? onSuccess(result.Value)
                            : onFailure(result.Errors);
                    }

                    public static TResult Match<TResult>(
                        this Result result,
                        Func<TResult> onSuccess,
                        Func<IReadOnlyCollection<ResultError>, TResult> onFailure)
                    {
                        ArgumentNullException.ThrowIfNull(onSuccess);
                        ArgumentNullException.ThrowIfNull(onFailure);

                        return result.IsSuccess
                            ? onSuccess()
                            : onFailure(result.Errors);
                    }


                    // =========================================================
                    // Tap
                    // =========================================================

                    public static Result<T> Tap<T>(
                        this Result<T> result,
                        Action<T> action)
                    {
                        ArgumentNullException.ThrowIfNull(action);

                        if (result.IsSuccess)
                            action(result.Value);

                        return result;
                    }

                    public static Result Tap(
                        this Result result,
                        Action action)
                    {
                        ArgumentNullException.ThrowIfNull(action);

                        if (result.IsSuccess)
                            action();

                        return result;
                    }


                    // =========================================================
                    // TapFailure
                    // =========================================================

                    public static Result<T> TapFailure<T>(
                        this Result<T> result,
                        Action<IReadOnlyCollection<ResultError>> action)
                    {
                        ArgumentNullException.ThrowIfNull(action);

                        if (result.IsFailure)
                            action(result.Errors);

                        return result;
                    }

                    public static Result TapFailure(
                        this Result result,
                        Action<IReadOnlyCollection<ResultError>> action)
                    {
                        ArgumentNullException.ThrowIfNull(action);

                        if (result.IsFailure)
                            action(result.Errors);

                        return result;
                    }


                    // =========================================================
                    // ToResult
                    // =========================================================

                    public static Result ToResult<T>(this Result<T> result)
                    {
                        return result.IsSuccess
                            ? Result.Success()
                            : Result.Failure(result.Errors);
                    }


                    // =========================================================
                    // Optional conversion
                    // =========================================================

                    public static Result<T> ToResult<T>(
                        this T? value,
                        ResultError error)
                    {
                        return value is null
                            ? Result<T>.Failure(error)
                            : Result<T>.Success(value);
                    }


                    // =========================================================
                    // Combine
                    // =========================================================

                    public static Result Combine(
                        this IEnumerable<Result> results)
                    {
                        ArgumentNullException.ThrowIfNull(results);

                        var materialized = results.ToList();

                        var errors = materialized
                            .Where(x => x.IsFailure)
                            .SelectMany(x => x.Errors)
                            .ToArray();

                        return errors.Length == 0
                            ? Result.Success()
                            : Result.Failure(errors);
                    }

                    public static Result Combine(params Result[] results)
                    {
                        ArgumentNullException.ThrowIfNull(results);

                        return results.Combine();
                    }


                    // =========================================================
                    // Combine Typed Results
                    // =========================================================

                    public static Result<IReadOnlyList<T>> Combine<T>(
                        this IEnumerable<Result<T>> results)
                    {
                        ArgumentNullException.ThrowIfNull(results);

                        var materialized = results.ToList();

                        var errors = materialized
                            .Where(x => x.IsFailure)
                            .SelectMany(x => x.Errors)
                            .ToArray();

                        if (errors.Length > 0)
                            return Result<IReadOnlyList<T>>.Failure(errors);

                        var values = materialized
                            .Select(x => x.Value)
                            .ToArray();

                        return Result<IReadOnlyList<T>>.Success(values);
                    }

                    public static Result<IReadOnlyList<T>> Combine<T>(
                        params Result<T>[] results)
                    {
                        ArgumentNullException.ThrowIfNull(results);

                        return results.Combine();
                    }


                    // =========================================================
                    // GetValueOrDefault
                    // =========================================================

                    public static T? GetValueOrDefault<T>(
                        this Result<T> result)
                    {
                        return result.IsSuccess
                            ? result.Value
                            : default;
                    }

                    public static T GetValueOrDefault<T>(
                        this Result<T> result,
                        T defaultValue)
                    {
                        return result.IsSuccess
                            ? result.Value
                            : defaultValue;
                    }


                    // =========================================================
                    // ThrowIfFailure
                    // =========================================================

                    public static Result<T> ThrowIfFailure<T>(
                        this Result<T> result)
                    {
                        if (result.IsFailure)
                            throw new InvalidOperationException(
                                string.Join(
                                    Environment.NewLine,
                                    result.Errors.Select(x => x.Message)));

                        return result;
                    }

                    public static Result ThrowIfFailure(
                        this Result result)
                    {
                        if (result.IsFailure)
                            throw new InvalidOperationException(
                                string.Join(
                                    Environment.NewLine,
                                    result.Errors.Select(x => x.Message)));

                        return result;
                    }
                }
                """
            },
             // ==========================================
            // Patterns - Specification
            // ==========================================
            {
                Path.Combine(projectDir, "Patterns", "Specification", "ISpecification.cs"),
                """
                                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                namespace Project.BuildingBlocks.Shared.Patterns.Specification;

                public interface ISpecification<T>
                {
                    Expression<Func<T, bool>> Criteria { get; }

                    bool IsSatisfiedBy(T entity);
                }

                public static class SpecificationExtensions
                {
                    public static ISpecification<T> And<T>(
                        this ISpecification<T> left,
                        ISpecification<T> right)
                    {
                        ArgumentNullException.ThrowIfNull(left);
                        ArgumentNullException.ThrowIfNull(right);

                        return new CombinedSpecification<T>(
                            left,
                            right,
                            CombinationType.And);
                    }

                    public static ISpecification<T> Or<T>(
                        this ISpecification<T> left,
                        ISpecification<T> right)
                    {
                        ArgumentNullException.ThrowIfNull(left);
                        ArgumentNullException.ThrowIfNull(right);

                        return new CombinedSpecification<T>(
                            left,
                            right,
                            CombinationType.Or);
                    }

                    public static ISpecification<T> Not<T>(
                        this ISpecification<T> specification)
                    {
                        ArgumentNullException.ThrowIfNull(specification);

                        return new NotSpecification<T>(specification);
                    }

                    public static bool IsSatisfiedBy<T>(
                        this IEnumerable<T> source,
                        ISpecification<T> specification)
                    {
                        ArgumentNullException.ThrowIfNull(source);
                        ArgumentNullException.ThrowIfNull(specification);

                        return source.Any(specification.IsSatisfiedBy);
                    }

                    public static IEnumerable<T> Where<T>(
                        this IEnumerable<T> source,
                        ISpecification<T> specification)
                    {
                        ArgumentNullException.ThrowIfNull(source);
                        ArgumentNullException.ThrowIfNull(specification);

                        return source.Where(specification.IsSatisfiedBy);
                    }
                }

                public abstract class Specification<T> : ISpecification<T>
                {
                    public abstract Expression<Func<T, bool>> Criteria { get; }

                    public virtual bool IsSatisfiedBy(T entity)
                    {
                        ArgumentNullException.ThrowIfNull(entity);

                        return Criteria.Compile()(entity);
                    }
                }

                internal enum CombinationType
                {
                    And,
                    Or
                }

                internal sealed class CombinedSpecification<T> : Specification<T>
                {
                    private readonly ISpecification<T> _left;
                    private readonly ISpecification<T> _right;
                    private readonly CombinationType _combinationType;

                    public CombinedSpecification(
                        ISpecification<T> left,
                        ISpecification<T> right,
                        CombinationType combinationType)
                    {
                        _left = left;
                        _right = right;
                        _combinationType = combinationType;
                    }

                    public override Expression<Func<T, bool>> Criteria
                    {
                        get
                        {
                            var parameter = Expression.Parameter(
                                typeof(T),
                                "entity");

                            var leftBody = ReplaceParameter(
                                _left.Criteria,
                                parameter);

                            var rightBody = ReplaceParameter(
                                _right.Criteria,
                                parameter);

                            var body = _combinationType == CombinationType.And
                                ? Expression.AndAlso(leftBody, rightBody)
                                : Expression.OrElse(leftBody, rightBody);

                            return Expression.Lambda<Func<T, bool>>(
                                body,
                                parameter);
                        }
                    }

                    private static Expression ReplaceParameter(
                        Expression expression,
                        ParameterExpression parameter)
                    {
                        return new ParameterReplaceVisitor(
                            expression,
                            parameter).Visit(expression)!;
                    }
                }

                internal sealed class NotSpecification<T> : Specification<T>
                {
                    private readonly ISpecification<T> _specification;

                    public NotSpecification(
                        ISpecification<T> specification)
                    {
                        _specification = specification;
                    }

                    public override Expression<Func<T, bool>> Criteria
                    {
                        get
                        {
                            var parameter = Expression.Parameter(
                                typeof(T),
                                "entity");

                            var body = _specification.Criteria.Body;

                            body = new ParameterReplaceVisitor(
                                _specification.Criteria.Parameters[0],
                                parameter).Visit(body)!;

                            return Expression.Lambda<Func<T, bool>>(
                                Expression.Not(body),
                                parameter);
                        }
                    }
                }

                internal sealed class ParameterReplaceVisitor : ExpressionVisitor
                {
                    private readonly ParameterExpression _source;
                    private readonly ParameterExpression _target;

                    public ParameterReplaceVisitor(
                        ParameterExpression source,
                        ParameterExpression target)
                    {
                        _source = source;
                        _target = target;
                    }

                    protected override Expression VisitParameter(
                        ParameterExpression node)
                    {
                        return node == _source
                            ? _target
                            : base.VisitParameter(node);
                    }
                }
                """
            },
            

        };
    }
}