using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Authors the game's content tables: what can be eaten, what gets manufactured, what can be
    /// upgraded and how the store unfolds.  Everything lands in the GameConfig / node assets, so a
    /// designer can retune any row afterwards without touching this file.
    /// </summary>
    public static class ContentFactory
    {
        // --------------------------------------------------------------- props

        public static List<PropDefinition> BuildProps(ThemeConfig theme)
        {
            var props = new List<PropDefinition>(32);

            void Add(string id, string name, string mesh, int tier, float mass, float footprint,
                PropCategory category, float weight, float scaleMin = 0.92f, float scaleMax = 1.1f)
            {
                props.Add(new PropDefinition
                {
                    id = id,
                    displayName = name,
                    meshKey = mesh,
                    tier = tier,
                    mass = mass,
                    baseValue = mass * 1.15f,
                    category = category,
                    spawnWeight = weight,
                    footprint = footprint,
                    scaleRange = new Vector2(scaleMin, scaleMax),
                    tint = Color.white,
                    accent = theme.neonCyan
                });
            }

            // Tier 1 - immediately edible.
            Add("trash_bag", "Trash Bag", "mesh_trash_bag", 1, 0.6f, 0.30f, PropCategory.Litter, 3.0f);
            Add("cone", "Traffic Cone", "mesh_cone", 1, 0.5f, 0.26f, PropCategory.Litter, 2.6f);
            Add("trash_can", "Trash Can", "mesh_trash_can", 1, 1.0f, 0.34f, PropCategory.Litter, 2.4f);
            Add("crate", "Crate", "mesh_crate", 1, 1.2f, 0.40f, PropCategory.Litter, 1.8f);
            Add("barrel", "Barrel", "mesh_barrel", 1, 1.4f, 0.36f, PropCategory.Litter, 1.4f);
            Add("newsbox", "News Box", "mesh_newsbox", 1, 1.6f, 0.34f, PropCategory.StreetFurniture, 1.2f);

            // Tier 2 - street furniture.
            Add("bollard", "Bollard", "mesh_bollard", 2, 1.8f, 0.30f, PropCategory.StreetFurniture, 1.4f);
            Add("parking_meter", "Parking Meter", "mesh_parking_meter", 2, 2.0f, 0.32f, PropCategory.StreetFurniture, 1.3f);
            Add("hydrant", "Hydrant", "mesh_hydrant", 2, 2.2f, 0.45f, PropCategory.StreetFurniture, 1.5f);
            Add("mailbox", "Mailbox", "mesh_mailbox", 2, 2.6f, 0.52f, PropCategory.StreetFurniture, 1.2f);
            Add("planter", "Planter", "mesh_planter", 2, 3.2f, 0.62f, PropCategory.StreetFurniture, 1.1f);
            Add("bench", "Bench", "mesh_bench", 2, 4.0f, 1.05f, PropCategory.StreetFurniture, 1.0f);

            // Tier 3 - big street fixtures.
            Add("street_lamp", "Street Lamp", "mesh_street_lamp", 3, 6.0f, 0.62f, PropCategory.StreetFurniture, 0.9f);
            Add("traffic_light", "Traffic Light", "mesh_traffic_light", 3, 6.5f, 0.62f, PropCategory.StreetFurniture, 0.7f);
            Add("tree", "Street Tree", "mesh_tree", 3, 7.0f, 1.10f, PropCategory.StreetFurniture, 1.1f);
            Add("phone_booth", "Phone Booth", "mesh_phone_booth", 3, 8.0f, 0.95f, PropCategory.StreetFurniture, 0.6f);
            Add("dumpster", "Dumpster", "mesh_dumpster", 3, 10f, 1.45f, PropCategory.StreetFurniture, 0.8f);
            Add("bus_stop", "Bus Shelter", "mesh_bus_stop", 3, 14f, 2.10f, PropCategory.StreetFurniture, 0.5f);

            // Tier 4 - traffic.
            Add("car_compact", "Compact Car", "mesh_car_compact", 4, 20f, 2.30f, PropCategory.Vehicle, 1.2f);
            Add("car_sedan", "Sedan", "mesh_car_sedan", 4, 26f, 2.60f, PropCategory.Vehicle, 1.2f);
            Add("car_taxi", "Taxi", "mesh_car_taxi", 4, 26f, 2.60f, PropCategory.Vehicle, 1.0f);
            Add("van", "Delivery Van", "mesh_van", 4, 34f, 3.00f, PropCategory.Vehicle, 0.8f);

            // Tier 5 - the skyline.
            Add("truck", "Box Truck", "mesh_truck", 5, 64f, 4.40f, PropCategory.Vehicle, 0.5f);
            Add("bus", "City Bus", "mesh_bus", 5, 70f, 5.00f, PropCategory.Vehicle, 0.5f);
            Add("kiosk", "Kiosk", "mesh_kiosk", 5, 45f, 2.10f, PropCategory.Structure, 0.8f, 0.95f, 1.05f);
            Add("water_tower", "Water Tower", "mesh_water_tower", 5, 90f, 2.40f, PropCategory.Structure, 0.4f, 0.95f, 1.1f);
            Add("building_slab", "Low Rise", "mesh_building_slab", 5, 180f, 5.20f, PropCategory.Structure, 1.0f, 0.9f, 1.15f);
            Add("building_step", "Stepped Block", "mesh_building_step", 5, 240f, 5.60f, PropCategory.Structure, 0.8f, 0.9f, 1.1f);
            Add("building_tower", "Tower", "mesh_building_tower", 5, 320f, 4.40f, PropCategory.Structure, 0.6f, 0.95f, 1.2f);

            // Pedestrians (they run away - catching one is a treat).
            Add("pedestrian", "Pedestrian", "mesh_stickman", 2, 3.5f, 0.45f, PropCategory.Pedestrian, 1f, 0.95f, 1.08f);

            return props;
        }

        // ------------------------------------------------------------ products

        public static List<ProductDefinition> BuildProducts(ThemeConfig theme)
        {
            return new List<ProductDefinition>
            {
                new ProductDefinition
                {
                    id = "product_chair", displayName = "Flatpack Chair", meshKey = "mesh_product_chair",
                    iconKey = "icon_box", tint = theme.coral, inputMass = 3f, craftSeconds = 1.1f,
                    basePrice = 14d, unlockTier = 1
                },
                new ProductDefinition
                {
                    id = "product_toy", displayName = "Wobble Toy", meshKey = "mesh_product_toy",
                    iconKey = "icon_star", tint = theme.hotMagenta, inputMass = 4.5f, craftSeconds = 1.3f,
                    basePrice = 32d, unlockTier = 2
                },
                new ProductDefinition
                {
                    id = "product_lamp", displayName = "Mood Lamp", meshKey = "mesh_product_lamp",
                    iconKey = "icon_bolt", tint = theme.sunsetAmber, inputMass = 5.5f, craftSeconds = 1.5f,
                    basePrice = 54d, unlockTier = 3
                },
                new ProductDefinition
                {
                    id = "product_tv", displayName = "Flat Screen", meshKey = "mesh_product_tv",
                    iconKey = "icon_machine", tint = theme.neonCyan, inputMass = 7f, craftSeconds = 1.7f,
                    basePrice = 78d, unlockTier = 4
                }
            };
        }

        // ------------------------------------------------------------ upgrades

        public static List<UpgradeDefinition> BuildUpgrades(ThemeConfig theme)
        {
            var list = new List<UpgradeDefinition>(12);

            void Add(string id, string name, string description, UpgradeEffect effect, double cost,
                float perLevel, int maxLevel, string icon, Color tint)
            {
                list.Add(new UpgradeDefinition
                {
                    id = id, displayName = name, description = description, effect = effect,
                    baseCost = cost, perLevel = perLevel, maxLevel = maxLevel, iconKey = icon, tint = tint
                });
            }

            Add("upg_hole_speed", "Void Thrusters", "", UpgradeEffect.HoleSpeed, 120d, 0.06f, 30, "icon_speed", theme.neonCyan);
            Add("upg_hole_capacity", "Deeper Void", "", UpgradeEffect.HoleCapacity, 200d, 0.12f, 30, "icon_capacity", theme.electricViolet);
            Add("upg_suction", "Gravity Well", "", UpgradeEffect.SuctionPower, 160d, 0.08f, 25, "icon_magnet", theme.hotMagenta);
            Add("upg_growth", "Event Horizon", "", UpgradeEffect.HoleRadius, 240d, 0.07f, 25, "icon_hole", theme.voidIndigo);
            Add("upg_unload", "Pneumatic Dump", "", UpgradeEffect.UnloadSpeed, 180d, 0.10f, 20, "icon_bolt", theme.sunsetAmber);
            Add("upg_machine", "Overclocked Crushers", "", UpgradeEffect.MachineSpeed, 320d, 0.09f, 30, "icon_machine", theme.mint);
            Add("upg_jam", "Self-Cleaning Dies", "", UpgradeEffect.JamResistance, 220d, 0.12f, 15, "icon_wrench", theme.coral);
            Add("upg_price", "Premium Branding", "", UpgradeEffect.SellPrice, 400d, 0.07f, 30, "icon_price", theme.sunsetAmber);
            Add("upg_customers", "Neon Billboard", "", UpgradeEffect.CustomerRate, 280d, 0.08f, 25, "icon_shopper", theme.hotMagenta);
            Add("upg_robots", "Hire Helper", "", UpgradeEffect.RobotCount, 2500d, 1f, 4, "icon_robot", theme.neonCyan);
            Add("upg_offline", "Night Shift", "", UpgradeEffect.OfflineRate, 900d, 0.10f, 20, "icon_offline", theme.electricViolet);

            return list;
        }

        // --------------------------------------------------------------- store

        public struct NodeSpec
        {
            public string Id;
            public string DisplayName;
            public FurnitureType Category;
            public string PrefabKey;
            public string ProductId;
            public Vector3 Position;
            public float Yaw;
            public Vector2 ZoneSize;
            public int Tier;
            public double BaseCost;
            public string[] Dependencies;
            public bool FromStart;
            public float RateMultiplier;
            public float CapacityMultiplier;
            public string IconKey;
        }

        /// <summary>
        /// The storefront floor plan.  Hopper by the entrance, a crafting row at the back, shelves
        /// facing the shop floor, register on the west wall - so the player's route is a tight loop.
        /// </summary>
        public static List<NodeSpec> BuildNodeSpecs()
        {
            var specs = new List<NodeSpec>(20);

            void Add(string id, string name, FurnitureType category, string prefab, Vector3 position, float yaw,
                Vector2 zone, int tier, double cost, string[] dependencies, bool fromStart = false,
                string product = "", float rate = 1f, float capacity = 1f, string icon = "icon_box")
            {
                specs.Add(new NodeSpec
                {
                    Id = id, DisplayName = name, Category = category, PrefabKey = prefab, ProductId = product,
                    Position = position, Yaw = yaw, ZoneSize = zone, Tier = tier, BaseCost = cost,
                    Dependencies = dependencies ?? new string[0], FromStart = fromStart,
                    RateMultiplier = rate, CapacityMultiplier = capacity, IconKey = icon
                });
            }

            // --- starting kit -------------------------------------------------
            Add("hopper_01", "Intake Hopper", FurnitureType.Hopper, "store_hopper",
                new Vector3(-9f, 0f, -5.5f), 0f, new Vector2(4.5f, 4.5f), 0, 0d, null, true, icon: "icon_capacity");
            Add("crusher_machine_01", "Chair Press", FurnitureType.Crafter, "store_machine",
                new Vector3(-4.5f, 0f, 3.5f), 180f, new Vector2(3.4f, 3.0f), 0, 0d, null, true, "product_chair", icon: "icon_machine");
            Add("display_shelf_01", "Chair Display", FurnitureType.Shelf, "store_shelf",
                new Vector3(-4.5f, 0f, -1.5f), 0f, new Vector2(3.2f, 2.4f), 0, 0d, null, true, "product_chair", icon: "icon_box");
            Add("checkout_counter_01", "Checkout", FurnitureType.Register, "store_register",
                new Vector3(-11f, 0f, 1.5f), 90f, new Vector2(3.6f, 3.2f), 0, 0d, null, true, icon: "icon_cash");

            // --- first expansion ----------------------------------------------
            Add("sorting_bin_01", "Sorting Belt", FurnitureType.Conveyor, "store_conveyor",
                new Vector3(-4.5f, 0f, 1f), 0f, new Vector2(3.4f, 1.8f), 1, 450d,
                new[] { "display_shelf_01" }, icon: "icon_gear");
            Add("crusher_machine_02", "Toy Moulder", FurnitureType.Crafter, "store_machine",
                new Vector3(0.5f, 0f, 3.5f), 180f, new Vector2(3.4f, 3.0f), 1, 900d,
                new[] { "display_shelf_01" }, product: "product_toy", icon: "icon_machine");
            Add("display_shelf_02", "Toy Display", FurnitureType.Shelf, "store_shelf",
                new Vector3(0.5f, 0f, -1.5f), 0f, new Vector2(3.2f, 2.4f), 1, 1400d,
                new[] { "crusher_machine_02" }, product: "product_toy");

            // --- mid game -------------------------------------------------------
            Add("robot_bay_01", "Helper Bay", FurnitureType.Robot, "store_robot",
                new Vector3(-11.5f, 0f, 5.5f), 0f, new Vector2(2.6f, 2.6f), 2, 2600d,
                new[] { "display_shelf_02" }, icon: "icon_robot");
            Add("crusher_machine_03", "Lamp Line", FurnitureType.Crafter, "store_machine",
                new Vector3(5.5f, 0f, 3.5f), 180f, new Vector2(3.4f, 3.0f), 2, 4200d,
                new[] { "display_shelf_02" }, product: "product_lamp", icon: "icon_machine");
            Add("display_shelf_03", "Lamp Display", FurnitureType.Shelf, "store_shelf",
                new Vector3(5.5f, 0f, -1.5f), 0f, new Vector2(3.2f, 2.4f), 2, 6000d,
                new[] { "crusher_machine_03" }, product: "product_lamp");
            Add("hopper_02", "Second Intake", FurnitureType.Hopper, "store_hopper",
                new Vector3(9f, 0f, -5.5f), 0f, new Vector2(4.5f, 4.5f), 3, 9000d,
                new[] { "display_shelf_03" }, icon: "icon_capacity");

            // --- late game ------------------------------------------------------
            Add("crusher_machine_04", "Screen Fab", FurnitureType.Crafter, "store_machine",
                new Vector3(10.5f, 0f, 3.5f), 180f, new Vector2(3.4f, 3.0f), 3, 15000d,
                new[] { "display_shelf_03" }, product: "product_tv", rate: 1.15f, icon: "icon_machine");
            Add("display_shelf_04", "Screen Display", FurnitureType.Shelf, "store_shelf",
                new Vector3(10.5f, 0f, -1.5f), 0f, new Vector2(3.2f, 2.4f), 3, 22000d,
                new[] { "crusher_machine_04" }, product: "product_tv", capacity: 1.25f);
            Add("robot_bay_02", "Second Helper", FurnitureType.Robot, "store_robot",
                new Vector3(-11.5f, 0f, 7.5f), 0f, new Vector2(2.6f, 2.6f), 4, 34000d,
                new[] { "display_shelf_04" }, icon: "icon_robot");

            // --- furnishing / flavour -------------------------------------------
            Add("decor_plant_01", "Corner Plant", FurnitureType.Decorative, "store_plant",
                new Vector3(-13.5f, 0f, -8f), 0f, new Vector2(2f, 2f), 1, 600d,
                new[] { "display_shelf_01" }, icon: "icon_star");
            Add("decor_sign_01", "Neon Sign", FurnitureType.Decorative, "store_sign",
                new Vector3(0f, 0f, 8.5f), 0f, new Vector2(2.6f, 2f), 2, 3200d,
                new[] { "display_shelf_02" }, icon: "icon_star");
            Add("decor_lamp_01", "Floor Lamp", FurnitureType.Decorative, "store_lamp",
                new Vector3(13f, 0f, -8f), 0f, new Vector2(2f, 2f), 2, 4800d,
                new[] { "display_shelf_02" }, icon: "icon_star");
            Add("decor_plant_02", "Big Fern", FurnitureType.Decorative, "store_plant",
                new Vector3(13.5f, 0f, 7.5f), 0f, new Vector2(2f, 2f), 3, 11000d,
                new[] { "display_shelf_03" }, icon: "icon_star");

            return specs;
        }
    }
}
