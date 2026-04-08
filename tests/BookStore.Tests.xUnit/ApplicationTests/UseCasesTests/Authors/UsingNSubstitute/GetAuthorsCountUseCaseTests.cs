using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    public static class GetAuthorsCountUseCaseTests
    {
        public sealed class UsingStandardAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = Substitute.For<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(10L, result.Value);
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("Failed to retrieve the authors count.", result.FirstError.Description);
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<long>(new InvalidOperationException("query failure")));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

                Assert.Equal("query failure", exception.Message);
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingFluentAssertions()
            {
                _mockAuthorsQueryService = Substitute.For<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().Be(10L);
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("Failed to retrieve the authors count.");
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<long>(new InvalidOperationException("query failure")));

                Func<Task> act = async () => await useCase.ExecuteAsync(request);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
            }
        }
    }
}
