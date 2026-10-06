using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModelUIManager : MonoBehaviour
{
    [Header("Models")]
    public GameObject[] models;

    [Header("Objects To Hide")]
    public GameObject HideObjectsParent;

    [Header("UI Buttons")]
    public Button nextButton;
    public Button previousButton;
    public Button hideObjects;

    [Header("UI Texts")]
    public TextMeshProUGUI hideObjectButtonText;

    private int currentIndex = 0;

    private void Start()
    {
        // Start with the first model
        ShowModel(0);
    }

    public void NextModel()
    {
        if (currentIndex >= models.Length - 1)
            return;

        currentIndex++;

        ShowModel(currentIndex);
    }

    public void PreviousModel()
    {
        if (currentIndex <= 0)
            return;

        currentIndex--;

        ShowModel(currentIndex);
    }

    // Called when Hide Objects button is clicked
    public void ToggleHideObjects()
    {
        if (HideObjectsParent == null)
            return;

        // Toggle GameObject
        bool isActive =
            !HideObjectsParent.activeSelf;

        HideObjectsParent.SetActive(isActive);

        // Update button text
        UpdateHideObjectsButton();
    }

    private void ShowModel(int index)
    {
        // Disable all models
        for (int i = 0; i < models.Length; i++)
        {
            models[i].SetActive(false);
        }

        // Enable selected model
        models[index].SetActive(true);

        // Update Next and Previous buttons
        UpdateButtons();

        // Update Hide Objects button
        UpdateHideObjectsButton();
    }

    private void UpdateButtons()
    {
        // Previous disabled at first model
        previousButton.interactable =
            currentIndex > 0;

        // Next disabled at last model
        nextButton.interactable =
            currentIndex < models.Length - 1;
    }

    private void UpdateHideObjectsButton()
    {
        // Hide Objects button is available
        // only for Model 0
        if (currentIndex == 0)
        {
            hideObjects.gameObject.SetActive(true);

            // Check if objects are currently visible
            if (HideObjectsParent.activeSelf)
            {
                hideObjectButtonText.SetText("Hide Objects");
            }
            else
            {
                hideObjectButtonText.SetText("Show Objects");
            }
        }
        else
        {
            // Hide Objects button is not available
            // for other models
            hideObjects.gameObject.SetActive(false);
        }
    }
}