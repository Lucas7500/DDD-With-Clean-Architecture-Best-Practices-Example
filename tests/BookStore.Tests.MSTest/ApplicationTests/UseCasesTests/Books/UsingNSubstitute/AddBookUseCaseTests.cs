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
using NSubstitute;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingNSubstitute
{
    [TestClass]
    public sealed class AddBookUseCaseTests
    {
        private Faker _faker = null!;
        private IUnitOfWork _mockUnitOfWork = null!;
        private IBooksRepository _mockBooksRepository = null!;
        private IAuthorsRepository _mockAuthorsRepository = null!;
        private IValidator<AddBookRequest> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockBooksRepository = Substitute.For<IBooksRepository>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
            _mockRequestValidator = Substitute.For<IValidator<AddBookRequest>>();

            _mockUnitOfWork.BooksRepository.Returns(_mockBooksRepository);
            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.IsNotNull(result.Value.CreatedBook);
            Assert.AreEqual(request.Title, result.Value.CreatedBook.Title);
            Assert.AreEqual(request.Price, result.Value.CreatedBook.Price);
            Assert.AreEqual(author.Id.Value, result.Value.CreatedBook.Author.Id);

            _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>());
            _ = _mockBooksRepository.Received(1).AddAsync(Arg.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddBookRequest request = CreateRequest();
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
            _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
        {
            AddBookRequest request = CreateRequest();
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns((Author?)null);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("Author not found.", result.FirstError.Description);

            _ = _mockBooksRepository.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == request.AuthorId), Arg.Any<CancellationToken>()).Returns(author);
            _mockBooksRepository.AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("repository failure")));

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while adding the book: repository failure", result.FirstError.Description);

            _ = _mockBooksRepository.Received(1).AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }

        private AddBookRequest CreateRequest(Guid? authorId = null)
        {
            return new AddBookRequest
            {
                Title = _faker.Lorem.Sentence(),
                AuthorId = authorId ?? Guid.NewGuid(),
                Price = _faker.Random.Decimal(1, 100)
            };
        }

        private Author CreateAuthor()
        {
            string authorName = _faker.Name.FullName();
            return new Author(authorName);
        }
    }
}
