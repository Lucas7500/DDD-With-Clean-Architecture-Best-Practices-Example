using BookStore.Domain.Exceptions;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.ValueObjects;

namespace BookStore.Tests.NUnit.DomainTests.ModelsTests
{
    [TestFixture]
    public sealed class BookTests
    {
        private Faker _faker;

        [SetUp]
        public void SetUp()
        {
            _faker = new Faker();
        }

        [Test]
        public void PartialArgsConstructor_GivenValidArgs_ShouldCreateBook()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(book.Id, Is.EqualTo(BookId.Empty));
                Assert.That(book.Title, Is.EqualTo(bookTitle));
                Assert.That(book.Price, Is.EqualTo(bookPrice));
                Assert.That(book.Author, Is.EqualTo(author));
                Assert.That(book.IsAvailable, Is.True);
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void PartialArgsConstructor_GivenInvalidTitle_ShouldThrowBusinessRuleValidationException(string? invalidTitle)
        {
            const int aboveMaxLength = 201;

            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, decimal.MaxValue);

            BusinessRuleValidationException nullOrWhitespaceException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(invalidTitle!, bookPrice, author));

            BusinessRuleValidationException aboveMaxLengthException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(_faker.Random.String(length: aboveMaxLength), bookPrice, author));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nullOrWhitespaceException, Is.Not.Null);
                Assert.That(aboveMaxLengthException, Is.Not.Null);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nullOrWhitespaceException.Message, Is.EqualTo("businessrule.book-title-has-invalid-length"));
                Assert.That(aboveMaxLengthException.Message, Is.EqualTo("businessrule.book-title-has-invalid-length"));
            }
        }

        [Test]
        public void PartialArgsConstructor_GivenInvalidPrice_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal invalidBookPrice = _faker.Random.Decimal(min: decimal.MinValue, max: decimal.MinusOne);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookTitle, invalidBookPrice, author));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("businessrule.book-price-must-be-positive"));
        }

        [Test]
        public void PartialArgsConstructor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            BusinessRuleValidationException invalidAuthorException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookTitle, bookPrice, null!));

            Assert.That(invalidAuthorException, Is.Not.Null);
            Assert.That(invalidAuthorException.Message, Is.EqualTo("businessrule.book-must-have-an-author"));
        }

        [Test]
        public void FullArgsConstructor_GivenValidArgs_ShouldCreateBook()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            BookId bookId = new(_faker.Random.Int(min: 1));
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);
            bool isAvailable = _faker.Random.Bool();

            Book book = new(bookId, bookTitle, bookPrice, isAvailable, author);

            Assert.That(book.Id, Is.EqualTo(bookId));
            Assert.That(book.Title, Is.EqualTo(bookTitle));
            Assert.That(book.Price, Is.EqualTo(bookPrice));
            Assert.That(book.Author, Is.EqualTo(author));
            Assert.That(book.IsAvailable, Is.EqualTo(isAvailable));
        }

        [Theory]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FullArgsConstructor_GivenInvalidTitle_ShouldThrowBusinessRuleValidationException(string? invalidTitle)
        {
            const int aboveMaxLength = 201;

            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            BookId bookId = new(_faker.Random.Int(min: 1));
            bool isAvailable = _faker.Random.Bool();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, decimal.MaxValue);

            BusinessRuleValidationException nullOrWhitespaceException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookId, invalidTitle!, bookPrice, isAvailable, author));

            BusinessRuleValidationException aboveMaxLengthException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookId, _faker.Random.String(length: aboveMaxLength), bookPrice, isAvailable, author));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nullOrWhitespaceException, Is.Not.Null);
                Assert.That(aboveMaxLengthException, Is.Not.Null);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nullOrWhitespaceException.Message, Is.EqualTo("businessrule.book-title-has-invalid-length"));
                Assert.That(aboveMaxLengthException.Message, Is.EqualTo("businessrule.book-title-has-invalid-length"));
            }
        }

        [Test]
        public void FullArgsConstructor_GivenInvalidPrice_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            BookId bookId = new(_faker.Random.Int(min: 1));
            bool isAvailable = _faker.Random.Bool();
            string bookTitle = _faker.Lorem.Sentence();
            decimal invalidBookPrice = _faker.Random.Decimal(min: decimal.MinValue, max: decimal.MinusOne);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookId, bookTitle, invalidBookPrice, isAvailable, author));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("businessrule.book-price-must-be-positive"));
        }

        [Test]
        public void FullArgsConstructor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            BookId bookId = new(_faker.Random.Int(min: 1));
            bool isAvailable = _faker.Random.Bool();
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            BusinessRuleValidationException invalidAuthorException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookId, bookTitle, bookPrice, isAvailable, null!));

            Assert.That(invalidAuthorException, Is.Not.Null);
            Assert.That(invalidAuthorException.Message, Is.EqualTo("businessrule.book-must-have-an-author"));
        }

        [Test]
        public void ChangeTitle_GivenValidTitle_ShouldUpdateBookTitle()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            string newTitle = _faker.Lorem.Sentence();
            book.ChangeTitle(newTitle);

            Assert.That(book.Title, Is.EqualTo(newTitle));
        }

        [Theory]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ChangeTitle_GivenInvalidTitle_ShouldThrowBusinessRuleValidationException(string? invalidTitle)
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => book.ChangeTitle(invalidTitle!));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("businessrule.book-title-has-invalid-length"));
        }

        [Test]
        public void ChangePrice_GivenValidPrice_ShouldUpdateBookPrice()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            decimal newPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            book.ChangePrice(newPrice);

            Assert.That(book.Price, Is.EqualTo(newPrice));
        }

        [Test]
        public void ChangePrice_GivenInvalidPrice_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            decimal invalidNewPrice = _faker.Random.Decimal(min: decimal.MinValue, max: decimal.MinusOne);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => book.ChangePrice(invalidNewPrice));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("businessrule.book-price-must-be-positive"));
        }

        [Test]
        public void MarkAsUnavailable_ShouldSetIsAvailableToFalse()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);
            book.MarkAsUnavailable();

            Assert.That(book.IsAvailable, Is.False);
        }

        [Test]
        public void MarkAsAvailable_ShouldSetIsAvailableToTrue()
        {
            BookId bookId = new(_faker.Random.Int(min: 1));
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookId, bookTitle, bookPrice, isAvailable: false, author);
            book.MarkAsAvailable();

            Assert.That(book.IsAvailable, Is.True);
        }

        [Test]
        public void ChangeAuthor_GivenValidAuthor_ShouldUpdateBookAuthor()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            string newAuthorName = _faker.Name.FullName();
            Author newAuthor = new(newAuthorName);

            book.ChangeAuthor(newAuthor);

            Assert.That(book.Author, Is.EqualTo(newAuthor));
        }

        [Test]
        public void ChangeAuthor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => book.ChangeAuthor(null!));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("businessrule.book-must-have-an-author"));
        }
    }
}
