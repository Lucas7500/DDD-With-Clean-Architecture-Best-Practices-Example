using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Persistence.Requests;
using BookStore.Domain.Persistence.Responses;
using ErrorOr;
using NSubstitute;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestClass]
    public sealed class GetAuthorsUseCaseTests
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
        public async Task ExecuteAsync_GivenValidRequest_ShouldReturnPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [new(Guid.NewGuid(), _faker.Name.FullName())];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, items.Count, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetAllAsync(request, Arg.Any<CancellationToken>()).Returns(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(pagedResult, result.Value);
            Assert.AreEqual(items.Count, result.Value.TotalCount);
            _ = _mockAuthorsQueryService.Received(1).GetAllAsync(request, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenEmptyResult_ShouldReturnEmptyPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, 0, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetAllAsync(request, Arg.Any<CancellationToken>()).Returns(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(0, result.Value.TotalCount);
            Assert.HasCount(0, result.Value.Items);
            _ = _mockAuthorsQueryService.Received(1).GetAllAsync(request, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            QueryRequest request = new();
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.GetAllAsync(request, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<PagedResult<AuthorResponse>>(new InvalidOperationException("query failure")));

            InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

            Assert.AreEqual("query failure", exception.Message);
            _ = _mockAuthorsQueryService.Received(1).GetAllAsync(request, Arg.Any<CancellationToken>());
        }
    }
}
