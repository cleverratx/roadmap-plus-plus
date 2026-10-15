using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RoadmapPlusPlus.BuildingBlocks.Http
{
    /// <summary>
    /// Превращает <see cref="Result"/> в ответ эндпоинта. Ошибка становится ProblemDetails с полем <c>code</c>,
    /// а если у ошибки валидации есть поля, к ним добавляется <c>errors</c>.
    /// </summary>
    public static class ResultHttpExtensions
    {
        public static Results<NoContent, ProblemHttpResult> ToHttpResult( this Result result )
        {
            return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
        }

        public static Results<Ok<T>, ProblemHttpResult> ToHttpResult<T>( this Result<T> result )
        {
            return result.IsSuccess ? TypedResults.Ok( result.Value ) : result.Error.ToProblem();
        }

        /// <summary>Успех отдаётся через <paramref name="onSuccess"/>, например <c>value => TypedResults.Created(...)</c>.</summary>
        public static Results<TSuccess, ProblemHttpResult> ToHttpResult<T, TSuccess>( this Result<T> result, Func<T, TSuccess> onSuccess )
            where TSuccess : IResult
        {
            return result.IsSuccess ? onSuccess( result.Value ) : result.Error.ToProblem();
        }

        public static ProblemHttpResult ToProblem( this Error error )
        {
            ProblemDetails problem = error.Fields.Count > 0
                ? new HttpValidationProblemDetails( error.Fields )
                : new ProblemDetails();

            problem.Status = StatusCodeOf( error.Type );
            problem.Detail = error.Message;
            problem.Extensions[ "code" ] = error.Code;

            return TypedResults.Problem( problem );
        }

        private static int StatusCodeOf( ErrorType type )
        {
            return type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => throw new ArgumentOutOfRangeException( nameof( type ), type, null ),
            };
        }
    }
}
