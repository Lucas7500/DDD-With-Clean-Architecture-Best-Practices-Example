using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Domain.Persistence.Requests;
using BookStore.Domain.Persistence.Responses;
using BookStore.Domain.ValueObjects;

namespace BookStore.Application.QueryServices.Contracts
{
    public interface IAuthorsQueryService
    {
        Task<PagedResult<AuthorResponse>> GetAllAsync(QueryRequest request, CancellationToken ct = default);
        Task<AuthorResponse?> GetByIdAsync(AuthorId id, CancellationToken ct = default);
        Task<long> CountAsync(CancellationToken ct = default);
    }
}
