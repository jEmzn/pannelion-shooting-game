using System;
using System.IO;
using System.Linq;
using TopDownShooter;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TopDownShooter.Editor
{
    public static class DemoBuilder
    {
        private const string Shapes = "Assets/Resources/Shapes";
        private const string Scenes = "Assets/Scenes";

        private static Sprite circle;
        private static Sprite square;
        private static Sprite ring;
        private static Material spriteMaterial;

        [MenuItem("Top Down Shooter/Generate Demo")]
        public static void Build()
        {
            Directory.CreateDirectory(Shapes);
            Directory.CreateDirectory(Scenes);
            GenerateSprite("Circle", (x, y) => x * x + y * y <= 0.94f);
            GenerateSprite("Square", (x, y) => true);
            GenerateSprite("Ring", (x, y) =>
            {
                float radius = x * x + y * y;
                return radius <= 0.96f && radius >= 0.48f;
            });
            AssetDatabase.Refresh();
            circle = AssetDatabase.LoadAssetAtPath<Sprite>(Shapes + "/Circle.png");
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Shapes + "/Square.png");
            ring = AssetDatabase.LoadAssetAtPath<Sprite>(Shapes + "/Ring.png");
            if (circle == null || square == null || ring == null)
                throw new Exception("Shape sprites did not import as sprites.");

            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                const string materialPath = Shapes + "/Shape Unlit.mat";
                spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (spriteMaterial == null)
                {
                    spriteMaterial = new Material(shader);
                    AssetDatabase.CreateAsset(spriteMaterial, materialPath);
                }
            }

            CreateStartScene();
            CreateStage(1);
            CreateStage(2);
            CreateStage(3);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Scenes + "/Start.unity", true),
                new EditorBuildSettingsScene(Scenes + "/Stage1.unity", true),
                new EditorBuildSettingsScene(Scenes + "/Stage2.unity", true),
                new EditorBuildSettingsScene(Scenes + "/Stage3.unity", true)
            };
            PlayerSettings.companyName = "Codex Demo";
            PlayerSettings.productName = "Top Down Shooter Demo";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.codex.topdownshooterdemo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Scenes + "/Start.unity");
            UnityEngine.Debug.Log("Top Down Shooter Demo generated: Start, Stage1, Stage2, Stage3.");
        }

        public static void BuildAndroid()
        {
            var projectParent = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            var output = Path.Combine(projectParent, "Top Down Shooter Demo.apk");
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android build failed: " + report.summary.result);
            UnityEngine.Debug.Log("Android APK created: " + output);
        }

        private static void GenerateSprite(string name, Func<float, float, bool> opaque)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size / 2f) / (size / 2f);
                float ny = (y + 0.5f - size / 2f) / (size / 2f);
                texture.SetPixel(x, y, opaque(nx, ny) ? Color.white : Color.clear);
            }
            texture.Apply();
            var path = Shapes + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void CreateStartScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            MakeShape("Background", square, new Color(0.045f, 0.08f, 0.13f),
                Vector2.zero, new Vector2(34f, 19f), -20);
            var flow = new GameObject("Game Flow");
            flow.AddComponent<GameFlow>();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scenes + "/Start.unity");
        }

        private static void CreateStage(int number)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            MakeShape("Arena Floor", square, new Color(0.075f, 0.13f, 0.18f),
                Vector2.zero, new Vector2(34f, 19f), -20);
            for (int x = -12; x <= 12; x += 3)
                MakeShape("Floor Grid Vertical", square, new Color(0.10f, 0.18f, 0.23f, 0.6f),
                    new Vector2(x, 0), new Vector2(0.025f, 13f), -19);
            for (int y = -6; y <= 6; y += 3)
                MakeShape("Floor Grid Horizontal", square, new Color(0.10f, 0.18f, 0.23f, 0.6f),
                    new Vector2(0, y), new Vector2(25f, 0.025f), -19);

            Wall("North Wall", new Vector2(0, 6.25f), new Vector2(25.5f, 0.55f));
            Wall("South Wall", new Vector2(0, -6.25f), new Vector2(25.5f, 0.55f));
            Wall("West Wall", new Vector2(-12.5f, 0), new Vector2(0.55f, 13f));
            Wall("East Wall", new Vector2(12.5f, 0), new Vector2(0.55f, 13f));

            if (number == 2)
            {
                Wall("Left Cover", new Vector2(-4.5f, 0.6f), new Vector2(1.4f, 2.4f));
                Wall("Right Cover", new Vector2(4.5f, 0.6f), new Vector2(1.4f, 2.4f));
            }
            if (number == 3)
            {
                Wall("Left Pillar", new Vector2(-6.5f, 1), new Vector2(1.3f, 1.3f));
                Wall("Right Pillar", new Vector2(6.5f, 1), new Vector2(1.3f, 1.3f));
            }

            CreatePlayer(new Vector2(0, -3.8f));
            var portal = MakeShape("Exit Portal", ring, new Color(0.38f, 1f, 0.65f),
                new Vector2(0, 4.8f), new Vector2(1.75f, 1.75f), 5).AddComponent<PortalController>();

            Health[] objectives;
            if (number == 1)
            {
                objectives = new[]
                {
                    CreateEnemy(EnemyKind.Chaser, new Vector2(-7, 3)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(7, 3)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(-6, -1)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(6, -1))
                };
            }
            else if (number == 2)
            {
                objectives = new[]
                {
                    CreateEnemy(EnemyKind.Chaser, new Vector2(-9, 3)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(-7, -2)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(0, 2)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(7, -2)),
                    CreateEnemy(EnemyKind.Chaser, new Vector2(9, 3)),
                    CreateEnemy(EnemyKind.Ranged, new Vector2(-5, 4)),
                    CreateEnemy(EnemyKind.Ranged, new Vector2(5, 4))
                };
            }
            else objectives = new[] { CreateEnemy(EnemyKind.Boss, new Vector2(0, 2.5f)) };

            var stage = new GameObject("Stage Controller").AddComponent<StageController>();
            stage.Configure(number, objectives, portal);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                Scenes + "/Stage" + number + ".unity");
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.06f, 0.1f);
            var lightObject = new GameObject("Global 2D Light");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static GameObject MakeShape(string name, Sprite sprite, Color color, Vector2 position,
            Vector2 scale, int sortingOrder)
        {
            var shape = new GameObject(name);
            shape.transform.position = position;
            shape.transform.localScale = scale;
            var renderer = shape.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
            return shape;
        }

        private static void Wall(string name, Vector2 position, Vector2 size)
        {
            var wall = MakeShape(name, square, new Color(0.22f, 0.33f, 0.43f), position, size, -2);
            wall.AddComponent<BoxCollider2D>();
            wall.AddComponent<WallMarker>();
        }

        private static void CreatePlayer(Vector2 position)
        {
            var player = MakeShape("Player", circle, new Color(0.23f, 0.88f, 0.95f),
                position, new Vector2(0.92f, 0.92f), 3);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>();
            player.AddComponent<Health>().Configure(10, 0.55f);
            var barrel = MakeShape("Barrel", square, new Color(0.8f, 1f, 1f),
                Vector2.zero, new Vector2(0.18f, 0.7f), 4);
            barrel.transform.SetParent(player.transform, false);
            barrel.transform.localPosition = new Vector3(0, 0.48f, 0);
            player.AddComponent<PlayerController>().Configure(circle, barrel.transform);
        }

        private static Health CreateEnemy(EnemyKind kind, Vector2 position)
        {
            bool boss = kind == EnemyKind.Boss;
            var enemy = MakeShape(kind + " Enemy", kind == EnemyKind.Ranged ? square : circle,
                boss ? new Color(0.90f, 0.22f, 0.52f) :
                kind == EnemyKind.Ranged ? new Color(1f, 0.57f, 0.25f) : new Color(0.93f, 0.34f, 0.36f),
                position, boss ? new Vector2(1.9f, 1.9f) : new Vector2(0.86f, 0.86f), 3);
            var body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (kind == EnemyKind.Ranged) enemy.AddComponent<BoxCollider2D>();
            else enemy.AddComponent<CircleCollider2D>();
            var health = enemy.AddComponent<Health>();
            health.Configure(boss ? 35 : kind == EnemyKind.Ranged ? 4 : 3);
            enemy.AddComponent<EnemyBrain>().Configure(kind, circle, boss ? 1.3f : kind == EnemyKind.Ranged ? 1.6f : 2.1f);
            return health;
        }
    }
}
