namespace HortaComunitaria.Api.Infrastructure.Files;

/// <summary>Reconhece o tipo de imagem pela assinatura do conteúdo, sem confiar no nome ou no Content-Type enviados.</summary>
internal static class ImageSignature
{
    /// <summary>Tipos aceitos nesta etapa: JPEG, PNG e WebP.</summary>
    internal static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF) return ("image/jpeg", "jpg");
        if (content.Length >= 8 && content[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ("image/png", "png");
        if (content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8)) return ("image/webp", "webp");
        return null;
    }
}
