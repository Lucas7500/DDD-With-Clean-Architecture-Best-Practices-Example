using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestClass]
    public sealed class UpdateAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private IUnitOfWork _mockUnitOfWork = null!;
        private IAuthorsRepository _mockAuthorsRepository = null!;
        private IValidator<UpdateAuthorRequest> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
            _mockRequestValidator = A.Fake<IValidator<UpdateAuthorRequest>>();

            A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newName, author.Name);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(initialName, author.Name);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' has been successfully updated.", result.Value.Message);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(invalidValidationResult);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == authorId.Value), A<CancellationToken>._)).Returns((Author?)null);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual($"Author with Id '{request.AuthorId}' was not found.", result.FirstError.Description);

            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
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

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while updating the author: commit failure", result.FirstError.Description);

            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
