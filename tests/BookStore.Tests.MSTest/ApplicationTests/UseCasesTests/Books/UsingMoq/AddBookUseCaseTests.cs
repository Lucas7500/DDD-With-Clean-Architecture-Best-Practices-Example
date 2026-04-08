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
using Moq;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingMoq
{
    [TestClass]
    public sealed class AddBookUseCaseTests
    {
        private Faker _faker = null!;
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IBooksRepository> _mockBooksRepository = null!;
        private Mock<IAuthorsRepository> _mockAuthorsRepository = null!;
        private Mock<IValidator<AddBookRequest>> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBooksRepository = new Mock<IBooksRepository>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();
            _mockRequestValidator = new Mock<IValidator<AddBookRequest>>();

            _mockUnitOfWork.Setup(x => x.BooksRepository).Returns(_mockBooksRepository.Object);
            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.IsNotNull(result.Value.CreatedBook);
            Assert.AreEqual(request.Title, result.Value.CreatedBook.Title);
            Assert.AreEqual(request.Price, result.Value.CreatedBook.Price);
            Assert.AreEqual(author.Id.Value, result.Value.CreatedBook.Author.Id);

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockBooksRepository.Verify(x => x.AddAsync(It.Is<Book>(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddBookRequest request = CreateRequest();
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
            AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
        {
            AddBookRequest request = CreateRequest();
            AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("Author not found.", result.FirstError.Description);

            _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == request.AuthorId), It.IsAny<CancellationToken>())).ReturnsAsync(author);
            _mockBooksRepository.Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while adding the book: repository failure", result.FirstError.Description);

            _mockBooksRepository.Verify(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
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
