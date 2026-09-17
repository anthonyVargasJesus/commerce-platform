using Inventory.Domain.Categories;
using Inventory.Domain.ProductTypes;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Persistence;

public static class InventoryDbContextSeed
{
    public static async Task SeedAsync(InventoryDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Inventory database already contains products; skipping seed.");
            return;
        }

        logger.LogInformation("Seeding inventory database with sample categories, product types and products...");

        var categories = new List<Category>
        {
            Category.Create("Laptops & Desktops", "Portable and desktop computers"),
            Category.Create("Monitors", "Computer displays"),
            Category.Create("Peripherals", "Keyboards, mice, webcams and other input devices"),
            Category.Create("Audio", "Headphones and speakers"),
            Category.Create("Networking", "Routers, switches, access points and cabling"),
            Category.Create("Storage", "SSDs, HDDs and flash drives"),
            Category.Create("Mobile Accessories", "Chargers, cables and power banks"),
            Category.Create("Office Supplies", "Furniture and stationery"),
            Category.Create("Printers & Imaging", "Printers, ink and photo paper"),
        };

        var productTypes = new List<ProductType>
        {
            ProductType.Create("Durable Equipment", "Long-lived assets: computers, monitors, networking gear, furniture"),
            ProductType.Create("Accessory", "Add-ons and peripherals for other equipment"),
            ProductType.Create("Consumable", "Items that get used up and need regular replenishment"),
            ProductType.Create("Legacy/Discontinued", "Superseded items still held in inventory"),
        };

        Guid CategoryId(string name) => categories.Single(c => c.Name == name).Id;
        Guid TypeId(string name) => productTypes.Single(t => t.Name == name).Id;

