using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestClass]
    public sealed class AddAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IAuthorsRepository> _mockAuthorsRepository = null!;
        private Mock<IValidator<AddAuthorRequest>> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuthorsRepository = new Mock<IAuthorsRepository>();
            _mockRequestValidator = new Mock<IValidator<AddAuthorRequest>>();

            _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

            _mockRequestValidator
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidationResult());

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.IsNotNull(result.Value.CreatedAuthor);
            Assert.AreEqual(request.Name, result.Value.CreatedAuthor.Name);

            _mockAuthorsRepository.Verify(
                x => x.AddAsync(It.Is<Author>(a => a.Name == request.Name), It.IsAny<CancellationToken>()),
                Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

            _mockRequestValidator
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidValidationResult);

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
        }

        [TestMethod]
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

            Assert.IsTrue(result.IsError);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while adding the author: repository failure", result.FirstError.Description);

            _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
