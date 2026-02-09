using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;

namespace BookStore.Tests.xUnit.ApplicationTests.MappersTests
{
    public static class AuthorsMapperExtensionsTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            [Fact]
            public void ToResponse_GivenValidAuthor_ShouldReturnAuthorResponseCorrectly()
            {
                string authorName = _faker.Name.FullName();
                Author author = new(authorName);

                AuthorResponse authorResponse = author.ToResponse();

                Assert.IsType<AuthorResponse>(authorResponse);

                Assert.Equal(author.Id.Value, authorResponse.Id);
                Assert.Equal(author.Name, authorResponse.Name);
            }

            [Fact]
            public void ToResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Author authorNull = null!;

                Assert.Throws<ArgumentNullException>(
                    () => AuthorsMapperExtensions.ToResponse(authorNull));
            }
        }

        public sealed class UsingFluentAssertions
        {
            [Fact]
            public void ToResponse_GivenValidAuthor_ShouldReturnAuthorResponseCorrectly()
            {
                string authorName = _faker.Name.FullName();
                Author author = new(authorName);

                AuthorResponse authorResponse = author.ToResponse();

                authorResponse.Should().BeOfType<AuthorResponse>();
                authorResponse.Id.Should().Be(author.Id.Value);
                authorResponse.Name.Should().Be(author.Name);
            }

            [Fact]
            public void ToResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Author authorNull = null!;
                Action act = () => AuthorsMapperExtensions.ToResponse(authorNull);
                act.Should().Throw<ArgumentNullException>();
            }
        }
    }
}
