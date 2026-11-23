using HubManagement.Core.Hubs.Entities;
using HubManagement.Core.Hubs.Models;

namespace HubManagement.Application.Analytics.Services
{
    internal static class PackageCustodyActivityFormatter
    {
        public static string Format(PackageCustodyHop hop, string? pkgNumber)
        {
            var pkg = string.IsNullOrWhiteSpace(pkgNumber) ? "Package" : pkgNumber;
            var staff = string.IsNullOrWhiteSpace(hop.StaffUsername) ? null : hop.StaffUsername;
            var staffSuffix = staff is null ? string.Empty : $", staff {staff}";

            return hop.Kind switch
            {
                PackageCustodyHopKinds.ReceivedAtHub =>
                    $"{pkg} received at {hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}{staffSuffix}",
                PackageCustodyHopKinds.Shelved =>
                    string.IsNullOrWhiteSpace(hop.LocationLabel)
                        ? $"{pkg} shelved at {hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}{staffSuffix}"
                        : $"{pkg} shelved at {hop.LocationLabel} ({hop.ToZone ?? PackageCustodyHopKinds.DefaultHubZone}){staffSuffix}",
                PackageCustodyHopKinds.ZoneTransfer =>
                    $"{pkg} left {hop.FromZone ?? "?"} → arrived {hop.ToZone ?? "?"}{staffSuffix}"
                    + (string.IsNullOrWhiteSpace(hop.LocationLabel) ? string.Empty : $". Shelf {hop.LocationLabel}"),
                PackageCustodyHopKinds.OutForDoorDelivery =>
                    $"{pkg} out for door delivery{(string.IsNullOrWhiteSpace(hop.LocationLabel) ? string.Empty : $" to {hop.LocationLabel}")}{staffSuffix}",
                PackageCustodyHopKinds.ArrivedAtTowerFloor =>
                    $"{pkg} arrived at {hop.ToZone ?? hop.LocationLabel ?? "tower/floor"}{staffSuffix}",
                PackageCustodyHopKinds.Delivered =>
                    $"{pkg} delivered{(string.IsNullOrWhiteSpace(hop.Notes) ? string.Empty : $" ({hop.Notes})")}{staffSuffix}",
                PackageCustodyHopKinds.DeliveryFailed =>
                    $"{pkg} delivery failed{(string.IsNullOrWhiteSpace(hop.Notes) ? string.Empty : $" — {hop.Notes}")}{staffSuffix}",
                PackageCustodyHopKinds.ReturnedToCage =>
                    $"{pkg} returned to {hop.ToZone ?? "cage"}{staffSuffix}",
                _ => $"{pkg} {hop.Kind}{staffSuffix}",
            };
        }
    }
}
