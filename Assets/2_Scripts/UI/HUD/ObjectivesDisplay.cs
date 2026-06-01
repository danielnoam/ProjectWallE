using System.Collections.Generic;
using System.Text;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class ObjectivesDisplay : MonoBehaviour
    {
        [Header("Objectives")]
        [SerializeField] private GameObject objectivesHolder;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private TMPWriter objectivesWriter;
        [SerializeField] private SOFontStyle objectiveStatusFontStyle;
        [SerializeField] private SOFontStyle objectiveCompletedFontStyle;

        [Header("Tutorial")]
        [SerializeField] private GameObject tutorialHolder;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private TMPWriter tutorialWriter;
        [SerializeField] private SOFontStyle tutorialActionFontStyle;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField] private Ease animationEase = Ease.InOutSine;

        private RectTransform _objectivesRect;
        private RectTransform _tutorialRect;
        private Tween _objectivesTween;
        private Tween _tutorialTween;

        private IReadOnlyList<BaseLevelObjective> _activeObjectives;
        private readonly StringBuilder _objectiveBuilder = new();
        private readonly StringBuilder _tutorialBuilder = new();

        private void Awake()
        {
            if (objectivesHolder)
            {
                _objectivesRect = objectivesHolder.GetComponent<RectTransform>();
                var csf = objectivesHolder.GetComponent<ContentSizeFitter>();
                if (csf) csf.enabled = false;
                if (_objectivesRect) _objectivesRect.sizeDelta = new Vector2(_objectivesRect.sizeDelta.x, 0f);
            }

            if (tutorialHolder)
            {
                _tutorialRect = tutorialHolder.GetComponent<RectTransform>();
                var csf = tutorialHolder.GetComponent<ContentSizeFitter>();
                if (csf) csf.enabled = false;
                if (_tutorialRect) _tutorialRect.sizeDelta = new Vector2(_tutorialRect.sizeDelta.x, 0f);
            }
            
            if (objectivesWriter) objectivesWriter.OnFinishWriter.AddListener(_ => objectivesWriter.enabled = false);
            if (tutorialWriter) tutorialWriter.OnFinishWriter.AddListener(_ => tutorialWriter.enabled = false);
        }

        private void OnEnable()
        {
            LevelManager.OnLevelInitializing += OnLevelInitializing;
            LevelManager.OnLeveTimeLineUpdated += OnLeveTimeLineUpdated;
            LevelManager.OnLevelCompleted += OnLevelCompleted;
            LevelManager.OnLevelFailed += OnLevelFailed;
            LevelManager.OnObjectivesAdded += OnObjectivesAdded;
            LevelManager.OnObjectivesCompleted += OnObjectivesCompleted;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelInitializing -= OnLevelInitializing;
            LevelManager.OnLeveTimeLineUpdated -= OnLeveTimeLineUpdated;
            LevelManager.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.OnLevelFailed -= OnLevelFailed;
            LevelManager.OnObjectivesAdded -= OnObjectivesAdded;
            LevelManager.OnObjectivesCompleted -= OnObjectivesCompleted;
        }

        private void OnDestroy()
        {
            if (_objectivesTween.isAlive) _objectivesTween.Stop();
            if (_tutorialTween.isAlive) _tutorialTween.Stop();
        }

        private void OnLevelInitializing()
        {
            _activeObjectives = null;
            HidePanels();
        }

        private void OnLevelFailed()
        {
            _activeObjectives = null;
            HidePanels();
        }

        private void OnLevelCompleted()
        {
            _activeObjectives = null;
            HidePanels();
        }

        private void OnObjectivesAdded(List<BaseLevelObjective> objectives)
        {
            _activeObjectives = objectives;
            UpdateObjectivesDisplay();
            if (objectivesWriter) objectivesWriter.enabled = true;

            BuildTutorialText();
            bool hasTutorial = _tutorialBuilder.Length > 0;
            if (hasTutorial && tutorialText) tutorialText.text = _tutorialBuilder.ToString();

            if (_objectivesTween.isAlive) _objectivesTween.Stop();
            _objectivesTween = AnimateHeight(_objectivesRect, GetContentHeight(_objectivesRect), showDuration);

            if (hasTutorial)
            {
                if (_objectivesTween.isAlive)
                    _objectivesTween.OnComplete(this, static self =>
                    {
                        self._tutorialTween = self.AnimateHeight(
                            self._tutorialRect, self.GetContentHeight(self._tutorialRect), self.showDuration);
                    });
                else
                {
                    if (_tutorialTween.isAlive) _tutorialTween.Stop();
                    _tutorialTween = AnimateHeight(_tutorialRect, GetContentHeight(_tutorialRect), showDuration);
                }
                
                if (tutorialWriter) tutorialWriter.enabled = true;
            }
        }

        private void OnObjectivesCompleted()
        {
            _activeObjectives = null;
            HidePanels();
        }

        private void OnLeveTimeLineUpdated(float timeRemaining)
        {
            if (_activeObjectives == null) return;
            UpdateObjectivesDisplay();
            UpdateTutorialDisplay();
        }

        private void HidePanels()
        {
            if (_objectivesTween.isAlive) _objectivesTween.Stop();
            if (_tutorialTween.isAlive) _tutorialTween.Stop();

            _tutorialTween = AnimateHeight(_tutorialRect, 0f, hideDuration);

            if (_tutorialTween.isAlive)
                _tutorialTween.OnComplete(this, static self =>
                {
                    self._objectivesTween = self.AnimateHeight(self._objectivesRect, 0f, self.hideDuration);
                });
            else
                _objectivesTween = AnimateHeight(_objectivesRect, 0f, hideDuration);
        }

        private void UpdateObjectivesDisplay()
        {
            if (objectivesWriter && objectivesWriter.enabled) return;
            
            _objectiveBuilder.Clear();

            foreach (var objective in _activeObjectives)
            {
                _objectiveBuilder.AppendLine(objective.IsCompleted
                    ? $"○ {(objectiveCompletedFontStyle ? objectiveCompletedFontStyle.ApplyStyle(objective.Description) : objective.Description)}"
                    : $"○ {objective.Description} - {(objectiveStatusFontStyle ? objectiveStatusFontStyle.ApplyStyle(objective.ProgressText) : objective.ProgressText)}");
            }

            if (objectivesText) objectivesText.text = _objectiveBuilder.ToString();

            float target = GetContentHeight(_objectivesRect);
            if (Mathf.Approximately(target, _objectivesRect ? _objectivesRect.sizeDelta.y : 0f)) return;

            if (_objectivesTween.isAlive) _objectivesTween.Stop();
            _objectivesTween = AnimateHeight(_objectivesRect, target, showDuration);
        }

        private void BuildTutorialText()
        {
            _tutorialBuilder.Clear();
            foreach (var objective in _activeObjectives)
            {
                if (objective.IsCompleted) continue;
                if (string.IsNullOrEmpty(objective.TutorialText)) continue;
                _tutorialBuilder.AppendLine(tutorialActionFontStyle
                    ? tutorialActionFontStyle.ApplyStyle(objective.TutorialText, '*')
                    : objective.TutorialText);
            }
        }

        private void UpdateTutorialDisplay()
        {
            if (tutorialWriter && tutorialWriter.enabled) return;
            
            BuildTutorialText();

            bool hasTutorial = _tutorialBuilder.Length > 0;
            if (hasTutorial && tutorialText) tutorialText.text = _tutorialBuilder.ToString();

            float target = hasTutorial ? GetContentHeight(_tutorialRect) : 0f;
            if (Mathf.Approximately(target, _tutorialRect ? _tutorialRect.sizeDelta.y : 0f)) return;

            if (_tutorialTween.isAlive) _tutorialTween.Stop();
            _tutorialTween = AnimateHeight(_tutorialRect, target, hasTutorial ? showDuration : hideDuration);
        }

        private float GetContentHeight(RectTransform rect)
        {
            if (!rect) return 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            return LayoutUtility.GetPreferredHeight(rect);
        }

        private Tween AnimateHeight(RectTransform rect, float target, float duration)
        {
            if (!rect) return default;

            float start = rect.sizeDelta.y;
            if (Mathf.Approximately(start, target)) return default;

            if (duration > 0) return Tween.Custom(start, target, duration, v => rect.sizeDelta = new Vector2(rect.sizeDelta.x, v), ease: animationEase, useUnscaledTime: true);

            rect.sizeDelta = new Vector2(rect.sizeDelta.x, target);
            return default;
        }
    }
}