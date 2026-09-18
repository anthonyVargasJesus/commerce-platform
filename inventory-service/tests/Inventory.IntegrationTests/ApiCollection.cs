namespace Inventory.IntegrationTests;

// One factory (and one set of containers) shared by every test class. The factory configures the host
// through process-wide environment variables, so two factories running in parallel would overwrite each other.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<InventoryApiFactory>
{
    public const string Name = "Api";
}
