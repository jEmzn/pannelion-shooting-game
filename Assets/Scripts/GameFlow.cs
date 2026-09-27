using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownShooter
{
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        public bool IsLocked { get; private set; }
        public int StageNumber { get; private set; }
        public StageController Stage { get; private set; }

        private GameUI ui;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ui = gameObject.AddComponent<GameUI>();
            StageNumber = 0;
        }

        private void Start() => ui.ShowStart();

        public void BindStage(StageController stage)
        {
            Stage = stage;
            StageNumber = stage.StageNumber;
            ui.ShowGameplay();
        }

        public void StartRun()
        {
            if (IsLocked) return;
            StartCoroutine(LoadStage(1));
        }

        public void AdvanceStage()
        {
            if (IsLocked || Stage == null || Stage.Remaining > 0) return;
            if (StageNumber < 3) StartCoroutine(LoadStage(StageNumber + 1));
            else StartCoroutine(ShowVictory());
        }

        public void UsePortal() => Stage?.Portal.TryUse();

        public void ShowDeath()
        {
            if (IsLocked) return;
            IsLocked = true;
            ui.ShowDeath();
        }

        public void RetryStage()
        {
            if (StageNumber < 1 || StageNumber > 3) return;
            StartCoroutine(LoadStage(StageNumber));
        }

        public void Replay() => StartCoroutine(LoadStage(1));

        private IEnumerator LoadStage(int next)
        {
            IsLocked = true;
            ui.SetInteractVisible(false);
            yield return Fade(1f, 0.22f);
            ui.HidePanels();
            Stage = null;
            StageNumber = next;

            var load = SceneManager.LoadSceneAsync("Stage" + next);
            while (!load.isDone) yield return null;

            yield return Fade(0f, 0.22f);
            IsLocked = false;
        }

        private IEnumerator ShowVictory()
        {
            IsLocked = true;
            ui.SetInteractVisible(false);
            yield return Fade(1f, 0.22f);
            ui.ShowVictory();
            yield return Fade(0f, 0.22f);
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = ui.FadeAlpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ui.FadeAlpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            ui.FadeAlpha = target;
        }
    }
}
