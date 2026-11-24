using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <summary>
    /// Snapshot-only sync: PackageDeliveryEvidence was added via hand-written
    /// 20261123143000_AddPackageDeliveryEvidence without updating the model snapshot,
    /// which blocked Database.Migrate() with PendingModelChangesWarning.
    /// </summary>
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
        }
    }
}
