using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;
using FakeItEasy;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingFakeItEasy
{
    [TestClass]
    public sealed class GetAuthorsCountUseCaseTests
    {
        private IAuthorsQueryService _mockAuthorsQueryService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockAuthorsQueryService = A.Fake<IAuthorsQueryService>();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(10L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(10L, result.Value);
            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).Returns(-1L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("Failed to retrieve the authors count.", result.FirstError.Description);
            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

            Assert.AreEqual("query failure", exception.Message);
            A.CallTo(() => _mockAuthorsQueryService.CountAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
    }
}
