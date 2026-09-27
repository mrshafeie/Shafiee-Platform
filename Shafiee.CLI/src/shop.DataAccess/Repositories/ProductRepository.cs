namespace shop.DataAccess.Repositories;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using shop.Domain.Entities;
using shop.DataAccess.Context;

public interface IProductRepository
{
    Product? GetById(long id);
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    IReadOnlyList<Product> GetAll();
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(Product entity);
    Task AddAsync(Product entity, CancellationToken cancellationToken = default);
    void AddRange(IEnumerable<Product> entities);
    Task AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    void Update(Product entity);
    void Delete(Product entity);
}

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationEfContext _context;

    public ProductRepository(ApplicationEfContext context)
    {
        _context = context;
    }

    public Product? GetById(long id)
    {
        return _context.Products.Find(id);
    }

    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FindAsync(new object[] { id }, cancellationToken);
    }

    public IReadOnlyList<Product> GetAll()
    {
        return _context.Products.AsNoTracking().ToList();
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public void Add(Product entity)
    {
        _context.Products.Add(entity);
    }

    public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(entity, cancellationToken);
    }

    public void AddRange(IEnumerable<Product> entities)
    {
        _context.Products.AddRange(entities);
    }

    public async Task AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(Product entity)
    {
        _context.Products.Update(entity);
    }

    public void Delete(Product entity)
    {
        _context.Products.Remove(entity);
    }
}