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

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    public static class UpdateAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<UpdateAuthorRequest>> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<UpdateAuthorRequest>>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                string newName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(newName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(initialName, author.Name);
                Assert.Equal($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal($"Author with Id '{request.AuthorId}' was not found.", result.FirstError.Description);

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);
                _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("commit failure"));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while updating the author: commit failure", result.FirstError.Description);

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<UpdateAuthorRequest>> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<UpdateAuthorRequest>>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                string newName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(newName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
            {
                AuthorId authorId = AuthorId.NewId();
                string initialName = _faker.Name.FullName();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
                Author author = new(initialName);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                author.Name.Should().Be(initialName);
                result.Value.Message.Should().Be($"Author with Id '{request.AuthorId}' has been successfully updated.");

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be($"Author with Id '{request.AuthorId}' was not found.");

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
                Author author = new(_faker.Name.FullName());
                UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);
                _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("commit failure"));

                ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while updating the author: commit failure");

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }
    }
}
