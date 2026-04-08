using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.Persistence.Contracts;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    public static class AddAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<AddAuthorRequest> _mockRequestValidator;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
                _mockRequestValidator = A.Fake<IValidator<AddAuthorRequest>>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.NotNull(result.Value.CreatedAuthor);
                Assert.Equal(request.Name, result.Value.CreatedAuthor.Name);

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>.That.Matches(a => a.Name == request.Name), A<CancellationToken>._))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(new FluentValidationResult());

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._))
                    .ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while adding the author: repository failure", result.FirstError.Description);

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<AddAuthorRequest> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();
                _mockRequestValidator = A.Fake<IValidator<AddAuthorRequest>>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.CreatedAuthor.Should().NotBeNull();
                result.Value.CreatedAuthor.Name.Should().Be(request.Name);

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>.That.Matches(a => a.Name == request.Name), A<CancellationToken>._))
                    .MustHaveHappenedOnceExactly();

                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileAdding_ShouldRollbackAndReturnFailureError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                A.CallTo(() => _mockRequestValidator.ValidateAsync(request, A<CancellationToken>._))
                    .Returns(new FluentValidationResult());
                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._))
                    .ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while adding the author: repository failure");

                A.CallTo(() => _mockAuthorsRepository.AddAsync(A<Author>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }
    }
}
