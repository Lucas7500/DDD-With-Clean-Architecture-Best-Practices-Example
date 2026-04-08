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

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
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
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
                _mockRequestValidator = A.Fake<IValidator<UpdateAuthorRequest>>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
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

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(newName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(initialName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal($"Author with Id '{request.AuthorId}' was not found.", result.FirstError.Description);

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("commit failure"));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while updating the author: commit failure", result.FirstError.Description);

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<UpdateAuthorRequest> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
                _mockRequestValidator = A.Fake<IValidator<UpdateAuthorRequest>>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
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

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(newName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(initialName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be($"Author with Id '{request.AuthorId}' was not found.");

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns(author);
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("commit failure"));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while updating the author: commit failure");

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }
    }
}
