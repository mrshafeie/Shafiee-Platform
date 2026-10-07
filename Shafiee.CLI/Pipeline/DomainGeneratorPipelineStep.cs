using Shafiee.SDK.Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shafiee.CodeGenerator.Pipelines.Steps;

public class DomainGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        // ۱. استخراج نام سولوشن از Target یا Options
        string? solutionName = context.Command.GetOption("solution");

        if (string.IsNullOrWhiteSpace(solutionName) || solutionName.Equals("domain", StringComparison.OrdinalIgnoreCase))
        {
            solutionName = context.Command.Target;
        }

        if (string.IsNullOrWhiteSpace(solutionName))
        {
            solutionName = "Shop";
        }

        // ۲. تعیین مسیر خروجی دقیق پروژه BuildingBlocks.Domain
        string desktopRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionRoot = Path.Combine(desktopRoot, solutionName);
        string projectDir = Path.Combine(solutionRoot, "src", "BuildingBlocks", $"{solutionName}.BuildingBlocks.Domain", $"{solutionName}.BuildingBlocks.Domain");

        // ۳. تولید دکشنری فایل‌ها و نوشتن روی دیسک
        var files = GetDomainFilesDictionary(solutionName);

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

        Console.WriteLine($"\n📊 [Domain Disk Summary] Successfully wrote {successCount} domain files directly to destination.");

        return Task.CompletedTask;
    }
    /// <summary>
    /// تولید ساختار دکشنری فایل‌های لایه Domain
    /// </summary>
    public Dictionary<string, string> GetDomainFilesDictionary(string solutionName)
    {
        var baseNamespace = $"{solutionName}.BuildingBlocks.Domain";

        return new Dictionary<string, string>
        {
            // 1. GlobalUsings
            ["GlobalUsings.cs"] =
                $$"""
                global using System;
                global using System.Collections;
                global using System.Collections.Generic;
                global using System.Linq;
                global using System.Linq.Expressions;
                global using System.Reflection;
                global using System.Threading;
                global using System.Threading.Tasks;
                """,

            // 2. Abstractions
            ["Abstractions/IEntity.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IEntity
                {
                    long Id { get; }
                }
                """,

            ["Abstractions/IEntityT.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IEntityT<out TId>
                {
                    TId Id { get; }
                }
                """,

            ["Abstractions/IAggregateRoot.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IAggregateRoot
                {
                }
                """,

            ["Abstractions/IDomainEvent.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IDomainEvent
                {
                    DateTime OccurredOnUtc { get; }
                }
                """,

            ["Abstractions/IDomainEventHandler.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IDomainEventHandler<in TDomainEvent>
                    where TDomainEvent : IDomainEvent
                {
                    Task HandleAsync(
                        TDomainEvent domainEvent,
                        CancellationToken cancellationToken = default);
                }
                """,

            ["Abstractions/IAuditableEntity.cs"] =
                $$"""
                namespace {{baseNamespace}}.Abstractions;

                public interface IAuditableEntity
                {
                    DateTime CreatedAtUtc { get; }

                    DateTime? ModifiedAtUtc { get; }

                    long? CreatedByUserId { get; }

                    long? ModifiedByUserId { get; }
                }
                """,

            // 3. Entities
            ["Entities/Entity.cs"] =
                $$"""
                using {{baseNamespace}}.Abstractions;

                namespace {{baseNamespace}}.Entities;

                public abstract class Entity : IEntity
                {
                    private readonly List<IDomainEvent> _domainEvents = [];

                    protected Entity()
                    {
                    }

                    protected Entity(long id)
                    {
                        Id = id;
                    }

                    public long Id { get; protected set; }

                    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
                        _domainEvents.AsReadOnly();

                    protected void AddDomainEvent(IDomainEvent domainEvent)
                    {
                        ArgumentNullException.ThrowIfNull(domainEvent);

                        _domainEvents.Add(domainEvent);
                    }

                    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
                    {
                        return _domainEvents.AsReadOnly();
                    }

                    public void ClearDomainEvents()
                    {
                        _domainEvents.Clear();
                    }

                    public override bool Equals(object? obj)
                    {
                        if (obj is not Entity other)
                            return false;

                        if (ReferenceEquals(this, other))
                            return true;

                        if (Id == 0 || other.Id == 0)
                            return false;

                        return Id == other.Id &&
                               GetType() == other.GetType();
                    }

                    public override int GetHashCode()
                    {
                        return HashCode.Combine(GetType(), Id);
                    }

                    public static bool operator ==(Entity? left, Entity? right)
                    {
                        return Equals(left, right);
                    }

                    public static bool operator !=(Entity? left, Entity? right)
                    {
                        return !Equals(left, right);
                    }
                }
                """,

            ["Entities/EntityT.cs"] =
                $$"""
                using {{baseNamespace}}.Abstractions;

                namespace {{baseNamespace}}.Entities;

                public abstract class Entity<TId> : IEntityT<TId>
                {
                    private readonly List<IDomainEvent> _domainEvents = [];

                    protected Entity()
                    {
                    }

                    protected Entity(TId id)
                    {
                        Id = id;
                    }

                    public TId Id { get; protected set; } = default!;

                    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
                        _domainEvents.AsReadOnly();

                    protected void AddDomainEvent(IDomainEvent domainEvent)
                    {
                        ArgumentNullException.ThrowIfNull(domainEvent);

                        _domainEvents.Add(domainEvent);
                    }

                    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
                    {
                        return _domainEvents.AsReadOnly();
                    }

                    public void ClearDomainEvents()
                    {
                        _domainEvents.Clear();
                    }
                }
                """,

            ["Entities/AggregateRoot.cs"] =
                $$"""
                using {{baseNamespace}}.Abstractions;

                namespace {{baseNamespace}}.Entities;

                public abstract class AggregateRoot : Entity, IAggregateRoot
                {
                    protected AggregateRoot()
                    {
                    }

                    protected AggregateRoot(long id)
                        : base(id)
                    {
                    }
                }
                """,

            // 4. Events
            ["Events/DomainEvent.cs"] =
                $$"""
                using {{baseNamespace}}.Abstractions;

                namespace {{baseNamespace}}.Events;

                public abstract record DomainEvent : IDomainEvent
                {
                    protected DomainEvent()
                    {
                        OccurredOnUtc = DateTime.UtcNow;
                    }

                    public DateTime OccurredOnUtc { get; }
                }
                """,

            ["Events/DomainEventHandler.cs"] =
                $$"""
                using {{baseNamespace}}.Abstractions;

                namespace {{baseNamespace}}.Events;

                public abstract class DomainEventHandler<TDomainEvent>
                    : IDomainEventHandler<TDomainEvent>
                    where TDomainEvent : IDomainEvent
                {
                    public abstract Task HandleAsync(
                        TDomainEvent domainEvent,
                        CancellationToken cancellationToken = default);
                }
                """,

            // 5. Value Objects
            ["ValueObjects/ValueObject.cs"] =
                $$"""
                namespace {{baseNamespace}}.ValueObjects;

                public abstract class ValueObject
                {
                    protected abstract IEnumerable<object?> GetEqualityComponents();

                    public override bool Equals(object? obj)
                    {
                        if (obj is not ValueObject other)
                            return false;

                        return GetEqualityComponents()
                            .SequenceEqual(other.GetEqualityComponents());
                    }

                    public override int GetHashCode()
                    {
                        return GetEqualityComponents()
                            .Aggregate(
                                0,
                                (current, obj) =>
                                    HashCode.Combine(current, obj));
                    }

                    public static bool operator ==(
                        ValueObject? left,
                        ValueObject? right)
                    {
                        return Equals(left, right);
                    }

                    public static bool operator !=(
                        ValueObject? left,
                        ValueObject? right)
                    {
                        return !Equals(left, right);
                    }
                }
                """,

            ["ValueObjects/Money.cs"] =
                $$"""
                namespace {{baseNamespace}}.ValueObjects;

                public sealed class Money : ValueObject
                {
                    private Money()
                    {
                    }

                    private Money(decimal amount, string currency)
                    {
                        if (amount < 0)
                            throw new ArgumentOutOfRangeException(
                                nameof(amount),
                                "Amount cannot be negative.");

                        if (string.IsNullOrWhiteSpace(currency))
                            throw new ArgumentException(
                                "Currency is required.",
                                nameof(currency));

                        Amount = amount;
                        Currency = currency.Trim().ToUpperInvariant();
                    }

                    public decimal Amount { get; private set; }

                    public string Currency { get; private set; } = string.Empty;

                    public static Money Create(
                        decimal amount,
                        string currency = "IRR")
                    {
                        return new Money(amount, currency);
                    }

                    public Money Add(Money other)
                    {
                        EnsureSameCurrency(other);

                        return new Money(
                            Amount + other.Amount,
                            Currency);
                    }

                    public Money Subtract(Money other)
                    {
                        EnsureSameCurrency(other);

                        var result = Amount - other.Amount;

                        if (result < 0)
                            throw new InvalidOperationException(
                                "Money result cannot be negative.");

                        return new Money(result, Currency);
                    }

                    public Money Multiply(decimal multiplier)
                    {
                        if (multiplier < 0)
                            throw new ArgumentOutOfRangeException(
                                nameof(multiplier));

                        return new Money(
                            Amount * multiplier,
                            Currency);
                    }

                    protected override IEnumerable<object?> GetEqualityComponents()
                    {
                        yield return Amount;
                        yield return Currency;
                    }

                    private void EnsureSameCurrency(Money other)
                    {
                        ArgumentNullException.ThrowIfNull(other);

                        if (!string.Equals(
                                Currency,
                                other.Currency,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "Currency mismatch.");
                        }
                    }

                    public override string ToString()
                    {
                        return $"{Amount} {Currency}";
                    }
                }
                """,

            ["ValueObjects/Address.cs"] =
                $$"""
                namespace {{baseNamespace}}.ValueObjects;

                public sealed class Address : ValueObject
                {
                    private Address()
                    {
                    }

                    private Address(
                        string country,
                        string province,
                        string city,
                        string? postalCode,
                        string addressLine,
                        string? plaque,
                        string? unit)
                    {
                        Country = Require(country, nameof(country));
                        Province = Require(province, nameof(province));
                        City = Require(city, nameof(city));
                        AddressLine = Require(addressLine, nameof(addressLine));

                        PostalCode = postalCode?.Trim();
                        Plaque = plaque?.Trim();
                        Unit = unit?.Trim();
                    }

                    public string Country { get; private set; } = string.Empty;

                    public string Province { get; private set; } = string.Empty;

                    public string City { get; private set; } = string.Empty;

                    public string? PostalCode { get; private set; }

                    public string AddressLine { get; private set; } = string.Empty;

                    public string? Plaque { get; private set; }

                    public string? Unit { get; private set; }

                    public static Address Create(
                        string country,
                        string province,
                        string city,
                        string addressLine,
                        string? postalCode = null,
                        string? plaque = null,
                        string? unit = null)
                    {
                        return new Address(
                            country,
                            province,
                            city,
                            postalCode,
                            addressLine,
                            plaque,
                            unit);
                    }

                    protected override IEnumerable<object?> GetEqualityComponents()
                    {
                        yield return Country;
                        yield return Province;
                        yield return City;
                        yield return PostalCode;
                        yield return AddressLine;
                        yield return Plaque;
                        yield return Unit;
                    }

                    private static string Require(
                        string value,
                        string parameterName)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            throw new ArgumentException(
                                $"{parameterName} is required.",
                                parameterName);

                        return value.Trim();
                    }

                    public override string ToString()
                    {
                        var items = new[]
                        {
                            Country,
                            Province,
                            City,
                            AddressLine,
                            Plaque,
                            Unit,
                            PostalCode
                        };

                        return string.Join(", ", items.Where(x => !string.IsNullOrWhiteSpace(x)));
                    }
                }
                """,

            // 6. Specifications
            ["Specifications/ISpecification.cs"] =
                $$"""
                namespace {{baseNamespace}}.Specifications;

                public interface ISpecification<T>
                {
                    Expression<Func<T, bool>> Criteria { get; }

                    IReadOnlyCollection<Expression<Func<T, object>>> Includes { get; }

                    IReadOnlyCollection<string> IncludeStrings { get; }

                    Expression<Func<T, object>>? OrderBy { get; }

                    Expression<Func<T, object>>? OrderByDescending { get; }

                    int? Skip { get; }

                    int? Take { get; }

                    bool AsNoTracking { get; }
                }
                """,

            ["Specifications/Specification.cs"] =
                $$"""
                namespace {{baseNamespace}}.Specifications;

                public abstract class Specification<T> : ISpecification<T>
                {
                    protected Specification(
                        Expression<Func<T, bool>> criteria)
                    {
                        Criteria = criteria ??
                                   throw new ArgumentNullException(nameof(criteria));
                    }

                    public Expression<Func<T, bool>> Criteria { get; }

                    private readonly List<Expression<Func<T, object>>> _includes = [];

                    private readonly List<string> _includeStrings = [];

                    public IReadOnlyCollection<Expression<Func<T, object>>> Includes =>
                        _includes.AsReadOnly();

                    public IReadOnlyCollection<string> IncludeStrings =>
                        _includeStrings.AsReadOnly();

                    public Expression<Func<T, object>>? OrderBy { get; private set; }

                    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

                    public int? Skip { get; private set; }

                    public int? Take { get; private set; }

                    public bool AsNoTracking { get; private set; }

                    protected void AddInclude(
                        Expression<Func<T, object>> include)
                    {
                        ArgumentNullException.ThrowIfNull(include);

                        _includes.Add(include);
                    }

                    protected void AddInclude(string include)
                    {
                        if (string.IsNullOrWhiteSpace(include))
                            throw new ArgumentException(
                                "Include cannot be empty.",
                                nameof(include));

                        _includeStrings.Add(include);
                    }

                    protected void ApplyOrderBy(
                        Expression<Func<T, object>> orderBy)
                    {
                        ArgumentNullException.ThrowIfNull(orderBy);

                        OrderBy = orderBy;
                    }

                    protected void ApplyOrderByDescending(
                        Expression<Func<T, object>> orderByDescending)
                    {
                        ArgumentNullException.ThrowIfNull(orderByDescending);

                        OrderByDescending = orderByDescending;
                    }

                    protected void ApplyPaging(
                        int skip,
                        int take)
                    {
                        if (skip < 0)
                            throw new ArgumentOutOfRangeException(nameof(skip));

                        if (take <= 0)
                            throw new ArgumentOutOfRangeException(nameof(take));

                        Skip = skip;
                        Take = take;
                    }

                    protected void ApplyNoTracking()
                    {
                        AsNoTracking = true;
                    }
                }
                """,

            // 7. Enumeration
            ["Enumerations/Enumeration.cs"] =
                $$"""
                namespace {{baseNamespace}}.Enumerations;

                public abstract class Enumeration : IComparable
                {
                    protected Enumeration(
                        int id,
                        string name)
                    {
                        if (id <= 0)
                            throw new ArgumentOutOfRangeException(nameof(id));

                        if (string.IsNullOrWhiteSpace(name))
                            throw new ArgumentException(
                                "Name is required.",
                                nameof(name));

                        Id = id;
                        Name = name;
                    }

                    public int Id { get; }

                    public string Name { get; }

                    public int CompareTo(object? obj)
                    {
                        if (obj is not Enumeration other)
                            return 1;

                        return Id.CompareTo(other.Id);
                    }

                    public override string ToString()
                    {
                        return Name;
                    }

                    public override bool Equals(object? obj)
                    {
                        if (obj is not Enumeration other)
                            return false;

                        return GetType() == other.GetType() &&
                               Id == other.Id;
                    }

                    public override int GetHashCode()
                    {
                        return HashCode.Combine(GetType(), Id);
                    }

                    public static bool operator ==(
                        Enumeration? left,
                        Enumeration? right)
                    {
                        return Equals(left, right);
                    }

                    public static bool operator !=(
                        Enumeration? left,
                        Enumeration? right)
                    {
                        return !Equals(left, right);
                    }

                    public static IEnumerable<T> GetAll<T>()
                        where T : Enumeration
                    {
                        return typeof(T)
                            .GetFields(
                                BindingFlags.Public |
                                BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                            .Where(field => field.FieldType == typeof(T))
                            .Select(field => field.GetValue(null))
                            .Cast<T>();
                    }
                }
                """,

            // 8. Exceptions
            ["Exceptions/DomainException.cs"] =
                $$"""
                namespace {{baseNamespace}}.Exceptions;

                public class DomainException : Exception
                {
                    public DomainException()
                    {
                    }

                    public DomainException(string message)
                        : base(message)
                    {
                    }

                    public DomainException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                    }
                }
                """,

            ["Exceptions/EntityNotFoundException.cs"] =
                $$"""
                namespace {{baseNamespace}}.Exceptions;

                public class EntityNotFoundException : DomainException
                {
                    public EntityNotFoundException(
                        string entityName,
                        object id)
                        : base($"Entity '{entityName}' with id '{id}' was not found.")
                    {
                        EntityName = entityName;
                        Id = id;
                    }

                    public string EntityName { get; }

                    public object Id { get; }
                }
                """,

            ["Exceptions/BusinessRuleException.cs"] =
                $$"""
                namespace {{baseNamespace}}.Exceptions;

                public class BusinessRuleException : DomainException
                {
                    public BusinessRuleException(
                        string message)
                        : base(message)
                    {
                    }

                    public BusinessRuleException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                    }
                }
                """,

            ["Exceptions/InvalidDomainStateException.cs"] =
                $$"""
                namespace {{baseNamespace}}.Exceptions;

                public class InvalidDomainStateException : DomainException
                {
                    public InvalidDomainStateException(
                        string message)
                        : base(message)
                    {
                    }

                    public InvalidDomainStateException(
                        string message,
                        Exception innerException)
                        : base(message, innerException)
                    {
                    }
                }
                """,

            // 9. Rules
            ["Rules/IBusinessRule.cs"] =
                $$"""
                namespace {{baseNamespace}}.Rules;

                public interface IBusinessRule
                {
                    string Message { get; }

                    bool IsBroken();
                }
                """,

            ["Rules/BusinessRule.cs"] =
                $$"""
                namespace {{baseNamespace}}.Rules;

                public abstract class BusinessRule : IBusinessRule
                {
                    protected BusinessRule(string message)
                    {
                        if (string.IsNullOrWhiteSpace(message))
                            throw new ArgumentException(
                                "Rule message is required.",
                                nameof(message));

                        Message = message;
                    }

                    public string Message { get; }

                    public abstract bool IsBroken();
                }
                """,

            // 10. Auditing
            ["Auditing/AuditAction.cs"] =
                $$"""
                namespace {{baseNamespace}}.Auditing;

                public enum AuditAction
                {
                    None = 0,

                    Create = 1,

                    Update = 2,

                    Delete = 3,

                    Restore = 4,

                    Login = 5,

                    Logout = 6,

                    View = 7,

                    Export = 8,

                    Import = 9
                }
                """,

            ["Auditing/AuditInfo.cs"] =
                $$"""
                namespace {{baseNamespace}}.Auditing;

                public sealed class AuditInfo
                {
                    private AuditInfo()
                    {
                    }

                    public AuditInfo(
                        AuditAction action,
                        DateTime occurredAtUtc,
                        long? userId = null,
                        string? ipAddress = null,
                        string? userAgent = null)
                    {
                        Action = action;
                        OccurredAtUtc = occurredAtUtc;
                        UserId = userId;
                        IpAddress = ipAddress;
                        UserAgent = userAgent;
                    }

                    public AuditAction Action { get; private set; }

                    public DateTime OccurredAtUtc { get; private set; }

                    public long? UserId { get; private set; }

                    public string? IpAddress { get; private set; }

                    public string? UserAgent { get; private set; }
                }
                """
        };
    }
}