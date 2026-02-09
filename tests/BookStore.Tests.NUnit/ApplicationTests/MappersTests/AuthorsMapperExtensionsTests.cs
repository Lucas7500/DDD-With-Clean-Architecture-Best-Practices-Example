using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;

namespace BookStore.Tests.NUnit.ApplicationTests.MappersTests
{
    [TestFixture]
    public sealed class AuthorsMapperExtensionsTests
    {
        private Faker _faker;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
        }

        [Test]
        public void ToResponse_GivenValidAuthor_ShouldReturnAuthorResponseCorrectly()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            AuthorResponse authorResponse = author.ToResponse();

            Assert.That(authorResponse, Is.TypeOf<AuthorResponse>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authorResponse.Id, Is.EqualTo(author.Id.Value));
                Assert.That(authorResponse.Name, Is.EqualTo(author.Name));
            }
        }

        [Test]
        public void ToResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Author authorNull = null!;

            Assert.Throws<ArgumentNullException>(
                () => AuthorsMapperExtensions.ToResponse(authorNull));
        }
    }
}
