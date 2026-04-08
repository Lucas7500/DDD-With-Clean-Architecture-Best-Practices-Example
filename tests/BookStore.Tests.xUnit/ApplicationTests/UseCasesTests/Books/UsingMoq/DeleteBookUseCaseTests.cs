using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Books.UsingMoq
{
    public static class DeleteBookUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IBooksRepository> _mockBooksRepository;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockBooksRepository = new Mock<IBooksRepository>();

                _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

                _mockBooksRepository
                    .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(book);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.Equal($"Book with Id {bookId.Value} deleted successfully.", result.Value.Message);

                _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

                _mockBooksRepository
                    .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Book?)null);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("The book with the specified Id was not found.", result.FirstError.Description);

                _mockBooksRepository.Verify(x => x.DeleteAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
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

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while deleting the book: repository failure", result.FirstError.Description);

                _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
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
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IBooksRepository> _mockBooksRepository;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockBooksRepository = new Mock<IBooksRepository>();

                _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingBook_ShouldDeleteBookAndCommit()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                Book book = CreateBook();
                DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

                _mockBooksRepository
                    .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(book);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.Message.Should().Be($"Book with Id {bookId.Value} deleted successfully.");

                _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingBook_ShouldReturnNotFoundError()
            {
                BookId bookId = new BookId(_faker.Random.Int(min: 1));
                DeleteBookUseCase useCase = new(_mockUnitOfWork.Object);

                _mockBooksRepository
                    .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Book?)null);

                ErrorOr<DeleteBookResponse> result = await useCase.ExecuteAsync(bookId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("The book with the specified Id was not found.");

                _mockBooksRepository.Verify(x => x.DeleteAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
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

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while deleting the book: repository failure");

                _mockBooksRepository.Verify(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
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
