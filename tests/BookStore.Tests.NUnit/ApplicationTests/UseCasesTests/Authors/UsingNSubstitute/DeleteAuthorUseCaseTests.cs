using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Persistence.Contracts;
using BookStore.Domain.Persistence.Contracts.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestFixture]
    public sealed class DeleteAuthorUseCaseTests
    {
        private Faker _faker;
        private IUnitOfWork _mockUnitOfWork;
        private IAuthorsRepository _mockAuthorsRepository;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockUnitOfWork = Substitute.For<IUnitOfWork>();
            _mockAuthorsRepository = Substitute.For<IAuthorsRepository>();

            _mockUnitOfWork.AuthorsRepository.Returns(_mockAuthorsRepository);
        }

        [Test]
        public async Task ExecuteAsync_GivenExistingAuthor_ShouldDeleteAuthorAndCommit()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(author);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.Not.Null);
                Assert.That(result.Value.Message, Is.EqualTo($"Author with Id {authorId.Value} deleted successfully."));
            }

            _ = _mockAuthorsRepository.Received(1).DeleteAsync(author, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingAuthor_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns((Author?)null);

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo("The author with the specified Id was not found."));
            }

            _ = _mockAuthorsRepository.DidNotReceive().DeleteAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ExecuteAsync_GivenExceptionWhileDeleting_ShouldRollbackAndReturnFailureError()
        {
            AuthorId authorId = AuthorId.NewId();
            Author author = new(_faker.Name.FullName());
            DeleteAuthorUseCase useCase = new(_mockUnitOfWork);

            _mockAuthorsRepository.GetByIdAsync(authorId, Arg.Any<CancellationToken>()).Returns(author);
            _mockAuthorsRepository.DeleteAsync(author, Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("repository failure")));

            ErrorOr<DeleteAuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("An error occurred while deleting the author: repository failure"));
            }

            _ = _mockAuthorsRepository.Received(1).DeleteAsync(author, Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            _ = _mockUnitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        }
    }
}
