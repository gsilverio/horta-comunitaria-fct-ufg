namespace HortaComunitaria.Api.IntegrationTests;

/// <summary>UUIDs v7 estáveis exclusivamente para as fixtures de testes.</summary>
internal static class DemoIds
{
    internal static readonly Guid Garden = Guid.Parse("01a11c0e-0d43-77e9-83f0-25404edc4cc5");
    internal static readonly Guid SectorOne = Guid.Parse("01a11c0e-0d4a-71e0-9b3e-6ff7bb54245b");
    internal static readonly Guid SectorTwo = Guid.Parse("01a11c0e-0d4a-7872-adc6-357d4ab33105");
    internal static readonly Guid Participant = Guid.Parse("01a11c0e-0d4c-7c58-884b-78bf1729acbf");
    /// <summary>Conta de teste com perfil Operador.</summary>
    internal static readonly Guid Operator = Guid.Parse("01a11c0e-0d4c-7928-ab6b-fd88c78ae086");
    /// <summary>Conta de teste com perfil Adm; autora padrão das gravações dos testes.</summary>
    internal static readonly Guid Administrator = Guid.Parse("01a11c0e-0d4c-7f31-a2d4-3c5e8b9a6f10");
    internal static readonly Guid[] Beds =
    [
        Guid.Parse("01a11c0e-0d4b-7411-b9f3-f5a646477e57"),
        Guid.Parse("01a11c0e-0d4b-7360-ac23-889dd3effc2e"),
        Guid.Parse("01a11c0e-0d4b-7883-a1fd-c98a47cb335c"),
        Guid.Parse("01a11c0e-0d4b-70ff-806c-238d20497b3e")
    ];
    internal static readonly Guid[] Assignments =
    [
        Guid.Parse("01a11c0e-0d4b-7245-b1bf-15b69ab63f3b"),
        Guid.Parse("01a11c0e-0d4c-7bae-a634-4d147aae8fb3"),
        Guid.Parse("01a11c0e-0d4c-7340-9335-bc1eb65def2a"),
        Guid.Parse("01a11c0e-0d4c-7ece-959d-227e5894deec")
    ];
}
