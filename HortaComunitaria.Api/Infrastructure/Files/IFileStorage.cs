namespace HortaComunitaria.Api.Infrastructure.Files;

/// <summary>
/// Fronteira de armazenamento de arquivos. A referência é gerada pelo servidor e nunca deriva do nome enviado;
/// o adaptador local do protótipo pode ser substituído por armazenamento de objetos sem alterar os slices.
/// </summary>
internal interface IFileStorage
{
    /// <summary>Grava o conteúdo e devolve a referência opaca criada.</summary>
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, string extension, CancellationToken cancellationToken);
    /// <summary>Abre o conteúdo para leitura; null quando o objeto não existe.</summary>
    Stream? OpenRead(string storageKey);
    /// <summary>Remove um objeto não confirmado (limpeza após falha da transação).</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
