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
using FakeItEasy;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Books.UsingFakeItEasy
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
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockBooksRepository = A.Fake<IBooksRepository>();
            _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
            _mockRequestValidator = A.Fake<IValidator<AddBookRequest>>();

            A.CallTo(() => _mockUnitOfWork.BooksRepository).Returns(_mockBooksRepository);
            A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns(author);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.IsNotNull(result.Value.CreatedBook);
            Assert.AreEqual(request.Title, result.Value.CreatedBook.Title);
            Assert.AreEqual(request.Price, result.Value.CreatedBook.Price);
            Assert.AreEqual(author.Id.Value, result.Value.CreatedBook.Author.Id);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>.That.Matches(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddBookRequest request = CreateRequest();
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(invalidValidationResult);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
        {
            AddBookRequest request = CreateRequest();
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns((Author?)null);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual("Author not found.", result.FirstError.Description);

            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns(author);
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while adding the book: repository failure", result.FirstError.Description);

            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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
