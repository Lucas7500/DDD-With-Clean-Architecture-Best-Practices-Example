using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;

namespace BookStore.Tests.MSTest.ApplicationTests.MappersTests
{
    [TestClass]
    public sealed class AuthorsMapperExtensionsTests
    {
        private Faker _faker = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
        }

        [TestMethod]
        public void ToResponse_GivenValidAuthor_ShouldReturnAuthorResponseCorrectly()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            AuthorResponse authorResponse = author.ToResponse();

            Assert.IsInstanceOfType<AuthorResponse>(authorResponse);

            Assert.AreEqual(author.Id.Value, authorResponse.Id);
            Assert.AreEqual(author.Name, authorResponse.Name);
        }

        [TestMethod]
        public void ToResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Author authorNull = null!;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => AuthorsMapperExtensions.ToResponse(authorNull));
        }
    }
}
