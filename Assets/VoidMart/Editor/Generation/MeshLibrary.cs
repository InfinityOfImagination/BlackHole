using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Every 3D asset in the game, authored in code.  Meshes bake their colours into vertex
    /// colours so the entire world can share one clay material and batch aggressively, while
    /// still looking hand-painted.  All props sit on y = 0 and are centred in XZ.
    /// </summary>
    public static class MeshLibrary
    {
        public delegate void Recipe(MeshBuilder builder, ThemeConfig theme, ArtConfig art);

        public static Dictionary<string, Recipe> Recipes()
        {
            return new Dictionary<string, Recipe>
            {
                // ---- utility -------------------------------------------------
                { "mesh_quad_xz", (b, t, a) => b.AddGroundQuad(Vector3.zero, Vector2.one, Color.white, 1f) },
                { "mesh_quad_xy", (b, t, a) => b.AddUprightQuad(Vector3.zero, Vector2.one, Color.white) },
                { "mesh_cube", (b, t, a) => b.AddBox(new Vector3(0f, 0.5f, 0f), Vector3.one, Color.white, a.meshBevel * 0.5f) },
                { "mesh_hole_ring", BuildHoleRing },
                { "mesh_pit_tube", BuildPitTube },

                // ---- street litter (tier 1) ---------------------------------
                { "mesh_trash_bag", BuildTrashBag },
                { "mesh_trash_can", BuildTrashCan },
                { "mesh_cone", BuildTrafficCone },
                { "mesh_hydrant", BuildHydrant },
                { "mesh_crate", BuildCrate },
                { "mesh_barrel", BuildBarrel },

                // ---- street furniture (tier 2-3) ----------------------------
                { "mesh_mailbox", BuildMailbox },
                { "mesh_bench", BuildBench },
                { "mesh_parking_meter", BuildParkingMeter },
                { "mesh_planter", BuildPlanter },
                { "mesh_newsbox", BuildNewsBox },
                { "mesh_bollard", BuildBollard },
                { "mesh_street_lamp", BuildStreetLamp },
                { "mesh_traffic_light", BuildTrafficLight },
                { "mesh_tree", BuildTree },
                { "mesh_dumpster", BuildDumpster },
                { "mesh_phone_booth", BuildPhoneBooth },
                { "mesh_bus_stop", BuildBusStop },

                // ---- vehicles (tier 4) --------------------------------------
                { "mesh_car_sedan", (b, t, a) => BuildCar(b, t, a, t.coral, 4.2f) },
                { "mesh_car_taxi", (b, t, a) => BuildCar(b, t, a, t.sunsetAmber, 4.3f) },
                { "mesh_car_compact", (b, t, a) => BuildCar(b, t, a, t.mint, 3.6f) },
                { "mesh_van", BuildVan },
                { "mesh_bus", BuildBus },
                { "mesh_truck", BuildTruck },

                // ---- structures (tier 5) ------------------------------------
                { "mesh_building_slab", (b, t, a) => BuildBuilding(b, t, a, 0) },
                { "mesh_building_step", (b, t, a) => BuildBuilding(b, t, a, 1) },
                { "mesh_building_tower", (b, t, a) => BuildBuilding(b, t, a, 2) },
                { "mesh_kiosk", BuildKiosk },
                { "mesh_water_tower", BuildWaterTower },

                // ---- characters ---------------------------------------------
                { "mesh_stickman", (b, t, a) => BuildStickman(b, t, a, t.paper, t.electricViolet) },
                { "mesh_customer_a", (b, t, a) => BuildStickman(b, t, a, t.cream, t.hotMagenta) },
                { "mesh_customer_b", (b, t, a) => BuildStickman(b, t, a, t.paper, t.neonCyan) },
                { "mesh_customer_c", (b, t, a) => BuildStickman(b, t, a, t.cream, t.mint) },
                { "mesh_robot", BuildRobot },

                // ---- storefront ----------------------------------------------
                { "mesh_hopper", BuildHopper },
                { "mesh_machine", BuildMachine },
                { "mesh_shelf", BuildShelf },
                { "mesh_counter", BuildCounter },
                { "mesh_conveyor", BuildConveyor },
                { "mesh_plant", BuildPlant },
                { "mesh_sign", BuildSign },
                { "mesh_floor_lamp", BuildFloorLamp },
                { "mesh_zone_pad", BuildZonePad },
                { "mesh_arrow", BuildArrow },
                { "mesh_wrench", BuildWrench },

                // ---- goods & fx ----------------------------------------------
                { "mesh_product_chair", BuildChair },
                { "mesh_product_toy", BuildToy },
                { "mesh_product_tv", BuildTv },
                { "mesh_product_lamp", BuildLampProduct },
                { "mesh_product_box", BuildProductBox },
                { "mesh_cash_bill", BuildCashBill },
                { "mesh_junk_chunk", BuildJunkChunk },
                { "mesh_gem", BuildGem }
            };
        }

        // ================================================================ utility

        static Color Shade(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * (1f + amount)),
                Mathf.Clamp01(color.g * (1f + amount)),
                Mathf.Clamp01(color.b * (1f + amount)),
                color.a);
        }

        static void BuildHoleRing(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddRing(new Vector3(0f, 0.012f, 0f), 0.5f, 0.58f, 48, t.neonCyan, new Color(t.hotMagenta.r, t.hotMagenta.g, t.hotMagenta.b, 0f));
            b.AddRing(new Vector3(0f, 0.008f, 0f), 0.58f, 0.66f, 48, Shade(t.electricViolet, -0.2f), new Color(t.voidInk.r, t.voidInk.g, t.voidInk.b, 0f));
        }

        static void BuildPitTube(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            // Authored with the rim at y = 0 so scaling the depth only ever digs downwards.
            b.AddTube(Vector3.zero, 0.5f, 1f, 32, t.voidIndigo, t.voidInk);
        }

        // ============================================================ tier 1 junk

        static void BuildTrashBag(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = new Color(0.16f, 0.17f, 0.22f);
            b.AddSphere(new Vector3(0f, 0.3f, 0f), 0.32f, 8, 5, body);
            b.AddSphere(new Vector3(0.1f, 0.5f, 0.05f), 0.18f, 6, 4, Shade(body, 0.2f));
            b.AddCylinder(new Vector3(0.05f, 0.56f, 0.02f), 0.09f, 0.03f, 0.18f, 6, Shade(body, 0.35f));
        }

        static void BuildTrashCan(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.36f, 0.40f, 0.48f);
            b.AddCylinder(Vector3.zero, 0.27f, 0.31f, 0.78f, 12, metal, true, Shade(metal, 0.15f));
            b.AddCylinder(new Vector3(0f, 0.78f, 0f), 0.34f, 0.30f, 0.1f, 12, Shade(metal, 0.3f));
            b.AddCylinder(new Vector3(0f, 0.88f, 0f), 0.08f, 0.06f, 0.07f, 8, t.sunsetAmber);
            b.AddBox(new Vector3(0f, 0.42f, 0.31f), new Vector3(0.22f, 0.22f, 0.02f), t.neonCyan * 0.85f);
        }

        static void BuildTrafficCone(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var orange = new Color(0.97f, 0.42f, 0.16f);
            b.AddBox(new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.42f), Shade(orange, -0.25f), 0.02f);
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 0.17f, 0.03f, 0.62f, 10, orange);
            b.AddCylinder(new Vector3(0f, 0.33f, 0f), 0.105f, 0.085f, 0.1f, 10, t.paper);
        }

        static void BuildHydrant(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var red = new Color(0.85f, 0.22f, 0.24f);
            b.AddCylinder(Vector3.zero, 0.19f, 0.17f, 0.12f, 10, Shade(red, -0.3f));
            b.AddCylinder(new Vector3(0f, 0.12f, 0f), 0.14f, 0.13f, 0.5f, 10, red);
            b.AddSphere(new Vector3(0f, 0.64f, 0f), 0.14f, 10, 5, red);
            b.AddCylinder(new Vector3(0f, 0.72f, 0f), 0.05f, 0.05f, 0.09f, 8, Shade(red, 0.3f));
            b.Push(new Vector3(0.14f, 0.42f, 0f), Quaternion.Euler(0f, 0f, -90f));
            b.AddCylinder(Vector3.zero, 0.07f, 0.07f, 0.09f, 8, Shade(red, 0.25f));
            b.Pop();
            b.Push(new Vector3(-0.14f, 0.42f, 0f), Quaternion.Euler(0f, 0f, 90f));
            b.AddCylinder(Vector3.zero, 0.07f, 0.07f, 0.09f, 8, Shade(red, 0.25f));
            b.Pop();
        }

        static void BuildCrate(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var wood = t.shelfWood;
            b.AddBox(new Vector3(0f, 0.28f, 0f), new Vector3(0.56f, 0.56f, 0.56f), wood, a.meshBevel);
            b.AddBox(new Vector3(0f, 0.28f, 0.285f), new Vector3(0.5f, 0.09f, 0.02f), Shade(wood, -0.25f));
            b.AddBox(new Vector3(0f, 0.28f, -0.285f), new Vector3(0.5f, 0.09f, 0.02f), Shade(wood, -0.25f));
        }

        static void BuildBarrel(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var blue = new Color(0.25f, 0.45f, 0.68f);
            b.AddCylinder(Vector3.zero, 0.28f, 0.28f, 0.82f, 12, blue, true, Shade(blue, 0.2f));
            b.AddCylinder(new Vector3(0f, 0.2f, 0f), 0.3f, 0.3f, 0.06f, 12, Shade(blue, -0.3f), false);
            b.AddCylinder(new Vector3(0f, 0.56f, 0f), 0.3f, 0.3f, 0.06f, 12, Shade(blue, -0.3f), false);
        }

        // ====================================================== street furniture

        static void BuildMailbox(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var blue = new Color(0.20f, 0.35f, 0.62f);
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(0.16f, 0.36f, 0.16f), Shade(blue, -0.4f));
            b.AddBox(new Vector3(0f, 0.7f, 0f), new Vector3(0.52f, 0.66f, 0.42f), blue, a.meshBevel * 2f);
            b.AddBox(new Vector3(0f, 0.92f, 0.22f), new Vector3(0.3f, 0.06f, 0.03f), Shade(blue, 0.5f));
            b.AddBox(new Vector3(0f, 0.55f, 0.215f), new Vector3(0.34f, 0.1f, 0.02f), t.paper);
        }

        static void BuildBench(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var wood = new Color(0.72f, 0.52f, 0.33f);
            var metal = new Color(0.28f, 0.30f, 0.36f);
            for (int i = -1; i <= 1; i += 2)
            {
                b.AddBox(new Vector3(i * 0.7f, 0.2f, 0f), new Vector3(0.08f, 0.4f, 0.5f), metal);
                b.AddBox(new Vector3(i * 0.7f, 0.62f, -0.22f), new Vector3(0.08f, 0.45f, 0.08f), metal);
            }
            for (int s = 0; s < 3; s++)
                b.AddBox(new Vector3(0f, 0.42f, -0.18f + s * 0.18f), new Vector3(1.7f, 0.07f, 0.15f), wood, 0.02f);
            for (int s = 0; s < 2; s++)
                b.AddBox(new Vector3(0f, 0.68f + s * 0.18f, -0.24f), new Vector3(1.7f, 0.07f, 0.12f), wood, 0.02f);
        }

        static void BuildParkingMeter(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.32f, 0.35f, 0.42f);
            b.AddCylinder(Vector3.zero, 0.1f, 0.06f, 1.05f, 10, metal);
            b.AddBox(new Vector3(0f, 1.2f, 0f), new Vector3(0.26f, 0.34f, 0.18f), Shade(metal, 0.25f), a.meshBevel);
            b.AddBox(new Vector3(0f, 1.26f, 0.095f), new Vector3(0.16f, 0.14f, 0.02f), t.neonCyan);
        }

        static void BuildPlanter(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var stone = new Color(0.62f, 0.60f, 0.58f);
            b.AddCylinder(Vector3.zero, 0.42f, 0.46f, 0.46f, 10, stone, true, Shade(stone, -0.2f));
            b.AddSphere(new Vector3(0f, 0.62f, 0f), 0.34f, 8, 5, t.foliage);
            b.AddSphere(new Vector3(0.18f, 0.72f, 0.1f), 0.2f, 7, 4, Shade(t.foliage, 0.25f));
        }

        static void BuildNewsBox(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = t.hotMagenta * 0.8f;
            body.a = 1f;
            b.AddBox(new Vector3(0f, 0.45f, 0f), new Vector3(0.44f, 0.8f, 0.36f), body, a.meshBevel);
            b.AddBox(new Vector3(0f, 0.72f, 0.19f), new Vector3(0.3f, 0.24f, 0.02f), t.paper);
            for (int i = -1; i <= 1; i += 2)
                b.AddCylinder(new Vector3(i * 0.16f, 0f, 0f), 0.04f, 0.04f, 0.08f, 6, new Color(0.2f, 0.2f, 0.24f));
        }

        static void BuildBollard(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.30f, 0.33f, 0.40f);
            b.AddCylinder(Vector3.zero, 0.13f, 0.11f, 0.8f, 10, metal);
            b.AddCylinder(new Vector3(0f, 0.8f, 0f), 0.12f, 0.05f, 0.1f, 10, Shade(metal, 0.3f));
            b.AddCylinder(new Vector3(0f, 0.6f, 0f), 0.12f, 0.12f, 0.07f, 10, t.sunsetAmber, false);
        }

        static void BuildStreetLamp(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.24f, 0.26f, 0.33f);
            b.AddCylinder(Vector3.zero, 0.16f, 0.12f, 0.18f, 10, Shade(metal, -0.3f));
            b.AddCylinder(new Vector3(0f, 0.18f, 0f), 0.09f, 0.07f, 3.6f, 8, metal);
            b.Push(new Vector3(0f, 3.78f, 0f), Quaternion.Euler(0f, 0f, -60f));
            b.AddCylinder(Vector3.zero, 0.07f, 0.06f, 0.7f, 8, metal);
            b.Pop();
            b.AddBox(new Vector3(0.58f, 3.95f, 0f), new Vector3(0.44f, 0.12f, 0.24f), Shade(metal, 0.2f), 0.03f);
            b.AddBox(new Vector3(0.58f, 3.87f, 0f), new Vector3(0.34f, 0.05f, 0.18f), t.sunsetAmber);
        }

        static void BuildTrafficLight(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.22f, 0.24f, 0.30f);
            b.AddCylinder(Vector3.zero, 0.15f, 0.1f, 0.16f, 10, Shade(metal, -0.3f));
            b.AddCylinder(new Vector3(0f, 0.16f, 0f), 0.08f, 0.07f, 3.1f, 8, metal);
            b.AddBox(new Vector3(0f, 3.6f, 0f), new Vector3(0.34f, 0.84f, 0.3f), Shade(metal, 0.15f), 0.04f);
            b.AddSphere(new Vector3(0f, 3.88f, 0.16f), 0.08f, 8, 4, new Color(0.85f, 0.25f, 0.25f));
            b.AddSphere(new Vector3(0f, 3.6f, 0.16f), 0.08f, 8, 4, t.sunsetAmber);
            b.AddSphere(new Vector3(0f, 3.32f, 0.16f), 0.08f, 8, 4, t.mint);
        }

        static void BuildTree(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var bark = new Color(0.36f, 0.27f, 0.22f);
            b.AddCylinder(Vector3.zero, 0.22f, 0.15f, 1.5f, 8, bark);
            b.AddSphere(new Vector3(0f, 2.1f, 0f), 0.78f, 9, 6, t.foliage);
            b.AddSphere(new Vector3(0.42f, 1.75f, 0.2f), 0.5f, 8, 5, Shade(t.foliage, 0.18f));
            b.AddSphere(new Vector3(-0.35f, 1.9f, -0.25f), 0.45f, 8, 5, Shade(t.foliage, -0.15f));
        }

        static void BuildDumpster(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var green = new Color(0.24f, 0.42f, 0.32f);
            b.AddBox(new Vector3(0f, 0.55f, 0f), new Vector3(1.9f, 1.0f, 1.05f), green, a.meshBevel * 2f);
            b.AddBox(new Vector3(0f, 1.08f, -0.26f), new Vector3(1.94f, 0.08f, 0.55f), Shade(green, 0.25f), 0.02f);
            b.AddBox(new Vector3(0f, 1.12f, 0.28f), new Vector3(1.94f, 0.08f, 0.55f), Shade(green, 0.35f), 0.02f);
            for (int i = -1; i <= 1; i += 2)
            {
                b.AddCylinder(new Vector3(i * 0.78f, 0f, 0.42f), 0.1f, 0.1f, 0.05f, 8, new Color(0.15f, 0.15f, 0.18f));
                b.AddCylinder(new Vector3(i * 0.78f, 0f, -0.42f), 0.1f, 0.1f, 0.05f, 8, new Color(0.15f, 0.15f, 0.18f));
            }
        }

        static void BuildPhoneBooth(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var frame = t.electricViolet;
            b.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(1.0f, 0.12f, 1.0f), Shade(frame, -0.5f), 0.02f);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    b.AddBox(new Vector3(x * 0.44f, 1.2f, z * 0.44f), new Vector3(0.1f, 2.2f, 0.1f), frame);
            b.AddBox(new Vector3(0f, 2.36f, 0f), new Vector3(1.06f, 0.18f, 1.06f), Shade(frame, 0.3f), 0.03f);
            b.AddBox(new Vector3(0f, 2.5f, 0f), new Vector3(0.7f, 0.12f, 0.7f), t.neonCyan);
            b.AddBox(new Vector3(0f, 1.2f, -0.42f), new Vector3(0.78f, 2.0f, 0.04f), new Color(0.6f, 0.82f, 0.9f, 1f));
        }

        static void BuildBusStop(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var frame = new Color(0.26f, 0.28f, 0.34f);
            for (int x = -1; x <= 1; x += 2)
                b.AddBox(new Vector3(x * 1.4f, 1.15f, -0.5f), new Vector3(0.12f, 2.3f, 0.12f), frame);
            b.AddBox(new Vector3(0f, 2.36f, 0f), new Vector3(3.1f, 0.12f, 1.3f), Shade(frame, 0.3f), 0.03f);
            b.AddBox(new Vector3(0f, 1.2f, -0.56f), new Vector3(2.9f, 2.1f, 0.05f), new Color(0.62f, 0.78f, 0.88f));
            b.AddBox(new Vector3(0f, 0.55f, 0.2f), new Vector3(2.4f, 0.1f, 0.45f), t.shelfWood, 0.02f);
            b.AddBox(new Vector3(1.28f, 1.35f, 0.1f), new Vector3(0.7f, 1.4f, 0.08f), t.hotMagenta);
        }

        // ============================================================== vehicles

        static void BuildCar(MeshBuilder b, ThemeConfig t, ArtConfig a, Color paint, float length)
        {
            float halfLength = length * 0.5f;
            var glass = new Color(0.45f, 0.62f, 0.75f);
            var tyre = new Color(0.11f, 0.11f, 0.14f);

            b.AddBox(new Vector3(0f, 0.55f, 0f), new Vector3(1.78f, 0.62f, length), paint, a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.02f, -0.12f), new Vector3(1.6f, 0.52f, length * 0.52f), Shade(paint, 0.12f), a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.05f, halfLength * 0.52f - 0.12f), new Vector3(1.5f, 0.4f, 0.06f), glass);
            b.AddBox(new Vector3(0f, 1.05f, -halfLength * 0.52f - 0.1f), new Vector3(1.5f, 0.4f, 0.06f), glass);

            b.AddBox(new Vector3(0f, 0.62f, halfLength - 0.04f), new Vector3(1.5f, 0.18f, 0.08f), t.cream);
            b.AddBox(new Vector3(0f, 0.62f, -halfLength + 0.04f), new Vector3(1.5f, 0.16f, 0.08f), new Color(0.8f, 0.2f, 0.2f));

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    b.Push(new Vector3(x * 0.86f, 0.32f, z * (halfLength - 0.85f)), Quaternion.Euler(0f, 0f, 90f));
                    b.AddCylinder(Vector3.zero, 0.32f, 0.32f, 0.2f, 10, tyre, true, Shade(tyre, 1.6f));
                    b.Pop();
                }
            }
        }

        static void BuildVan(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var paint = new Color(0.92f, 0.93f, 0.95f);
            var tyre = new Color(0.11f, 0.11f, 0.14f);
            b.AddBox(new Vector3(0f, 1.1f, -0.3f), new Vector3(2.0f, 1.7f, 3.4f), paint, a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 0.85f, 1.9f), new Vector3(1.95f, 1.2f, 1.4f), Shade(paint, -0.08f), a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.15f, 2.58f), new Vector3(1.75f, 0.6f, 0.06f), new Color(0.45f, 0.62f, 0.75f));
            b.AddBox(new Vector3(0f, 1.3f, -2.0f), new Vector3(1.85f, 1.2f, 0.06f), Shade(paint, -0.2f));
            b.AddBox(new Vector3(1.01f, 1.2f, -0.3f), new Vector3(0.03f, 0.9f, 2.4f), t.electricViolet);
            b.AddBox(new Vector3(-1.01f, 1.2f, -0.3f), new Vector3(0.03f, 0.9f, 2.4f), t.electricViolet);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = 0; z < 2; z++)
                {
                    b.Push(new Vector3(x * 0.95f, 0.36f, z == 0 ? 1.6f : -1.4f), Quaternion.Euler(0f, 0f, 90f));
                    b.AddCylinder(Vector3.zero, 0.36f, 0.36f, 0.22f, 10, tyre, true, Shade(tyre, 1.6f));
                    b.Pop();
                }
            }
        }

        static void BuildBus(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var paint = t.sunsetAmber;
            var glass = new Color(0.4f, 0.58f, 0.72f);
            var tyre = new Color(0.1f, 0.1f, 0.13f);

            b.AddBox(new Vector3(0f, 1.55f, 0f), new Vector3(2.5f, 2.3f, 9f), paint, a.meshBevel * 4f);
            b.AddBox(new Vector3(0f, 2.75f, 0f), new Vector3(2.35f, 0.2f, 8.6f), Shade(paint, -0.2f), 0.04f);
            for (int i = 0; i < 6; i++)
            {
                float z = -3.4f + i * 1.36f;
                b.AddBox(new Vector3(1.26f, 1.95f, z), new Vector3(0.05f, 0.85f, 1.0f), glass);
                b.AddBox(new Vector3(-1.26f, 1.95f, z), new Vector3(0.05f, 0.85f, 1.0f), glass);
            }
            b.AddBox(new Vector3(0f, 1.95f, 4.51f), new Vector3(2.2f, 1.0f, 0.05f), glass);
            b.AddBox(new Vector3(0f, 1.95f, -4.51f), new Vector3(2.2f, 1.0f, 0.05f), glass);
            b.AddBox(new Vector3(0f, 2.62f, 4.45f), new Vector3(1.5f, 0.3f, 0.1f), t.voidIndigo);

            for (int x = -1; x <= 1; x += 2)
            {
                foreach (float z in new[] { 3.1f, -2.2f, -3.4f })
                {
                    b.Push(new Vector3(x * 1.2f, 0.45f, z), Quaternion.Euler(0f, 0f, 90f));
                    b.AddCylinder(Vector3.zero, 0.45f, 0.45f, 0.26f, 12, tyre, true, Shade(tyre, 1.5f));
                    b.Pop();
                }
            }
        }

        static void BuildTruck(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var cab = t.electricViolet;
            var box = new Color(0.86f, 0.88f, 0.92f);
            var tyre = new Color(0.1f, 0.1f, 0.13f);

            b.AddBox(new Vector3(0f, 1.35f, 2.4f), new Vector3(2.3f, 1.9f, 2.2f), cab, a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.85f, 3.45f), new Vector3(2.0f, 0.8f, 0.08f), new Color(0.45f, 0.62f, 0.75f));
            b.AddBox(new Vector3(0f, 1.8f, -1.3f), new Vector3(2.45f, 2.6f, 5.2f), box, a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.8f, -3.92f), new Vector3(2.2f, 2.3f, 0.06f), Shade(box, -0.18f));
            b.AddBox(new Vector3(1.24f, 1.9f, -1.3f), new Vector3(0.03f, 1.0f, 4.2f), t.hotMagenta);

            for (int x = -1; x <= 1; x += 2)
            {
                foreach (float z in new[] { 2.5f, -1.4f, -2.7f })
                {
                    b.Push(new Vector3(x * 1.12f, 0.46f, z), Quaternion.Euler(0f, 0f, 90f));
                    b.AddCylinder(Vector3.zero, 0.46f, 0.46f, 0.28f, 12, tyre, true, Shade(tyre, 1.5f));
                    b.Pop();
                }
            }
        }

        // ============================================================ structures

        static void BuildBuilding(MeshBuilder b, ThemeConfig t, ArtConfig a, int variant)
        {
            Color wall = variant == 0 ? t.concreteA : variant == 1 ? t.concreteB : t.concreteC;
            Color trim = Shade(wall, -0.3f);
            var window = new Color(0.36f, 0.48f, 0.62f);
            var lit = t.sunsetAmber;

            float width = variant == 2 ? 5.2f : 7.2f;
            float depth = variant == 2 ? 5.2f : 6.4f;
            float height = variant == 0 ? 9f : variant == 1 ? 12f : 16f;

            b.AddBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), wall, a.meshBevel);
            b.AddBox(new Vector3(0f, height + 0.22f, 0f), new Vector3(width + 0.45f, 0.44f, depth + 0.45f), trim, 0.05f);

            if (variant == 1)
            {
                b.AddBox(new Vector3(0f, height + 2.2f, 0f), new Vector3(width * 0.62f, 4f, depth * 0.62f), Shade(wall, 0.08f), a.meshBevel);
                b.AddBox(new Vector3(0f, height + 4.3f, 0f), new Vector3(width * 0.66f, 0.3f, depth * 0.66f), trim, 0.04f);
            }
            if (variant == 2)
            {
                b.AddCylinder(new Vector3(0f, height + 0.4f, 0f), 0.16f, 0.06f, 3.2f, 6, trim);
                b.AddSphere(new Vector3(0f, height + 3.7f, 0f), 0.26f, 8, 5, t.hotMagenta);
            }

            int floors = Mathf.FloorToInt(height / 2.4f);
            for (int floor = 0; floor < floors; floor++)
            {
                float y = 1.6f + floor * 2.4f;
                if (y > height - 0.9f) break;
                int columns = Mathf.Max(2, Mathf.FloorToInt(width / 1.9f));
                for (int c = 0; c < columns; c++)
                {
                    float x = -width * 0.5f + width * (c + 0.5f) / columns;
                    Color glass = ((floor * 7 + c * 3) % 5 == 0) ? lit : window;
                    b.AddBox(new Vector3(x, y, depth * 0.5f + 0.02f), new Vector3(0.92f, 1.24f, 0.06f), glass);
                    b.AddBox(new Vector3(x, y, -depth * 0.5f - 0.02f), new Vector3(0.92f, 1.24f, 0.06f), window);
                }
                int sideColumns = Mathf.Max(2, Mathf.FloorToInt(depth / 1.9f));
                for (int c = 0; c < sideColumns; c++)
                {
                    float z = -depth * 0.5f + depth * (c + 0.5f) / sideColumns;
                    Color glass = ((floor * 5 + c * 2) % 4 == 0) ? lit : window;
                    b.AddBox(new Vector3(width * 0.5f + 0.02f, y, z), new Vector3(0.06f, 1.24f, 0.92f), glass);
                    b.AddBox(new Vector3(-width * 0.5f - 0.02f, y, z), new Vector3(0.06f, 1.24f, 0.92f), window);
                }
            }

            // Ground floor shopfront keeps the street readable.
            b.AddBox(new Vector3(0f, 0.95f, depth * 0.5f + 0.04f), new Vector3(width * 0.8f, 1.7f, 0.08f), new Color(0.3f, 0.42f, 0.55f));
            b.AddBox(new Vector3(0f, 2.05f, depth * 0.5f + 0.1f), new Vector3(width * 0.55f, 0.36f, 0.12f), variant == 0 ? t.neonCyan : variant == 1 ? t.hotMagenta : t.mint);
        }

        static void BuildKiosk(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = t.hotMagenta * 0.85f;
            body.a = 1f;
            b.AddBox(new Vector3(0f, 1.05f, 0f), new Vector3(2.6f, 2.1f, 2.2f), body, a.meshBevel * 2f);
            b.AddBox(new Vector3(0f, 2.22f, 0f), new Vector3(3.0f, 0.24f, 2.6f), Shade(body, -0.35f), 0.05f);
            b.AddBox(new Vector3(0f, 1.35f, 1.12f), new Vector3(1.9f, 0.9f, 0.06f), new Color(0.55f, 0.72f, 0.85f));
            b.AddBox(new Vector3(0f, 0.75f, 1.2f), new Vector3(2.1f, 0.1f, 0.4f), t.cream);
            b.AddBox(new Vector3(0f, 2.62f, 0.6f), new Vector3(1.6f, 0.5f, 0.1f), t.neonCyan);
        }

        static void BuildWaterTower(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.55f, 0.57f, 0.60f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.0f;
                b.Push(offset, Quaternion.Euler(Mathf.Cos(angle) * 6f, 0f, -Mathf.Sin(angle) * 6f));
                b.AddCylinder(Vector3.zero, 0.12f, 0.1f, 3.4f, 6, Shade(metal, -0.25f));
                b.Pop();
            }
            b.AddCylinder(new Vector3(0f, 3.4f, 0f), 1.5f, 1.5f, 2.2f, 14, metal, true, Shade(metal, 0.15f));
            b.AddCylinder(new Vector3(0f, 5.6f, 0f), 1.5f, 0.2f, 0.9f, 14, Shade(metal, -0.18f));
            b.AddBox(new Vector3(0f, 4.4f, 1.52f), new Vector3(1.7f, 0.6f, 0.06f), t.neonCyan);
        }

        // ============================================================ characters

        static void BuildStickman(MeshBuilder b, ThemeConfig t, ArtConfig a, Color skin, Color shirt)
        {
            var trousers = new Color(0.22f, 0.24f, 0.34f);
            b.AddCylinder(new Vector3(-0.09f, 0f, 0f), 0.07f, 0.06f, 0.42f, 6, trousers);
            b.AddCylinder(new Vector3(0.09f, 0f, 0f), 0.07f, 0.06f, 0.42f, 6, trousers);
            b.AddBox(new Vector3(-0.09f, 0.03f, 0.05f), new Vector3(0.14f, 0.06f, 0.22f), Shade(trousers, -0.4f), 0.02f);
            b.AddBox(new Vector3(0.09f, 0.03f, 0.05f), new Vector3(0.14f, 0.06f, 0.22f), Shade(trousers, -0.4f), 0.02f);

            b.AddBox(new Vector3(0f, 0.68f, 0f), new Vector3(0.36f, 0.54f, 0.22f), shirt, a.meshBevel);
            b.AddCylinder(new Vector3(-0.22f, 0.5f, 0f), 0.05f, 0.05f, 0.42f, 6, shirt);
            b.AddCylinder(new Vector3(0.22f, 0.5f, 0f), 0.05f, 0.05f, 0.42f, 6, shirt);
            b.AddSphere(new Vector3(-0.22f, 0.5f, 0f), 0.055f, 6, 4, skin);
            b.AddSphere(new Vector3(0.22f, 0.5f, 0f), 0.055f, 6, 4, skin);

            b.AddSphere(new Vector3(0f, 1.08f, 0f), 0.19f, 10, 7, skin);
            b.AddSphere(new Vector3(-0.07f, 1.11f, 0.16f), 0.028f, 5, 3, new Color(0.09f, 0.08f, 0.12f));
            b.AddSphere(new Vector3(0.07f, 1.11f, 0.16f), 0.028f, 5, 3, new Color(0.09f, 0.08f, 0.12f));
            b.AddBox(new Vector3(0f, 1.22f, 0f), new Vector3(0.4f, 0.05f, 0.4f), Shade(shirt, -0.3f), 0.02f);
            b.AddCylinder(new Vector3(0f, 1.24f, 0f), 0.17f, 0.15f, 0.13f, 8, Shade(shirt, -0.3f));
        }

        static void BuildRobot(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var shell = t.paper;
            var accent = t.neonCyan;
            b.AddBox(new Vector3(0f, 0.52f, 0f), new Vector3(0.5f, 0.6f, 0.42f), shell, a.meshBevel * 2f);
            b.AddBox(new Vector3(0f, 0.52f, 0.22f), new Vector3(0.3f, 0.2f, 0.03f), accent);
            b.AddSphere(new Vector3(0f, 0.98f, 0f), 0.22f, 10, 7, shell);
            b.AddBox(new Vector3(0f, 1.0f, 0.18f), new Vector3(0.26f, 0.12f, 0.05f), t.voidInk);
            b.AddSphere(new Vector3(-0.06f, 1.0f, 0.2f), 0.035f, 6, 4, accent);
            b.AddSphere(new Vector3(0.06f, 1.0f, 0.2f), 0.035f, 6, 4, accent);
            b.AddCylinder(new Vector3(0f, 1.2f, 0f), 0.02f, 0.02f, 0.14f, 5, Shade(shell, -0.4f));
            b.AddSphere(new Vector3(0f, 1.36f, 0f), 0.05f, 6, 4, t.hotMagenta);
            b.AddSphere(new Vector3(-0.32f, 0.6f, 0f), 0.11f, 8, 5, accent);
            b.AddSphere(new Vector3(0.32f, 0.6f, 0f), 0.11f, 8, 5, accent);
            b.AddCylinder(new Vector3(0f, 0.08f, 0f), 0.26f, 0.2f, 0.14f, 10, Shade(shell, -0.25f));
            b.AddRing(new Vector3(0f, 0.04f, 0f), 0.08f, 0.24f, 14, accent, new Color(accent.r, accent.g, accent.b, 0f));
        }

        // ============================================================ storefront

        static void BuildHopper(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = t.machineBody;
            var accent = t.machineAccent;
            b.AddBox(new Vector3(0f, 0.12f, 0f), new Vector3(2.9f, 0.24f, 2.9f), Shade(body, -0.4f), 0.04f);
            b.AddCylinder(new Vector3(0f, 0.24f, 0f), 0.55f, 1.5f, 1.7f, 12, body, false);
            b.AddCylinder(new Vector3(0f, 1.94f, 0f), 1.5f, 1.55f, 0.18f, 12, accent, false);
            b.AddRing(new Vector3(0f, 2.12f, 0f), 1.2f, 1.56f, 12, Shade(accent, -0.25f), accent);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                b.AddBox(new Vector3(Mathf.Cos(angle) * 1.25f, 1.0f, Mathf.Sin(angle) * 1.25f), new Vector3(0.12f, 1.9f, 0.12f), Shade(body, -0.35f));
            }
            b.AddBox(new Vector3(0f, 0.5f, 1.02f), new Vector3(0.7f, 0.3f, 0.06f), t.neonCyan);
        }

        static void BuildMachine(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = t.machineBody;
            var accent = t.machineAccent;
            b.AddBox(new Vector3(0f, 0.1f, 0f), new Vector3(2.5f, 0.2f, 1.8f), Shade(body, -0.45f), 0.03f);
            b.AddBox(new Vector3(0f, 1.0f, 0f), new Vector3(2.1f, 1.6f, 1.5f), body, a.meshBevel * 3f);
            b.AddBox(new Vector3(0f, 1.86f, 0f), new Vector3(2.2f, 0.16f, 1.6f), Shade(body, -0.25f), 0.03f);
            b.AddBox(new Vector3(0f, 1.15f, 0.77f), new Vector3(1.2f, 0.72f, 0.06f), new Color(0.18f, 0.2f, 0.3f));
            b.AddBox(new Vector3(0f, 1.15f, 0.8f), new Vector3(1.05f, 0.58f, 0.03f), t.neonCyan * 0.8f);
            b.AddCylinder(new Vector3(-0.72f, 1.94f, 0f), 0.26f, 0.26f, 0.5f, 10, accent, true, Shade(accent, 0.2f));
            b.AddCylinder(new Vector3(0.72f, 1.94f, 0f), 0.2f, 0.2f, 0.34f, 10, Shade(accent, -0.2f));
            b.AddBox(new Vector3(0.95f, 0.55f, 0.6f), new Vector3(0.24f, 0.24f, 0.24f), t.coral, 0.04f);
            b.AddCylinder(new Vector3(0f, 2.02f, -0.5f), 0.16f, 0.2f, 0.7f, 8, Shade(body, -0.3f));
        }

        static void BuildShelf(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var frame = t.shelfWood;
            var metal = new Color(0.72f, 0.75f, 0.82f);
            b.AddBox(new Vector3(0f, 0.06f, 0f), new Vector3(2.4f, 0.12f, 1.0f), Shade(frame, -0.35f), 0.02f);
            for (int x = -1; x <= 1; x += 2)
                b.AddBox(new Vector3(x * 1.14f, 0.85f, 0f), new Vector3(0.12f, 1.7f, 0.95f), frame, 0.02f);
            for (int s = 0; s < 3; s++)
                b.AddBox(new Vector3(0f, 0.42f + s * 0.52f, 0f), new Vector3(2.3f, 0.08f, 0.92f), metal, 0.02f);
            b.AddBox(new Vector3(0f, 1.78f, -0.4f), new Vector3(2.3f, 0.26f, 0.08f), t.hotMagenta);
        }

        static void BuildCounter(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var body = t.cream;
            var top = t.electricViolet;
            b.AddBox(new Vector3(0f, 0.5f, 0f), new Vector3(2.6f, 1.0f, 1.0f), body, a.meshBevel * 2f);
            b.AddBox(new Vector3(0f, 1.03f, 0f), new Vector3(2.75f, 0.1f, 1.15f), top, 0.03f);
            b.AddBox(new Vector3(-0.7f, 1.26f, 0f), new Vector3(0.6f, 0.36f, 0.5f), new Color(0.24f, 0.26f, 0.34f), 0.04f);
            b.AddBox(new Vector3(-0.7f, 1.42f, 0.2f), new Vector3(0.42f, 0.22f, 0.04f), t.mint);
            b.AddBox(new Vector3(0.85f, 1.16f, 0f), new Vector3(0.5f, 0.16f, 0.36f), t.sunsetAmber, 0.03f);
            b.AddBox(new Vector3(0f, 0.55f, 0.52f), new Vector3(2.2f, 0.5f, 0.04f), t.neonCyan * 0.7f);
        }

        static void BuildConveyor(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var frame = new Color(0.4f, 0.43f, 0.5f);
            var belt = new Color(0.16f, 0.17f, 0.22f);
            b.AddBox(new Vector3(0f, 0.36f, 0f), new Vector3(3.2f, 0.12f, 0.9f), belt, 0.02f);
            b.AddBox(new Vector3(0f, 0.2f, 0f), new Vector3(3.0f, 0.2f, 0.7f), frame, 0.02f);
            for (int i = -1; i <= 1; i += 2)
            {
                b.Push(new Vector3(i * 1.6f, 0.3f, 0f), Quaternion.Euler(90f, 0f, 0f));
                b.AddCylinder(new Vector3(0f, -0.45f, 0f), 0.16f, 0.16f, 0.9f, 10, frame);
                b.Pop();
            }
            for (int i = 0; i < 5; i++)
                b.AddBox(new Vector3(-1.3f + i * 0.65f, 0.43f, 0f), new Vector3(0.08f, 0.02f, 0.86f), Shade(belt, 0.8f));
        }

        static void BuildPlant(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var pot = t.coral;
            b.AddCylinder(Vector3.zero, 0.3f, 0.36f, 0.42f, 10, pot, true, Shade(pot, -0.2f));
            b.AddSphere(new Vector3(0f, 0.78f, 0f), 0.38f, 8, 6, t.mint * 0.8f);
            b.AddSphere(new Vector3(0.22f, 0.62f, 0.14f), 0.24f, 7, 5, t.mint);
            b.AddSphere(new Vector3(-0.2f, 0.7f, -0.16f), 0.2f, 7, 5, Shade(t.mint, -0.2f));
        }

        static void BuildSign(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddCylinder(Vector3.zero, 0.12f, 0.09f, 1.6f, 8, new Color(0.3f, 0.32f, 0.4f));
            b.AddBox(new Vector3(0f, 2.0f, 0f), new Vector3(1.8f, 0.8f, 0.12f), t.voidIndigo, 0.04f);
            b.AddBox(new Vector3(0f, 2.0f, 0.08f), new Vector3(1.6f, 0.6f, 0.03f), t.neonCyan);
            b.AddRing(new Vector3(0f, 2.46f, 0f), 0.0f, 0.2f, 10, t.hotMagenta, t.hotMagenta);
        }

        static void BuildFloorLamp(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddCylinder(Vector3.zero, 0.26f, 0.2f, 0.08f, 10, new Color(0.3f, 0.32f, 0.4f));
            b.AddCylinder(new Vector3(0f, 0.08f, 0f), 0.05f, 0.04f, 1.6f, 6, new Color(0.35f, 0.37f, 0.45f));
            b.AddCylinder(new Vector3(0f, 1.68f, 0f), 0.16f, 0.34f, 0.46f, 10, t.cream, false);
            b.AddCylinder(new Vector3(0f, 1.7f, 0f), 0.14f, 0.3f, 0.06f, 10, t.sunsetAmber, false);
        }

        static void BuildZonePad(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddRing(new Vector3(0f, 0.02f, 0f), 0f, 0.46f, 28, new Color(t.neonCyan.r, t.neonCyan.g, t.neonCyan.b, 0.25f), new Color(t.neonCyan.r, t.neonCyan.g, t.neonCyan.b, 0.05f));
            b.AddRing(new Vector3(0f, 0.03f, 0f), 0.46f, 0.5f, 28, t.neonCyan, t.neonCyan);
        }

        static void BuildArrow(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddBox(new Vector3(0f, 0.04f, -0.12f), new Vector3(0.22f, 0.06f, 0.4f), t.mint, 0.02f);
            b.Push(new Vector3(0f, 0.04f, 0.26f), Quaternion.Euler(90f, 0f, 0f));
            b.AddCylinder(new Vector3(0f, -0.18f, 0f), 0.22f, 0f, 0.36f, 3, t.mint);
            b.Pop();
        }

        static void BuildWrench(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var metal = new Color(0.82f, 0.85f, 0.9f);
            b.AddBox(new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.62f, 0.09f), metal, 0.03f);
            b.AddCylinder(new Vector3(0f, 0.6f, 0f), 0.2f, 0.2f, 0.09f, 8, metal, false);
            b.AddBox(new Vector3(0f, 0.72f, 0f), new Vector3(0.16f, 0.2f, 0.12f), t.coral, 0.02f);
            b.AddCylinder(new Vector3(0f, 0.05f, 0f), 0.16f, 0.16f, 0.09f, 8, metal, false);
        }

        // ================================================================= goods

        static void BuildChair(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var wood = t.shelfWood;
            var seat = t.coral;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    b.AddBox(new Vector3(x * 0.16f, 0.14f, z * 0.16f), new Vector3(0.05f, 0.28f, 0.05f), wood);
            b.AddBox(new Vector3(0f, 0.31f, 0f), new Vector3(0.42f, 0.06f, 0.42f), seat, 0.02f);
            b.AddBox(new Vector3(0f, 0.52f, -0.18f), new Vector3(0.4f, 0.36f, 0.05f), seat, 0.02f);
        }

        static void BuildToy(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddSphere(new Vector3(0f, 0.2f, 0f), 0.2f, 10, 7, t.hotMagenta);
            b.AddSphere(new Vector3(0f, 0.44f, 0f), 0.13f, 9, 6, t.sunsetAmber);
            b.AddSphere(new Vector3(-0.1f, 0.5f, 0f), 0.06f, 6, 4, t.neonCyan);
            b.AddSphere(new Vector3(0.1f, 0.5f, 0f), 0.06f, 6, 4, t.neonCyan);
        }

        static void BuildTv(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddBox(new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.06f, 0.18f), new Color(0.2f, 0.22f, 0.3f), 0.02f);
            b.AddCylinder(new Vector3(0f, 0.08f, 0f), 0.04f, 0.04f, 0.1f, 6, new Color(0.25f, 0.27f, 0.35f));
            b.AddBox(new Vector3(0f, 0.36f, 0f), new Vector3(0.66f, 0.4f, 0.06f), new Color(0.16f, 0.17f, 0.24f), 0.02f);
            b.AddBox(new Vector3(0f, 0.36f, 0.035f), new Vector3(0.6f, 0.34f, 0.01f), t.neonCyan * 0.85f);
        }

        static void BuildLampProduct(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddCylinder(Vector3.zero, 0.14f, 0.12f, 0.04f, 10, new Color(0.3f, 0.32f, 0.4f));
            b.AddCylinder(new Vector3(0f, 0.04f, 0f), 0.03f, 0.025f, 0.32f, 6, new Color(0.4f, 0.42f, 0.5f));
            b.AddCylinder(new Vector3(0f, 0.36f, 0f), 0.09f, 0.18f, 0.22f, 10, t.cream, false);
        }

        static void BuildProductBox(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            var card = new Color(0.85f, 0.68f, 0.45f);
            b.AddBox(new Vector3(0f, 0.18f, 0f), new Vector3(0.36f, 0.36f, 0.36f), card, a.meshBevel);
            b.AddBox(new Vector3(0f, 0.365f, 0f), new Vector3(0.37f, 0.02f, 0.08f), Shade(card, -0.3f));
            b.AddBox(new Vector3(0f, 0.2f, 0.185f), new Vector3(0.18f, 0.12f, 0.01f), t.electricViolet);
        }

        static void BuildCashBill(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddBox(new Vector3(0f, 0.01f, 0f), new Vector3(0.34f, 0.02f, 0.17f), t.mint, 0.01f);
            b.AddBox(new Vector3(0f, 0.022f, 0f), new Vector3(0.12f, 0.005f, 0.09f), Shade(t.mint, -0.35f));
        }

        static void BuildJunkChunk(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddBox(new Vector3(0f, 0.1f, 0f), new Vector3(0.22f, 0.2f, 0.18f), new Color(0.45f, 0.47f, 0.55f), 0.03f);
            b.AddBox(new Vector3(0.1f, 0.2f, 0.06f), new Vector3(0.12f, 0.12f, 0.14f), new Color(0.55f, 0.5f, 0.42f), 0.02f);
            b.AddBox(new Vector3(-0.09f, 0.16f, -0.05f), new Vector3(0.1f, 0.14f, 0.1f), t.sunsetAmber * 0.7f, 0.02f);
        }

        static void BuildGem(MeshBuilder b, ThemeConfig t, ArtConfig a)
        {
            b.AddCylinder(new Vector3(0f, 0.12f, 0f), 0.16f, 0f, 0.2f, 6, t.neonCyan);
            b.Push(Vector3.zero, Quaternion.Euler(180f, 0f, 0f));
            b.AddCylinder(new Vector3(0f, -0.12f, 0f), 0.16f, 0f, 0.14f, 6, Shade(t.neonCyan, -0.25f));
            b.Pop();
        }
    }
}
