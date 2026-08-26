using System;
using System.IO;
using Peloton.Simulation.Race;

namespace Peloton.Application;

public interface IRaceScenarioCatalog
{
    RaceScenario Resolve(string scenarioId);
}

public sealed class ContentValidationException : IOException
{
    public ContentValidationException(
        string issueCode,
        string resourcePath,
        string jsonPath,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issueCode);
        IssueCode = issueCode;
        ResourcePath = resourcePath ?? string.Empty;
        JsonPath = jsonPath ?? string.Empty;
    }

    public string IssueCode { get; }

    public string ResourcePath { get; }

    public string JsonPath { get; }
}
