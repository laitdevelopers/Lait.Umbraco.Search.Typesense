using Microsoft.Extensions.Logging;

using Umbraco.Cms.Infrastructure.Migrations;

namespace Umbraco.Cms.Integrations.Search.Typesense.Migrations
{
    public class AddTypesenseIndicesTable : MigrationBase
    {
        public AddTypesenseIndicesTable(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", nameof(AddTypesenseIndicesTable));

            if (TableExists(Constants.TypesenseIndicesTableName))
                Logger.LogDebug("The database table {DbTable} already exists, skipping.", Constants.TypesenseIndicesTableName);
            else
                Create.Table<TypesenseIndex>().Do();
        }
    }
}
