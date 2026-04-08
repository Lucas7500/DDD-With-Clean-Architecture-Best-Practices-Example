using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    public static class GetAuthorByIdUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IAuthorsQueryService> _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = new Mock<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
            {
                AuthorId authorId = AuthorId.NewId();
                AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync(authorResponse);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.False(result.IsError);
                Assert.Equal(authorId.Value, result.Value.Id);
                Assert.Equal(authorResponse.Name, result.Value.Name);
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync((AuthorResponse?)null);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal($"Author with Id '{authorId}' was not found.", result.FirstError.Description);
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(authorId));

                Assert.Equal("query failure", exception.Message);
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
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
            public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
            {
                AuthorId authorId = AuthorId.NewId();
                AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync(authorResponse);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeFalse();
                result.Value.Id.Should().Be(authorId.Value);
                result.Value.Name.Should().Be(authorResponse.Name);
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync((AuthorResponse?)null);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be($"Author with Id '{authorId}' was not found.");
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

                _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

                Func<Task> act = async () => await useCase.ExecuteAsync(authorId);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }
    }
}
