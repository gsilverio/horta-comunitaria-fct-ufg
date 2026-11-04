namespace HortaComunitaria.Api.Common.Results;

/// <summary>Resultado que contém exclusivamente um valor ou uma rejeição esperada.</summary>
public sealed class Result<T>
{
    private readonly T? value;
    private readonly Error? error;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        this.value = value;
        this.error = error;
    }

    /// <summary>Indica que o valor está disponível.</summary>
    public bool IsSuccess { get; }
    /// <summary>Acesso ao valor somente no caminho de sucesso.</summary>
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("Resultado sem valor.");
    /// <summary>Acesso ao erro somente no caminho de falha.</summary>
    public Error Error => !IsSuccess ? error! : throw new InvalidOperationException("Resultado sem erro.");
    /// <summary>Cria sucesso com valor obrigatório.</summary>
    public static Result<T> Success(T value) => new(true, value ?? throw new ArgumentNullException(nameof(value)), null);
    /// <summary>Cria falha com erro obrigatório.</summary>
    public static Result<T> Failure(Error error) => new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    /// <summary>Exige tradução explícita dos dois caminhos.</summary>
    public TResult Match<TResult>(Func<T, TResult> success, Func<Error, TResult> failure) =>
        IsSuccess ? success(Value) : failure(Error);
}

/// <summary>Representa uma operação bem-sucedida sem conteúdo.</summary>
public readonly record struct Unit;
