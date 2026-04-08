using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FakeItEasy;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingFakeItEasy
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
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockBooksRepository = A.Fake<IBooksRepository>();

            A.CallTo(() => _mockUnitOfWork.BooksRepository).Returns(_mockBooksRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns(book);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual($"Book with Id {bookId.Value} deleted successfully.", result.Value.Message);

            A.CallTo(() => _mockBooksRepository.DeleteAsync(book, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns((Book?)null);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("The book with the specified Id was not found.", result.FirstError.Description);

            A.CallTo(() => _mockBooksRepository.DeleteAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns(book);
            A.CallTo(() => _mockBooksRepository.DeleteAsync(book, A<CancellationToken>._))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while deleting the book: repository failure", result.FirstError.Description);

            A.CallTo(() => _mockBooksRepository.DeleteAsync(book, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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
