using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.xUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    public static class DeleteAuthorUseCaseTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;

            public UsingStandardAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(author);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.False(result.IsError);
                Assert.NotNull(result.Value);
                Assert.Equal($"Author with Id {authorId.Value} deleted successfully.", result.Value.Message);

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Author?)null);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
                Assert.Equal("The author with the specified Id was not found.", result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(author);
                _mockAuthorsRepository
                    .Setup(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                Assert.True(result.IsError);
                Assert.Single(result.Errors);
                Assert.Equal(ErrorType.Failure, result.FirstError.Type);
                Assert.Equal("An error occurred while deleting the author: repository failure", result.FirstError.Description);

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }

        public sealed class UsingFluentAssertions
        {
            private readonly Mock<IUnitOfWork> _mockUnitOfWork;
            private readonly Mock<IAuthorsRepository> _mockAuthorsRepository;

            public UsingFluentAssertions()
            {
                _mockUnitOfWork = new Mock<IUnitOfWork>();
                _mockAuthorsRepository = new Mock<IAuthorsRepository>();

                _mockUnitOfWork.Setup(x => x.AuthorsRepository).Returns(_mockAuthorsRepository.Object);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(author);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeFalse();
                result.Value.Should().NotBeNull();
                result.Value.Message.Should().Be($"Author with Id {authorId.Value} deleted successfully.");

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
            {
                AuthorId authorId = AuthorId.NewId();
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Author?)null);

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.NotFound);
                result.FirstError.Description.Should().Be("The author with the specified Id was not found.");

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(It.IsAny<Author>(), It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
            }

            [Fact]
            public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
            {
                AuthorId authorId = AuthorId.NewId();
                Author author = new(_faker.Name.FullName());
                DeleteAuthorUseCase useCase = new(_mockUnitOfWork.Object);

                _mockAuthorsRepository
                    .Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(author);
                _mockAuthorsRepository
                    .Setup(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("repository failure"));

                ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

                result.IsError.Should().BeTrue();
                result.Errors.Should().HaveCount(1);
                result.FirstError.Type.Should().Be(ErrorType.Failure);
                result.FirstError.Description.Should().Be("An error occurred while deleting the author: repository failure");

                _mockAuthorsRepository.Verify(x => x.DeleteAsync(author, It.IsAny<CancellationToken>()), Moq.Times.Once);
                _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Moq.Times.Never);
                _mockUnitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
            }
        }
    }
}
