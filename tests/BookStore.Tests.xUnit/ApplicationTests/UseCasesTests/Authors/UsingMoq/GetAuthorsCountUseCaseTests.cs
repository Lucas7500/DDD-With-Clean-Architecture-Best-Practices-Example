using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    public static class GetAuthorsCountUseCaseTests
    {
        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IAuthorsQueryService> _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = new Mock<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.False(result.IsError);
                Assert.Equal(10L, result.Value);
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("Failed to retrieve the authors count.", result.FirstError.Description);
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

                Assert.Equal("query failure", exception.Message);
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly Mock<IAuthorsQueryService> _mockAuthorsQueryService;

            public UsingFluentAssertions()
            {
                _mockAuthorsQueryService = new Mock<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeFalse();
                result.Value.Should().Be(10L);
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(-1L);

                ErrorOr<long> result = await useCase.ExecuteAsync(request);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("Failed to retrieve the authors count.");
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
                GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

                Func<Task> act = async () => await useCase.ExecuteAsync(request);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }
    }
}
