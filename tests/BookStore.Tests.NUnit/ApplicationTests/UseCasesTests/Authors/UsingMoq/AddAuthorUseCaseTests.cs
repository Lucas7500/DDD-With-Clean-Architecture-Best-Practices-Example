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

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestFixture]
    public sealed class AddAuthorUseCaseTests
    {
        private Faker _faker;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IAuthorsRepository> _mockAuthorsRepository;
        private Mock<IValidator<AddAuthorRequest>> _mockRequestValidator;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();
            _mockRequestValidator = new Mock<IValidator<AddAuthorRequest>>();

            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidationResult());

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

            _mockAuthorsRepository.Verify(
                x => x.AddAsync(It.Is<Author>(a => a.Name == request.Name), It.IsAny<CancellationToken>()),
                Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

            _mockRequestValidator
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidValidationResult);

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Validation));
                Assert.That(result.FirstError.Description, Is.EqualTo(invalidValidationResult.ToString()));
            }

            _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidationResult());
            _mockAuthorsRepository
                .Setup(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while adding the author: repository failure"));
            }

            _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
