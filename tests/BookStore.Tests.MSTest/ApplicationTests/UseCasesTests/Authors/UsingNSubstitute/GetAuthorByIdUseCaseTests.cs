using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;
using NSubstitute;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestClass]
    public sealed class GetAuthorByIdUseCaseTests
    {
        private Faker _faker = null!;
        private IAuthorsQueryService _mockAuthorsQueryService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
            _mockAuthorsQueryService = Substitute.For<IAuthorsQueryService>();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
        {
            AuthorId authorId = AuthorId.NewId();
            AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(authorResponse);

            ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(authorId.Value, result.Value.Id);
            Assert.AreEqual(authorResponse.Name, result.Value.Name);
            _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns((AuthorResponse?)null);

            ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.NotFound, result.FirstError.Type);
            Assert.AreEqual($"Author with Id '{authorId}' was not found.", result.FirstError.Description);
            _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            AuthorId authorId = AuthorId.NewId();
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<AuthorResponse?>(new InvalidOperationException("query failure")));

            InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => useCase.ExecuteAsync(authorId));

            Assert.AreEqual("query failure", exception.Message);
            _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
        }
    }
}
