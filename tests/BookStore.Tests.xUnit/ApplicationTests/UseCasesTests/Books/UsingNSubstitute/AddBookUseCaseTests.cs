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

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Books.UsingNSubstitute
{
    public static class AddBookUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IBooksRepository _mockBooksRepository;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<AddBookRequest> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockBooksRepository = Substitute.For<IBooksRepository>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<AddBookRequest>>();

                _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.NotNull(result.Value.CreatedBook);
                Assert.Equal(request.Title, result.Value.CreatedBook.Title);
                Assert.Equal(request.Price, result.Value.CreatedBook.Price);
                Assert.Equal(author.Id.Value, result.Value.CreatedBook.Author.Id);

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>());
                _ = _mockBooksRepository.Received(1).AddAsync(Arg.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddBookRequest request = CreateRequest();
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
                _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
            {
                AddBookRequest request = CreateRequest();
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns((Author?)null);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("Author not found.", result.FirstError.Description);

                _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);
                _mockBooksRepository.AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("repository failure")));

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while adding the book: repository failure", result.FirstError.Description);

                _ = _mockBooksRepository.Received(1).AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
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
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IBooksRepository _mockBooksRepository;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<AddBookRequest> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockBooksRepository = Substitute.For<IBooksRepository>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<AddBookRequest>>();

                _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.CreatedBook.Should().NotBeNull();
                result.Value.CreatedBook.Title.Should().Be(request.Title);
                result.Value.CreatedBook.Price.Should().Be(request.Price);
                result.Value.CreatedBook.Author.Id.Should().Be(author.Id.Value);

                _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>());
                _ = _mockBooksRepository.Received(1).AddAsync(Arg.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddBookRequest request = CreateRequest();
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
                _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
            {
                AddBookRequest request = CreateRequest();
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns((Author?)null);

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("Author not found.");

                _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                Author author = CreateAuthor();
                AddBookRequest request = CreateRequest(author.Id.Value);
                AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
                _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);
                _mockBooksRepository.AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("repository failure")));

                ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while adding the book: repository failure");

                _ = _mockBooksRepository.Received(1).AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
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
