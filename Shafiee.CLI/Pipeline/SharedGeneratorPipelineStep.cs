namespace Shafiee.CLI.PipelineSteps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shafiee.SDK.Pipeline;

public class SharedGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        // ۱. دریافت نام سولشن (پیش‌فرض: Shop)
        string solutionName = context.Command.GetOption("solution");
        if (string.IsNullOrEmpty(solutionName) || solutionName.Equals("shared", StringComparison.OrdinalIgnoreCase))
        {
            solutionName = "Shop";
        }

        // ۲. ساخت مسیر مطلق و دقیق روی دسکتاپ (بدون درگیر شدن با درایو F و مشکلات مسیر نسبی)
        string desktopRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionRoot = Path.Combine(desktopRoot, solutionName);
        // مسیر دقیق پروژه که شامل فایل csproj است
        string projectDir = Path.Combine(solutionRoot, "src", "BuildingBlocks", $"{solutionName}.BuildingBlocks.Shared", $"{solutionName}.BuildingBlocks.Shared");
        var files = GetSharedSourceFiles(projectDir);

        int successCount = 0;
        foreach (var file in files)
        {
            string targetPath = file.Key; // مسیر کامل و مطلق فایل
            string? fileDir = Path.GetDirectoryName(targetPath);

            // اگر پوشه‌ها وجود نداشتند، به صورت خودکار بساز
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            // نوشتن مستقیم محتوا روی دیسک
            File.WriteAllText(targetPath, file.Value);
            Console.WriteLine($"✅ [Written] {targetPath}");
            successCount++;
        }

        Console.WriteLine($"\n📊 [Disk Summary] Successfully wrote {successCount} files directly to destination.");

        return Task.CompletedTask;
    }

    private static Dictionary<string, string> GetSharedSourceFiles(string projectDir)
    {
        return new Dictionary<string, string>
        {
            {
                Path.Combine(projectDir, "Abstractions", "IClock.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateTimeOffset NowOffset { get; }"
            },
            {
                Path.Combine(projectDir, "Abstractions", "ICurrentUser.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }"
            },
            {
                Path.Combine(projectDir, "Abstractions", "IExecutionContext.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IExecutionContext
{
    string? CorrelationId { get; }
    string? RequestId { get; }
    string? TraceId { get; }
    string? ClientIp { get; }
    string? UserAgent { get; }"
            },
            {
                Path.Combine(projectDir, "Abstractions", "IIdGenerator.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Abstractions;

public interface IIdGenerator
{
    Guid NewGuid();
}"
            },
            {
                Path.Combine(projectDir, "Results", "ResultStatus.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Results;

public enum ResultStatus
{
    Success = 200,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    Error = 500
}"
            },
            {
                Path.Combine(projectDir, "Results", "ResultError.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Results;

public sealed record ResultError
{
    public ResultError(string code, string message, string? field = null)
    {
        Code = code;
        Message = message;
        Field = field;
    }

    public string Code { get; init; }
    public string Message { get; init; }
    public string? Field { get; init; }"
            },
            {
                Path.Combine(projectDir, "Results", "Result.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<ResultError> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<ResultError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<ResultError>());
    public static Result Failure(ResultError error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<ResultError> errors) => new(false, errors.ToArray());
}"
            },
            {
                Path.Combine(projectDir, "Results", "ResultT.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Results;

public sealed class Result<TValue> : Result
{
    public TValue? Value { get; }

    private Result(TValue value) : base(true, Array.Empty<ResultError>()) => Value = value;
    private Result(IReadOnlyList<ResultError> errors) : base(false, errors) => Value = default;

    public static Result<TValue> Success(TValue value) => new(value);
    public new static Result<TValue> Failure(ResultError error) => new(new[] { error });
    public new static Result<TValue> Failure(IEnumerable<ResultError> errors) => new(errors.ToArray());
}"
            },
            {
                Path.Combine(projectDir, "Pagination", "PageRequest.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}"
            },
            {
                Path.Combine(projectDir, "Pagination", "PaginationMetadata.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PaginationMetadata(int Page, int PageSize, long TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}"
            },
            {
                Path.Combine(projectDir, "Pagination", "PageResponse.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Pagination;

public sealed record PageResponse<T>(IReadOnlyList<T> Items, PaginationMetadata Metadata)
{
    public static PageResponse<T> Create(IEnumerable<T> items, int page, int pageSize, long totalCount) =>
        new(items.ToList(), new PaginationMetadata(page, pageSize, totalCount));
}"
            },
            {
                Path.Combine(projectDir, "Extensions", "StringExtensions.cs"),
                @"namespace Shafiee.BuildingBlocks.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
    public static string TrimSafe(this string? value) => value?.Trim() ?? string.Empty;
}"
            }
        };
    }
}