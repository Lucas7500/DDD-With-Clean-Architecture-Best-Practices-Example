using BookStore.Application.DTOs.Books.Responses;
using BookStore.Domain.Models.BookModel;

namespace BookStore.Application.Mappers
{
    public static class BooksMapperExtensions
    {
        public static BookOnlyResponse ToBookOnlyBookResponse(this Book book)
        {
            ArgumentNullException.ThrowIfNull(book);

            return new BookOnlyResponse
            (
                Id: book.Id.Value,
                Title: book.Title,
                Price: book.Price,
                IsAvailable: book.IsAvailable);
        }
        
        public static BookWithAuthorResponse ToBookWithAuthorResponse(this Book book)
        {
            ArgumentNullException.ThrowIfNull(book);

            return new BookWithAuthorResponse
            (
                Id: book.Id.Value,
                Title: book.Title,
                Price: book.Price,
                IsAvailable: book.IsAvailable,
                Author: book.Author.ToResponse());
        }
    }
}
