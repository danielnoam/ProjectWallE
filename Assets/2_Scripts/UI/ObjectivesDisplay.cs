using System.Collections.Generic;
using System.Text;
using DNExtensions.Systems.Scriptables;
using ProjectWallE.GameLoop;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class ObjectivesDisplay : MonoBehaviour
    {
        [Header("Objectives")]
        [SerializeField] private GameObject objectivesHolder;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private SOFontStyle objectiveStatusFontStyle;
        [SerializeField] private SOFontStyle objectiveCompletedFontStyle;

        [Header("Tutorial")]
        [SerializeField] private GameObject tutorialHolder;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private SOFontStyle tutorialActionFontStyle;

        private IReadOnlyList<BaseLevelObjective> _activeObjectives;
        private readonly StringBuilder _objectiveBuilder = new();
        private readonly StringBuilder _tutorialBuilder = new();

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

        private void OnLevelInitializing()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
            tutorialHolder?.SetActive(false);
        }

        private void OnLevelFailed()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
            tutorialHolder?.SetActive(false);
        }

        private void OnLevelCompleted()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
            tutorialHolder?.SetActive(false);
        }

        private void OnObjectivesAdded(List<BaseLevelObjective> objectives)
        {
            _activeObjectives = objectives;
            objectivesHolder?.SetActive(true);
            UpdateTutorialDisplay();
        }

        private void OnObjectivesCompleted()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
            tutorialHolder?.SetActive(false);
        }

        private void OnLeveTimeLineUpdated(float timeRemaining)
        {
            if (_activeObjectives == null) return;
            UpdateObjectivesDisplay();
            UpdateTutorialDisplay();
        }

        private void UpdateObjectivesDisplay()
        {
            _objectiveBuilder.Clear();

            foreach (var objective in _activeObjectives)
            {
                _objectiveBuilder.AppendLine(objective.IsCompleted
                    ? $"○ {objectiveCompletedFontStyle.ApplyStyle(objective.Description)}"
                    : $"○ {objective.Description} - {objectiveStatusFontStyle.ApplyStyle(objective.ProgressText)}");
            }

            if (objectivesText) objectivesText.text = _objectiveBuilder.ToString();
        }

        private void UpdateTutorialDisplay()
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

            bool hasTutorial = _tutorialBuilder.Length > 0;
            tutorialHolder?.SetActive(hasTutorial);
            if (hasTutorial && tutorialText) tutorialText.text = _tutorialBuilder.ToString();
        }
    }
}