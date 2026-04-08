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

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
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
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<AddAuthorRequest>>();

                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator
                    .ValidateAsync(request, Arg.Any<CancellationToken>())
                    .Returns(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.NotNull(result.Value.CreatedAuthor);
                Assert.Equal(request.Name, result.Value.CreatedAuthor.Name);

                _ = _mockAuthorsRepository.Received(1)
                    .AddAsync(Arg.Is<Author>(a => a.Name == request.Name), Arg.Any<CancellationToken>());

                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                _mockRequestValidator
                    .ValidateAsync(request, Arg.Any<CancellationToken>())
                    .Returns(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Validation, result.FirstError.Type);
                Assert.Equal(invalidValidationResult.ToString(), result.FirstError.Description);

                _ = _mockAuthorsRepository.DidNotReceive().AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
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

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while adding the author: repository failure", result.FirstError.Description);

                _ = _mockAuthorsRepository.Received(1).AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;
            private readonly IValidator<AddAuthorRequest> _mockRequestValidator;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = Substitute.For<IUnitOfWork>();
                _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
                _mockRequestValidator = Substitute.For<IValidator<AddAuthorRequest>>();

                _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

                _mockRequestValidator
                    .ValidateAsync(request, Arg.Any<CancellationToken>())
                    .Returns(new FluentValidationResult());

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.CreatedAuthor.Should().NotBeNull();
                result.Value.CreatedAuthor.Name.Should().Be(request.Name);

                _ = _mockAuthorsRepository.Received(1)
                    .AddAsync(Arg.Is<Author>(a => a.Name == request.Name), Arg.Any<CancellationToken>());
                
                _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
            {
                AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
                AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
                FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

                _mockRequestValidator
                    .ValidateAsync(request, Arg.Any<CancellationToken>())
                    .Returns(invalidValidationResult);

                ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Validation);
                result.FirstError.Description.Should().Be(invalidValidationResult.ToString());

                _ = _mockAuthorsRepository.DidNotReceive().AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
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

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while adding the author: repository failure");

                _ = _mockAuthorsRepository.Received(1).AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
                _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
            }
        }
    }
}
