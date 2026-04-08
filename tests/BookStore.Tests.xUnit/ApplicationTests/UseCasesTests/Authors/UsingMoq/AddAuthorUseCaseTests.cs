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

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    public static class AddAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<AddAuthorRequest>> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<AddAuthorRequest>>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator
                    .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.NotNull(result.Value.CreatedAuthor);
                Assert.Equal(request.Name, result.Value.CreatedAuthor.Name);

                _mockAuthorsRepository.Verify(
                    x => x.AddAsync(It.Is<Author>(a => a.Name == request.Name), It.IsAny<CancellationToken>()),
                    Moq.Times.Once);

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                _mockRequestValidator
                    .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
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

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while adding the author: repository failure", result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;
            private readonly Mock<IValidator<AddAuthorRequest>> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();
                _mockRequestValidator = new Mock<IValidator<AddAuthorRequest>>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);

                _mockRequestValidator
                    .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.CreatedAuthor.Should().NotBeNull();
                result.Value.CreatedAuthor.Name.Should().Be(request.Name);

                _mockAuthorsRepository.Verify(
                    x => x.AddAsync(It.Is<Author>(a => a.Name == request.Name), It.IsAny<CancellationToken>()),
                    Moq.Times.Once);

                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork.Object, _mockRequestValidator.Object);
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                _mockRequestValidator
                    .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
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

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while adding the author: repository failure");

                _mockAuthorsRepository.Verify(x => x.AddAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }
    }
}
