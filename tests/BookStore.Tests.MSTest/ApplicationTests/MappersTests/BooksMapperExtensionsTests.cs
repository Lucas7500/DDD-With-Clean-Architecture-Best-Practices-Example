using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;

namespace BookStore.Tests.MSTest.ApplicationTests.MappersTests
{
    [TestClass]
    public sealed class BooksMapperExtensionsTests
    {
        private Faker _faker = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
        }

        [TestMethod]
        public void ToBookOnlyBookResponse_GivenValidBook_ShouldReturnBookOnlyResponseCorrectly()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(1, 100);
            string authorName = _faker.Name.FullName();
            Author bookAuthor = new(authorName);

            Book book = new(bookTitle, bookPrice, bookAuthor);
            BookOnlyResponse response = book.ToBookOnlyBookResponse();

            Assert.IsInstanceOfType<BookOnlyResponse>(response);

            Assert.AreEqual(book.Id.Value, response.Id);
            Assert.AreEqual(book.Title, response.Title);
            Assert.AreEqual(book.Price, response.Price);
            Assert.AreEqual(book.IsAvailable, response.IsAvailable);
        }

        [TestMethod]
        public void ToBookOnlyBookResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Book bookNull = null!;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => BooksMapperExtensions.ToBookOnlyBookResponse(bookNull));
        }

        [TestMethod]
        public void ToBookWithAuthorResponse_GivenValidBook_ShouldReturnBookWithAuthorCorrectly()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(1, 100);
            string authorName = _faker.Name.FullName();
            Author bookAuthor = new(authorName);

            Book book = new(bookTitle, bookPrice, bookAuthor);
            BookWithAuthorResponse response = book.ToBookWithAuthorResponse();

            Assert.IsInstanceOfType<BookWithAuthorResponse>(response);

            Assert.AreEqual(book.Id.Value, response.Id);
            Assert.AreEqual(book.Title, response.Title);
            Assert.AreEqual(book.Price, response.Price);
            Assert.AreEqual(book.IsAvailable, response.IsAvailable);

            Assert.IsNotNull(response.Author);
            Assert.AreEqual(book.Author.Id.Value, response.Author.Id);
            Assert.AreEqual(book.Author.Name, response.Author.Name);
        }

        [TestMethod]
        public void ToBookWithAuthorResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Book bookNull = null!;

            Assert.ThrowsExactly<ArgumentNullException>(
                () => BooksMapperExtensions.ToBookWithAuthorResponse(bookNull));
        }
    }
}
