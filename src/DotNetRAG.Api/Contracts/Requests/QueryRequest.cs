namespace DotNetRAG.Api.Contracts.Requests;

public sealed record QueryRequest(string Question, int? TopK = null);
