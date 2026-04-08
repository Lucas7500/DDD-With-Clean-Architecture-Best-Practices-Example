using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using NSubstitute;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingNSubstitute
{
    [TestClass]
    public sealed class DeleteBookUseCaseTests
    {
        private Faker _faker = null!;
        private IUnitOfWork _mockUnitOfWork = null!;
        private IBooksRepository _mockBooksRepository = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockBooksRepository = Substitute.For<IBooksRepository>();

            _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Book with Id {bookId.Value} deleted successfully.", result.Value.Message);

            _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns((Book?)null);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The book with the specified Id was not found.", result.FirstError.Description);

            _ = _mockBooksRepository.DidNotReceive().DeleteAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);
            _mockBooksRepository.DeleteAsync(book, Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("repository failure")));

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the book: repository failure", result.FirstError.Description);

            _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
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
