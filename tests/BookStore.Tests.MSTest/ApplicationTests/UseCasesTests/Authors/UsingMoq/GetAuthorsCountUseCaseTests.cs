using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;
using Moq;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingMoq
{
    [TestClass]
    public sealed class GetAuthorsCountUseCaseTests
    {
        private Mock<IAuthorsQueryService> _mockAuthorsQueryService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockAuthorsQueryService = new Mock<IAuthorsQueryService>();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(10L, result.Value);
            _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(-1L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("Failed to retrieve the authors count.", result.FirstError.Description);
            _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService.Object);

            _mockAuthorsQueryService.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("query failure"));

            InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

            Assert.AreEqual("query failure", exception.Message);
            _mockAuthorsQueryService.Verify(x => x.CountAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        }
    }
}
