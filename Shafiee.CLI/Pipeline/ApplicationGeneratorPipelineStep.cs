using Shafiee.SDK.Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


public sealed class ApplicationGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(
        PipelineContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        var solutionName = ResolveSolutionName(context);
        var options = ResolveOptions(context);

        var solutionRoot = ResolveSolutionRoot(solutionName);

        var projectDirectory = Path.Combine(
            solutionRoot,
            "src",
            "BuildingBlocks",
            $"{solutionName}.BuildingBlocks.Application");

        var files = GetApplicationFilesDictionary(
            solutionName,
            options);

        var writtenCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetPath = Path.Combine(
                projectDirectory,
                file.Key);

            var directory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(targetPath, file.Value);

            Console.WriteLine($"[Application] Written: {targetPath}");

            writtenCount++;
        }

        Console.WriteLine(
            $"[Application] Generated {writtenCount} file(s) for '{solutionName}'.");

        return Task.CompletedTask;
    }

    private static string ResolveSolutionName(PipelineContext context)
    {
        var solutionName =
            context.Command.GetOption("solution");

        if (string.IsNullOrWhiteSpace(solutionName) ||
            solutionName.Equals(
                "application",
                StringComparison.OrdinalIgnoreCase))
        {
            solutionName = context.Command.Target;
        }

        return string.IsNullOrWhiteSpace(solutionName)
            ? "Shop"
            : solutionName.Trim();
    }

    private static string ResolveSolutionRoot(string solutionName)
    {
        var desktopRoot = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop),
            "ShafieeSolutions");

        return Path.Combine(
            desktopRoot,
            solutionName);
    }

    private static ApplicationGenerationOptions ResolveOptions(
        PipelineContext context)
    {
        return new ApplicationGenerationOptions
        {
            IncludeCqrs = IsEnabled(context, "cqrs", true),

            IncludeBehaviors =
                IsEnabled(context, "behaviors", true),

            IncludeLoggingBehavior =
                IsEnabled(context, "behavior-logging", true),

            IncludePerformanceBehavior =
                IsEnabled(context, "behavior-performance", true),

            IncludeExceptionBehavior =
                IsEnabled(context, "behavior-exception", true),

            IncludeValidationBehavior =
                IsEnabled(context, "behavior-validation", false),

            IncludeAuthorizationBehavior =
                IsEnabled(context, "behavior-authorization", false),

            IncludeTransactionBehavior =
                IsEnabled(context, "behavior-transaction", false),

            IncludeCachingBehavior =
                IsEnabled(context, "behavior-caching", false),

            IncludeRetryBehavior =
                IsEnabled(context, "behavior-retry", false),

            IncludeIdempotencyBehavior =
                IsEnabled(context, "behavior-idempotency", false),

            IncludeAuditBehavior =
                IsEnabled(context, "behavior-audit", false),

            IncludeRateLimitBehavior =
                IsEnabled(context, "behavior-ratelimit", false)
        };
    }

    private static bool IsEnabled(
        PipelineContext context,
        string optionName,
        bool defaultValue)
    {
        var value = context.Command.GetOption(optionName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return !value.Equals(
            "false",
            StringComparison.OrdinalIgnoreCase);
    }

    public Dictionary<string, string> GetApplicationFilesDictionary(
        string solutionName,
        ApplicationGenerationOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionName);
        ArgumentNullException.ThrowIfNull(options);

        var baseNamespace =
            $"{solutionName}.BuildingBlocks.Application";

        var files =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        AddCoreFiles(
            files,
            baseNamespace);

        if (options.IncludeCqrs)
        {
            AddCqrsFiles(
                files,
                baseNamespace);

            AddDispatcherFiles(
                files,
                baseNamespace);

            if (options.IncludeBehaviors)
            {
                AddBehaviorFiles(
                    files,
                    baseNamespace,
                    options);
            }

            AddApplicationRegistrationFile(
                files,
                baseNamespace,
                options);
        }
        else
        {
            AddMinimalApplicationRegistrationFile(
                files,
                baseNamespace);
        }

        return files;
    }

    private static void AddCoreFiles(
        Dictionary<string, string> files,
        string baseNamespace)
    {
        files["GlobalUsings.cs"] =
            """
            global using System;
            global using System.Collections.Generic;
            global using System.Diagnostics;
            global using System.Linq;
            global using System.Reflection;
            global using System.Threading;
            global using System.Threading.Tasks;

            global using Microsoft.Extensions.DependencyInjection;
            global using Microsoft.Extensions.Logging;
            """;

        files["Abstractions/IApplicationService.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Marker abstraction for application services.
            /// </summary>
            public interface IApplicationService
            {
            }
            """;

        files["Abstractions/IApplicationModule.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Describes an application module.
            /// </summary>
            public interface IApplicationModule
            {
                string Name { get; }

                string Version { get; }
            }
            """;

        /*
         * IMPORTANT:
         *
         * IClock
         * ICurrentUser
         * IExecutionContext
         * IIdGenerator
         *
         * intentionally DO NOT live here.
         *
         * They belong to BuildingBlocks.Shared.
         */
        files["Abstractions/ITransactionManager.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Application-level transaction abstraction.
            ///
            /// The concrete implementation belongs to Persistence/Infrastructure.
            /// </summary>
            public interface ITransactionManager
            {
                ValueTask BeginAsync(
                    CancellationToken cancellationToken = default);

                ValueTask CommitAsync(
                    CancellationToken cancellationToken = default);

                ValueTask RollbackAsync(
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/ICacheService.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Application cache abstraction.
            ///
            /// Redis, memory cache or another provider belongs to Infrastructure.
            /// </summary>
            public interface ICacheService
            {
                ValueTask<T?> GetAsync<T>(
                    string key,
                    CancellationToken cancellationToken = default);

                ValueTask SetAsync<T>(
                    string key,
                    T value,
                    TimeSpan? expiration = null,
                    CancellationToken cancellationToken = default);

                ValueTask RemoveAsync(
                    string key,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IAuthorizationService.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Application authorization abstraction.
            ///
            /// Actual authentication/authorization integration belongs
            /// to Security/Identity infrastructure.
            /// </summary>
            public interface IAuthorizationService
            {
                ValueTask<bool> AuthorizeAsync(
                    string? policy,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IIdempotencyStore.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Stores request execution results for idempotent operations.
            /// </summary>
            public interface IIdempotencyStore
            {
                ValueTask<bool> ExistsAsync(
                    string key,
                    CancellationToken cancellationToken = default);

                ValueTask StoreAsync(
                    string key,
                    TimeSpan? expiration = null,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IAuditWriter.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Application audit abstraction.
            /// </summary>
            public interface IAuditWriter
            {
                ValueTask WriteAsync(
                    string action,
                    object? data = null,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IRateLimitService.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Application-level rate limiting abstraction.
            /// </summary>
            public interface IRateLimitService
            {
                ValueTask<bool> IsAllowedAsync(
                    string key,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Abstractions/IRetryPolicy.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            /// <summary>
            /// Determines whether an operation should be retried.
            ///
            /// The implementation decides which failures are transient.
            /// </summary>
            public interface IRetryPolicy
            {
                ValueTask<T> ExecuteAsync<T>(
                    Func<CancellationToken, ValueTask<T>> operation,
                    CancellationToken cancellationToken = default);
            }
            """;
    }

    private static void AddCqrsFiles(
        Dictionary<string, string> files,
        string baseNamespace)
    {
        files["CQRS/Common/Unit.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Common;

            /// <summary>
            /// Represents a void application result.
            /// </summary>
            public readonly struct Unit : IEquatable<Unit>
            {
                public static readonly Unit Value = new();

                public bool Equals(Unit other)
                    => true;

                public override bool Equals(object? obj)
                    => obj is Unit;

                public override int GetHashCode()
                    => 0;

                public override string ToString()
                    => "()";

                public static bool operator ==(
                    Unit left,
                    Unit right)
                    => true;

                public static bool operator !=(
                    Unit left,
                    Unit right)
                    => false;
            }
            """;

        files["CQRS/Commands/ICommand.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Commands;

            public interface ICommand
            {
            }
            """;

        files["CQRS/Commands/ICommand{TResult}.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Commands;

            public interface ICommand<out TResult> : ICommand
            {
            }
            """;

        files["CQRS/Commands/ICommandHandler.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Commands;

            public interface ICommandHandler<in TCommand>
                where TCommand : ICommand
            {
                ValueTask HandleAsync(
                    TCommand command,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["CQRS/Commands/ICommandHandler{TCommand,TResult}.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Commands;

            public interface ICommandHandler<
                in TCommand,
                TResult>
                where TCommand : ICommand<TResult>
            {
                ValueTask<TResult> HandleAsync(
                    TCommand command,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["CQRS/Queries/IQuery.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Queries;

            public interface IQuery
            {
            }
            """;

        files["CQRS/Queries/IQuery{TResult}.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Queries;

            public interface IQuery<out TResult> : IQuery
            {
            }
            """;

        files["CQRS/Queries/IQueryHandler.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Queries;

            public interface IQueryHandler<in TQuery>
                where TQuery : IQuery
            {
                ValueTask HandleAsync(
                    TQuery query,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["CQRS/Queries/IQueryHandler{TQuery,TResult}.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Queries;

            public interface IQueryHandler<
                in TQuery,
                TResult>
                where TQuery : IQuery<TResult>
            {
                ValueTask<TResult> HandleAsync(
                    TQuery query,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["CQRS/Events/IEvent.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Events;

            public interface IEvent
            {
                DateTime OccurredOn { get; }
            }
            """;

        files["CQRS/Events/IEventHandler.cs"] =
            $$"""
            namespace {{baseNamespace}}.CQRS.Events;

            public interface IEventHandler<in TEvent>
                where TEvent : IEvent
            {
                ValueTask HandleAsync(
                    TEvent @event,
                    CancellationToken cancellationToken = default);
            }
            """;
    }

    private static void AddDispatcherFiles(
        Dictionary<string, string> files,
        string baseNamespace)
    {
        files["Abstractions/ICommandDispatcher.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            using {{baseNamespace}}.CQRS.Commands;

            public interface ICommandDispatcher
            {
                ValueTask SendAsync<TCommand>(
                    TCommand command,
                    CancellationToken cancellationToken = default)
                    where TCommand : ICommand;

                ValueTask<TResult> SendAsync<TCommand, TResult>(
                    TCommand command,
                    CancellationToken cancellationToken = default)
                    where TCommand : ICommand<TResult>;
            }
            """;

        files["Abstractions/IQueryDispatcher.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            using {{baseNamespace}}.CQRS.Queries;

            public interface IQueryDispatcher
            {
                ValueTask SendAsync<TQuery>(
                    TQuery query,
                    CancellationToken cancellationToken = default)
                    where TQuery : IQuery;

                ValueTask<TResult> SendAsync<TQuery, TResult>(
                    TQuery query,
                    CancellationToken cancellationToken = default)
                    where TQuery : IQuery<TResult>;
            }
            """;

        files["Abstractions/IEventPublisher.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            using {{baseNamespace}}.CQRS.Events;

            public interface IEventPublisher
            {
                ValueTask PublishAsync<TEvent>(
                    TEvent @event,
                    CancellationToken cancellationToken = default)
                    where TEvent : IEvent;
            }
            """;

        files["Abstractions/IApplicationDispatcher.cs"] =
            $$"""
            namespace {{baseNamespace}}.Abstractions;

            public interface IApplicationDispatcher :
                ICommandDispatcher,
                IQueryDispatcher,
                IEventPublisher
            {
            }
            """;

        files["Pipelines/IPipelineBehavior.cs"] =
            $$"""
            namespace {{baseNamespace}}.Pipelines;

            public interface IPipelineBehavior<in TRequest, TResponse>
            {
                ValueTask<TResponse> HandleAsync(
                    TRequest request,
                    Func<ValueTask<TResponse>> next,
                    CancellationToken cancellationToken = default);
            }
            """;

        files["Pipelines/Attributes/AuthorizeAttribute.cs"] =
            $$"""
            namespace {{baseNamespace}}.Pipelines.Attributes;

            [AttributeUsage(
                AttributeTargets.Class,
                AllowMultiple = true,
                Inherited = true)]
            public sealed class AuthorizeAttribute : Attribute
            {
                public AuthorizeAttribute()
                {
                }

                public AuthorizeAttribute(string policy)
                {
                    Policy = policy;
                }

                public string? Roles { get; init; }

                public string? Policy { get; init; }
            }
            """;

        files["Pipelines/Attributes/CacheableAttribute.cs"] =
            $$"""
            namespace {{baseNamespace}}.Pipelines.Attributes;

            [AttributeUsage(
                AttributeTargets.Class,
                AllowMultiple = false,
                Inherited = true)]
            public sealed class CacheableAttribute : Attribute
            {
                public CacheableAttribute(
                    int durationInSeconds = 60)
                {
                    if (durationInSeconds <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(durationInSeconds));
                    }

                    DurationInSeconds = durationInSeconds;
                }

                public int DurationInSeconds { get; }

                public string? CacheKeyPrefix { get; init; }
            }
            """;

        files["Pipelines/Attributes/IdempotentAttribute.cs"] =
            $$"""
            namespace {{baseNamespace}}.Pipelines.Attributes;

            [AttributeUsage(
                AttributeTargets.Class,
                AllowMultiple = false,
                Inherited = true)]
            public sealed class IdempotentAttribute : Attribute
            {
                public IdempotentAttribute(
                    string? keyPrefix = null)
                {
                    KeyPrefix = keyPrefix;
                }

                public string? KeyPrefix { get; init; }

                public int ExpirationInSeconds { get; init; } = 86400;
            }
            """;
    }

    private static void AddBehaviorFiles(
        Dictionary<string, string> files,
        string baseNamespace,
        ApplicationGenerationOptions options)
    {
        if (options.IncludeExceptionBehavior)
        {
            files["Pipelines/Behaviors/UnhandledExceptionBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public sealed class UnhandledExceptionBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly
                        ILogger<UnhandledExceptionBehavior<TRequest, TResponse>>
                        _logger;

                    public UnhandledExceptionBehavior(
                        ILogger<UnhandledExceptionBehavior<TRequest, TResponse>>
                            logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        try
                        {
                            return await next();
                        }
                        catch (OperationCanceledException)
                            when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(
                                exception,
                                "Unhandled application exception. Request: {RequestType}",
                                typeof(TRequest).FullName);

                            throw;
                        }
                    }
                }
                """;
        }

        if (options.IncludeLoggingBehavior)
        {
            files["Pipelines/Behaviors/LoggingBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public sealed class LoggingBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly
                        ILogger<LoggingBehavior<TRequest, TResponse>>
                        _logger;

                    public LoggingBehavior(
                        ILogger<LoggingBehavior<TRequest, TResponse>>
                            logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var requestType =
                            typeof(TRequest).FullName
                            ?? typeof(TRequest).Name;

                        _logger.LogInformation(
                            "Application request started: {RequestType}",
                            requestType);

                        try
                        {
                            var response = await next();

                            _logger.LogInformation(
                                "Application request completed: {RequestType}",
                                requestType);

                            return response;
                        }
                        catch
                        {
                            _logger.LogWarning(
                                "Application request failed: {RequestType}",
                                requestType);

                            throw;
                        }
                    }
                }
                """;
        }

        if (options.IncludePerformanceBehavior)
        {
            files["Pipelines/Behaviors/PerformanceBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public sealed class PerformanceBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private const long DefaultWarningThresholdMilliseconds = 500;

                    private readonly
                        ILogger<PerformanceBehavior<TRequest, TResponse>>
                        _logger;

                    public PerformanceBehavior(
                        ILogger<PerformanceBehavior<TRequest, TResponse>>
                            logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var started = Stopwatch.GetTimestamp();

                        var response = await next();

                        var elapsedMilliseconds =
                            Stopwatch.GetElapsedTime(started)
                                .TotalMilliseconds;

                        if (elapsedMilliseconds >=
                            DefaultWarningThresholdMilliseconds)
                        {
                            _logger.LogWarning(
                                "Long-running application request: {RequestType}. " +
                                "Elapsed: {ElapsedMilliseconds} ms",
                                typeof(TRequest).FullName,
                                elapsedMilliseconds);
                        }

                        return response;
                    }
                }
                """;
        }

        if (options.IncludeValidationBehavior)
        {
            /*
             * Validation is intentionally delegated to the
             * BuildingBlocks.Validation package.
             *
             * The Application layer does not create a second validator system.
             */
            files["Pipelines/Behaviors/ValidationBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                /*
                 * This behavior is intentionally kept as an integration point.
                 *
                 * The concrete validator abstraction must come from:
                 *
                 * {{baseNamespace.Replace(".Application", ".Validation")}}
                 *
                 * once the Validation BuildingBlock contract is finalized.
                 *
                 * Do not create another IValidator abstraction here.
                 */

                public sealed class ValidationBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    public ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        return next();
                    }
                }
                """;
        }

        if (options.IncludeAuthorizationBehavior)
        {
            files["Pipelines/Behaviors/AuthorizationBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.Pipelines.Attributes;

                public sealed class AuthorizationBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly
                        IAuthorizationService _authorizationService;

                    public AuthorizationBehavior(
                        IAuthorizationService authorizationService)
                    {
                        _authorizationService =
                            authorizationService;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var attributes =
                            typeof(TRequest)
                                .GetCustomAttributes<AuthorizeAttribute>(
                                    inherit: true)
                                .ToArray();

                        if (attributes.Length == 0)
                        {
                            return await next();
                        }

                        foreach (var attribute in attributes)
                        {
                            if (!string.IsNullOrWhiteSpace(attribute.Policy))
                            {
                                var allowed =
                                    await _authorizationService
                                        .AuthorizeAsync(
                                            attribute.Policy,
                                            cancellationToken);

                                if (!allowed)
                                {
                                    throw new UnauthorizedAccessException(
                                        $"Authorization policy '{attribute.Policy}' failed.");
                                }

                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(attribute.Roles))
                            {
                                var roles =
                                    attribute.Roles
                                        .Split(
                                            ',',
                                            StringSplitOptions.RemoveEmptyEntries |
                                            StringSplitOptions.TrimEntries);

                                foreach (var role in roles)
                                {
                                    var allowed =
                                        await _authorizationService
                                            .AuthorizeAsync(
                                                role,
                                                cancellationToken);

                                    if (!allowed)
                                    {
                                        throw new UnauthorizedAccessException(
                                            $"Authorization requirement '{role}' failed.");
                                    }
                                }
                            }
                            else
                            {
                                var allowed =
                                    await _authorizationService
                                        .AuthorizeAsync(
                                            null,
                                            cancellationToken);

                                if (!allowed)
                                {
                                    throw new UnauthorizedAccessException(
                                        "Authorization failed.");
                                }
                            }
                        }

                        return await next();
                    }
                }
                """;
        }

        if (options.IncludeTransactionBehavior)
        {
            files["Pipelines/Behaviors/TransactionBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;

                public sealed class TransactionBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ITransactionManager _transactionManager;

                    public TransactionBehavior(
                        ITransactionManager transactionManager)
                    {
                        _transactionManager =
                            transactionManager;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        await _transactionManager.BeginAsync(
                            cancellationToken);

                        try
                        {
                            var response = await next();

                            await _transactionManager.CommitAsync(
                                cancellationToken);

                            return response;
                        }
                        catch
                        {
                            try
                            {
                                await _transactionManager.RollbackAsync(
                                    cancellationToken);
                            }
                            catch
                            {
                                // Never hide the original exception.
                            }

                            throw;
                        }
                    }
                }
                """;
        }

        if (options.IncludeCachingBehavior)
        {
            files["Pipelines/Behaviors/CachingBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using System.Security.Cryptography;
                using System.Text;
                using System.Text.Json;
                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.Pipelines.Attributes;

                public sealed class CachingBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ICacheService _cacheService;

                    public CachingBehavior(
                        ICacheService cacheService)
                    {
                        _cacheService = cacheService;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var attribute =
                            typeof(TRequest)
                                .GetCustomAttribute<CacheableAttribute>();

                        if (attribute is null)
                        {
                            return await next();
                        }

                        var key =
                            CreateCacheKey(
                                request,
                                attribute);

                        var cached =
                            await _cacheService.GetAsync<TResponse>(
                                key,
                                cancellationToken);

                        if (cached is not null)
                        {
                            return cached;
                        }

                        var result = await next();

                        await _cacheService.SetAsync(
                            key,
                            result,
                            TimeSpan.FromSeconds(
                                attribute.DurationInSeconds),
                            cancellationToken);

                        return result;
                    }

                    private static string CreateCacheKey(
                        TRequest request,
                        CacheableAttribute attribute)
                    {
                        var json =
                            JsonSerializer.Serialize(
                                request,
                                new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy =
                                        JsonNamingPolicy.CamelCase
                                });

                        var bytes =
                            SHA256.HashData(
                                Encoding.UTF8.GetBytes(json));

                        var hash =
                            Convert.ToHexString(bytes);

                        var prefix =
                            string.IsNullOrWhiteSpace(
                                attribute.CacheKeyPrefix)
                                ? typeof(TRequest).FullName
                                : attribute.CacheKeyPrefix;

                        return $"application:v1:{prefix}:{hash}";
                    }
                }
                """;
        }

        if (options.IncludeRetryBehavior)
        {
            files["Pipelines/Behaviors/RetryBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;

                public sealed class RetryBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly IRetryPolicy _retryPolicy;

                    public RetryBehavior(
                        IRetryPolicy retryPolicy)
                    {
                        _retryPolicy = retryPolicy;
                    }

                    public ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        return _retryPolicy.ExecuteAsync(
                            _ => next(),
                            cancellationToken);
                    }
                }
                """;
        }

        if (options.IncludeIdempotencyBehavior)
        {
            files["Pipelines/Behaviors/IdempotencyBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using System.Security.Cryptography;
                using System.Text;
                using System.Text.Json;
                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.Pipelines.Attributes;

                public sealed class IdempotencyBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly IIdempotencyStore _store;

                    public IdempotencyBehavior(
                        IIdempotencyStore store)
                    {
                        _store = store;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var attribute =
                            typeof(TRequest)
                                .GetCustomAttribute<IdempotentAttribute>();

                        if (attribute is null)
                        {
                            return await next();
                        }

                        var key =
                            CreateKey(
                                request,
                                attribute);

                        if (await _store.ExistsAsync(
                            key,
                            cancellationToken))
                        {
                            throw new InvalidOperationException(
                                $"The request has already been processed. Key: {key}");
                        }

                        var result = await next();

                        await _store.StoreAsync(
                            key,
                            TimeSpan.FromSeconds(
                                attribute.ExpirationInSeconds),
                            cancellationToken);

                        return result;
                    }

                    private static string CreateKey(
                        TRequest request,
                        IdempotentAttribute attribute)
                    {
                        var json =
                            JsonSerializer.Serialize(request);

                        var hash =
                            Convert.ToHexString(
                                SHA256.HashData(
                                    Encoding.UTF8.GetBytes(json)));

                        var prefix =
                            string.IsNullOrWhiteSpace(
                                attribute.KeyPrefix)
                                ? typeof(TRequest).FullName
                                : attribute.KeyPrefix;

                        return $"idempotency:v1:{prefix}:{hash}";
                    }
                }
                """;
        }

        if (options.IncludeAuditBehavior)
        {
            files["Pipelines/Behaviors/AuditBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;

                public sealed class AuditBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly IAuditWriter _auditWriter;

                    public AuditBehavior(
                        IAuditWriter auditWriter)
                    {
                        _auditWriter = auditWriter;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var requestType =
                            typeof(TRequest).FullName
                            ?? typeof(TRequest).Name;

                        var response = await next();

                        await _auditWriter.WriteAsync(
                            requestType,
                            request,
                            cancellationToken);

                        return response;
                    }
                }
                """;
        }

        if (options.IncludeRateLimitBehavior)
        {
            files["Pipelines/Behaviors/RateLimitBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;

                public sealed class RateLimitBehavior<
                    TRequest,
                    TResponse> :
                    IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly IRateLimitService _rateLimitService;

                    public RateLimitBehavior(
                        IRateLimitService rateLimitService)
                    {
                        _rateLimitService =
                            rateLimitService;
                    }

                    public async ValueTask<TResponse> HandleAsync(
                        TRequest request,
                        Func<ValueTask<TResponse>> next,
                        CancellationToken cancellationToken = default)
                    {
                        var key =
                            $"request:{typeof(TRequest).FullName}";

                        var allowed =
                            await _rateLimitService.IsAllowedAsync(
                                key,
                                cancellationToken);

                        if (!allowed)
                        {
                            throw new InvalidOperationException(
                                "Application request rate limit exceeded.");
                        }

                        return await next();
                    }
                }
                """;
        }
    }

    private static void AddApplicationRegistrationFile(
        Dictionary<string, string> files,
        string baseNamespace,
        ApplicationGenerationOptions options)
    {
        files["Extensions/ApplicationServiceCollectionExtensions.cs"] =
            $$"""
            namespace Microsoft.Extensions.DependencyInjection;

            using {{baseNamespace}}.Abstractions;
            using {{baseNamespace}}.CQRS.Commands;
            using {{baseNamespace}}.CQRS.Events;
            using {{baseNamespace}}.CQRS.Queries;
            using {{baseNamespace}}.Dispatchers;
            using {{baseNamespace}}.Pipelines;
            using {{baseNamespace}}.Pipelines.Behaviors;

            public static class ApplicationServiceCollectionExtensions
            {
                public static IServiceCollection AddApplicationServices(
                    this IServiceCollection services,
                    params Assembly[] assemblies)
                {
                    ArgumentNullException.ThrowIfNull(services);

                    if (assemblies is null ||
                        assemblies.Length == 0)
                    {
                        assemblies =
                        [
                            typeof(ApplicationServiceCollectionExtensions)
                                .Assembly
                        ];
                    }

                    services.AddScoped<
                        ICommandDispatcher,
                        CommandDispatcher>();

                    services.AddScoped<
                        IQueryDispatcher,
                        QueryDispatcher>();

                    services.AddScoped<
                        IEventPublisher,
                        EventPublisher>();

                    services.AddScoped<
                        IApplicationDispatcher,
                        ApplicationDispatcher>();

                    RegisterHandlers(
                        services,
                        assemblies);

                    {{BuildBehaviorRegistrations(options)}}

                    return services;
                }

                private static void RegisterHandlers(
                    IServiceCollection services,
                    IEnumerable<Assembly> assemblies)
                {
                    foreach (var assembly in assemblies.Distinct())
                    {
                        foreach (var type in GetLoadableTypes(assembly))
                        {
                            if (!type.IsClass ||
                                type.IsAbstract ||
                                type.IsGenericTypeDefinition)
                            {
                                continue;
                            }

                            foreach (var serviceType in type.GetInterfaces())
                            {
                                if (!serviceType.IsGenericType)
                                {
                                    continue;
                                }

                                var definition =
                                    serviceType.GetGenericTypeDefinition();

                                if (definition ==
                                        typeof(ICommandHandler<>) ||
                                    definition ==
                                        typeof(ICommandHandler<,>) ||
                                    definition ==
                                        typeof(IQueryHandler<>) ||
                                    definition ==
                                        typeof(IQueryHandler<,>) ||
                                    definition ==
                                        typeof(IEventHandler<>))
                                {
                                    services.AddScoped(
                                        serviceType,
                                        type);
                                }
                            }
                        }
                    }
                }

                private static IEnumerable<Type> GetLoadableTypes(
                    Assembly assembly)
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        return exception.Types
                            .Where(static type => type is not null)
                            .Cast<Type>();
                    }
                }

                private static void AddBehavior<TBehavior>(
                    IServiceCollection services)
                    where TBehavior : class
                {
                    services.AddScoped(
                        typeof(IPipelineBehavior<,>),
                        typeof(TBehavior));
                }
            }
            """;

        /*
         * The generated registration method is assembled separately
         * because feature flags are known at generation time.
         */
        files["Extensions/ApplicationBehaviorRegistration.cs"] =
            $$"""
            namespace {{baseNamespace}}.Extensions;

            internal static class ApplicationBehaviorRegistration
            {
                internal static void Register(
                    IServiceCollection services)
                {
                    {{BuildBehaviorRegistrations(options)}}
                }
            }
            """;
    }

    private static void AddMinimalApplicationRegistrationFile(
        Dictionary<string, string> files,
        string baseNamespace)
    {
        files["Extensions/ApplicationServiceCollectionExtensions.cs"] =
            $$"""
            namespace Microsoft.Extensions.DependencyInjection;

            using {{baseNamespace}}.Abstractions;

            public static class ApplicationServiceCollectionExtensions
            {
                public static IServiceCollection AddApplicationServices(
                    this IServiceCollection services)
                {
                    ArgumentNullException.ThrowIfNull(services);

                    return services;
                }
            }
            """;
    }

    private static string BuildBehaviorRegistrations(
        ApplicationGenerationOptions options)
    {
        if (!options.IncludeBehaviors)
        {
            return string.Empty;
        }

        var lines = new List<string>();

        if (options.IncludeExceptionBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(UnhandledExceptionBehavior<,>));
                """);
        }

        if (options.IncludeLoggingBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(LoggingBehavior<,>));
                """);
        }

        if (options.IncludePerformanceBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(PerformanceBehavior<,>));
                """);
        }

        if (options.IncludeRateLimitBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(RateLimitBehavior<,>));
                """);
        }

        if (options.IncludeAuthorizationBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(AuthorizationBehavior<,>));
                """);
        }

        if (options.IncludeValidationBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(ValidationBehavior<,>));
                """);
        }

        if (options.IncludeIdempotencyBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(IdempotencyBehavior<,>));
                """);
        }

        if (options.IncludeCachingBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(CachingBehavior<,>));
                """);
        }

        if (options.IncludeTransactionBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(TransactionBehavior<,>));
                """);
        }

        if (options.IncludeRetryBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(RetryBehavior<,>));
                """);
        }

        if (options.IncludeAuditBehavior)
        {
            lines.Add(
                """
                services.AddScoped(
                    typeof(IPipelineBehavior<,>),
                    typeof(AuditBehavior<,>));
                """);
        }

        return string.Join(
            Environment.NewLine,
            lines);
    }
}

public sealed class ApplicationGenerationOptions
{
    public bool IncludeCqrs { get; init; }

    public bool IncludeBehaviors { get; init; }

    public bool IncludeLoggingBehavior { get; init; }

    public bool IncludePerformanceBehavior { get; init; }

    public bool IncludeExceptionBehavior { get; init; }

    public bool IncludeValidationBehavior { get; init; }

    public bool IncludeAuthorizationBehavior { get; init; }

    public bool IncludeTransactionBehavior { get; init; }

    public bool IncludeCachingBehavior { get; init; }

    public bool IncludeRetryBehavior { get; init; }

    public bool IncludeIdempotencyBehavior { get; init; }

    public bool IncludeAuditBehavior { get; init; }

    public bool IncludeRateLimitBehavior { get; init; }
}