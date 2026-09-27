using System;
using System.Linq;
using TopDownShooter;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TopDownShooter.Editor
{
    public static class DemoSmokeTest
    {
        private static int phase;
        private static double deadline;

        [MenuItem("Top Down Shooter/Run Play Mode Smoke Test")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Start")
            {
                UnityEngine.Debug.LogError("[SMOKE] Enter Play mode from the Start scene before running this test.");
                return;
            }
            phase = 0;
            deadline = EditorApplication.timeSinceStartup + 45;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            UnityEngine.Debug.Log("[SMOKE] Running stage, portal, retry, and twin-stick checks.");
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new Exception("Timed out in phase " + phase);
                var flow = GameFlow.Instance;
                if (flow == null) return;

                switch (phase)
                {
                    case 0:
                        flow.StartRun();
                        phase = 1;
                        break;
                    case 1:
                        if (flow.StageNumber != 1 || flow.Stage == null || flow.IsLocked) return;
                        Check(flow.Stage.Remaining == 4 && !flow.Stage.Portal.IsActive, "Stage 1 portal starts locked");
                        Check(!flow.Stage.Portal.TryUse(), "Locked portal rejects interaction");
                        CheckTwoSticks();
                        KillAllEnemies();
                        Check(flow.Stage.Remaining == 0 && flow.Stage.Portal.IsActive, "Stage 1 portal opens after all four enemies");
                        Check(!flow.Stage.Portal.TryUse(), "Portal rejects interaction from outside range");
                        MovePlayerToPortal();
                        phase = 2;
                        break;
                    case 2:
                        if (!flow.Stage.Portal.TryUse()) return;
                        Check(!flow.Stage.Portal.TryUse(), "Portal ignores a second interaction");
                        phase = 3;
                        break;
                    case 3:
                        if (flow.StageNumber != 2 || flow.Stage == null || flow.IsLocked) return;
                        Check(flow.Stage.Remaining == 7 && !flow.Stage.Portal.IsActive, "Stage 2 starts with seven targets");
                        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Check(player.Health.Current == 10, "Health refills between stages");
                        player.Health.TakeDamage(100);
                        Check(flow.IsLocked && GameUI.Instance.IsDeathVisible, "Defeat opens retry screen");
                        flow.RetryStage();
                        phase = 4;
                        break;
                    case 4:
                        if (flow.StageNumber != 2 || flow.Stage == null || flow.IsLocked || GameUI.Instance.IsDeathVisible) return;
                        Check(flow.Stage.Remaining == 7 && !flow.Stage.Portal.IsActive, "Retry resets stage 2 objectives");
                        Check(UnityEngine.Object.FindAnyObjectByType<PlayerController>().Health.Current == 10,
                            "Retry restores full health");
                        KillAllEnemies();
                        MovePlayerToPortal();
                        phase = 5;
                        break;
                    case 5:
                        if (!flow.Stage.Portal.TryUse()) return;
                        phase = 6;
                        break;
                    case 6:
                        if (flow.StageNumber != 3 || flow.Stage == null || flow.IsLocked) return;
                        Check(flow.Stage.Remaining == 1 && !flow.Stage.Portal.IsActive, "Boss stage starts locked");
                        Check(flow.Stage.BossHealth != null && flow.Stage.BossHealth.Maximum == 35,
                            "Boss has distinct health");
                        flow.Stage.BossHealth.TakeDamage(100);
                        Check(flow.Stage.Remaining == 0 && flow.Stage.Portal.IsActive,
                            "Boss defeat opens final portal");
                        MovePlayerToPortal();
                        phase = 7;
                        break;
                    case 7:
                        if (!flow.Stage.Portal.TryUse()) return;
                        phase = 8;
                        break;
                    case 8:
                        if (!GameUI.Instance.IsVictoryVisible) return;
                        Check(flow.IsLocked, "Victory screen ends gameplay");
                        EditorApplication.update -= Tick;
                        UnityEngine.Debug.Log("[SMOKE] PASS: three stages, portal gates, retry, health reset, final victory, and two simultaneous sticks.");
                        break;
                }
            }
            catch (Exception error)
            {
                EditorApplication.update -= Tick;
                UnityEngine.Debug.LogError("[SMOKE] FAIL: " + error);
            }
        }

        private static void KillAllEnemies()
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyBrain>())
                enemy.GetComponent<Health>().TakeDamage(100);
        }

        private static void MovePlayerToPortal()
        {
            var stage = GameFlow.Instance.Stage;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            player.GetComponent<Rigidbody2D>().position = stage.Portal.transform.position;
            player.transform.position = stage.Portal.transform.position;
        }

        private static void CheckTwoSticks()
        {
            var sticks = UnityEngine.Object.FindObjectsByType<VirtualStick>();
            Check(sticks.Length == 2, "Two virtual sticks exist");
            var move = sticks.Single(stick => stick.name == "Movement Stick");
            var aim = sticks.Single(stick => stick.name == "Aim Stick");
            var moveRect = (RectTransform)move.transform;
            var aimRect = (RectTransform)aim.transform;
            var moveEvent = new PointerEventData(EventSystem.current)
            {
                pointerId = 101,
                position = RectTransformUtility.WorldToScreenPoint(null, moveRect.TransformPoint(new Vector3(60, 0, 0)))
            };
            var aimEvent = new PointerEventData(EventSystem.current)
            {
                pointerId = 202,
                position = RectTransformUtility.WorldToScreenPoint(null, aimRect.TransformPoint(new Vector3(0, 60, 0)))
            };
            move.OnPointerDown(moveEvent);
            aim.OnPointerDown(aimEvent);
            Check(move.Held && aim.Held && move.Value.sqrMagnitude > 0.1f && aim.Value.sqrMagnitude > 0.1f,
                "Both touch sticks accept separate pointers at once");
            move.OnPointerUp(moveEvent);
            aim.OnPointerUp(aimEvent);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
