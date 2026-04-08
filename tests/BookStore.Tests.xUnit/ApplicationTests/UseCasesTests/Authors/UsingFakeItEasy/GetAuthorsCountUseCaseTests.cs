using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    public static class GetAuthorsCountUseCaseTests
    {
        public sealed class UsingStandardAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(10L, result.Value);
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("Failed to retrieve the authors count.", result.FirstError.Description);
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

                Assert.Equal("query failure", exception.Message);
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
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
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().Be(10L);
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("Failed to retrieve the authors count.");
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

                Func<Task> act = async () => await useCase.ExecuteAsync(request);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }
    }
}