        var products = new List<Product>
        {
            // Laptops & desktops
            Product.Create("ELEC-LAP-DEL15", "Dell XPS 15 Laptop", 1899.99m, 24, 5, "15.6\" 4K OLED, Intel Core i7-14700H, 32GB RAM, 1TB SSD", CategoryId("Laptops & Desktops"), TypeId("Durable Equipment")),
            Product.Create("ELEC-LAP-MBP14", "Apple MacBook Pro 14\"", 2199.00m, 15, 4, "M4 Pro chip, 18GB unified memory, 512GB SSD", CategoryId("Laptops & Desktops"), TypeId("Durable Equipment")),
            Product.Create("ELEC-LAP-THX1", "Lenovo ThinkPad X1 Carbon", 1649.50m, 3, 6, "14\" WUXGA, Intel Core Ultra 7, 16GB RAM, 1TB SSD", CategoryId("Laptops & Desktops"), TypeId("Durable Equipment")),
            Product.Create("ELEC-DSK-OPTX", "HP OptiPlex Tower Desktop", 899.00m, 18, 5, "Intel Core i5-14500, 16GB RAM, 512GB SSD, Windows 11 Pro", CategoryId("Laptops & Desktops"), TypeId("Durable Equipment")),
            Product.Create("ELEC-DSK-MMIN", "Apple Mac Mini M4", 699.00m, 22, 6, "M4 chip, 16GB unified memory, 256GB SSD", CategoryId("Laptops & Desktops"), TypeId("Durable Equipment")),

            // Monitors
            Product.Create("ELEC-MON-DEL27", "Dell UltraSharp U2723QE 27\"", 549.99m, 30, 8, "4K UHD IPS Black, USB-C hub, height adjustable stand", CategoryId("Monitors"), TypeId("Durable Equipment")),
            Product.Create("ELEC-MON-LG34", "LG UltraWide 34WN80C 34\"", 429.99m, 4, 6, "21:9 QHD curved monitor, USB-C 60W charging", CategoryId("Monitors"), TypeId("Durable Equipment")),
            Product.Create("ELEC-MON-SAM24", "Samsung ViewFinity S6 24\"", 199.99m, 45, 10, "Full HD IPS monitor with HDR10", CategoryId("Monitors"), TypeId("Durable Equipment")),

            // Peripherals
            Product.Create("ELEC-PER-LOGMX", "Logitech MX Master 3S Mouse", 99.99m, 60, 15, "Wireless mouse with quiet clicks and 8K DPI tracking", CategoryId("Peripherals"), TypeId("Accessory")),
            Product.Create("ELEC-PER-LOGKB", "Logitech MX Keys Keyboard", 109.99m, 55, 15, "Wireless illuminated keyboard, multi-device", CategoryId("Peripherals"), TypeId("Accessory")),
            Product.Create("ELEC-PER-KEYCH", "Keychron K8 Mechanical Keyboard", 89.00m, 2, 10, "Hot-swappable, Gateron Brown switches, wireless", CategoryId("Peripherals"), TypeId("Accessory")),
            Product.Create("ELEC-PER-RAZDA", "Razer DeathAdder V3 Mouse", 69.99m, 40, 10, "Ergonomic gaming mouse, 30K DPI optical sensor", CategoryId("Peripherals"), TypeId("Accessory")),
            Product.Create("ELEC-PER-WEBCM", "Logitech Brio 4K Webcam", 199.99m, 0, 8, "Ultra HD webcam with HDR and Windows Hello support", CategoryId("Peripherals"), TypeId("Accessory")),

            // Audio
            Product.Create("ELEC-AUD-SNYWH", "Sony WH-1000XM5 Headphones", 349.99m, 28, 8, "Noise-cancelling over-ear wireless headphones", CategoryId("Audio"), TypeId("Accessory")),
            Product.Create("ELEC-AUD-APDPR", "Apple AirPods Pro (2nd gen)", 249.00m, 50, 12, "Active noise cancellation, USB-C charging case", CategoryId("Audio"), TypeId("Accessory")),
            Product.Create("ELEC-AUD-JBLFL", "JBL Flip 6 Bluetooth Speaker", 129.95m, 33, 10, "Portable waterproof speaker, 12h battery life", CategoryId("Audio"), TypeId("Accessory")),

            // Networking
            Product.Create("NET-RTR-UBQD7", "Ubiquiti UniFi Dream Machine Pro", 379.00m, 12, 4, "All-in-one UniFi network appliance with 10G SFP+", CategoryId("Networking"), TypeId("Durable Equipment")),
            Product.Create("NET-SWT-TPL24", "TP-Link 24-Port Gigabit Switch", 149.99m, 9, 5, "Unmanaged rackmount Ethernet switch, 24x RJ45", CategoryId("Networking"), TypeId("Durable Equipment")),
            Product.Create("NET-AP-UBQ6L", "Ubiquiti UniFi 6 Lite Access Point", 99.00m, 26, 8, "Wi-Fi 6 access point, PoE powered", CategoryId("Networking"), TypeId("Durable Equipment")),
            Product.Create("NET-CBL-CAT6R", "Cat 6 Ethernet Cable 50ft", 14.99m, 120, 25, "Snagless UTP patch cable, black", CategoryId("Networking"), TypeId("Accessory")),

            // Storage
            Product.Create("STOR-SSD-SAM2T", "Samsung 990 Pro NVMe SSD 2TB", 149.99m, 40, 10, "PCIe 4.0 M.2 SSD, up to 7450MB/s read", CategoryId("Storage"), TypeId("Durable Equipment")),
            Product.Create("STOR-SSD-CRU1T", "Crucial X9 Portable SSD 1TB", 89.99m, 65, 15, "USB-C portable SSD, up to 1050MB/s", CategoryId("Storage"), TypeId("Accessory")),
            Product.Create("STOR-HDD-WDR4T", "WD Red Plus 4TB NAS HDD", 109.99m, 5, 8, "5400 RPM NAS hard drive, CMR technology", CategoryId("Storage"), TypeId("Durable Equipment")),
            Product.Create("STOR-USB-SAN128", "SanDisk Extreme Pro USB 128GB", 34.99m, 90, 20, "USB 3.2 flash drive, up to 420MB/s read", CategoryId("Storage"), TypeId("Accessory")),

            // Mobile accessories
            Product.Create("MOB-CHG-ANK65", "Anker 65W GaN Charger", 49.99m, 70, 15, "3-port compact fast charger, USB-C PD", CategoryId("Mobile Accessories"), TypeId("Accessory")),
            Product.Create("MOB-CBL-USBC2M", "USB-C to USB-C Cable 2m", 12.99m, 150, 30, "Braided nylon cable, 100W power delivery", CategoryId("Mobile Accessories"), TypeId("Accessory")),
            Product.Create("MOB-BNK-ANK20", "Anker PowerCore 20000mAh", 54.99m, 38, 10, "Portable power bank with USB-C fast charging", CategoryId("Mobile Accessories"), TypeId("Accessory")),

            // Office supplies
            Product.Create("OFF-CHR-ERGO1", "ErgoChair Pro Mesh Office Chair", 329.00m, 14, 4, "Ergonomic mesh chair with lumbar support and armrests", CategoryId("Office Supplies"), TypeId("Durable Equipment")),
            Product.Create("OFF-DSK-STND1", "FlexiDesk Standing Desk 160cm", 449.00m, 8, 3, "Electric height-adjustable desk with memory presets", CategoryId("Office Supplies"), TypeId("Durable Equipment")),
            Product.Create("OFF-STY-PAPA4", "Premium A4 Paper Ream (500 sheets)", 6.49m, 300, 50, "80gsm multipurpose printing paper", CategoryId("Office Supplies"), TypeId("Consumable")),
            Product.Create("OFF-STY-PENBX", "Ballpoint Pens Box of 50", 11.99m, 200, 40, "Medium point black ink pens", CategoryId("Office Supplies"), TypeId("Consumable")),
            Product.Create("OFF-STY-NOTBK", "Hardcover Notebook A5", 8.99m, 180, 30, "Dotted grid pages, 192 sheets", CategoryId("Office Supplies"), TypeId("Consumable")),
            Product.Create("OFF-STR-SHLF1", "5-Tier Steel Storage Shelf", 89.99m, 6, 3, "Adjustable warehouse shelving unit, 500kg capacity", CategoryId("Office Supplies"), TypeId("Durable Equipment")),

            // Printers & imaging
            Product.Create("PRT-LSR-HP4001", "HP LaserJet Pro 4001dw", 349.00m, 10, 4, "Monochrome laser printer with duplex and Wi-Fi", CategoryId("Printers & Imaging"), TypeId("Durable Equipment")),
            Product.Create("PRT-INK-HP414", "HP 414 Ink Cartridge Set", 44.99m, 55, 15, "Tri-color and black ink cartridges, standard yield", CategoryId("Printers & Imaging"), TypeId("Consumable")),
            Product.Create("PRT-PAP-PHOT", "Glossy Photo Paper 4x6 (100 pack)", 9.99m, 75, 20, "High-gloss inkjet photo paper", CategoryId("Printers & Imaging"), TypeId("Consumable")),

            // Discontinued / legacy items
            Product.Create("ELEC-MON-DEL22", "Dell E2216H 22\" Monitor (Legacy)", 129.99m, 3, 5, "1080p TN monitor, VGA/DVI only", CategoryId("Monitors"), TypeId("Legacy/Discontinued")),
            Product.Create("ELEC-PER-WKBRD", "Wired USB Keyboard (Legacy)", 14.99m, 20, 10, "Basic full-size wired keyboard", CategoryId("Peripherals"), TypeId("Legacy/Discontinued")),
        };

        products.Single(p => p.Sku.Value == "ELEC-MON-DEL22").Deactivate();
        products.Single(p => p.Sku.Value == "ELEC-PER-WKBRD").Deactivate();

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.ProductTypes.AddRangeAsync(productTypes, cancellationToken);
        await context.Products.AddRangeAsync(products, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {CategoryCount} categories, {TypeCount} product types and {ProductCount} products.",
            categories.Count,
            productTypes.Count,
            products.Count);
    }
}
