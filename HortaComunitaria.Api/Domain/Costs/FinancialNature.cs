namespace HortaComunitaria.Api.Domain.Costs;
/// <summary>Previsão e realização são naturezas distintas do lançamento.</summary>
public enum FinancialNature
{
    /// <summary>Previsão conhecida, separada da realização.</summary>
    Planned,
    /// <summary>Despesa realizada conhecida.</summary>
    Actual
}
