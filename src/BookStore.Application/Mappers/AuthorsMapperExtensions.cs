using BookStore.Application.DTOs.Authors.Responses;
using BookStore.Domain.Models.AuthorModel;

namespace BookStore.Application.Mappers
{
    public static class AuthorsMapperExtensions
    {
        public static AuthorResponse ToResponse(this Author author)
        {
            ArgumentNullException.ThrowIfNull(author);

            return new AuthorResponse(
                Id: author.Id.Value,
                Name: author.Name);
        }
    }
}
