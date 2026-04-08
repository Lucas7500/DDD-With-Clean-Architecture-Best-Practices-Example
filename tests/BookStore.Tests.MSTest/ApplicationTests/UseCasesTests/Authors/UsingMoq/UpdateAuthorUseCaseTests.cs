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
using Moq;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestClass]
    public sealed class UpdateAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IAuthorsRepository> _mockAuthorsRepository = null!;
        private Mock<IValidator<UpdateAuthorRequest>> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();
            _mockRequestValidator = new Mock<IValidator<UpdateAuthorRequest>>();

            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newName, author.Name);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(initialName, author.Name);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' was not found.", result.FirstError.Description);

            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
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

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while updating the author: commit failure", result.FirstError.Description);

            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
