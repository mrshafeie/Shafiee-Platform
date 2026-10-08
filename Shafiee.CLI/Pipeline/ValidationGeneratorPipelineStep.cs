using Shafiee.SDK.Pipeline;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


public sealed class ValidationGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        string? solutionName =
            context.Command.GetOption("solution");

        if (string.IsNullOrWhiteSpace(solutionName) ||
            solutionName.Equals(
                "validation",
                StringComparison.OrdinalIgnoreCase))
        {
            solutionName = context.Command.Target;
        }

        if (string.IsNullOrWhiteSpace(solutionName))
        {
            solutionName = "Shop";
        }

        /*
         * Validation Generator
         *
         * Default:
         * Everything is enabled.
         *
         * Optional:
         * --validation-di=false
         * --validation-tests=false
         * --validation-iranian=false
         * --validation-file=false
         * --validation-network=false
         * --validation-advanced=false
         */

        bool includeDependencyInjection =
            context.Command
                .GetOption("validation-di")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        bool includeTests =
            context.Command
                .GetOption("validation-tests")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        bool includeIranian =
            context.Command
                .GetOption("validation-iranian")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        bool includeFile =
            context.Command
                .GetOption("validation-file")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        bool includeNetwork =
            context.Command
                .GetOption("validation-network")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        bool includeAdvanced =
            context.Command
                .GetOption("validation-advanced")
                ?.Equals(
                    "false",
                    StringComparison.OrdinalIgnoreCase) != true;

        string desktopRoot =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Desktop),
                "ShafieeSolutions");

        string solutionRoot =
            Path.Combine(
                desktopRoot,
                solutionName);

        string projectDir =
            Path.Combine(
                solutionRoot,
                "src",
                "BuildingBlocks",
                $"{solutionName}.BuildingBlocks.Validation", $"{solutionName}.BuildingBlocks.Validation");

        var files =
            GetValidationFilesDictionary(
                solutionName,
                includeDependencyInjection,
                includeTests,
                includeIranian,
                includeFile,
                includeNetwork,
                includeAdvanced);

        int successCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string targetPath =
                Path.Combine(
                    projectDir,
                    file.Key);

            string? fileDirectory =
                Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(fileDirectory) &&
                !Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            File.WriteAllText(
                targetPath,
                file.Value);

            Console.WriteLine(
                $"✅ [Written] {targetPath}");

            successCount++;
        }

        Console.WriteLine(
            $"\n📊 [Validation Disk Summary] " +
            $"Successfully wrote {successCount} validation files.");

        Console.WriteLine(
            $"📁 [Validation Destination] {projectDir}");

        return Task.CompletedTask;
    }

    public Dictionary<string, string> GetValidationFilesDictionary(
        string solutionName,
        bool includeDependencyInjection = true,
        bool includeTests = true,
        bool includeIranian = true,
        bool includeFile = true,
        bool includeNetwork = true,
        bool includeAdvanced = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            solutionName);

        string baseNamespace =
            $"{solutionName}.BuildingBlocks.Validation";

        var files =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["GlobalUsings.cs"] =
                    $$"""
                    global using System;
                    global using System.Collections;
                    global using System.Collections.Concurrent;
                    global using System.Collections.Generic;
                    global using System.Collections.ObjectModel;
                    global using System.Globalization;
                    global using System.IO;
                    global using System.Linq;
                    global using System.Linq.Expressions;
                    global using System.Net;
                    global using System.Net.Mail;
                    global using System.Net.Sockets;
                    global using System.Reflection;
                    global using System.Text;
                    global using System.Text.RegularExpressions;
                    global using System.Threading;
                    global using System.Threading.Tasks;

                    global using {{baseNamespace}}.Abstractions;
                    global using {{baseNamespace}}.Builders;
                    global using {{baseNamespace}}.Exceptions;
                    global using {{baseNamespace}}.Extensions;
                    global using {{baseNamespace}}.Internal;
                    global using {{baseNamespace}}.Localization;
                    global using {{baseNamespace}}.Models;
                    global using {{baseNamespace}}.Services;
                    """,

                [$"{solutionName}.BuildingBlocks.Validation.csproj"] =
                    $$"""
                    <Project Sdk="Microsoft.NET.Sdk">

                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <Nullable>enable</Nullable>

                        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                        <EnableNETAnalyzers>true</EnableNETAnalyzers>
                        <AnalysisLevel>latest</AnalysisLevel>

                        <GenerateDocumentationFile>false</GenerateDocumentationFile>
                        <LangVersion>latest</LangVersion>
                      </PropertyGroup>

                      <ItemGroup>
                        <ProjectReference Include="..\{{solutionName}}.BuildingBlocks.Shared\{{solutionName}}.BuildingBlocks.Shared.csproj" />
                      </ItemGroup>

                      {{(includeDependencyInjection
                          ? """
                          <ItemGroup>
                            <FrameworkReference Include="Microsoft.AspNetCore.App" />
                          </ItemGroup>
                          """
                          : string.Empty)}}

                    </Project>
                    """
            };

        AddAbstractions(
            files,
            baseNamespace);

        AddModels(
            files,
            baseNamespace);

        AddInternal(
            files,
            baseNamespace);

        AddValidators(
            files,
            baseNamespace);

        AddBuilders(
            files,
            baseNamespace);

        AddGeneralRules(
            files,
            baseNamespace);

        AddStringRules(
            files,
            baseNamespace);

        AddNumericRules(
            files,
            baseNamespace);

        AddDateTimeRules(
            files,
            baseNamespace,
            includeAdvanced);

        AddComparisonRules(
            files,
            baseNamespace,
            includeAdvanced);

        AddCollectionRules(
            files,
            baseNamespace,
            includeAdvanced);

        AddEnumRules(
            files,
            baseNamespace);

        AddIdentityRules(
            files,
            baseNamespace);

        AddCustomRules(
            files,
            baseNamespace);

        AddServices(
            files,
            baseNamespace);

        AddLocalization(
            files,
            baseNamespace);

        AddExceptions(
            files,
            baseNamespace);

        AddExtensions(
            files,
            baseNamespace);

        if (includeNetwork)
        {
            AddNetworkRules(
                files,
                baseNamespace);
        }

        if (includeFile)
        {
            AddFileRules(
                files,
                baseNamespace);
        }

        if (includeIranian)
        {
            AddIranianRules(
                files,
                baseNamespace);
        }

        if (includeDependencyInjection)
        {
            AddDependencyInjection(
                files,
                baseNamespace);
        }

        if (includeTests)
        {
            AddTests(
                files,
                baseNamespace);
        }

        return files;
    }

    // ============================================================
    // Abstractions
    // ============================================================

    private static void AddAbstractions(
        Dictionary<string, string> files,
        string ns)
    {
        files["Abstractions/IValidator.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidator<in T>
            {
                ValidationResult Validate(
                    T instance,
                    ValidationOptions? options = null);

                ValueTask<ValidationResult> ValidateAsync(
                    T instance,
                    ValidationOptions? options = null,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IAsyncValidator.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IAsyncValidator<in T>
            {
                ValueTask<ValidationResult> ValidateAsync(
                    T instance,
                    ValidationOptions? options = null,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IValidationRule.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidationRule<in T>
            {
                string? PropertyName { get; }

                bool HasAsync { get; }

                IReadOnlyList<ValidationFailure> Validate(
                    T instance,
                    IValidationContext context);

                ValueTask<IReadOnlyList<ValidationFailure>>
                    ValidateAsync(
                        T instance,
                        IValidationContext context,
                        CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IPropertyValidator<in T, in TProperty>
            {
                ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context);
            }
            """;

        files["Abstractions/IAsyncPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IAsyncPropertyValidator<in T, in TProperty>
            {
                ValueTask<ValidationFailure?> ValidateAsync(
                    T instance,
                    TProperty value,
                    IValidationContext context,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IValidationContext.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidationContext
            {
                object? Instance { get; }

                object? RootInstance { get; }

                ValidationOptions Options { get; }

                string PropertyName { get; }

                string PropertyPath { get; }

                int Depth { get; }

                IServiceProvider? Services { get; }

                IReadOnlyDictionary<string, object?> Items { get; }

                IValidationContext CreateChild(
                    object? instance,
                    string propertyName,
                    string propertyPath);
            }
            """;

        files["Abstractions/IValidationService.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidationService
            {
                ValidationResult Validate<T>(
                    T instance,
                    ValidationOptions? options = null);

                ValueTask<ValidationResult> ValidateAsync<T>(
                    T instance,
                    ValidationOptions? options = null,
                    CancellationToken cancellationToken = default);

                ValidationResult Validate(
                    object instance,
                    ValidationOptions? options = null);

                ValueTask<ValidationResult> ValidateAsync(
                    object instance,
                    ValidationOptions? options = null,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IValidationMessageProvider.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidationMessageProvider
            {
                string GetMessage(
                    ValidationFailure failure);
            }
            """;

        files["Abstractions/IValidationClock.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IValidationClock
            {
                DateTimeOffset UtcNow { get; }

                DateTimeOffset LocalNow { get; }

                DateOnly Today { get; }

                TimeOnly CurrentTime { get; }
            }
            """;

        files["Abstractions/IFileValidationInfo.cs"] =
            $$"""
            namespace {{ns}}.Abstractions;

            public interface IFileValidationInfo
            {
                string? FileName { get; }

                string? Extension { get; }

                string? ContentType { get; }

                long Length { get; }
            }
            """;
    }

    // ============================================================
    // Models
    // ============================================================

    private static void AddModels(
        Dictionary<string, string> files,
        string ns)
    {
        files["Models/ValidationSeverity.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public enum ValidationSeverity
            {
                Error = 0,
                Warning = 1,
                Info = 2
            }
            """;

        files["Models/ValidationMode.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public enum ValidationMode
            {
                All = 0,
                ErrorsOnly = 1,
                ErrorsAndWarnings = 2
            }
            """;

        files["Models/ValidationOptions.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public sealed class ValidationOptions
            {
                public static ValidationOptions Default { get; } =
                    new();

                public bool StopOnFirstFailure { get; init; }

                public bool StopOnFirstPropertyFailure { get; init; }

                public bool DeduplicateErrors { get; init; } = true;

                public bool IncludeAttemptedValue { get; init; } = true;

                public bool IncludeMetadata { get; init; } = true;

                public bool ThrowOnFailure { get; init; }

                public bool AllowSynchronousAsyncExecution { get; init; }

                public bool ValidateNullInstance { get; init; }

                public ValidationMode Mode { get; init; } =
                    ValidationMode.All;

                public int MaxFailures { get; init; } = 1000;

                public int MaxDepth { get; init; } = 64;

                public ValidationOptions Clone()
                {
                    return new ValidationOptions
                    {
                        StopOnFirstFailure =
                            StopOnFirstFailure,

                        StopOnFirstPropertyFailure =
                            StopOnFirstPropertyFailure,

                        DeduplicateErrors =
                            DeduplicateErrors,

                        IncludeAttemptedValue =
                            IncludeAttemptedValue,

                        IncludeMetadata =
                            IncludeMetadata,

                        ThrowOnFailure =
                            ThrowOnFailure,

                        AllowSynchronousAsyncExecution =
                            AllowSynchronousAsyncExecution,

                        ValidateNullInstance =
                            ValidateNullInstance,

                        Mode =
                            Mode,

                        MaxFailures =
                            MaxFailures,

                        MaxDepth =
                            MaxDepth
                    };
                }
            }
            """;

        files["Models/ValidationFailure.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public sealed class ValidationFailure
            {
                public string PropertyName { get; init; } =
                    string.Empty;

                public string Path { get; init; } =
                    string.Empty;

                public string ErrorCode { get; init; } =
                    ValidationErrorCodes.Custom;

                public string ErrorMessage { get; init; } =
                    string.Empty;

                public object? AttemptedValue { get; init; }

                public ValidationSeverity Severity { get; init; } =
                    ValidationSeverity.Error;

                public IReadOnlyDictionary<string, object?> Metadata
                    { get; init; } =
                    new Dictionary<string, object?>();

                public IReadOnlyDictionary<string, object?> Arguments
                    { get; init; } =
                    new Dictionary<string, object?>();

                public static ValidationFailure Create(
                    IValidationContext context,
                    string errorCode,
                    string message)
                {
                    return new ValidationFailure
                    {
                        PropertyName =
                            context.PropertyName,

                        Path =
                            context.PropertyPath,

                        ErrorCode =
                            errorCode,

                        ErrorMessage =
                            message,

                        AttemptedValue =
                            context.Options.IncludeAttemptedValue
                                ? context.Instance
                                : null
                    };
                }
            }
            """;

        files["Models/ValidationResult.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public sealed class ValidationResult
            {
                private readonly List<ValidationFailure> _failures =
                    [];

                public IReadOnlyList<ValidationFailure> Errors =>
                    _failures;

                public bool IsValid =>
                    _failures.All(
                        x => x.Severity !=
                             ValidationSeverity.Error);

                public bool HasWarnings =>
                    _failures.Any(
                        x => x.Severity ==
                             ValidationSeverity.Warning);

                public bool HasInfos =>
                    _failures.Any(
                        x => x.Severity ==
                             ValidationSeverity.Info);

                public int Count =>
                    _failures.Count;

                internal void Add(
                    ValidationFailure failure)
                {
                    ArgumentNullException.ThrowIfNull(failure);

                    _failures.Add(failure);
                }

                internal void AddRange(
                    IEnumerable<ValidationFailure> failures)
                {
                    foreach (var failure in failures)
                    {
                        Add(failure);
                    }
                }
            }
            """;

        files["Models/ValidationPath.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public static class ValidationPath
            {
                public static string Combine(
                    string? parent,
                    string? child)
                {
                    if (string.IsNullOrWhiteSpace(parent))
                        return child ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(child))
                        return parent;

                    if (child.StartsWith("[",
                            StringComparison.Ordinal))
                    {
                        return parent + child;
                    }

                    return $"{parent}.{child}";
                }

                public static string ForIndex(
                    string path,
                    int index)
                    => $"{path}[{index}]";
            }
            """;

        files["Models/ValidationErrorCodes.cs"] =
            $$"""
            namespace {{ns}}.Models;

            public static class ValidationErrorCodes
            {
                public const string Required =
                    "validation.required";

                public const string NotNull =
                    "validation.not_null";

                public const string Null =
                    "validation.null";

                public const string NotEmpty =
                    "validation.not_empty";

                public const string Empty =
                    "validation.empty";

                public const string MinLength =
                    "validation.min_length";

                public const string MaxLength =
                    "validation.max_length";

                public const string Length =
                    "validation.length";

                public const string ExactLength =
                    "validation.exact_length";

                public const string Contains =
                    "validation.contains";

                public const string StartsWith =
                    "validation.starts_with";

                public const string EndsWith =
                    "validation.ends_with";

                public const string Regex =
                    "validation.regex";

                public const string Email =
                    "validation.email";

                public const string Url =
                    "validation.url";

                public const string Uri =
                    "validation.uri";

                public const string Positive =
                    "validation.positive";

                public const string Negative =
                    "validation.negative";

                public const string NonNegative =
                    "validation.non_negative";

                public const string NonPositive =
                    "validation.non_positive";

                public const string GreaterThan =
                    "validation.greater_than";

                public const string GreaterThanOrEqual =
                    "validation.greater_than_or_equal";

                public const string LessThan =
                    "validation.less_than";

                public const string LessThanOrEqual =
                    "validation.less_than_or_equal";

                public const string Range =
                    "validation.range";

                public const string DecimalPlaces =
                    "validation.decimal_places";

                public const string Precision =
                    "validation.precision";

                public const string Equal =
                    "validation.equal";

                public const string NotEqual =
                    "validation.not_equal";

                public const string Comparison =
                    "validation.comparison";

                public const string Past =
                    "validation.past";

                public const string Future =
                    "validation.future";

                public const string Today =
                    "validation.today";

                public const string Before =
                    "validation.before";

                public const string After =
                    "validation.after";

                public const string MinDate =
                    "validation.min_date";

                public const string MaxDate =
                    "validation.max_date";

                public const string DateRange =
                    "validation.date_range";

                public const string MinCount =
                    "validation.min_count";

                public const string MaxCount =
                    "validation.max_count";

                public const string ExactCount =
                    "validation.exact_count";

                public const string Unique =
                    "validation.unique";

                public const string Distinct =
                    "validation.distinct";

                public const string InvalidEnum =
                    "validation.invalid_enum";

                public const string InvalidEnumFlags =
                    "validation.invalid_enum_flags";

                public const string Guid =
                    "validation.guid";

                public const string Ulid =
                    "validation.ulid";

                public const string Identifier =
                    "validation.identifier";

                public const string IpAddress =
                    "validation.ip_address";

                public const string IPv4 =
                    "validation.ipv4";

                public const string IPv6 =
                    "validation.ipv6";

                public const string DomainName =
                    "validation.domain_name";

                public const string Port =
                    "validation.port";

                public const string MacAddress =
                    "validation.mac_address";

                public const string FileName =
                    "validation.file_name";

                public const string FileExtension =
                    "validation.file_extension";

                public const string FileSize =
                    "validation.file_size";

                public const string MimeType =
                    "validation.mime_type";

                public const string IranianNationalCode =
                    "validation.iranian_national_code";

                public const string IranianMobile =
                    "validation.iranian_mobile";

                public const string IranianPhone =
                    "validation.iranian_phone";

                public const string IranianPostalCode =
                    "validation.iranian_postal_code";

                public const string IranianBankCard =
                    "validation.iranian_bank_card";

                public const string IranianSheba =
                    "validation.iranian_sheba";

                public const string IranianEconomicCode =
                    "validation.iranian_economic_code";

                public const string PersianDigits =
                    "validation.persian_digits";

                public const string Custom =
                    "validation.custom";
            }
            """;
    }

    // ============================================================
    // Internal
    // ============================================================

    private static void AddInternal(
        Dictionary<string, string> files,
        string ns)
    {
        files["Internal/PropertyPathResolver.cs"] =
            $$"""
            namespace {{ns}}.Internal;

            public static class PropertyPathResolver
            {
                public static string Resolve<T, TProperty>(
                    Expression<Func<T, TProperty>> expression)
                {
                    ArgumentNullException.ThrowIfNull(
                        expression);

                    var members =
                        new Stack<string>();

                    Expression? current =
                        expression.Body;

                    while (current is UnaryExpression unary &&
                           unary.NodeType ==
                           ExpressionType.Convert)
                    {
                        current =
                            unary.Operand;
                    }

                    while (current is MemberExpression member)
                    {
                        members.Push(
                            member.Member.Name);

                        current =
                            member.Expression;
                    }

                    return string.Join(
                        ".",
                        members);
                }
            }
            """;

        files["Internal/ExpressionPropertyAccessor.cs"] =
            $$"""
            namespace {{ns}}.Internal;

            public sealed class ExpressionPropertyAccessor<T, TProperty>
            {
                private readonly Func<T, TProperty> _getter;

                public ExpressionPropertyAccessor(
                    Expression<Func<T, TProperty>> expression)
                {
                    ArgumentNullException.ThrowIfNull(
                        expression);

                    _getter =
                        expression.Compile();
                }

                public TProperty Get(
                    T instance)
                    => _getter(instance);
            }
            """;

        files["Internal/ReferenceEqualityComparer.cs"] =
            $$"""
            namespace {{ns}}.Internal;

            internal sealed class ReferenceEqualityComparer :
                IEqualityComparer<object>
            {
                public static ReferenceEqualityComparer Instance { get; } =
                    new();

                private ReferenceEqualityComparer()
                {
                }

                public new bool Equals(
                    object? x,
                    object? y)
                    => ReferenceEquals(x, y);

                public int GetHashCode(
                    object obj)
                    => RuntimeHelpers.GetHashCode(obj);
            }
            """;

        files["Internal/DateTimeValueHelper.cs"] =
            $$"""
            namespace {{ns}}.Internal;

            internal static class DateTimeValueHelper
            {
                public static bool TryGetDate(
                    object? value,
                    out DateOnly date)
                {
                    switch (value)
                    {
                        case DateOnly dateOnly:
                            date = dateOnly;
                            return true;

                        case DateTime dateTime:
                            date =
                                DateOnly.FromDateTime(
                                    dateTime);
                            return true;

                        case DateTimeOffset offset:
                            date =
                                DateOnly.FromDateTime(
                                    offset.LocalDateTime);
                            return true;

                        default:
                            date = default;
                            return false;
                    }
                }

                public static bool TryGetDateTime(
                    object? value,
                    out DateTimeOffset dateTime)
                {
                    switch (value)
                    {
                        case DateTimeOffset offset:
                            dateTime = offset;
                            return true;

                        case DateTime valueDate:
                            dateTime =
                                valueDate.Kind ==
                                DateTimeKind.Utc
                                    ? new DateTimeOffset(
                                        valueDate,
                                        TimeSpan.Zero)
                                    : new DateTimeOffset(
                                        valueDate);

                            return true;

                        case DateOnly dateOnly:
                            dateTime =
                                new DateTimeOffset(
                                    dateOnly.ToDateTime(
                                        TimeOnly.MinValue));

                            return true;

                        default:
                            dateTime = default;
                            return false;
                    }
                }
            }
            """;
    }

    // ============================================================
    // Validators
    // ============================================================

    private static void AddValidators(
        Dictionary<string, string> files,
        string ns)
    {
        files["Validators/Validator.cs"] =
            $$"""
            namespace {{ns}}.Validators;

            public abstract class Validator<T> :
                IValidator<T>
            {
                private readonly Lazy<
                    IReadOnlyList<IValidationRule<T>>> _rules;

                protected Validator()
                {
                    _rules =
                        new Lazy<
                            IReadOnlyList<IValidationRule<T>>>(
                            BuildRules,
                            LazyThreadSafetyMode.ExecutionAndPublication);
                }

                protected abstract void Configure(
                    ValidatorBuilder<T> builder);

                private IReadOnlyList<IValidationRule<T>>
                    BuildRules()
                {
                    var builder =
                        new ValidatorBuilder<T>();

                    Configure(builder);

                    return builder.Build();
                }

                public ValidationResult Validate(
                    T instance,
                    ValidationOptions? options = null)
                {
                    return ValidateAsync(
                            instance,
                            options)
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();
                }

                public async ValueTask<ValidationResult>
                    ValidateAsync(
                        T instance,
                        ValidationOptions? options = null,
                        CancellationToken cancellationToken = default)
                {
                    var effectiveOptions =
                        options?.Clone()
                        ?? ValidationOptions.Default.Clone();

                    if (instance is null &&
                        !effectiveOptions.ValidateNullInstance)
                    {
                        return new ValidationResult();
                    }

                    var context =
                        new ValidationContext(
                            instance,
                            effectiveOptions,
                            cancellationToken);

                    var result =
                        new ValidationResult();

                    foreach (var rule in _rules.Value)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var failures =
                            await rule.ValidateAsync(
                                instance,
                                context,
                                cancellationToken);

                        result.AddRange(failures);

                        if (effectiveOptions.StopOnFirstFailure &&
                            !result.IsValid)
                        {
                            break;
                        }

                        if (result.Count >=
                            effectiveOptions.MaxFailures)
                        {
                            break;
                        }
                    }

                    result =
                        result.Filter(
                            effectiveOptions.Mode);

                    if (effectiveOptions.DeduplicateErrors)
                    {
                        result =
                            result.Deduplicate();
                    }

                    if (effectiveOptions.ThrowOnFailure &&
                        !result.IsValid)
                    {
                        throw new ValidationException(
                            result);
                    }

                    return result;
                }
            }
            """;

        files["Validators/AsyncValidator.cs"] =
            $$"""
            namespace {{ns}}.Validators;

            public abstract class AsyncValidator<T> :
                Validator<T>,
                IAsyncValidator<T>
            {
            }
            """;

        files["Validators/CompositeValidator.cs"] =
            $$"""
            namespace {{ns}}.Validators;

            public sealed class CompositeValidator<T> :
                IValidator<T>
            {
                private readonly IReadOnlyList<IValidator<T>>
                    _validators;

                public CompositeValidator(
                    IEnumerable<IValidator<T>> validators)
                {
                    ArgumentNullException.ThrowIfNull(
                        validators);

                    _validators =
                        validators.ToArray();
                }

                public ValidationResult Validate(
                    T instance,
                    ValidationOptions? options = null)
                    => ValidateAsync(
                            instance,
                            options)
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();

                public async ValueTask<ValidationResult>
                    ValidateAsync(
                        T instance,
                        ValidationOptions? options = null,
                        CancellationToken cancellationToken = default)
                {
                    var result =
                        new ValidationResult();

                    foreach (var validator in _validators)
                    {
                        var current =
                            await validator.ValidateAsync(
                                instance,
                                options,
                                cancellationToken);

                        result.AddRange(
                            current.Errors);

                        if (options?.StopOnFirstFailure == true &&
                            !current.IsValid)
                        {
                            break;
                        }
                    }

                    return result;
                }
            }
            """;
    }

    // ============================================================
    // Builders
    // ============================================================

    private static void AddBuilders(
        Dictionary<string, string> files,
        string ns)
    {
        files["Builders/ValidatorBuilder.cs"] =
            $$"""
            namespace {{ns}}.Builders;

            public sealed class ValidatorBuilder<T>
            {
                private readonly List<IValidationRule<T>>
                    _rules = [];

                private bool _built;

                public RuleBuilder<T, TProperty>
                    RuleFor<TProperty>(
                        Expression<Func<T, TProperty>>
                            expression)
                {
                    EnsureNotBuilt();

                    return new RuleBuilder<T, TProperty>(
                        this,
                        expression);
                }

                public CollectionRuleBuilder<T, TItem>
                    RuleForEach<TItem>(
                        Expression<Func<T, IEnumerable<TItem>>>
                            expression)
                {
                    EnsureNotBuilt();

                    return new CollectionRuleBuilder<T, TItem>(
                        this,
                        expression);
                }

                public ValidatorBuilder<T>
                    AddRule(
                        IValidationRule<T> rule)
                {
                    EnsureNotBuilt();

                    ArgumentNullException.ThrowIfNull(
                        rule);

                    if (!_rules.Contains(rule))
                    {
                        _rules.Add(rule);
                    }

                    return this;
                }

                internal IReadOnlyList<IValidationRule<T>>
                    Build()
                {
                    _built = true;

                    return _rules.ToArray();
                }

                private void EnsureNotBuilt()
                {
                    if (_built)
                    {
                        throw new InvalidOperationException(
                            "ValidatorBuilder has already been built.");
                    }
                }
            }
            """;

        files["Builders/RuleBuilder.cs"] =
            $$"""
            namespace {{ns}}.Builders;

            public sealed class RuleBuilder<T, TProperty>
            {
                private readonly ValidatorBuilder<T> _parent;

                internal readonly
                    PropertyValidationRule<T, TProperty>
                    Rule;

                internal RuleBuilder(
                    ValidatorBuilder<T> parent,
                    Expression<Func<T, TProperty>>
                        expression)
                {
                    _parent =
                        parent ??
                        throw new ArgumentNullException(
                            nameof(parent));

                    Rule =
                        new PropertyValidationRule<T, TProperty>(
                            expression);

                    _parent.AddRule(Rule);
                }

                public RuleBuilder<T, TProperty>
                    SetValidator(
                        IPropertyValidator<T, TProperty>
                            validator)
                {
                    ArgumentNullException.ThrowIfNull(
                        validator);

                    Rule.AddValidator(
                        validator);

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    SetAsyncValidator(
                        IAsyncPropertyValidator<T, TProperty>
                            validator)
                {
                    ArgumentNullException.ThrowIfNull(
                        validator);

                    Rule.AddAsyncValidator(
                        validator);

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    SetValidator<TChild>(
                        IValidator<TChild>
                            validator)
                    {
                        ArgumentNullException.ThrowIfNull(
                            validator);

                        Rule.AddChildValidator(
                            validator);

                        return this;
                    }

                public RuleBuilder<T, TProperty>
                    WithMessage(
                        string message)
                {
                    Rule.Message =
                        message ??
                        throw new ArgumentNullException(
                            nameof(message));

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    WithErrorCode(
                        string errorCode)
                {
                    Rule.ErrorCode =
                        errorCode ??
                        throw new ArgumentNullException(
                            nameof(errorCode));

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    WithSeverity(
                        ValidationSeverity severity)
                {
                    Rule.Severity =
                        severity;

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    When(
                        Func<T, bool> condition)
                {
                    ArgumentNullException.ThrowIfNull(
                        condition);

                    Rule.AddCondition(
                        condition);

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    When(
                        Func<
                            T,
                            IValidationContext,
                            bool>
                            condition)
                {
                    ArgumentNullException.ThrowIfNull(
                        condition);

                    Rule.AddCondition(
                        condition);

                    return this;
                }

                public RuleBuilder<T, TProperty>
                    WhenAsync(
                        Func<
                            T,
                            IValidationContext,
                            CancellationToken,
                            ValueTask<bool>>
                            condition)
                {
                    ArgumentNullException.ThrowIfNull(
                        condition);

                    Rule.AddAsyncCondition(
                        condition);

                    return this;
                }
            }
            """;

        files["Builders/CollectionRuleBuilder.cs"] =
            $$"""
            namespace {{ns}}.Builders;

            public sealed class CollectionRuleBuilder<T, TItem>
            {
                private readonly
                    Internal.CollectionValidationRule<T, TItem>
                    _rule;

                internal CollectionRuleBuilder(
                    ValidatorBuilder<T> parent,
                    Expression<Func<T, IEnumerable<TItem>>>
                        expression)
                {
                    ArgumentNullException.ThrowIfNull(
                        parent);

                    _rule =
                        new Internal.CollectionValidationRule<
                            T,
                            TItem>(
                            expression);

                    parent.AddRule(_rule);
                }

                public CollectionRuleBuilder<T, TItem>
                    SetValidator(
                        IValidator<TItem> validator)
                {
                    ArgumentNullException.ThrowIfNull(
                        validator);

                    _rule.SetValidator(
                        validator);

                    return this;
                }

                public CollectionRuleBuilder<T, TItem>
                    When(
                        Func<T, bool> condition)
                {
                    _rule.SetCondition(
                        condition);

                    return this;
                }
            }
            """;
    }

    // ============================================================
    // General Rules
    // ============================================================

    private static void AddGeneralRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/General/RequiredPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.General;

            public sealed class RequiredPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (value is null)
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Required,
                            "Value is required.");
                    }

                    if (value is string text &&
                        string.IsNullOrWhiteSpace(text))
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Required,
                            "Value is required.");
                    }

                    return null;
                }
            }
            """;

        files["Rules/General/NotNullPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.General;

            public sealed class NotNullPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    return value is null
                        ? ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.NotNull,
                            "Value must not be null.")
                        : null;
                }
            }
            """;

        files["Rules/General/NotEmptyPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.General;

            public sealed class NotEmptyPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (value is null)
                        return null;

                    if (value is string text &&
                        string.IsNullOrWhiteSpace(text))
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.NotEmpty,
                            "Value must not be empty.");
                    }

                    if (value is ICollection collection &&
                        collection.Count == 0)
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.NotEmpty,
                            "Collection must not be empty.");
                    }

                    return null;
                }
            }
            """;
    }

    // ============================================================
    // String Rules
    // ============================================================

    private static void AddStringRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/String/MinLengthPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.String;

            public sealed class MinLengthPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly int _minimum;

                public MinLengthPropertyValidator(
                    int minimum)
                {
                    if (minimum < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(minimum));

                    _minimum =
                        minimum;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (value is not string text)
                        return null;

                    if (text.Length < _minimum)
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.MinLength,
                            $"Value must contain at least {_minimum} characters.");
                    }

                    return null;
                }
            }
            """;

        files["Rules/String/MaxLengthPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.String;

            public sealed class MaxLengthPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly int _maximum;

                public MaxLengthPropertyValidator(
                    int maximum)
                {
                    if (maximum < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(maximum));

                    _maximum =
                        maximum;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (value is not string text)
                        return null;

                    if (text.Length > _maximum)
                    {
                        return ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.MaxLength,
                            $"Value must contain at most {_maximum} characters.");
                    }

                    return null;
                }
            }
            """;

        files["Rules/String/EmailPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.String;

            public sealed class EmailPropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;

                    try
                    {
                        var address =
                            new MailAddress(value);

                        if (!string.Equals(
                                address.Address,
                                value,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return Failure(context);
                        }
                    }
                    catch (FormatException)
                    {
                        return Failure(context);
                    }

                    return null;
                }

                private static ValidationFailure Failure(
                    IValidationContext context)
                {
                    return ValidationFailure.Create(
                        context,
                        ValidationErrorCodes.Email,
                        "Value must be a valid email address.");
                }
            }
            """;

        files["Rules/String/RegexPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.String;

            public sealed class RegexPropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                private readonly Regex _regex;

                public RegexPropertyValidator(
                    string pattern)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                        throw new ArgumentException(
                            "Regex pattern is required.",
                            nameof(pattern));

                    _regex =
                        new Regex(
                            pattern,
                            RegexOptions.Compiled |
                            RegexOptions.CultureInvariant);
                }

                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrEmpty(value))
                        return null;

                    return _regex.IsMatch(value)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Regex,
                            "Value has an invalid format.");
                }
            }
            """;
    }

    // ============================================================
    // Numeric Rules
    // ============================================================

    private static void AddNumericRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Numeric/NumericValueHelper.cs"] =
            $$"""
            namespace {{ns}}.Rules.Numeric;

            internal static class NumericValueHelper
            {
                public static bool TryConvert(
                    object? value,
                    out decimal result)
                {
                    result =
                        default;

                    if (value is null)
                        return false;

                    try
                    {
                        result =
                            Convert.ToDecimal(
                                value,
                                CultureInfo.InvariantCulture);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            """;

        files["Rules/Numeric/PositivePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Numeric;

            public sealed class PositivePropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!NumericValueHelper.TryConvert(
                            value,
                            out var number))
                    {
                        return null;
                    }

                    return number > 0
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Positive,
                            "Value must be positive.");
                }
            }
            """;

        files["Rules/Numeric/NegativePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Numeric;

            public sealed class NegativePropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!NumericValueHelper.TryConvert(
                            value,
                            out var number))
                    {
                        return null;
                    }

                    return number < 0
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Negative,
                            "Value must be negative.");
                }
            }
            """;
    }

    // ============================================================
    // DateTime
    // ============================================================

    private static void AddDateTimeRules(
        Dictionary<string, string> files,
        string ns,
        bool enabled)
    {
        if (!enabled)
            return;

        files["Rules/DateTime/SystemValidationClock.cs"] =
            $$"""
            namespace {{ns}}.Rules.DateTime;

            public sealed class SystemValidationClock :
                IValidationClock
            {
                public DateTimeOffset UtcNow =>
                    DateTimeOffset.UtcNow;

                public DateTimeOffset LocalNow =>
                    DateTimeOffset.Now;

                public DateOnly Today =>
                    DateOnly.FromDateTime(
                        DateTime.Now);

                public TimeOnly CurrentTime =>
                    TimeOnly.FromDateTime(
                        DateTime.Now);
            }
            """;

        files["Rules/DateTime/PastPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.DateTime;

            public sealed class PastPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!Internal.DateTimeValueHelper
                            .TryGetDateTime(
                                value,
                                out var dateTime))
                    {
                        return null;
                    }

                    var now =
                        context.Services
                            ?.GetService<IValidationClock>()
                            ?.UtcNow
                        ?? DateTimeOffset.UtcNow;

                    return dateTime < now
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Past,
                            "Date must be in the past.");
                }
            }
            """;

        files["Rules/DateTime/FuturePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.DateTime;

            public sealed class FuturePropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!Internal.DateTimeValueHelper
                            .TryGetDateTime(
                                value,
                                out var dateTime))
                    {
                        return null;
                    }

                    var now =
                        context.Services
                            ?.GetService<IValidationClock>()
                            ?.UtcNow
                        ?? DateTimeOffset.UtcNow;

                    return dateTime > now
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Future,
                            "Date must be in the future.");
                }
            }
            """;

        files["Rules/DateTime/TodayPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.DateTime;

            public sealed class TodayPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!Internal.DateTimeValueHelper
                            .TryGetDate(
                                value,
                                out var date))
                    {
                        return null;
                    }

                    var today =
                        context.Services
                            ?.GetService<IValidationClock>()
                            ?.Today
                        ?? DateOnly.FromDateTime(
                            DateTime.Now);

                    return date == today
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Today,
                            "Date must be today.");
                }
            }
            """;
    }

    // ============================================================
    // Comparison
    // ============================================================

    private static void AddComparisonRules(
        Dictionary<string, string> files,
        string ns,
        bool enabled)
    {
        if (!enabled)
            return;

        files["Rules/Comparison/ComparisonOperator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Comparison;

            public enum ComparisonOperator
            {
                Equal,
                NotEqual,
                GreaterThan,
                GreaterThanOrEqual,
                LessThan,
                LessThanOrEqual
            }
            """;

        files["Rules/Comparison/PropertyComparisonValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Comparison;

            public sealed class PropertyComparisonValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly Func<T, TProperty> _otherGetter;

                private readonly
                    ComparisonOperator _operator;

                public PropertyComparisonValidator(
                    Expression<Func<T, TProperty>>
                        otherProperty,
                    ComparisonOperator
                        comparisonOperator)
                {
                    ArgumentNullException.ThrowIfNull(
                        otherProperty);

                    _otherGetter =
                        otherProperty.Compile();

                    _operator =
                        comparisonOperator;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    var other =
                        _otherGetter(instance);

                    var comparison =
                        Comparer<TProperty>.Default.Compare(
                            value,
                            other);

                    bool valid =
                        _operator switch
                        {
                            ComparisonOperator.Equal =>
                                comparison == 0,

                            ComparisonOperator.NotEqual =>
                                comparison != 0,

                            ComparisonOperator.GreaterThan =>
                                comparison > 0,

                            ComparisonOperator.GreaterThanOrEqual =>
                                comparison >= 0,

                            ComparisonOperator.LessThan =>
                                comparison < 0,

                            ComparisonOperator.LessThanOrEqual =>
                                comparison <= 0,

                            _ => false
                        };

                    return valid
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Comparison,
                            "Values do not satisfy the comparison.");
                }
            }
            """;
    }

    // ============================================================
    // Collections
    // ============================================================

    private static void AddCollectionRules(
        Dictionary<string, string> files,
        string ns,
        bool enabled)
    {
        if (!enabled)
            return;

        files["Rules/Collections/CollectionHelper.cs"] =
            $$"""
            namespace {{ns}}.Rules.Collections;

            internal static class CollectionHelper
            {
                public static bool TryGetCount(
                    object? value,
                    out int count)
                {
                    if (value is ICollection collection)
                    {
                        count =
                            collection.Count;

                        return true;
                    }

                    if (value is IEnumerable enumerable)
                    {
                        count =
                            enumerable.Cast<object?>()
                                .Count();

                        return true;
                    }

                    count =
                        0;

                    return false;
                }
            }
            """;

        files["Rules/Collections/MinCountPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Collections;

            public sealed class MinCountPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly int _minimum;

                public MinCountPropertyValidator(
                    int minimum)
                {
                    if (minimum < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(minimum));

                    _minimum =
                        minimum;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!CollectionHelper.TryGetCount(
                            value,
                            out var count))
                    {
                        return null;
                    }

                    return count >= _minimum
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.MinCount,
                            $"Collection must contain at least {_minimum} item(s).");
                }
            }
            """;

        files["Rules/Collections/MaxCountPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Collections;

            public sealed class MaxCountPropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly int _maximum;

                public MaxCountPropertyValidator(
                    int maximum)
                {
                    if (maximum < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(maximum));

                    _maximum =
                        maximum;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    if (!CollectionHelper.TryGetCount(
                            value,
                            out var count))
                    {
                        return null;
                    }

                    return count <= _maximum
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.MaxCount,
                            $"Collection must contain at most {_maximum} item(s).");
                }
            }
            """;
    }

    // ============================================================
    // Enum
    // ============================================================

    private static void AddEnumRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Enum/DefinedEnumPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Enum;

            public sealed class DefinedEnumPropertyValidator<T, TEnum> :
                IPropertyValidator<T, TEnum>
                where TEnum : struct, Enum
            {
                public ValidationFailure? Validate(
                    T instance,
                    TEnum value,
                    IValidationContext context)
                {
                    return Enum.IsDefined(
                            typeof(TEnum),
                            value)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.InvalidEnum,
                            "Value is not a defined enum value.");
                }
            }
            """;
    }

    // ============================================================
    // Identity
    // ============================================================

    private static void AddIdentityRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Identity/GuidPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Identity;

            public sealed class GuidPropertyValidator<T> :
                IPropertyValidator<T, Guid>
            {
                public ValidationFailure? Validate(
                    T instance,
                    Guid value,
                    IValidationContext context)
                {
                    return value == Guid.Empty
                        ? ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Guid,
                            "GUID must not be empty.")
                        : null;
                }
            }
            """;

        files["Rules/Identity/UlidPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Identity;

            public sealed class UlidPropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                private const string Alphabet =
                    "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;

                    string normalized =
                        value.ToUpperInvariant();

                    if (normalized.Length != 26)
                        return Failure(context);

                    if (normalized[0] > '7')
                        return Failure(context);

                    foreach (char character in normalized)
                    {
                        if (!Alphabet.Contains(character))
                            return Failure(context);
                    }

                    return null;
                }

                private static ValidationFailure Failure(
                    IValidationContext context)
                    => ValidationFailure.Create(
                        context,
                        ValidationErrorCodes.Ulid,
                        "Value must be a valid ULID.");
            }
            """;
    }

    // ============================================================
    // Network
    // ============================================================

    private static void AddNetworkRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Network/IpAddressPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Network;

            public sealed class IpAddressPropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;

                    return IPAddress.TryParse(
                            value,
                            out _)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.IpAddress,
                            "Value must be a valid IP address.");
                }
            }
            """;

        files["Rules/Network/PortPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Network;

            public sealed class PortPropertyValidator<T> :
                IPropertyValidator<T, int>
            {
                public ValidationFailure? Validate(
                    T instance,
                    int value,
                    IValidationContext context)
                {
                    return value is >= 1 and <= 65535
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.Port,
                            "Port must be between 1 and 65535.");
                }
            }
            """;
    }

    // ============================================================
    // File
    // ============================================================

    private static void AddFileRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/File/FileSizePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.File;

            public sealed class FileSizePropertyValidator<T> :
                IPropertyValidator<T, IFileValidationInfo?>
            {
                private readonly long _maximum;

                public FileSizePropertyValidator(
                    long maximum)
                {
                    if (maximum < 0)
                        throw new ArgumentOutOfRangeException(
                            nameof(maximum));

                    _maximum =
                        maximum;
                }

                public ValidationFailure? Validate(
                    T instance,
                    IFileValidationInfo? value,
                    IValidationContext context)
                {
                    if (value is null)
                        return null;

                    return value.Length <= _maximum
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.FileSize,
                            $"File size must not exceed {_maximum} bytes.");
                }
            }
            """;

        files["Rules/File/FileExtensionPropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.File;

            public sealed class FileExtensionPropertyValidator<T> :
                IPropertyValidator<T, IFileValidationInfo?>
            {
                private readonly HashSet<string> _extensions;

                public FileExtensionPropertyValidator(
                    IEnumerable<string> extensions)
                {
                    _extensions =
                        extensions
                            .Select(
                                Normalize)
                            .ToHashSet(
                                StringComparer.OrdinalIgnoreCase);
                }

                public ValidationFailure? Validate(
                    T instance,
                    IFileValidationInfo? value,
                    IValidationContext context)
                {
                    if (value is null)
                        return null;

                    var extension =
                        Normalize(
                            value.Extension);

                    return _extensions.Contains(
                            extension)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.FileExtension,
                            "File extension is not allowed.");
                }

                private static string Normalize(
                    string? extension)
                {
                    if (string.IsNullOrWhiteSpace(
                            extension))
                    {
                        return string.Empty;
                    }

                    return extension.StartsWith(".")
                        ? extension
                        : "." + extension;
                }
            }
            """;
    }

    // ============================================================
    // Iranian
    // ============================================================

    private static void AddIranianRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Iranian/PersianDigitHelper.cs"] =
            $$"""
            namespace {{ns}}.Rules.Iranian;

            internal static class PersianDigitHelper
            {
                public static string ToEnglishDigits(
                    string value)
                {
                    return value
                        .Replace('۰', '0')
                        .Replace('۱', '1')
                        .Replace('۲', '2')
                        .Replace('۳', '3')
                        .Replace('۴', '4')
                        .Replace('۵', '5')
                        .Replace('۶', '6')
                        .Replace('۷', '7')
                        .Replace('۸', '8')
                        .Replace('۹', '9')
                        .Replace('٠', '0')
                        .Replace('١', '1')
                        .Replace('٢', '2')
                        .Replace('٣', '3')
                        .Replace('٤', '4')
                        .Replace('٥', '5')
                        .Replace('٦', '6')
                        .Replace('٧', '7')
                        .Replace('٨', '8')
                        .Replace('٩', '9');
                }
            }
            """;

        files["Rules/Iranian/IranianNationalCodePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Iranian;

            public sealed class IranianNationalCodePropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;

                    string code =
                        PersianDigitHelper
                            .ToEnglishDigits(value)
                            .Trim()
                            .Replace("-", "");

                    if (code.Length != 10 ||
                        !code.All(char.IsDigit) ||
                        code.Distinct().Count() == 1)
                    {
                        return Failure(context);
                    }

                    int check =
                        code[9] - '0';

                    int sum = 0;

                    for (int i = 0; i < 9; i++)
                    {
                        sum +=
                            (code[i] - '0') *
                            (10 - i);
                    }

                    int remainder =
                        sum % 11;

                    bool valid =
                        remainder < 2
                            ? check == remainder
                            : check == 11 - remainder;

                    return valid
                        ? null
                        : Failure(context);
                }

                private static ValidationFailure Failure(
                    IValidationContext context)
                    => ValidationFailure.Create(
                        context,
                        ValidationErrorCodes.IranianNationalCode,
                        "Iranian national code is invalid.");
            }
            """;

        files["Rules/Iranian/IranianMobilePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Iranian;

            public sealed class IranianMobilePropertyValidator<T> :
                IPropertyValidator<T, string?>
            {
                public ValidationFailure? Validate(
                    T instance,
                    string? value,
                    IValidationContext context)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;

                    string mobile =
                        PersianDigitHelper
                            .ToEnglishDigits(value)
                            .Replace(" ", "")
                            .Replace("-", "");

                    if (mobile.StartsWith(
                            "+98",
                            StringComparison.Ordinal))
                    {
                        mobile =
                            "0" + mobile[3..];
                    }
                    else if (mobile.StartsWith(
                                 "0098",
                                 StringComparison.Ordinal))
                    {
                        mobile =
                            "0" + mobile[4..];
                    }

                    return mobile.Length == 11 &&
                           mobile.StartsWith(
                               "09",
                               StringComparison.Ordinal) &&
                           mobile.All(char.IsDigit)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            ValidationErrorCodes.IranianMobile,
                            "Iranian mobile number is invalid.");
                }
            }
            """;
    }

    // ============================================================
    // Custom
    // ============================================================

    private static void AddCustomRules(
        Dictionary<string, string> files,
        string ns)
    {
        files["Rules/Custom/PredicatePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Custom;

            public sealed class PredicatePropertyValidator<T, TProperty> :
                IPropertyValidator<T, TProperty>
            {
                private readonly
                    Func<T, TProperty, bool>
                    _predicate;

                private readonly string _message;
                private readonly string _errorCode;

                public PredicatePropertyValidator(
                    Func<T, TProperty, bool> predicate,
                    string message,
                    string errorCode =
                        ValidationErrorCodes.Custom)
                {
                    _predicate =
                        predicate ??
                        throw new ArgumentNullException(
                            nameof(predicate));

                    _message =
                        message;

                    _errorCode =
                        errorCode;
                }

                public ValidationFailure? Validate(
                    T instance,
                    TProperty value,
                    IValidationContext context)
                {
                    return _predicate(
                            instance,
                            value)
                        ? null
                        : ValidationFailure.Create(
                            context,
                            _errorCode,
                            _message);
                }
            }
            """;

        files["Rules/Custom/AsyncPredicatePropertyValidator.cs"] =
            $$"""
            namespace {{ns}}.Rules.Custom;

            public sealed class AsyncPredicatePropertyValidator<T, TProperty> :
                IAsyncPropertyValidator<T, TProperty>
            {
                private readonly Func<
                    T,
                    TProperty,
                    IValidationContext,
                    CancellationToken,
                    ValueTask<bool>> _predicate;

                private readonly string _message;
                private readonly string _errorCode;

                public AsyncPredicatePropertyValidator(
                    Func<
                        T,
                        TProperty,
                        IValidationContext,
                        CancellationToken,
                        ValueTask<bool>>
                        predicate,
                    string message,
                    string errorCode =
                        ValidationErrorCodes.Custom)
                {
                    _predicate =
                        predicate ??
                        throw new ArgumentNullException(
                            nameof(predicate));

                    _message =
                        message;

                    _errorCode =
                        errorCode;
                }

                public async ValueTask<
                    ValidationFailure?>
                    ValidateAsync(
                        T instance,
                        TProperty value,
                        IValidationContext context,
                        CancellationToken cancellationToken)
                {
                    bool valid =
                        await _predicate(
                            instance,
                            value,
                            context,
                            cancellationToken);

                    return valid
                        ? null
                        : ValidationFailure.Create(
                            context,
                            _errorCode,
                            _message);
                }
            }
            """;
    }

    // ============================================================
    // Services
    // ============================================================

    private static void AddServices(
        Dictionary<string, string> files,
        string ns)
    {
        files["Services/ValidationContext.cs"] =
            $$"""
            namespace {{ns}}.Services;

            public sealed class ValidationContext :
                IValidationContext
            {
                private readonly
                    Dictionary<string, object?>
                    _items;

                internal ValidationContext(
                    object? instance,
                    ValidationOptions options,
                    CancellationToken cancellationToken,
                    object? rootInstance = null,
                    string propertyName = "",
                    string propertyPath = "",
                    int depth = 0,
                    IServiceProvider? services = null,
                    Dictionary<string, object?>? items = null)
                {
                    Instance =
                        instance;

                    RootInstance =
                        rootInstance ?? instance;

                    Options =
                        options;

                    CancellationToken =
                        cancellationToken;

                    PropertyName =
                        propertyName;

                    PropertyPath =
                        propertyPath;

                    Depth =
                        depth;

                    Services =
                        services;

                    _items =
                        items ??
                        new Dictionary<string, object?>(
                            StringComparer.Ordinal);
                }

                public object? Instance { get; }

                public object? RootInstance { get; }

                public ValidationOptions Options { get; }

                public string PropertyName { get; }

                public string PropertyPath { get; }

                public int Depth { get; }

                public IServiceProvider? Services { get; }

                public CancellationToken CancellationToken { get; }

                public IReadOnlyDictionary<string, object?> Items =>
                    _items;

                public IValidationContext CreateChild(
                    object? instance,
                    string propertyName,
                    string propertyPath)
                {
                    return new ValidationContext(
                        instance,
                        Options,
                        CancellationToken,
                        RootInstance,
                        propertyName,
                        propertyPath,
                        Depth + 1,
                        Services,
                        _items);
                }
            }
            """;

        files["Services/ValidationService.cs"] =
            $$"""
            namespace {{ns}}.Services;

            public sealed class ValidationService :
                IValidationService
            {
                private readonly IServiceProvider _services;

                public ValidationService(
                    IServiceProvider services)
                {
                    _services =
                        services;
                }

                public ValidationResult Validate<T>(
                    T instance,
                    ValidationOptions? options = null)
                {
                    return ValidateAsync(
                            instance,
                            options)
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();
                }

                public ValueTask<ValidationResult>
                    ValidateAsync<T>(
                        T instance,
                        ValidationOptions? options = null,
                        CancellationToken cancellationToken = default)
                {
                    var validator =
                        _services.GetService<
                            IValidator<T>>();

                    if (validator is null)
                    {
                        throw new InvalidOperationException(
                            $"No validator registered for '{typeof(T).FullName}'.");
                    }

                    return validator.ValidateAsync(
                        instance,
                        options,
                        cancellationToken);
                }

                public ValidationResult Validate(
                    object instance,
                    ValidationOptions? options = null)
                {
                    return ValidateAsync(
                            instance,
                            options)
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();
                }

                public async ValueTask<ValidationResult>
                    ValidateAsync(
                        object instance,
                        ValidationOptions? options = null,
                        CancellationToken cancellationToken = default)
                {
                    ArgumentNullException.ThrowIfNull(
                        instance);

                    var validatorType =
                        typeof(IValidator<>)
                            .MakeGenericType(
                                instance.GetType());

                    var validator =
                        _services.GetService(
                            validatorType);

                    if (validator is null)
                    {
                        throw new InvalidOperationException(
                            $"No validator registered for '{instance.GetType().FullName}'.");
                    }

                    return await (
                        (dynamic)validator)
                        .ValidateAsync(
                            (dynamic)instance,
                            options,
                            cancellationToken);
                }
            }
            """;

        files["Services/ValidatorRegistry.cs"] =
            $$"""
            namespace {{ns}}.Services;

            public sealed class ValidatorRegistry
            {
                private readonly IServiceProvider _services;

                public ValidatorRegistry(
                    IServiceProvider services)
                {
                    _services =
                        services;
                }

                public IValidator<T> Get<T>()
                {
                    var validator =
                        _services.GetService<
                            IValidator<T>>();

                    if (validator is null)
                    {
                        throw new InvalidOperationException(
                            $"No validator registered for '{typeof(T).FullName}'.");
                    }

                    return validator;
                }
            }
            """;
    }

    // ============================================================
    // Localization
    // ============================================================

    private static void AddLocalization(
        Dictionary<string, string> files,
        string ns)
    {
        files[
            "Localization/DefaultValidationMessageProvider.cs"] =
            $$"""
            namespace {{ns}}.Localization;

            public sealed class DefaultValidationMessageProvider :
                IValidationMessageProvider
            {
                public string GetMessage(
                    ValidationFailure failure)
                    => failure.ErrorMessage;
            }
            """;

        files[
            "Localization/ValidationMessageFormatter.cs"] =
            $$"""
            namespace {{ns}}.Localization;

            public static class ValidationMessageFormatter
            {
                public static string Format(
                    string message,
                    IReadOnlyDictionary<string, object?>
                        arguments)
                {
                    if (string.IsNullOrEmpty(message) ||
                        arguments.Count == 0)
                    {
                        return message;
                    }

                    foreach (var argument in arguments)
                    {
                        message =
                            message.Replace(
                                "{" +
                                argument.Key +
                                "}",
                                Convert.ToString(
                                    argument.Value,
                                    CultureInfo.InvariantCulture)
                                ?? string.Empty,
                                StringComparison.Ordinal);
                    }

                    return message;
                }
            }
            """;
    }

    // ============================================================
    // Exceptions
    // ============================================================

    private static void AddExceptions(
        Dictionary<string, string> files,
        string ns)
    {
        files["Exceptions/ValidationException.cs"] =
            $$"""
            namespace {{ns}}.Exceptions;

            public sealed class ValidationException :
                Exception
            {
                public ValidationResult Result { get; }

                public IReadOnlyList<ValidationFailure>
                    Errors =>
                    Result.Errors;

                public ValidationException(
                    ValidationResult result)
                    : base("Validation failed.")
                {
                    Result =
                        result ??
                        throw new ArgumentNullException(
                            nameof(result));
                }
            }
            """;

        files["Exceptions/ValidatorNotFoundException.cs"] =
            $$"""
            namespace {{ns}}.Exceptions;

            public sealed class ValidatorNotFoundException :
                InvalidOperationException
            {
                public Type ModelType { get; }

                public ValidatorNotFoundException(
                    Type modelType)
                    : base(
                        $"No validator was registered for '{modelType.FullName}'.")
                {
                    ModelType =
                        modelType;
                }
            }
            """;
    }

    // ============================================================
    // Extensions
    // ============================================================

    private static void AddExtensions(
        Dictionary<string, string> files,
        string ns)
    {
        files["Extensions/ValidationResultExtensions.cs"] =
            $$"""
            namespace {{ns}}.Extensions;

            public static class ValidationResultExtensions
            {
                public static ValidationResult Deduplicate(
                    this ValidationResult result)
                {
                    ArgumentNullException.ThrowIfNull(
                        result);

                    var output =
                        new ValidationResult();

                    var unique =
                        new HashSet<string>(
                            StringComparer.Ordinal);

                    foreach (var failure in result.Errors)
                    {
                        string key =
                            $"{failure.Path}|{failure.ErrorCode}|{failure.ErrorMessage}";

                        if (unique.Add(key))
                        {
                            output.Add(failure);
                        }
                    }

                    return output;
                }

                public static ValidationResult Filter(
                    this ValidationResult result,
                    ValidationMode mode)
                {
                    if (mode == ValidationMode.All)
                        return result;

                    var output =
                        new ValidationResult();

                    foreach (var failure in result.Errors)
                    {
                        if (mode ==
                            ValidationMode.ErrorsOnly &&
                            failure.Severity !=
                            ValidationSeverity.Error)
                        {
                            continue;
                        }

                        if (mode ==
                            ValidationMode.ErrorsAndWarnings &&
                            failure.Severity ==
                            ValidationSeverity.Info)
                        {
                            continue;
                        }

                        output.Add(failure);
                    }

                    return output;
                }

                public static void ThrowIfInvalid(
                    this ValidationResult result)
                {
                    ArgumentNullException.ThrowIfNull(
                        result);

                    if (!result.IsValid)
                    {
                        throw new ValidationException(
                            result);
                    }
                }
            }
            """;

        files["Extensions/ValidationExtensions.cs"] =
            $$"""
            namespace {{ns}}.Extensions;

            public static class ValidationExtensions
            {
                public static ValidationResult Validate<T>(
                    this T instance,
                    IValidator<T> validator,
                    ValidationOptions? options = null)
                {
                    ArgumentNullException.ThrowIfNull(
                        validator);

                    return validator.Validate(
                        instance,
                        options);
                }

                public static ValueTask<ValidationResult>
                    ValidateAsync<T>(
                        this T instance,
                        IValidator<T> validator,
                        ValidationOptions? options = null,
                        CancellationToken cancellationToken = default)
                {
                    ArgumentNullException.ThrowIfNull(
                        validator);

                    return validator.ValidateAsync(
                        instance,
                        options,
                        cancellationToken);
                }
            }
            """;
    }

    // ============================================================
    // Dependency Injection
    // ============================================================

    private static void AddDependencyInjection(
        Dictionary<string, string> files,
        string ns)
    {
        files[
            "DependencyInjection/ValidationServiceCollectionExtensions.cs"] =
            $$"""
            namespace Microsoft.Extensions.DependencyInjection;

            using System.Reflection;

            using {{ns}}.Abstractions;
            using {{ns}}.Services;
            using {{ns}}.Rules.DateTime;

            public static class ValidationServiceCollectionExtensions
            {
                public static IServiceCollection
                    AddShafieeValidation(
                        this IServiceCollection services)
                {
                    ArgumentNullException.ThrowIfNull(
                        services);

                    services.AddSingleton<
                        IValidationClock,
                        SystemValidationClock>();

                    services.AddSingleton<
                        IValidationMessageProvider,
                        {{ns}}.Localization.DefaultValidationMessageProvider>();

                    services.AddScoped<
                        IValidationService,
                        ValidationService>();

                    services.AddScoped<
                        ValidatorRegistry>();

                    return services;
                }

                public static IServiceCollection
                    AddShafieeValidation(
                        this IServiceCollection services,
                        params Assembly[] assemblies)
                {
                    ArgumentNullException.ThrowIfNull(
                        assemblies);

                    services.AddShafieeValidation();

                    foreach (var assembly in assemblies)
                    {
                        RegisterValidators(
                            services,
                            assembly);
                    }

                    return services;
                }

                private static void RegisterValidators(
                    IServiceCollection services,
                    Assembly assembly)
                {
                    ArgumentNullException.ThrowIfNull(
                        assembly);

                    var validatorInterface =
                        typeof(IValidator<>);

                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsClass ||
                            type.IsAbstract ||
                            type.IsGenericTypeDefinition)
                        {
                            continue;
                        }

                        foreach (var @interface
                                 in type.GetInterfaces())
                        {
                            if (!@interface.IsGenericType)
                                continue;

                            if (@interface.GetGenericTypeDefinition()
                                != validatorInterface)
                            {
                                continue;
                            }

                            services.AddScoped(
                                @interface,
                                type);
                        }
                    }
                }
            }
            """;
    }

    // ============================================================
    // Tests
    // ============================================================

    private static void AddTests(
        Dictionary<string, string> files,
        string ns)
    {
        files["Tests/ValidationSmokeTests.cs"] =
            $$"""
            namespace {{ns}}.Tests;

            public sealed class ValidationSmokeTests
            {
                public void GeneratedSuccessfully()
                {
                    Assert.True(true);
                }
            }

            internal static class Assert
            {
                public static void True(
                    bool condition)
                {
                    if (!condition)
                    {
                        throw new InvalidOperationException(
                            "Assertion failed.");
                    }
                }
            }
            """;
    }
}