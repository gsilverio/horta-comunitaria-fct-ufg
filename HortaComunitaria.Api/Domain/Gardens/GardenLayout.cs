using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Gardens;

/// <summary>Configuração visual versionada; coordenadas livres não representam medidas reais.</summary>
public sealed class GardenLayout
{
    private GardenLayout() { }
    /// <summary>Uma configuração por horta.</summary>
    public Guid GardenId { get; private set; }
    /// <summary>Documento geométrico validado, sem dependência de tipos HTTP.</summary>
    public string Document { get; private set; } = "";
    /// <summary>Versão para impedir sobrescrita de outra edição.</summary>
    public int Version { get; private set; }
    /// <summary>Chave da última gravação, preservada em repetição técnica.</summary>
    public Guid OperationId { get; private set; }
    /// <summary>Assinatura do conteúdo original da tentativa.</summary>
    public string RequestHash { get; private set; } = "";
    /// <summary>Instante confirmado da gravação.</summary>
    public DateTimeOffset SavedAt { get; private set; }
    /// <summary>Autor resolvido no servidor.</summary>
    public Guid SavedByUserId { get; private set; }
    /// <summary>Cria a configuração inicial.</summary>
    public static GardenLayout Create(Guid gardenId) => new() { GardenId = gardenId };
    /// <summary>Confirma um documento já validado com concorrência explícita.</summary>
    public Result<bool> Save(int expectedVersion, Guid operationId, string hash, string document, Guid actor, DateTimeOffset at)
    {
        if (Version != expectedVersion) return Result<bool>.Failure(new("layout.version_conflict", "O mapa foi alterado em outra edição. Recarregue a configuração antes de salvar.", ErrorCategory.Conflict));
        if (operationId == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(document)) return Result<bool>.Failure(Error.Validation("layout", "Configuração, operação e autoria válidas são obrigatórias."));
        Document = document; OperationId = operationId; RequestHash = hash; SavedByUserId = actor; SavedAt = at; Version++;
        return Result<bool>.Success(true);
    }
}
