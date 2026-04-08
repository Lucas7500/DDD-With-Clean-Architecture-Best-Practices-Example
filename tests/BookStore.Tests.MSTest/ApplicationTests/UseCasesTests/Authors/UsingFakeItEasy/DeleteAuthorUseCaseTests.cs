using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FakeItEasy;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestClass]
    public sealed class DeleteAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private IUnitOfWork _mockUnitOfWork = null!;
        private IAuthorsRepository _mockAuthorsRepository = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockAuthorsRepository = A.Fake<IAuthorsRepository>();

            A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Author with Id {authorId.Value} deleted successfully.", result.Value.Message);

            A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns((Author?)null);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The author with the specified Id was not found.", result.FirstError.Description);

            A.CallTo(() => _mockAuthorsRepository.DeleteAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);
            A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the author: repository failure", result.FirstError.Description);

            A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
