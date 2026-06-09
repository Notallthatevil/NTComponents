namespace NTComponents.Tests.E2E;

public static class PlaywrightE2ECollection {
    public const string Name = "Playwright E2E";
}

[CollectionDefinition(PlaywrightE2ECollection.Name, DisableParallelization = true)]
public sealed class PlaywrightE2ECollectionDefinition;
