using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;

namespace BookStore.Tests.NUnit.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestFixture]
    public sealed class GetAuthorsCountUseCaseTests
    {
        private IAuthorsQueryService _mockAuthorsQueryService;

        [SetUp]
        public void SetUp()
        {
            _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
        }

        [Test]
        public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(10L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsError, Is.False);
                Assert.That(result.Value, Is.EqualTo(10L));
            }

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(-1L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.That(result.IsError, Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.FirstError.Type, Is.EqualTo(ErrorType.Failure));
                Assert.That(result.FirstError.Description, Is.EqualTo("Failed to retrieve the authors count."));
            }

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(request));

            Assert.That(exception.Message, Is.EqualTo("query failure"));
            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
