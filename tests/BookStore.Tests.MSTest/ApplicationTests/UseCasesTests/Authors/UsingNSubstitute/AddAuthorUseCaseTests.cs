using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestClass]
    public sealed class AddAuthorUseCaseTests
    {
        private Faker _faker = null!;
        private IUnitOfWork _mockUnitOfWork = null!;
        private IAuthorsRepository _mockAuthorsRepository = null!;
        private IValidator<AddAuthorRequest> _mockRequestValidator = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();
            _mockRequestValidator = Substitute.For<IValidator<AddAuthorRequest>>();

            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);

            _mockRequestValidator
                .ValidateAsync(request, Arg.Any<CancellationToken>())
                .Returns(new FluentValidationResult());

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Value);
            Assert.IsNotNull(result.Value.CreatedAuthor);
            Assert.AreEqual(request.Name, result.Value.CreatedAuthor.Name);

            _ = _mockAuthorsRepository.Received(1)
                .AddAsync(Arg.Is<Author>(a => a.Name == request.Name), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenInvalidRequest_ShouldReturnValidationError()
        {
            AddAuthorRequest request = new() { Name = _faker.Name.FullName() };
            AddAuthorUseCase useCase = new(_mockUnitOfWork, _mockRequestValidator);
            FluentValidationResult invalidValidationResult = new([new ValidationFailure(nameof(AddAuthorRequest.Name), "Name is required")]);

            _mockRequestValidator
                .ValidateAsync(request, Arg.Any<CancellationToken>())
                .Returns(invalidValidationResult);

            ErrorOr<AddAuthorResponse> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Validation, result.FirstError.Type);
            Assert.AreEqual(invalidValidationResult.ToString(), result.FirstError.Description);

            _ = _mockAuthorsRepository.DidNotReceive().AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
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

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("An error occurred while adding the author: repository failure", result.FirstError.Description);

            _ = _mockAuthorsRepository.Received(1).AddAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }
    }
}
