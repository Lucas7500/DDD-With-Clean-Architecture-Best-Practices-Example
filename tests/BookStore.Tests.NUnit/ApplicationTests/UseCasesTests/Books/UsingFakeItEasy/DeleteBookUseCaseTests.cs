using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Books.UsingFakeItEasy
{
    [TestFixture]
    public sealed class DeleteBookUseCaseTests
    {
        private Faker _faker;
        private IUnitOfWork _mockUnitOfWork;
        private IBooksRepository _mockBooksRepository;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockBooksRepository = A.Fake<IBooksRepository>();

            A.CallTo(() => _mockUnitOfWork.BooksRepository).Returns(_mockBooksRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns(book);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value.Message, Is.EqualTo($"Book with Id {bookId.Value} deleted successfully."));
            }

            A.CallTo(() => _mockBooksRepository.DeleteAsync(book, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns((Book?)null);

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo("The book with the specified Id was not found."));
            }

            A.CallTo(() => _mockBooksRepository.DeleteAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            BookId bookId = new BookId(_faker.Random.Int(min: 1));
            Book book = CreateBook();
            DeleteBookUseCase useCase = new(_mockUnitOfWork);

            A.CallTo(() => _mockBooksRepository.GetByIdAsync(bookId, A<CancellationToken>._)).Returns(book);
            A.CallTo(() => _mockBooksRepository.DeleteAsync(book, A<CancellationToken>._))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while deleting the book: repository failure"));
            }

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
