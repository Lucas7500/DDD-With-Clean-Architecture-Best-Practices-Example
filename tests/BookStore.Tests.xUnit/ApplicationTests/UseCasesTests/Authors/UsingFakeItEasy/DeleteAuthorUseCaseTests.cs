using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    public static class DeleteAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.Equal($"Author with Id {authorId.Value} deleted successfully.", result.Value.Message);

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns((Author?)null);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("The author with the specified Id was not found.", result.FirstError.Description);

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);
                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while deleting the author: repository failure", result.FirstError.Description);

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly IUnitOfWork _mockUnitOfWork;
            private readonly IAuthorsRepository _mockAuthorsRepository;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = A.Fake<IUnitOfWork>();
                _mockAuthorsRepository = A.Fake<IAuthorsRepository>();

                A.CallTo(() => _mockUnitOfWork.AuthorsRepository).Returns(_mockAuthorsRepository);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.Message.Should().Be($"Author with Id {authorId.Value} deleted successfully.");

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns((Author?)null);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("The author with the specified Id was not found.");

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(A<Author>._, A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustNotHaveHappened();
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

                A.CallTo(() => _mockAuthorsRepository.GetByIdAsync(authorId, A<CancellationToken>._)).Returns(author);
                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while deleting the author: repository failure");

                A.CallTo(() => _mockAuthorsRepository.DeleteAsync(author, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
                A.CallTo(() => _mockUnitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
                A.CallTo(() => _mockUnitOfWork.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            }
        }
    }
}
