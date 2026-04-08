using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using Moq;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestClass]
    public sealed class DeleteAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IAuthorsRepository> _mockAuthorsRepository = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();

            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

            _mockAuthorsRepository
                .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Author with Id {authorId.Value} deleted successfully.", result.Value.Message);

            _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

            _mockAuthorsRepository
                .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Author?)null);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The author with the specified Id was not found.", result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.DeleteAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

            _mockAuthorsRepository
                .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);
            _mockAuthorsRepository
                .Setup(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the author: repository failure", result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
