using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors.Contracts;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;

namespace BookStore.Application.UseCases.Authors
{
    internal sealed class UpdateAuthorUseCase(IUnitOfWork unitOfWork, IValidator<UpdateAuthorRequest> requestValidator) : IUpdateAuthorUseCase
    {
        public async Task<ErrorOr<UpdateAuthorResponse>> ExecuteAsync(UpdateAuthorRequest request, CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await requestValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Error.Validation(description: validationResult.ToString());
            }

            AuthorId authorId = new(request.AuthorId);
            Author? author = await unitOfWork.AuthorsRepository.GetByIdAsync(authorId, cancellationToken);

            if (author is null)
            {
                return Error.NotFound(description: $"Author with Id '{request.AuthorId}' was not found.");
            }

            if (request.NewName is not null)
            {
                author.ChangeName(request.NewName);
            }

            try
            {
                await unitOfWork.CommitAsync(cancellationToken);
                return new UpdateAuthorResponse($"Author with Id '{request.AuthorId}' has been successfully updated.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Error.Failure(description: $"An error occurred while updating the author: {ex.Message}");
            }
        }
    }
}
