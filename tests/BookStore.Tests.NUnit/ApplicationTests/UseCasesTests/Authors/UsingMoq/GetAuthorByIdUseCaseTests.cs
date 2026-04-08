using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.ValueObjects;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestFixture]
    public sealed class GetAuthorByIdUseCaseTests
    {
        private Faker _faker;
        private Mock<IAuthorsQueryService> _mockAuthorsQueryService;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockAuthorsQueryService = new Mock<IAuthorsQueryService>();
        }

        [Test]
        public async Task ExecuteAsync_GivenExistingAuthorId_ShouldReturnAuthor()
        {
            AuthorId authorId = AuthorId.NewId();
            AuthorResponse authorResponse = new(authorId.Value, _faker.Name.FullName());
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync(authorResponse);

            ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value.Id, Is.EqualTo(authorId.Value));
                Assert.That(result.Value.Name, Is.EqualTo(authorResponse.Name));
            }

            _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_GivenMissingAuthorId_ShouldReturnNotFoundError()
        {
            AuthorId authorId = AuthorId.NewId();
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ReturnsAsync((AuthorResponse?)null);

            ErrorOr<AuthorResponse> result = await useCase.ExecuteAsync(authorId);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.NotFound));
                Assert.That(result.FirstError.Description, Is.EqualTo($"Author with Id '{authorId}' was not found."));
            }

            _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [Test]
        public void ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            AuthorId authorId = AuthorId.NewId();
            GetAuthorByIdUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(authorId));

            Assert.That(exception.Message, Is.EqualTo("query failure"));
            _mockAuthorsQueryService.Verify(x => x.GetByIdAsync(authorId, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
