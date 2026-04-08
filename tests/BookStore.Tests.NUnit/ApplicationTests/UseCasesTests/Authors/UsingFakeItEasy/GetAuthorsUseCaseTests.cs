using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using BookStore.Domain.Persistence.Requests;
using BookStore.Domain.Persistence.Responses;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestFixture]
    public sealed class GetAuthorsUseCaseTests
    {
        private Faker _faker;
        private IAuthorsQueryService _mockAuthorsQueryService;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
            _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
        }

        [Test]
        public async Task ExecuteAsync_GivenValidRequest_ShouldReturnPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [new(Guid.NewGuid(), _faker.Name.FullName())];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, items.Count, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.EqualTo(pagedResult));
                Assert.That(result.Value.TotalCount, Is.EqualTo(items.Count));
            }

            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ExecuteAsync_GivenEmptyResult_ShouldReturnEmptyPagedAuthors()
        {
            QueryRequest request = new();
            List<AuthorResponse> items = [];
            PagedResult<AuthorResponse> pagedResult = new(1, 20, 0, items);
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).Returns(pagedResult);

            ErrorOr<PagedResult<AuthorResponse>> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value.Items, Is.Empty);
                Assert.That(result.Value.TotalCount, Is.EqualTo(0));
            }

            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            QueryRequest request = new();
            GetAuthorsUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(request));

            Assert.That(exception.Message, Is.EqualTo("query failure"));
            A.CallTo(() => _mockAuthorsQueryService.GetAllAsync(request, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
