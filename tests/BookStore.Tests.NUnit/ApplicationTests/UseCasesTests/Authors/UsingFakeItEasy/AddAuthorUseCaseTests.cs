using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestFixture]
    public sealed class AddAuthorUseCaseTests
    {
        private Faker _faker;
        private IUnitOfWork _mockUnitOfWork;
        private IAuthorsRepository _mockAuthorsRepository;
        private IValidator<AddAuthorRequest> _mockRequestValidator;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = A.Fake<IUnitOfWork>();
            _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
            _mockRequestValidator = A.Fake<IValidator<AddAuthorRequest>>();

            A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                .Returns(new FluentValidationResult());

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Not.Null);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.CreatedAuthor, Is.Not.Null);
                Assert.That(result.Value.CreatedAuthor.Name, Is.EqualTo(request.Name));
            }

            A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>.That.Matches(a => a.Name == request.Name), A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
            
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                .Returns(invalidValidationResult);

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                .Returns(new FluentValidationResult());
            A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while adding the author: repository failure"));
            }

            A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
