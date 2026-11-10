using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Infrastructure.Files;

/// <summary>Configuração validada do armazenamento local de fotografias do protótipo.</summary>
internal sealed class FileStorageOptions
{
    /// <summary>Diretório raiz; relativo ao content root quando não absoluto.</summary>
    public string RootPath { get; set; } = "App_Data/evidences";
    /// <summary>Tamanho máximo por arquivo em bytes.</summary>
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Armazena objetos em disco local com nomes gerados (UUID v7 + extensão conhecida), em subpastas por mês.</summary>
internal sealed partial class LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment) : IFileStorage
{
    private string Root => Path.GetFullPath(Path.IsPathRooted(options.Value.RootPath)
        ? options.Value.RootPath : Path.Combine(environment.ContentRootPath, options.Value.RootPath));

    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, string extension, CancellationToken cancellationToken)
    {
        if (!ExtensionPattern().IsMatch(extension)) throw new ArgumentException("Extensão inválida.", nameof(extension));
        var key = $"{DateTime.UtcNow:yyyy-MM}/{Guid.CreateVersion7():N}.{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await stream.WriteAsync(content, cancellationToken);
        return key;
    }

    public Stream? OpenRead(string storageKey)
    {
        var path = Resolve(storageKey);
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true) : null;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string storageKey)
    {
        if (!KeyPattern().IsMatch(storageKey)) throw new ArgumentException("Referência de armazenamento inválida.", nameof(storageKey));
        var path = Path.GetFullPath(Path.Combine(Root, storageKey));
        if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("Referência fora do armazenamento.", nameof(storageKey));
        return path;
    }

    [GeneratedRegex("^[a-z0-9]{2,5}$")]
    private static partial Regex ExtensionPattern();
    [GeneratedRegex("^[0-9]{4}-[0-9]{2}/[0-9a-f]{32}\\.[a-z0-9]{2,5}$")]
    private static partial Regex KeyPattern();
}

/// <summary>Registro da fronteira de arquivos.</summary>
internal static class FileStorageExtensions
{
    internal static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>().Bind(configuration.GetSection("FileStorage"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.RootPath), "Configure FileStorage:RootPath.")
            .Validate(x => x.MaxFileBytes is > 0 and <= 25 * 1024 * 1024, "FileStorage:MaxFileBytes deve estar entre 1 byte e 25 MB.")
            .ValidateOnStart();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
