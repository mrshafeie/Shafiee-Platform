namespace shop.Business.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using shop.Business.Contracts;
using shop.Business.DTOs;
using shop.DataAccess.Repositories;
using shop.DataAccess.UnitOfWork;
using shop.Domain.Entities;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public Product? GetById(long id)
    {
        return _repository.GetById(id);
    }

    public Task<Product?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public ProductDto? GetDtoById(long id)
    {
        var entity = _repository.GetById(id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<ProductDto?> GetDtoByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public IReadOnlyList<ProductDto> GetAll()
    {
        return _repository.GetAll().Select(MapToDto).ToList();
    }

    public long Insert(ProductDto dto)
    {
        var entity = MapToEntity(dto);
        _repository.Add(entity);
        _unitOfWork.SaveChanges();
        return entity.Id;
    }

    public async Task<long> InsertAsync(
        ProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(dto);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public void InsertMany(IEnumerable<ProductDto> dtos)
    {
        var entities = dtos.Select(MapToEntity).ToList();
        _repository.AddRange(entities);
        _unitOfWork.SaveChanges();
    }

    public async Task InsertManyAsync(
        IEnumerable<ProductDto> dtos,
        CancellationToken cancellationToken = default)
    {
        var entities = dtos.Select(MapToEntity).ToList();
        await _repository.AddRangeAsync(entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public bool Update(ProductDto dto)
    {
        var entity = _repository.GetById(dto.Id);
        if (entity is null) return false;

        MapToEntity(dto, entity);
        _repository.Update(entity);
        return _unitOfWork.SaveChanges() > 0;
    }

    public async Task<bool> UpdateAsync(
        ProductDto dto,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken);
        if (entity is null) return false;

        MapToEntity(dto, entity);
        _repository.Update(entity);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    public void Delete(long id)
    {
        var entity = _repository.GetById(id);
        if (entity is null) return;

        _repository.Delete(entity);
        _unitOfWork.SaveChanges();
    }

    public async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return;

        _repository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public bool SoftDelete(long id)
    {
        var entity = _repository.GetById(id);
        if (entity is null) return false;

        entity.IsActive = false;
        _repository.Update(entity);
        return _unitOfWork.SaveChanges() > 0;
    }

    public async Task<bool> SoftDeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        entity.IsActive = false;
        _repository.Update(entity);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    private static ProductDto MapToDto(Product entity)
    {
        return new ProductDto
        {
             = entity.,
             = entity.,
             = entity.,
             = entity.,
             = entity.,
        };
    }

    private static Product MapToEntity(ProductDto dto)
    {
        return new Product
        {
             = dto.,
             = dto.,
             = dto.,
             = dto.,
             = dto.,
            CreatedAt = System.DateTime.UtcNow
        };
    }

    private static void MapToEntity(ProductDto dto, Product entity)
    {
        entity. = dto.;
        entity. = dto.;
        entity. = dto.;
        entity. = dto.;
        entity. = dto.;
        entity.UpdatedAt = System.DateTime.UtcNow;
    }
}