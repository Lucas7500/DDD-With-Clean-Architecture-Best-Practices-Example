using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Persistence.Requests;
using BookStore.Domain.Persistence.Responses;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestFixture]
    public sealed class GetAuthorsUseCaseTests
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
        public async Task ExecuteAsync_GivenValidRequest_ShouldReturnPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [new(Guid.NewGuid(), _faker.Name.FullName())];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, items.Count, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetAllAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.EqualTo(pagedResult));
                Assert.That(result.Value.TotalCount, Is.EqualTo(items.Count));
            }

            _mockAuthorsQueryService.Verify(x => x.GetAllAsync(request, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_GivenEmptyResult_ShouldReturnEmptyPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, 0, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetAllAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value.Items, Is.Empty);
                Assert.That(result.Value.TotalCount, Is.EqualTo(0));
            }

            _mockAuthorsQueryService.Verify(x => x.GetAllAsync(request, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [Test]
        public void ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            QueryRequest request = new();
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.GetAllAsync(request, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(request));

            Assert.That(exception.Message, Is.EqualTo("query failure"));
            _mockAuthorsQueryService.Verify(x => x.GetAllAsync(request, It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
