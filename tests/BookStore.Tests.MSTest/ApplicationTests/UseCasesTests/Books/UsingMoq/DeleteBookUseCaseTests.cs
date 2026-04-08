using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using Moq;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingMoq
{
    [TestClass]
    public sealed class DeleteBookUseCaseTests
    {
        private Faker _faker = null!;
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IBooksRepository> _mockBooksRepository = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBooksRepository = new Mock<IBooksRepository>();

            _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

            _mockBooksRepository
                .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(book);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Book with Id {bookId.Value} deleted successfully.", result.Value.Message);

            _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

            _mockBooksRepository
                .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Book?)null);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The book with the specified Id was not found.", result.FirstError.Description);

            _mockBooksRepository.Verify(x => x.DeleteAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

            _mockBooksRepository
                .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(book);
            _mockBooksRepository
                .Setup(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the book: repository failure", result.FirstError.Description);

            _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        private Book CreateBook()
        {
            string title = _faker.Lorem.Sentence();
            decimal price = _faker.Random.Decimal(1, 100);
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            return new Book(title, price, author);
        }
    }
}
