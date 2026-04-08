using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    public static class GetAuthorByIdUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IAuthorsQueryService _mockAuthorsQueryService;

            public UsingStandardAssertions()
            {
                _mockAuthorsQueryService = Substitute.For<IAuthorsQueryService>();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
            {
                AuthorId authorId = AuthorId.NewId();
                AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(authorResponse);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.False(result.IsError);
                Assert.Equal(authorId.Value, result.Value.Id);
                Assert.Equal(authorResponse.Name, result.Value.Name);
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns((AuthorResponse?)null);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal($"Author with Id '{authorId}' was not found.", result.FirstError.Description);
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<AuthorResponse?>(new InvalidOperationException("query failure")));

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(authorId));

                Assert.Equal("query failure", exception.Message);
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
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
            public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
            {
                AuthorId authorId = AuthorId.NewId();
                AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(authorResponse);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeFalse();
                result.Value.Id.Should().Be(authorId.Value);
                result.Value.Name.Should().Be(authorResponse.Name);
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns((AuthorResponse?)null);

                ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be($"Author with Id '{authorId}' was not found.");
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
            {
                AuthorId authorId = AuthorId.NewId();
                GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService);

                _mockAuthorsQueryService.GetByIdAsync(authorId, Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<AuthorResponse?>(new InvalidOperationException("query failure")));

                Func<Task> act = async () => await useCase.ExecuteAsync(authorId);

                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("query failure");
                _ = _mockAuthorsQueryService.Received(1).GetByIdAsync(authorId, Arg.Any<CancellationToken>());
            }
        }
    }
}
