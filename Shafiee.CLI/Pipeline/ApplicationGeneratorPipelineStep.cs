using Shafiee.SDK.Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shafiee.CodeGenerator.Pipelines.Steps;

public class ApplicationGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        string? solutionName = context.Command.GetOption("solution");

        if (string.IsNullOrWhiteSpace(solutionName) || solutionName.Equals("application", StringComparison.OrdinalIgnoreCase))
        {
            solutionName = context.Command.Target;
        }

        if (string.IsNullOrWhiteSpace(solutionName))
        {
            solutionName = "Shop";
        }

        bool includeCqrs = context.Command.GetOption("cqrs")?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;
        bool includeBehaviors = includeCqrs && (context.Command.GetOption("behaviors")?.Equals("false", StringComparison.OrdinalIgnoreCase) != true);

        // تنظیمات سفارشی رفتارها
        bool includeLogging = includeBehaviors && context.Command.GetOption("behavior-logging")?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;
        bool includePerformance = includeBehaviors && context.Command.GetOption("behavior-performance")?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;
        bool includeException = includeBehaviors && context.Command.GetOption("behavior-exception")?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;
        bool includeValidation = includeBehaviors && context.Command.GetOption("behavior-validation")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeAuthorization = includeBehaviors && context.Command.GetOption("behavior-authorization")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeTransaction = includeBehaviors && context.Command.GetOption("behavior-transaction")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeCaching = includeBehaviors && context.Command.GetOption("behavior-caching")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeRetry = includeBehaviors && context.Command.GetOption("behavior-retry")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeIdempotency = includeBehaviors && context.Command.GetOption("behavior-idempotency")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeAudit = includeBehaviors && context.Command.GetOption("behavior-audit")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        bool includeRateLimit = includeBehaviors && context.Command.GetOption("behavior-ratelimit")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        string desktopRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionRoot = Path.Combine(desktopRoot, solutionName);
        string projectDir = Path.Combine(solutionRoot, "src", "BuildingBlocks", $"{solutionName}.BuildingBlocks.Application");

        var files = GetApplicationFilesDictionary(
            solutionName,
            includeCqrs,
            includeBehaviors,
            includeLogging,
            includePerformance,
            includeException,
            includeValidation,
            includeAuthorization,
            includeTransaction,
            includeCaching,
            includeRetry,
            includeIdempotency,
            includeAudit,
            includeRateLimit);

        int successCount = 0;

        foreach (var file in files)
        {
            string targetPath = Path.Combine(projectDir, file.Key);
            string? fileDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            File.WriteAllText(targetPath, file.Value);
            Console.WriteLine($"✅ [Written] {targetPath}");
            successCount++;
        }

        Console.WriteLine($"\n📊 [Application Disk Summary] Successfully wrote {successCount} application files directly to destination.");

        return Task.CompletedTask;
    }

    public Dictionary<string, string> GetApplicationFilesDictionary(
        string solutionName,
        bool includeCqrs,
        bool includeBehaviors,
        bool includeLogging = true,
        bool includePerformance = true,
        bool includeException = true,
        bool includeValidation = false,
        bool includeAuthorization = false,
        bool includeTransaction = false,
        bool includeCaching = false,
        bool includeRetry = false,
        bool includeIdempotency = false,
        bool includeAudit = false,
        bool includeRateLimit = false)
    {
        var baseNamespace = $"{solutionName}.BuildingBlocks.Application";

        var files = new Dictionary<string, string>
        {
            ["GlobalUsings.cs"] =
                $$"""
                global using System;
                global using System.Collections.Generic;
                global using System.Diagnostics;
                global using System.Linq;
                global using System.Reflection;
                global using System.Threading;
                global using System.Threading.Tasks;
                global using Microsoft.Extensions.DependencyInjection;
                global using Microsoft.Extensions.Logging;
                """,

            #region Abstractions & Contracts
            ["Abstractions/IApplicationService.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IApplicationService
                {
                }
                """,

            ["Abstractions/ITransactionManager.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface ITransactionManager
                {
                    ValueTask BeginAsync(CancellationToken cancellationToken = default);
                    ValueTask CommitAsync(CancellationToken cancellationToken = default);
                    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
                }
                """,

            ["Abstractions/IIdGenerator.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IIdGenerator
                {
                    Guid NewGuid();
                    string NewString();
                }
                """,

            ["Abstractions/IDateTimeProvider.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IDateTimeProvider
                {
                    DateTime UtcNow { get; }
                    DateTime LocalNow { get; }
                }
                """,

            ["Abstractions/IApplicationModule.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IApplicationModule
                {
                    string Name { get; }
                    string Version { get; }
                }
                """,

            ["Abstractions/ICacheService.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface ICacheService
                {
                    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
                    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
                    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
                }
                """,

            ["Abstractions/ICurrentUser.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface ICurrentUser
                {
                    string? Id { get; }
                    string? UserName { get; }
                    bool IsAuthenticated { get; }
                    IEnumerable<string> Roles { get; }
                    bool HasRole(string role);
                }
                """,
            #endregion

            #region Common Services Implementation
            ["Services/SystemDateTimeProvider.cs"] =
                $$"""
                namespace {{baseNamespace}}.Services;

                using {{baseNamespace}}.Abstractions;

                public class SystemDateTimeProvider : IDateTimeProvider
                {
                    public DateTime UtcNow => DateTime.UtcNow;
                    public DateTime LocalNow => DateTime.Now;
                }
                """,

            ["Services/GuidIdGenerator.cs"] =
                $$"""
                namespace {{baseNamespace}}.Services;

                using {{baseNamespace}}.Abstractions;

                public class GuidIdGenerator : IIdGenerator
                {
                    public Guid NewGuid() => Guid.NewGuid();
                    public string NewString() => Guid.NewGuid().ToString("N");
                }
                """,
            #endregion

            #region CQRS Base Interfaces
            ["CQRS/Common/Unit.cs"] =
                $$"""
                namespace {{baseNamespace}}.CQRS.Common;

                public readonly struct Unit : IEquatable<Unit>
                {
                    public static readonly Unit Value = new();
                    public override string ToString() => "()";
                    public bool Equals(Unit other) => true;
                    public override bool Equals(object? obj) => obj is Unit;
                    public override int GetHashCode() => 0;
                    public static bool operator ==(Unit left, Unit right) => true;
                    public static bool operator !=(Unit left, Unit right) => false;
                }
                """,

            ["CQRS/Commands/ICommand.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Commands;\n\npublic interface ICommand { }""",
            ["CQRS/Commands/ICommand{TResult}.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Commands;\n\npublic interface ICommand<out TResult> : ICommand { }""",
            ["CQRS/Commands/ICommandHandler.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Commands;\n\npublic interface ICommandHandler<in TCommand> where TCommand : ICommand { ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken = default); }""",
            ["CQRS/Commands/ICommandHandler{TCommand,TResult}.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Commands;\n\npublic interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult> { ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default); }""",

            ["CQRS/Queries/IQuery.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Queries;\n\npublic interface IQuery { }""",
            ["CQRS/Queries/IQuery{TResult}.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Queries;\n\npublic interface IQuery<out TResult> : IQuery { }""",
            ["CQRS/Queries/IQueryHandler.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Queries;\n\npublic interface IQueryHandler<in TQuery> where TQuery : IQuery { ValueTask HandleAsync(TQuery query, CancellationToken cancellationToken = default); }""",
            ["CQRS/Queries/IQueryHandler{TQuery,TResult}.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Queries;\n\npublic interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult> { ValueTask<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default); }""",

            ["CQRS/Events/IEvent.cs"] = $$"""namespace {{baseNamespace}}.CQRS.Events;\n\npublic interface IEvent { DateTime OccurredOn { get; } }""",
            ["CQRS/Events/IEventHandler.cs"] = $$"""namespace {baseNamespace}.CQRS.Events;\n\npublic interface IEventHandler<in TEvent> where TEvent : IEvent { ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default); }""",
            #endregion

            #region Dispatchers & Pipeline Abstractions
            ["Abstractions/ICommandDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface ICommandDispatcher
                {
                    ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : CQRS.Commands.ICommand;
                    ValueTask<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : CQRS.Commands.ICommand<TResult>;
                }
                """,

            ["Abstractions/IQueryDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IQueryDispatcher
                {
                    ValueTask SendAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default) where TQuery : CQRS.Queries.IQuery;
                    ValueTask<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : CQRS.Queries.IQuery<TResult>;
                }
                """,

            ["Abstractions/IEventPublisher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IEventPublisher
                {
                    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : CQRS.Events.IEvent;
                }
                """,

            ["Abstractions/IApplicationDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IApplicationDispatcher : ICommandDispatcher, IQueryDispatcher, IEventPublisher
                {
                }
                """,

            ["Pipelines/IPipelineBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines;

                public interface IPipelineBehavior<in TRequest, TResponse>
                {
                    ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default);
                }
                """,

            ["Pipelines/IValidator.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines;

                public interface IValidator<in T>
                {
                    ValueTask ValidateAsync(T instance, CancellationToken cancellationToken = default);
                }
                """,

            ["Pipelines/Attributes/AuthorizeAttribute.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Attributes;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
                public class AuthorizeAttribute : Attribute
                {
                    public string? Roles { get; set; }
                    public string? Policy { get; set; }
                }
                """,

            ["Pipelines/Attributes/CacheableAttribute.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Attributes;

                [AttributeUsage(AttributeTargets.Class, Inherited = true)]
                public class CacheableAttribute : Attribute
                {
                    public int DurationInSeconds { get; }
                    public string? CacheKeyPrefix { get; set; }

                    public CacheableAttribute(int durationInSeconds = 60)
                    {
                        DurationInSeconds = durationInSeconds;
                    }
                }
                """,
            #endregion

            #region Dispatcher Implementations
            ["Dispatchers/CommandDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Dispatchers;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.CQRS.Commands;
                using {{baseNamespace}}.Pipelines;

                public class CommandDispatcher : ICommandDispatcher
                {
                    private readonly IServiceProvider _serviceProvider;

                    public CommandDispatcher(IServiceProvider serviceProvider)
                    {
                        _serviceProvider = serviceProvider;
                    }

                    public async ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
                    {
                        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
                        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TCommand, CQRS.Common.Unit>>().Reverse().ToArray();

                        Func<ValueTask<CQRS.Common.Unit>> pipeline = async () =>
                        {
                            await handler.HandleAsync(command, cancellationToken);
                            return CQRS.Common.Unit.Value;
                        };

                        foreach (var behavior in behaviors)
                        {
                            var currentPipeline = pipeline;
                            pipeline = () => behavior.HandleAsync(command, currentPipeline, cancellationToken);
                        }

                        await pipeline();
                    }

                    public async ValueTask<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
                    {
                        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
                        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse().ToArray();

                        Func<ValueTask<TResult>> pipeline = () => handler.HandleAsync(command, cancellationToken);

                        foreach (var behavior in behaviors)
                        {
                            var currentPipeline = pipeline;
                            pipeline = () => behavior.HandleAsync(command, currentPipeline, cancellationToken);
                        }

                        return await pipeline();
                    }
                }
                """,

            ["Dispatchers/QueryDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Dispatchers;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.CQRS.Queries;
                using {{baseNamespace}}.Pipelines;

                public class QueryDispatcher : IQueryDispatcher
                {
                    private readonly IServiceProvider _serviceProvider;

                    public QueryDispatcher(IServiceProvider serviceProvider)
                    {
                        _serviceProvider = serviceProvider;
                    }

                    public async ValueTask SendAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery
                    {
                        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery>>();
                        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TQuery, CQRS.Common.Unit>>().Reverse().ToArray();

                        Func<ValueTask<CQRS.Common.Unit>> pipeline = async () =>
                        {
                            await handler.HandleAsync(query, cancellationToken);
                            return CQRS.Common.Unit.Value;
                        };

                        foreach (var behavior in behaviors)
                        {
                            var currentPipeline = pipeline;
                            pipeline = () => behavior.HandleAsync(query, currentPipeline, cancellationToken);
                        }

                        await pipeline();
                    }

                    public async ValueTask<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
                    {
                        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
                        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TQuery, TResult>>().Reverse().ToArray();

                        Func<ValueTask<TResult>> pipeline = () => handler.HandleAsync(query, cancellationToken);

                        foreach (var behavior in behaviors)
                        {
                            var currentPipeline = pipeline;
                            pipeline = () => behavior.HandleAsync(query, currentPipeline, cancellationToken);
                        }

                        return await pipeline();
                    }
                }
                """,

            ["Dispatchers/EventPublisher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Dispatchers;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.CQRS.Events;

                public class EventPublisher : IEventPublisher
                {
                    private readonly IServiceProvider _serviceProvider;

                    public EventPublisher(IServiceProvider serviceProvider)
                    {
                        _serviceProvider = serviceProvider;
                    }

                    public async ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
                    {
                        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
                        foreach (var handler in handlers)
                        {
                            await handler.HandleAsync(@event, cancellationToken);
                        }
                    }
                }
                """,

            ["Dispatchers/ApplicationDispatcher.cs"] =
                $$"""
                namespace {{baseNamespace}}.Dispatchers;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.CQRS.Commands;
                using {{baseNamespace}}.CQRS.Events;
                using {{baseNamespace}}.CQRS.Queries;

                public class ApplicationDispatcher : IApplicationDispatcher
                {
                    private readonly ICommandDispatcher _commandDispatcher;
                    private readonly IQueryDispatcher _queryDispatcher;
                    private readonly IEventPublisher _eventPublisher;

                    public ApplicationDispatcher(
                        ICommandDispatcher commandDispatcher,
                        IQueryDispatcher queryDispatcher,
                        IEventPublisher eventPublisher)
                    {
                        _commandDispatcher = commandDispatcher;
                        _queryDispatcher = queryDispatcher;
                        _eventPublisher = eventPublisher;
                    }

                    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
                        => _commandDispatcher.SendAsync(command, cancellationToken);

                    public ValueTask<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
                        => _commandDispatcher.SendAsync<TCommand, TResult>(command, cancellationToken);

                    public ValueTask SendAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery
                        => _queryDispatcher.SendAsync(query, cancellationToken);

                    public ValueTask<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
                        => _queryDispatcher.SendAsync<TQuery, TResult>(query, cancellationToken);

                    public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
                        => _eventPublisher.PublishAsync(@event, cancellationToken);
                }
                """,
            #endregion

            #region Pipeline Behaviors Implementation
            ["Pipelines/Behaviors/UnhandledExceptionBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

                    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        try
                        {
                            return await next();
                        }
                        catch (Exception ex)
                        {
                            var requestName = typeof(TRequest).Name;
                            _logger.LogError(ex, "Application Request: Unhandled Exception for Request {Name} {@Request}", requestName, request);
                            throw;
                        }
                    }
                }
                """,

            ["Pipelines/Behaviors/LoggingBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

                    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        var requestName = typeof(TRequest).Name;
                        _logger.LogInformation("Processing request {RequestName}: {@Request}", requestName, request);

                        var response = await next();

                        _logger.LogInformation("Completed request {RequestName}", requestName);
                        return response;
                    }
                }
                """,

            ["Pipelines/Behaviors/PerformanceBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly Stopwatch _timer = new();
                    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

                    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
                    {
                        _logger = logger;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        _timer.Start();

                        var response = await next();

                        _timer.Stop();

                        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

                        if (elapsedMilliseconds > 500)
                        {
                            var requestName = typeof(TRequest).Name;
                            _logger.LogWarning("Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
                                requestName, elapsedMilliseconds, request);
                        }

                        return response;
                    }
                }
                """,

            ["Pipelines/Behaviors/ValidationBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly IEnumerable<IValidator<TRequest>> _validators;

                    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
                    {
                        _validators = validators;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        if (_validators.Any())
                        {
                            foreach (var validator in _validators)
                            {
                                await validator.ValidateAsync(request, cancellationToken);
                            }
                        }

                        return await next();
                    }
                }
                """,

            ["Pipelines/Behaviors/AuthorizationBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.Pipelines.Attributes;

                public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ICurrentUser _currentUser;

                    public AuthorizationBehavior(ICurrentUser currentUser)
                    {
                        _currentUser = currentUser;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        var authorizeAttributes = typeof(TRequest).GetCustomAttributes<AuthorizeAttribute>().ToList();

                        if (authorizeAttributes.Any())
                        {
                            if (!_currentUser.IsAuthenticated)
                            {
                                throw new UnauthorizedAccessException("User is not authenticated.");
                            }

                            foreach (var attr in authorizeAttributes)
                            {
                                if (!string.IsNullOrWhiteSpace(attr.Roles))
                                {
                                    var roles = attr.Roles.Split(',');
                                    var hasRole = roles.Any(role => _currentUser.HasRole(role.Trim()));
                                    if (!hasRole)
                                    {
                                        throw new UnauthorizedAccessException("User is not authorized to execute this request.");
                                    }
                                }
                            }
                        }

                        return await next();
                    }
                }
                """,

            ["Pipelines/Behaviors/TransactionBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;

                public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ITransactionManager _transactionManager;

                    public TransactionBehavior(ITransactionManager transactionManager)
                    {
                        _transactionManager = transactionManager;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        try
                        {
                            await _transactionManager.BeginAsync(cancellationToken);
                            var response = await next();
                            await _transactionManager.CommitAsync(cancellationToken);
                            return response;
                        }
                        catch
                        {
                            await _transactionManager.RollbackAsync(cancellationToken);
                            throw;
                        }
                    }
                }
                """,

            ["Pipelines/Behaviors/CachingBehavior.cs"] =
                $$"""
                namespace {{baseNamespace}}.Pipelines.Behaviors;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.Pipelines.Attributes;

                public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                {
                    private readonly ICacheService _cacheService;

                    public CachingBehavior(ICacheService cacheService)
                    {
                        _cacheService = cacheService;
                    }

                    public async ValueTask<TResponse> HandleAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken cancellationToken = default)
                    {
                        var cacheAttribute = typeof(TRequest).GetCustomAttribute<CacheableAttribute>();
                        if (cacheAttribute == null)
                        {
                            return await next();
                        }

                        var cacheKey = $"{cacheAttribute.CacheKeyPrefix ?? typeof(TRequest).Name}_{request.GetHashCode()}";
                        var cachedResult = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);

                        if (cachedResult != null)
                        {
                            return cachedResult;
                        }

                        var result = await next();
                        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(cacheAttribute.DurationInSeconds), cancellationToken);

                        return result;
                    }
                }
                """,
            #endregion

            #region Extension Registration
            ["Extensions/ApplicationServiceCollectionExtensions.cs"] =
                $$"""
                namespace Microsoft.Extensions.DependencyInjection;

                using {{baseNamespace}}.Abstractions;
                using {{baseNamespace}}.CQRS.Commands;
                using {{baseNamespace}}.CQRS.Events;
                using {{baseNamespace}}.CQRS.Queries;
                using {{baseNamespace}}.Dispatchers;
                using {{baseNamespace}}.Pipelines;
                using {{baseNamespace}}.Pipelines.Behaviors;
                using {{baseNamespace}}.Services;

                public static class ApplicationServiceCollectionExtensions
                {
                    public static IServiceCollection AddApplicationServices(
                        this IServiceCollection services, 
                        Assembly[] assemblies)
                    {
                        // ثبت Infrastructure Services اولیه
                        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
                        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

                        // ثبت Dispatcherها
                        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
                        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
                        services.AddScoped<IEventPublisher, EventPublisher>();
                        services.AddScoped<IApplicationDispatcher, ApplicationDispatcher>();

                        // ثبت خودکار Handlerها
                        services.ScanAndRegisterHandlers(assemblies);

                        // ثبت Pipeline Behaviorهای اصلی به‌ترتیب اجرای بهینه
                        if ({{includeException.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
                        }
                        if ({{includeLogging.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                        }
                        if ({{includePerformance.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
                        }
                        if ({{includeAuthorization.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
                        }
                        if ({{includeValidation.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                        }
                        if ({{includeCaching.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
                        }
                        if ({{includeTransaction.ToString().ToLower()}})
                        {
                            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
                        }

                        return services;
                    }

                    private static IServiceCollection ScanAndRegisterHandlers(this IServiceCollection services, Assembly[] assemblies)
                    {
                        foreach (var assembly in assemblies)
                        {
                            var types = assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract)
                                .ToList();

                            foreach (var type in types)
                            {
                                var interfaces = type.GetInterfaces();
                                foreach (var @interface in interfaces)
                                {
                                    if (!@interface.IsGenericType) continue;

                                    var genericDef = @interface.GetGenericTypeDefinition();
                                    if (genericDef == typeof(ICommandHandler<>) ||
                                        genericDef == typeof(ICommandHandler<,>) ||
                                        genericDef == typeof(IQueryHandler<,>) ||
                                        genericDef == typeof(IEventHandler<>))
                                    {
                                        services.AddScoped(@interface, type);
                                    }
                                }
                            }
                        }

                        return services;
                    }
                }
                """
            #endregion
        };

        return files;
    }
}