using System.IO.Compression;

namespace HortaComunitaria.Api.Features.Demonstration;

/// <summary>
/// Ilustrações PNG geradas para as fotografias fictícias do cenário demonstrativo: céu, solo, canteiro e plantas
/// estilizadas, sem depender de arquivos externos. Não representam fotos reais; os títulos as identificam como ilustrações.
/// </summary>
internal static class DemoIllustration
{
    /// <summary>Tema da cena: cor das plantas e dos frutos (frutos nulos para folhosas).</summary>
    internal sealed record Theme((byte R, byte G, byte B) Leaf, (byte R, byte G, byte B)? Fruit, int Rows, int Seed);

    internal static byte[] Png(Theme theme, int width = 640, int height = 420)
    {
        var pixels = new byte[width * height * 3];
        var random = new Random(theme.Seed);
        void Set(int x, int y, (byte R, byte G, byte B) c)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            var i = (y * width + x) * 3; pixels[i] = c.R; pixels[i + 1] = c.G; pixels[i + 2] = c.B;
        }
        void Disc(int cx, int cy, int r, (byte R, byte G, byte B) c)
        {
            for (var y = -r; y <= r; y++) for (var x = -r; x <= r; x++) if (x * x + y * y <= r * r) Set(cx + x, cy + y, c);
        }
        var horizon = height * 38 / 100;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (y < horizon)
                {
                    var t = y / (double)horizon;
                    Set(x, y, ((byte)(150 + 70 * t), (byte)(195 + 40 * t), (byte)(235 - 10 * t)));
                }
                else
                {
                    var noise = random.Next(-10, 10);
                    Set(x, y, ((byte)Math.Clamp(120 + noise, 0, 255), (byte)Math.Clamp(86 + noise, 0, 255), (byte)Math.Clamp(58 + noise, 0, 255)));
                }
            }
        Disc(width - 90, 70, 34, (250, 222, 120));
        // Canteiros em perspectiva: faixas mais escuras, com plantas maiores à frente.
        for (var row = 0; row < theme.Rows; row++)
        {
            var depth = (row + 1) / (double)theme.Rows;
            var y = horizon + (int)((height - horizon) * (0.12 + 0.8 * depth));
            var band = (int)(10 + 26 * depth);
            for (var yy = y - band / 2; yy < y + band / 2; yy++) for (var x = 0; x < width; x++) Set(x, yy, (96, 66, 44));
            var size = (int)(8 + 22 * depth);
            for (var x = size; x < width; x += size * 2 + random.Next(4, 14))
            {
                var leaf = ((byte)Math.Clamp(theme.Leaf.R + random.Next(-18, 18), 0, 255), (byte)Math.Clamp(theme.Leaf.G + random.Next(-18, 18), 0, 255), theme.Leaf.B);
                Disc(x, y - size / 2, size, leaf);
                Disc(x - size / 2, y - size / 3, size * 2 / 3, leaf);
                Disc(x + size / 2, y - size / 3, size * 2 / 3, leaf);
                if (theme.Fruit is { } fruit && random.Next(3) > 0) Disc(x + random.Next(-size / 2, size / 2), y - size / 2 + random.Next(-size / 3, size / 3), Math.Max(3, size / 4), fruit);
            }
        }
        return Encode(pixels, width, height);
    }

    private static byte[] Encode(byte[] rgb, int width, int height)
    {
        using var raw = new MemoryStream();
        for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(rgb, y * width * 3, width * 3); }
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) { raw.Position = 0; raw.CopyTo(zlib); }
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        WriteInt(header, 0, width); WriteInt(header, 4, height); header[8] = 8; header[9] = 2; // 8 bits, RGB
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", compressed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4]; WriteInt(length, 0, data.Length); output.Write(length);
        var typed = new byte[4 + data.Length];
        for (var i = 0; i < 4; i++) typed[i] = (byte)type[i];
        data.CopyTo(typed, 4);
        output.Write(typed);
        var crc = new byte[4]; WriteInt(crc, 0, (int)Crc32(typed)); output.Write(crc);
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24); buffer[offset + 1] = (byte)(value >> 16); buffer[offset + 2] = (byte)(value >> 8); buffer[offset + 3] = (byte)value;
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
