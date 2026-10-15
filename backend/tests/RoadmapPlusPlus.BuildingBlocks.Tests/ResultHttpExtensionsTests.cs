using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RoadmapPlusPlus.BuildingBlocks.Http;

namespace RoadmapPlusPlus.BuildingBlocks.Tests
{
    public sealed class ResultHttpExtensionsTests
    {
        public static TheoryData<ErrorType, int> StatusByErrorType { get; } = new()
        {
            { ErrorType.Validation, StatusCodes.Status400BadRequest },
            { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
            { ErrorType.NotFound, StatusCodes.Status404NotFound },
            { ErrorType.Conflict, StatusCodes.Status409Conflict },
        };

        [Theory]
        [MemberData( nameof( StatusByErrorType ) )]
        public async Task ErrorBecomesProblemWithStatusAndCode( ErrorType type, int status )
        {
            Error error = type switch
            {
                ErrorType.Validation => Error.Validation( "test.invalid", "Неверно." ),
                ErrorType.Forbidden => Error.Forbidden( "test.forbidden", "Нельзя." ),
                ErrorType.NotFound => Error.NotFound( "test.not_found", "Не найдено." ),
                _ => Error.Conflict( "test.conflict", "Конфликт." ),
            };

            (HttpContext context, JsonElement body) = await ExecuteAsync( error.ToProblem() );

            Assert.Equal( status, context.Response.StatusCode );
            Assert.Equal( "application/problem+json", context.Response.ContentType );
            Assert.Equal( status, body.GetProperty( "status" ).GetInt32() );
            Assert.Equal( error.Code, body.GetProperty( "code" ).GetString() );
            Assert.Equal( error.Message, body.GetProperty( "detail" ).GetString() );
        }

        [Fact]
        public async Task ValidationErrorListsFields()
        {
            Error error = Error.Validation(
                "test.invalid",
                "Неверно.",
                new Dictionary<string, string[]> { [ "title" ] = [ "Название обязательно." ] } );

            (_, JsonElement body) = await ExecuteAsync( error.ToProblem() );

            JsonElement title = body.GetProperty( "errors" ).GetProperty( "title" );
            Assert.Equal( "Название обязательно.", title[ 0 ].GetString() );
        }

        [Fact]
        public async Task ValidationErrorWithoutFieldsHasNoErrors()
        {
            Error error = Error.Validation( "test.invalid_credentials", "Неверный email или пароль." );

            (HttpContext context, JsonElement body) = await ExecuteAsync( error.ToProblem() );

            Assert.Equal( StatusCodes.Status400BadRequest, context.Response.StatusCode );
            Assert.False( body.TryGetProperty( "errors", out _ ) );
            Assert.Equal( "Bad Request", body.GetProperty( "title" ).GetString() );
        }

        [Fact]
        public async Task SuccessWithoutValueIsNoContent()
        {
            (HttpContext context, _) = await ExecuteAsync( Result.Success().ToHttpResult() );

            Assert.Equal( StatusCodes.Status204NoContent, context.Response.StatusCode );
        }

        [Fact]
        public async Task SuccessWithValueIsOk()
        {
            Result<int> result = 42;

            (HttpContext context, JsonElement body) = await ExecuteAsync( result.ToHttpResult() );

            Assert.Equal( StatusCodes.Status200OK, context.Response.StatusCode );
            Assert.Equal( 42, body.GetInt32() );
        }

        [Fact]
        public async Task SuccessUsesCustomResult()
        {
            Result<int> result = 42;

            (HttpContext context, _) = await ExecuteAsync( result.ToHttpResult( id => TypedResults.Created( $"/items/{id}", id ) ) );

            Assert.Equal( StatusCodes.Status201Created, context.Response.StatusCode );
            Assert.Equal( "/items/42", context.Response.Headers.Location.ToString() );
        }

        [Fact]
        public async Task FailureOfResultWithValueIsProblem()
        {
            Result<int> result = Error.NotFound( "test.not_found", "Не найдено." );

            (HttpContext context, JsonElement body) = await ExecuteAsync( result.ToHttpResult() );

            Assert.Equal( StatusCodes.Status404NotFound, context.Response.StatusCode );
            Assert.Equal( "test.not_found", body.GetProperty( "code" ).GetString() );
        }

        private static async Task<(HttpContext Context, JsonElement Body)> ExecuteAsync( IResult result )
        {
            ServiceProvider services = new ServiceCollection()
                .AddLogging()
                .AddProblemDetails()
                .BuildServiceProvider();

            using MemoryStream body = new();
            DefaultHttpContext context = new() { RequestServices = services };
            context.Response.Body = body;

            await result.ExecuteAsync( context );

            if ( body.Length == 0 )
            {
                return (context, default);
            }

            body.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync( body );
            return (context, document.RootElement.Clone());
        }
    }
}
