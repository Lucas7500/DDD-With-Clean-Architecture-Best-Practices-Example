using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    public static class UpdateAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<UpdateAuthorRequest> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<UpdateAuthorRequest>>();

                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                string newName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(newName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(initialName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal($"Author with Id '{request.AuthorId}' was not found.", result.FirstError.Description);

                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);
                _mockUnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("commit failure")));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while updating the author: commit failure", result.FirstError.Description);

                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<UpdateAuthorRequest> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<UpdateAuthorRequest>>();

                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                string newName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(newName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(initialName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be($"Author with Id '{request.AuthorId}' was not found.");

                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);
                _mockUnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("commit failure")));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while updating the author: commit failure");

                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }
        }
    }
}
