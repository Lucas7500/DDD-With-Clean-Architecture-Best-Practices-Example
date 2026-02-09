using BookStore.Domain.Exceptions;
using BookStore.Domain.Models.AuthorModel;
using BookStore.Domain.Models.BookModel;
using BookStore.Domain.ValueObjects;

namespace BookStore.Tests.MSTest.DomainTests.ModelsTests
{
    [TestClass]
    public sealed class BookTests
    {
        private Faker _faker = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _faker = new Faker();
        }

        [TestMethod]
        public void PartialArgsConstructor_GivenValidArgs_ShouldCreateBook()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            Assert.AreEqual(book.Id, BookId.Empty);
            Assert.AreEqual(bookTitle, book.Title);
            Assert.AreEqual(bookPrice, book.Price);
            Assert.AreEqual(author, book.Author);
            Assert.IsTrue(book.IsAvailable);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
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

            Assert.IsNotNull(nullOrWhitespaceException);
            Assert.AreEqual("businessrule.book-title-has-invalid-length", nullOrWhitespaceException.Message);

            Assert.IsNotNull(aboveMaxLengthException);
            Assert.AreEqual("businessrule.book-title-has-invalid-length", aboveMaxLengthException.Message);
        }

        [TestMethod]
        public void PartialArgsConstructor_GivenInvalidPrice_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal invalidBookPrice = _faker.Random.Decimal(min: decimal.MinValue, max: decimal.MinusOne);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookTitle, invalidBookPrice, author));

            Assert.IsNotNull(exception);
            Assert.AreEqual("businessrule.book-price-must-be-positive", exception.Message);
        }

        [TestMethod]
        public void PartialArgsConstructor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            BusinessRuleValidationException invalidAuthorException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookTitle, bookPrice, null!));

            Assert.IsNotNull(invalidAuthorException);
            Assert.AreEqual("businessrule.book-must-have-an-author", invalidAuthorException.Message);
        }

        [TestMethod]
        public void FullArgsConstructor_GivenValidArgs_ShouldCreateBook()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            BookId bookId = new(_faker.Random.Int(min: 1));
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);
            bool isAvailable = _faker.Random.Bool();

            Book book = new(bookId, bookTitle, bookPrice, isAvailable, author);

            Assert.AreEqual(bookId, book.Id);
            Assert.AreEqual(bookTitle, book.Title);
            Assert.AreEqual(bookPrice, book.Price);
            Assert.AreEqual(author, book.Author);
            Assert.AreEqual(isAvailable, book.IsAvailable);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
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

            Assert.IsNotNull(nullOrWhitespaceException);
            Assert.AreEqual("businessrule.book-title-has-invalid-length", nullOrWhitespaceException.Message);

            Assert.IsNotNull(aboveMaxLengthException);
            Assert.AreEqual("businessrule.book-title-has-invalid-length", aboveMaxLengthException.Message);
        }

        [TestMethod]
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

            Assert.IsNotNull(exception);
            Assert.AreEqual("businessrule.book-price-must-be-positive", exception.Message);
        }

        [TestMethod]
        public void FullArgsConstructor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            BookId bookId = new(_faker.Random.Int(min: 1));
            bool isAvailable = _faker.Random.Bool();
            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            BusinessRuleValidationException invalidAuthorException = Assert.Throws<BusinessRuleValidationException>(
                () => new Book(bookId, bookTitle, bookPrice, isAvailable, null!));

            Assert.IsNotNull(invalidAuthorException);
            Assert.AreEqual("businessrule.book-must-have-an-author", invalidAuthorException.Message);
        }

        [TestMethod]
        public void ChangeTitle_GivenValidTitle_ShouldUpdateBookTitle()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            string newTitle = _faker.Lorem.Sentence();
            book.ChangeTitle(newTitle);

            Assert.AreEqual(newTitle, book.Title);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ChangeTitle_GivenInvalidTitle_ShouldThrowBusinessRuleValidationException(string? invalidTitle)
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => book.ChangeTitle(invalidTitle!));

            Assert.IsNotNull(exception);
            Assert.AreEqual("businessrule.book-title-has-invalid-length", exception.Message);
        }

        [TestMethod]
        public void ChangePrice_GivenValidPrice_ShouldUpdateBookPrice()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            decimal newPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            book.ChangePrice(newPrice);

            Assert.AreEqual(newPrice, book.Price);
        }

        [TestMethod]
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

            Assert.IsNotNull(exception);
            Assert.AreEqual("businessrule.book-price-must-be-positive", exception.Message);
        }

        [TestMethod]
        public void MarkAsUnavailable_ShouldSetIsAvailableToFalse()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);
            book.MarkAsUnavailable();

            Assert.IsFalse(book.IsAvailable);
        }

        [TestMethod]
        public void MarkAsAvailable_ShouldSetIsAvailableToTrue()
        {
            BookId bookId = new(_faker.Random.Int(min: 1));
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookId, bookTitle, bookPrice, isAvailable: false, author);
            book.MarkAsAvailable();

            Assert.IsTrue(book.IsAvailable);
        }

        [TestMethod]
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

            Assert.AreEqual(newAuthor, book.Author);
        }

        [TestMethod]
        public void ChangeAuthor_GivenInvalidAuthor_ShouldThrowBusinessRuleValidationException()
        {
            string authorName = _faker.Name.FullName();
            Author author = new(authorName);

            string bookTitle = _faker.Lorem.Sentence();
            decimal bookPrice = _faker.Random.Decimal(min: decimal.Zero, max: decimal.MaxValue);

            Book book = new(bookTitle, bookPrice, author);

            BusinessRuleValidationException exception = Assert.Throws<BusinessRuleValidationException>(
                () => book.ChangeAuthor(null!));

            Assert.IsNotNull(exception);
            Assert.AreEqual("businessrule.book-must-have-an-author", exception.Message);
        }
    }
}
