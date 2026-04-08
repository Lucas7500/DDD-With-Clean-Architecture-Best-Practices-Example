using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Persistence.Requests;
using BookStore.Domain.Persistence.Responses;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    public static class GetAuthorsUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldReturnPagedAuthors()
            {
                QueryRequest request = new();
                List<AuthorResponse> items = [new(Guid.NewGuid(), _faker.Name.FullName())];
                PagedResult<AuthorResponse> pagedResult = new(1, 20, items.Count, items);
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

                ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(pagedResult, result.Value);
                Assert.Equal(items.Count, result.Value.TotalCount);
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenEmptyResult_ShouldReturnEmptyPagedAuthors()
            {
                QueryRequest request = new();
                List<AuthorResponse> items = [];
                PagedResult<AuthorResponse> pagedResult = new(1, 20, 0, items);
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

                ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Empty(result.Value.Items);
                Assert.Equal(0, result.Value.TotalCount);
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                QueryRequest request = new();
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

                Assert.Equal("query failure", exception.Message);
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingFluentAssertions()
            {
                _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidRequest_ShouldReturnPagedAuthors()
            {
                QueryRequest request = new();
                List<AuthorResponse> items = [new(Guid.NewGuid(), _faker.Name.FullName())];
                PagedResult<AuthorResponse> pagedResult = new(1, 20, items.Count, items);
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

                ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().Be(pagedResult);
                result.Value.TotalCount.Should().Be(items.Count);
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenEmptyResult_ShouldReturnEmptyPagedAuthors()
            {
                QueryRequest request = new();
                List<AuthorResponse> items = [];
                PagedResult<AuthorResponse> pagedResult = new(1, 20, 0, items);
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

                ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Items.Should().BeEmpty();
                result.Value.TotalCount.Should().Be(0);
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                QueryRequest request = new();
                GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

                Func<Task> act = async () => await useCase.ExecuteAsync(request);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }
    }
}
