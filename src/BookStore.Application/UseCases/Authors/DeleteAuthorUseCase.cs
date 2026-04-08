using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors.Contracts;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Application.UseCases.Authors
{
    internal sealed class DeleteAuthorUseCase(IUnitOfWork unitOfWork) : IDeleteAuthorUseCase
    {
        public async Task<ErrorOr<DeleteAuthorResponse>> ExecuteAsync(AuthorId request, CancellationToken cancellationToken = default)
        {
            Author? author = await unitOfWork.AuthorsRepository.GetByIdAsync(request, cancellationToken);

            if (author is null)
            {
                return Error.NotFound(description: "The author with the specified Id was not found.");
            }

            try
            {
                await unitOfWork.AuthorsRepository.DeleteAsync(author, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return new DeleteAuthorResponse($"Author with Id {request.Value} deleted successfully.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Error.Failure(description: $"An error occurred while deleting the author: {ex.Message}");
            }
        }
    }
}
