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

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestFixture]
    public sealed class UpdateAuthorUseCaseTests
    {
        private Faker _faker;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IAuthorsRepository> _mockAuthorsRepository;
        private Mock<IValidator<UpdateAuthorRequest>> _mockRequestValidator;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();
            _mockRequestValidator = new Mock<IValidator<UpdateAuthorRequest>>();

            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequestWithNewName_ShouldUpdateAuthorNameAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            string initialName = _faker.Name.FullName();
            string newName = _faker.Name.FullName();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = newName };
            Author author = new(initialName);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(author.Name, Is.EqualTo(newName));
                Assert.That(result.Value.Message, Is.EqualTo($"Author with Id '{request.AuthorId}' has been successfully updated."));
            }

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequestWithoutNewName_ShouldCommitWithoutChangingAuthorName()
        {
            AuthorId authorId = AuthorId.NewId();
            string initialName = _faker.Name.FullName();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = null };
            Author author = new(initialName);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(author.Name, Is.EqualTo(initialName));
                Assert.That(result.Value.Message, Is.EqualTo($"Author with Id '{request.AuthorId}' has been successfully updated."));
            }

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            UpdateAuthorRequest request = new() { AuthorId = Guid.NewGuid(), NewName = _faker.Name.FullName() };
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(UpdateAuthorRequest.NewName), "Name is required")]);
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(invalidValidationResult);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            _mockAuthorsRepository.Verify(x => x.GetByIdAsync(It.IsAny<AuthorId>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync((Author?)null);

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo($"Author with Id '{request.AuthorId}' was not found."));
            }

            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileCommitting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            UpdateAuthorRequest request = new() { AuthorId = authorId.Value, NewName = _faker.Name.FullName() };
            Author author = new(_faker.Name.FullName());
            UpdateAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository.Setup(x => x.GetByIdAsync(It.Is<AuthorId>(id => id.Value == authorId.Value), It.IsAny<CancellationToken>())).ReturnsAsync(author);
            _mockUnitOfWork.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("commit failure"));

            ErrorOr<UpdateAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while updating the author: commit failure"));
            }

            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
