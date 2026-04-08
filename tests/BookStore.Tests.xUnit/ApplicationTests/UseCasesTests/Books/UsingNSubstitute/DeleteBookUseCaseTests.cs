using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Books.UsingNSubstitute
{
    public static class DeleteBookUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IBooksRepository _mockBooksRepository;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockBooksRepository = Substitute.For<IBooksRepository>();

                _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.Equal($"Book with Id {bookId.Value} deleted successfully.", result.Value.Message);

                _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns((Book?)null);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("The book with the specified Id was not found.", result.FirstError.Description);

                _ = _mockBooksRepository.DidNotReceive().DeleteAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);
                _mockBooksRepository.DeleteAsync(book, Arg.Any<CancellationToken>())
                    .Returns(Task.FromException(new InvalidOperationException("repository failure")));

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while deleting the book: repository failure", result.FirstError.Description);

                _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }

            private static Book CreateBook()
            {
                string title = _faker.Lorem.Sentence();
                decimal price = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author author = new(authorName);

                return new Book(title, price, author);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IBooksRepository _mockBooksRepository;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockBooksRepository = Substitute.For<IBooksRepository>();

                _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.Message.Should().Be($"Book with Id {bookId.Value} deleted successfully.");

                _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns((Book?)null);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("The book with the specified Id was not found.");

                _ = _mockBooksRepository.DidNotReceive().DeleteAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork);

                _mockBooksRepository.GetByIdAsync(bookId, Arg.Any<CancellationToken>()).Returns(book);
                _mockBooksRepository.DeleteAsync(book, Arg.Any<CancellationToken>())
                    .Returns(Task.FromException(new InvalidOperationException("repository failure")));

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while deleting the book: repository failure");

                _ = _mockBooksRepository.Received(1).DeleteAsync(book, Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }

            private static Book CreateBook()
            {
                string title = _faker.Lorem.Sentence();
                decimal price = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author author = new(authorName);

                return new Book(title, price, author);
            }
        }
    }
}
