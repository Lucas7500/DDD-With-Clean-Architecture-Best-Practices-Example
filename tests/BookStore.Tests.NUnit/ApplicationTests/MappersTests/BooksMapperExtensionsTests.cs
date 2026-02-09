using BookStore.Application.DTOs.Books.Responses;
using BookStore.Application.Mappers;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;

namespace BookStore.Tests.NUnit.ApplicationTests.MappersTests
{
    [TestFixture]
    public sealed class BooksMapperExtensionsTests
    {
        private Faker _faker;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
        }

        [Test]
        public void ToBookOnlyBookResponse_GivenValidBook_ShouldReturnBookOnlyResponseCorrectly()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(1, 100);
            string authorName = _faker.Name.FullName();
            Author bookAuthor = new(authorName);

            Book book = new(bookTitle, bookPrice, bookAuthor);
            BookOnlyResponse response = book.ToBookOnlyBookResponse();

            Assert.That(response, Is.TypeOf<BookOnlyResponse>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Id, Is.EqualTo(book.Id.Value));
                Assert.That(response.Title, Is.EqualTo(book.Title));
                Assert.That(response.Price, Is.EqualTo(book.Price));
                Assert.That(response.IsAvailable, Is.EqualTo(book.IsAvailable));
            }
        }

        [Test]
        public void ToBookOnlyBookResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Book bookNull = null!;

            Assert.Throws<ArgumentNullException>(
                () => BooksMapperExtensions.ToBookOnlyBookResponse(bookNull));
        }

        [Test]
        public void ToBookWithAuthorResponse_GivenValidBook_ShouldReturnBookWithAuthorCorrectly()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(1, 100);
            string authorName = _faker.Name.FullName();
            Author bookAuthor = new(authorName);

            Book book = new(bookTitle, bookPrice, bookAuthor);
            BookWithAuthorResponse response = book.ToBookWithAuthorResponse();

            Assert.That(response, Is.TypeOf<BookWithAuthorResponse>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Id, Is.EqualTo(book.Id.Value));
                Assert.That(response.Title, Is.EqualTo(book.Title));
                Assert.That(response.Price, Is.EqualTo(book.Price));
                Assert.That(response.IsAvailable, Is.EqualTo(book.IsAvailable));

                Assert.That(response.Author, Is.Not.Null);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Author.Id, Is.EqualTo(book.Author.Id.Value));
                Assert.That(response.Author.Name, Is.EqualTo(book.Author.Name));
            }
        }

        [Test]
        public void ToBookWithAuthorResponse_GivenNull_ShouldThrowArgumentNullException()
        {
            Book bookNull = null!;

            Assert.Throws<ArgumentNullException>(
                () => BooksMapperExtensions.ToBookWithAuthorResponse(bookNull));
        }
    }
}
