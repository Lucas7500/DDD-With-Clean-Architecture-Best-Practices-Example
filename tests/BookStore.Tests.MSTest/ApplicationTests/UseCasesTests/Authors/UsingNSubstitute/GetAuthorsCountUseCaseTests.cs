using BookStore.Application.DTOs.Authors.Requests;
using BookStore.Application.QueryServices.Contracts;
using BookStore.Application.UseCases.Authors;
using ErrorOr;
using NSubstitute;

namespace BookStore.Tests.MSTest.ApplicationTests.UseCasesTests.Authors.UsingNSubstitute
{
    [TestClass]
    public sealed class GetAuthorsCountUseCaseTests
    {
        private IAuthorsQueryService _mockAuthorsQueryService = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockAuthorsQueryService = Substitute.For<IAuthorsQueryService>();
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenValidCount_ShouldReturnCount()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(10L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(10L, result.Value);
            _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenNegativeCount_ShouldReturnFailureError()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>()).Returns(-1L);

            ErrorOr<long> result = await useCase.ExecuteAsync(request);

            Assert.IsTrue(result.IsError);
            Assert.HasCount(1, result.Errors);
            Assert.AreEqual(ErrorType.Failure, result.FirstError.Type);
            Assert.AreEqual("Failed to retrieve the authors count.", result.FirstError.Description);
            _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task ExecuteAsync_GivenQueryServiceThrowsException_ShouldPropagateException()
        {
            GetAuthorsCountRequest request = GetAuthorsCountRequest.Instance;
            GetAuthorsCountUseCase useCase = new(_mockAuthorsQueryService);

            _mockAuthorsQueryService.CountAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<long>(new InvalidOperationException("query failure")));

            InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));

            Assert.AreEqual("query failure", exception.Message);
            _ = _mockAuthorsQueryService.Received(1).CountAsync(Arg.Any<CancellationToken>());
        }
    }
}
