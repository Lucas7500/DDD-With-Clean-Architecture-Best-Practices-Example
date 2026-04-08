using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.Mappers;
using BookStore.Application.UseCases.Authors.Contracts;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;

namespace BookStore.Application.UseCases.Authors
{
    internal sealed class AddAuthorUseCase(IUnitOfWork unitOfWork, IValidator<AddAuthorRequest> requestValidator) : IAddAuthorUseCase
    {
        public async Task<ErrorOr<AddAuthorResponse>> ExecuteAsync(AddAuthorRequest request, CancellationToken cancellationToken = default)
        {
            ValidationResult validationResult = await requestValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Error.Validation(description: validationResult.ToString());
            }

            Author newAuthor = new(request.Name);

            try
            {
                await unitOfWork.AuthorsRepository.AddAsync(newAuthor, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return new AddAuthorResponse(newAuthor.ToResponse());
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Error.Failure(description: $"An error occurred while adding the author: {ex.Message}");
            }
        }
    }
}
