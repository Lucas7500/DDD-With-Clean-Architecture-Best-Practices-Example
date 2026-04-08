using BookStore.Application.DTOs.Books.Requests;
using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.UseCases.Books;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.Persistence.Contracts.Books;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Books.UsingMoq
{
    public static class AddBookUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IBooksRepository> _mockBooksRepository;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<AddBookRequest>> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockBooksRepository = new Mock<IBooksRepository>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<AddBookRequest>>();

                _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.NotNull(result.Value.CreatedBook);
                Assert.Equal(request.Title, result.Value.CreatedBook.Title);
                Assert.Equal(request.Price, result.Value.CreatedBook.Price);
                Assert.Equal(author.Id.Value, result.Value.CreatedBook.Author.Id);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockBooksRepository.Verify(x => x.AddAsync(It.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddBookRequest request = CreateRequest();
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
            {
                AddBookRequest request = CreateRequest();
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("Author not found.", result.FirstError.Description);

                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);
                _mockBooksRepository.Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while adding the book: repository failure", result.FirstError.Description);

                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            private static AddBookRequest CreateRequest(Guid? authorId = null)
            {
                return new AddBookRequest
                {
                    Title = _faker.Lorem.Sentence(),
                    AuthorId = authorId ?? Guid.NewGuid(),
                    Price = _faker.Random.Decimal(1, 100)
                };
            }

            private static Author CreateAuthor()
            {
                string authorName = _faker.Name.FullName();
                return new Author(authorName);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IBooksRepository> _mockBooksRepository;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<AddBookRequest>> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockBooksRepository = new Mock<IBooksRepository>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<AddBookRequest>>();

                _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.CreatedBook.Should().NotBeNull();
                result.Value.CreatedBook.Title.Should().Be(request.Title);
                result.Value.CreatedBook.Price.Should().Be(request.Price);
                result.Value.CreatedBook.Author.Id.Should().Be(author.Id.Value);

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockBooksRepository.Verify(x => x.AddAsync(It.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddBookRequest request = CreateRequest();
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
            {
                AddBookRequest request = CreateRequest();
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("Author not found.");

                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
                _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);
                _mockBooksRepository.Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while adding the book: repository failure");

                _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            private static AddBookRequest CreateRequest(Guid? authorId = null)
            {
                return new AddBookRequest
                {
                    Title = _faker.Lorem.Sentence(),
                    AuthorId = authorId ?? Guid.NewGuid(),
                    Price = _faker.Random.Decimal(1, 100)
                };
            }

            private static Author CreateAuthor()
            {
                string authorName = _faker.Name.FullName();
                return new Author(authorName);
            }
        }
    }
}
