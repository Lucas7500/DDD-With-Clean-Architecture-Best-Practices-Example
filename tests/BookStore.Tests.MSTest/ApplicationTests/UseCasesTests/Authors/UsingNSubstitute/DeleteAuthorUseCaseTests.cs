using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using NSubstitute;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
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
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();

            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(author);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Author with Id {authorId.Value} deleted successfully.", result.Value.Message);

            _ = _mockAuthorsRepository.Received(1).DeleteAsync(author, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns((Author?)null);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The author with the specified Id was not found.", result.FirstError.Description);

            _ = _mockAuthorsRepository.DidNotReceive().DeleteAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(author);
            _mockAuthorsRepository.DeleteAsync(author, Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("repository failure")));

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the author: repository failure", result.FirstError.Description);

            _ = _mockAuthorsRepository.Received(1).DeleteAsync(author, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }
    }
}
