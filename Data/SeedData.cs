using System;
using System.Collections.Generic;
using System.Linq;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Data
{
    public static class SeedData
    {
        public static void EnsureSeedData(AppDbContext db, bool force = false)
        {
            if (db.Residents.Any() && !force) return;

            if (force)
            {
                // Remove existing data in the correct order to respect FKs
                var existingHist = db.DeliveryHistories.ToList();
                if (existingHist.Any()) db.DeliveryHistories.RemoveRange(existingHist);

                var existingOtps = db.OtpCodes.ToList();
                if (existingOtps.Any()) db.OtpCodes.RemoveRange(existingOtps);

                var existingPackages = db.Packages.ToList();
                if (existingPackages.Any()) db.Packages.RemoveRange(existingPackages);

                var existingResidents = db.Residents.ToList();
                if (existingResidents.Any()) db.Residents.RemoveRange(existingResidents);

                db.SaveChanges();
            }

            var rnd = new Random(42);

            // Create 15 residents
            var names = new[] {
                "Alice Smith", "Bob Jones", "Carol White", "David Brown", "Eve Davis",
                "Frank Wilson", "Grace Lee", "Hank Clark", "Ivy Lewis", "Jack Walker",
                "Kara Hall", "Liam Young", "Mona King", "Nate Wright", "Olivia Scott"
            };

            var residents = new List<Resident>();
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var email = name.ToLower().Replace(' ', '.') + "@example.com";
                var phone = $"555-{1000 + i:D4}";
                residents.Add(new Resident { Name = name, Email = email, PhoneNumber = phone, IsMember = (i % 2 == 0) });
            }

            db.Residents.AddRange(residents);
            db.SaveChanges();

            // Create 25 packages; assign most to residents, leave some unassigned
            var packages = new List<Package>();
            for (int i = 0; i < 25; i++)
            {
                var tracking = $"TN{100000 + i}";
                var desc = $"Sample package #{i + 1}";
                // Received within last 10 days
                var receivedAt = DateTime.UtcNow.AddDays(-rnd.Next(0, 10)).AddHours(-rnd.Next(0, 24));

                Guid? residentId = null;
                // Assign first 20 packages to residents in round-robin
                if (i < 20)
                {
                    var r = residents[i % residents.Count];
                    residentId = r.Id;
                }

                packages.Add(new Package { TrackingNumber = tracking, Description = desc, ResidentId = residentId, ReceivedAt = receivedAt, Courier = $"Courier {i % 3 + 1}" });
            }

            db.Packages.AddRange(packages);
            db.SaveChanges();

            // Create 5 delivery history entries for some picked-up packages
            var pickedPackages = packages.Where(p => p.ResidentId != null).OrderBy(p => p.ReceivedAt).Take(5).ToList();
            foreach (var pkg in pickedPackages)
            {
                pkg.IsPickedUp = true;
                db.DeliveryHistories.Add(new DeliveryHistory { PackageId = pkg.Id, DeliveredAt = pkg.ReceivedAt.AddDays(rnd.Next(0, 5)), Notes = "Picked up (seed)" });
            }

            db.SaveChanges();
        }
    }
}
