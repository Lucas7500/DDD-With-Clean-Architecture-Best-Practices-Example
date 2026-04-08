using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Books.UsingMoq
{
    [TestFixture]
    public sealed class DeleteBookUseCaseTests
    {
        private Faker _faker;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBooksRepository> _mockBooksRepository;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBooksRepository = new Mock<IBooksRepository>();

            _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
        }

        [Test]
        public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

            _mockBooksRepository
                .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(book);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value.Message, Is.EqualTo($"Book with Id {bookId.Value} deleted successfully."));
            }

            _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

            _mockBooksRepository
                .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Book?)null);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo("The book with the specified Id was not found."));
            }

            _mockBooksRepository.Verify(x => x.DeleteAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
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

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while deleting the book: repository failure"));
            }

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
