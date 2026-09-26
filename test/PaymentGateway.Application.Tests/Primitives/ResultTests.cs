using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Tests.Primitives;

public sealed class ResultTests
{
    private static readonly Error SomeError = Error.From(new ErrorRecord("Some.Code", "Some message."));

    [Fact]
    public void SuccessHasNoErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FailureCarriesErrors()
    {
        var other = Error.From(new ErrorRecord("Other.Code", "Other message."));

        var result = Result.Failure([SomeError, other]);

        Assert.True(result.IsFailure);
        Assert.Equal([SomeError, other], result.Errors);
    }

    [Fact]
    public void FailureWithoutErrorsIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void SuccessValueIsAccessible()
    {
        Assert.Equal("value", Result.Success("value").Value);
    }

    [Fact]
    public void FailureValueThrows()
    {
        var result = Result.Failure<string>(SomeError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversionFromValueSucceeds()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void ImplicitConversionFromNullFailsWithNullValueError()
    {
        Result<string> result = (string?)null;

        Assert.True(result.IsFailure);
        Assert.Equal([Error.NullValue], result.Errors);
    }
}
