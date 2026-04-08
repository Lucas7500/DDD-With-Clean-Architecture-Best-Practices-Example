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

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Books.UsingFakeItEasy
{
    [TestFixture]
    public sealed class AddBookUseCaseTests
    {
        private Faker _faker;
        private IUnitOfWork _mockUnitOfWork;
        private IBooksRepository _mockBooksRepository;
        private IAuthorsRepository _mockAuthorsRepository;
        private IValidator<AddBookRequest> _mockRequestValidator;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockBooksRepository = A.Fake<IBooksRepository>();
            _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
            _mockRequestValidator = A.Fake<IValidator<AddBookRequest>>();

            A.CallTo(() => _mockUnitOfWork.BooksRepository).Returns(_mockBooksRepository);
            A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddBookAndCommit()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns(author);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value.CreatedBook, Is.Not.Null);
                Assert.That(result.Value.CreatedBook.Title, Is.EqualTo(request.Title));
                Assert.That(result.Value.CreatedBook.Price, Is.EqualTo(request.Price));
                Assert.That(result.Value.CreatedBook.Author.Id, Is.EqualTo(author.Id.Value));
            }

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>.That.Matches(b => b.Title == request.Title && b.Price == request.Price && b.Author == author), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddBookRequest request = CreateRequest();
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddBookRequest.Title), "Title is required")]);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(invalidValidationResult);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenAuthorNotFound_ShouldReturnNotFoundError()
        {
            AddBookRequest request = CreateRequest();
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns((Author?)null);

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo("Author not found."));
            }

            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            Author author = CreateAuthor();
            AddBookRequest request = CreateRequest(author.Id.Value);
            AddBookUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._)).Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(A<AuthorId>.That.Matches(id => id.Value == request.AuthorId), A<CancellationToken>._)).Returns(author);
            A.CallTo(() => _mockBooksRepository.AddAsync(A<Book>._, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<AddBookResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while adding the book: repository failure"));
            }

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
