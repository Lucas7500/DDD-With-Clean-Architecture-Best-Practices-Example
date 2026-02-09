using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;

namespace BookStore.Tests.xUnit.ApplicationTests.MappersTests
{
    public static class BooksMapperExtensionsTests
    {
        private static readonly Faker _faker = new();

        public sealed class UsingStandardAssertions
        {
            [Fact]
            public void ToBookOnlyBookResponse_GivenValidBook_ShouldReturnBookOnlyResponseCorrectly()
            {
                string bookTitle = _faker.Lorem.Sentence();
                decimal bookPrice = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author bookAuthor = new(authorName);

                Book book = new(bookTitle, bookPrice, bookAuthor);
                BookOnlyResponse response = book.ToBookOnlyBookResponse();

                Assert.IsType<BookOnlyResponse>(response);

                Assert.Equal(book.Id.Value, response.Id);
                Assert.Equal(book.Title, response.Title);
                Assert.Equal(book.Price, response.Price);
                Assert.Equal(book.IsAvailable, response.IsAvailable);
            }
            
            [Fact]
            public void ToBookOnlyBookResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Book bookNull = null!;

                Assert.Throws<ArgumentNullException>(
                    () => BooksMapperExtensions.ToBookOnlyBookResponse(bookNull));
            }
            
            [Fact]
            public void ToBookWithAuthorResponse_GivenValidBook_ShouldReturnBookWithAuthorCorrectly()
            {
                string bookTitle = _faker.Lorem.Sentence();
                decimal bookPrice = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author bookAuthor = new(authorName);
                
                Book book = new(bookTitle, bookPrice, bookAuthor);
                BookWithAuthorResponse response = book.ToBookWithAuthorResponse();
                
                Assert.IsType<BookWithAuthorResponse>(response);

                Assert.Equal(book.Id.Value, response.Id);
                Assert.Equal(book.Title, response.Title);
                Assert.Equal(book.Price, response.Price);
                Assert.Equal(book.IsAvailable, response.IsAvailable);
                
                Assert.NotNull(response.Author);
                Assert.Equal(book.Author.Id.Value, response.Author.Id);
                Assert.Equal(book.Author.Name, response.Author.Name);
            }

            [Fact]
            public void ToBookWithAuthorResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Book bookNull = null!;

                Assert.Throws<ArgumentNullException>(
                    () => BooksMapperExtensions.ToBookWithAuthorResponse(bookNull));
            }
        }

        public sealed class UsingFluentAssertions
        {
            [Fact]
            public void ToBookOnlyBookResponse_GivenValidBook_ShouldReturnBookOnlyResponseCorrectly()
            {
                string bookTitle = _faker.Lorem.Sentence();
                decimal bookPrice = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author bookAuthor = new(authorName);

                Book book = new(bookTitle, bookPrice, bookAuthor);
                BookOnlyResponse response = book.ToBookOnlyBookResponse();

                response.Should().BeOfType<BookOnlyResponse>();

                response.Id.Should().Be(book.Id.Value);
                response.Title.Should().Be(book.Title);
                response.Price.Should().Be(book.Price);
                response.IsAvailable.Should().Be(book.IsAvailable);
            }

            [Fact]
            public void ToBookOnlyBookResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Book bookNull = null!;
                Action act = () => BooksMapperExtensions.ToBookOnlyBookResponse(bookNull);
                act.Should().ThrowExactly<ArgumentNullException>();
            }

            [Fact]
            public void ToBookWithAuthorResponse_GivenValidBook_ShouldReturnBookWithAuthorCorrectly()
            {
                string bookTitle = _faker.Lorem.Sentence();
                decimal bookPrice = _faker.Random.Decimal(1, 100);
                string authorName = _faker.Name.FullName();
                Author bookAuthor = new(authorName);

                Book book = new(bookTitle, bookPrice, bookAuthor);
                BookWithAuthorResponse response = book.ToBookWithAuthorResponse();

                response.Should().BeOfType<BookWithAuthorResponse>();

                response.Id.Should().Be(book.Id.Value);
                response.Title.Should().Be(book.Title);
                response.Price.Should().Be(book.Price);
                response.IsAvailable.Should().Be(book.IsAvailable);

                response.Author.Should().NotBeNull();
                response.Author.Id.Should().Be(book.Author.Id.Value);
                response.Author.Name.Should().Be(book.Author.Name);
            }

            [Fact]
            public void ToBookWithAuthorResponse_GivenNull_ShouldThrowArgumentNullException()
            {
                Book bookNull = null!;
                Action act = () => BooksMapperExtensions.ToBookWithAuthorResponse(bookNull);
                act.Should().ThrowExactly<ArgumentNullException>();
            }
        }
    }
}
