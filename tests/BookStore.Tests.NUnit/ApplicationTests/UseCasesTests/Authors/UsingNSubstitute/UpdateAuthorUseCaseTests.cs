using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestFixture]
    public sealed class UpdateAuthorUseCaseTests
    {
        private Faker _faker;
        private IUnitOfWork _mockUnitOfWork;
        private IAuthorsRepository _mockAuthorsRepository;
        private IValidator<UpdateAuthorRequest> _mockRequestValidator;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
            _mockRequestValidator = Substitute.For<IValidator<UpdateAuthorRequest>>();

            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            string initialName = _faker.Name.FullName();
            string newName = _faker.Name.FullName();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
            Author author = new(initialName);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(author.Name, Is.EqualTo(newName));
                Assert.That(result.Value.Message, Is.EqualTo($"Author with Id '{request.AuthorId}' has been successfully updated."));
            }

            _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
        {
            AuthorId authorId = AuthorId.NewId();
            string initialName = _faker.Name.FullName();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
            Author author = new(initialName);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(author.Name, Is.EqualTo(initialName));
                Assert.That(result.Value.Message, Is.EqualTo($"Author with Id '{request.AuthorId}' has been successfully updated."));
            }

            _ = _mockAuthorsRepository.Received(1).GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(invalidValidationResult);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            _ = _mockAuthorsRepository.DidNotReceive().GetByIdAsync(Arg.Any<AuthorId>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns((Author?)null);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo($"Author with Id '{request.AuthorId}' was not found."));
            }

            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            Author author = new(_faker.Name.FullName());
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new FluentValidationResult());
            _mockAuthorsRepository.GetByIdAsync(Arg.Is<AuthorId>(id => id.Value == authorId.Value), Arg.Any<CancellationToken>()).Returns(author);
            _mockUnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("commit failure")));

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while updating the author: commit failure"));
            }

            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }
    }
}
