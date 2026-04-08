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

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
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
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
            _mockRequestValidator = Substitute.For<IValidator<AddAuthorRequest>>();

            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator
                .ValidateAsync(request, Arg.Any<CancellationToken>())
                .Returns(new FluentValidationResult());

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.False);
            Assert.That(result.Value, Is.Not.Null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Value.CreatedAuthor, Is.Not.Null);
                Assert.That(result.Value.CreatedAuthor.Name, Is.EqualTo(request.Name));
            }

            _ = _mockAuthorsRepository.Received(1)
                .AddAsync(Arg.Is<Author>(a => a.Name == request.Name), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

            _mockRequestValidator
                .ValidateAsync(request, Arg.Any<CancellationToken>())
                .Returns(invalidValidationResult);

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            _ = _mockAuthorsRepository.DidNotReceive().AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator
                .ValidateAsync(request, Arg.Any<CancellationToken>())
                .Returns(new FluentValidationResult());
            _mockAuthorsRepository
                .AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("repository failure")));

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while adding the author: repository failure"));
            }

            _ = _mockAuthorsRepository.Received(1).AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }
    }
}
