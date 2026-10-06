using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModelViewerApp
{
    public class ModelViewerUI : MonoBehaviour
    {
        public static ModelViewerUI Instance { get; private set; }

        [Header("TMP Font")]
        public TMP_FontAsset fontAsset;

        [Header("UI Panels")]
        public RectTransform leftSidebar;
        public RectTransform rightSidebar;
        public RectTransform topBar;
        public RectTransform centerViewportUI;

        [Header("Model List")]
        public Transform modelListContainer;
        public TextMeshProUGUI modelListCountText;

        [Header("Model Details")]
        public Image detailThumbnail;
        public TextMeshProUGUI detailModelName;
        public TextMeshProUGUI detailSubtitle;
        public TextMeshProUGUI detailVertices;
        public TextMeshProUGUI detailTriangles;
        public TextMeshProUGUI detailDimensions;
        public TextMeshProUGUI detailMaterials;
        public TextMeshProUGUI detailTextures;

        [Header("Checklist")]
        public Transform checklistContainer;

        [Header("Status Card")]
        public Image statusCardBg;
        public Image statusCardIcon;
        public TextMeshProUGUI statusCardTitle;
        public TextMeshProUGUI statusCardSubtitle;

        [Header("Action Buttons")]
        public Button finalizeButton;
        public TextMeshProUGUI finalizeButtonText;
        public Image finalizeButtonIcon;
        public Button addCommentsButton;
        public Image addCommentsButtonIcon;

        [Header("Center Overlays")]
        public Button orbitModeBtn;
        public Button panModeBtn;
        public Button zoomModeBtn;
        public Button focusSelectedBtn;
        public Button fitModelBtn;
        public Button resetViewBtn;
        public TMP_Dropdown viewModeDropdown;
        public TextMeshProUGUI selectionTagText;
        public RawImage gizmoRawImage;
        public Image miniThumbnail;

        [Header("Bottom Bar Quick Mode")]
        public Button bottomOrbitBtn;
        public Button bottomPanBtn;
        public Button bottomZoomBtn;
        public Button bottomResetBtn;

        [Header("Modals")]
        public GameObject commentsModal;
        public TMP_InputField commentInput;
        public Transform commentsListContainer;
        public Button postCommentBtn;
        public Button closeCommentsBtn;

        public GameObject finalizeModal;
        public TextMeshProUGUI finalizeModalTitle;
        public TextMeshProUGUI finalizeModalMessage;
        public TMP_InputField reviewerNameInput;
        public Button confirmFinalizeBtn;
        public Button cancelFinalizeBtn;

        public GameObject uploadProgressModal;
        public Image uploadProgressBar;
        public TextMeshProUGUI uploadStatusText;

        // Internal UI card references
        private List<GameObject> modelCardObjects = new List<GameObject>();
        private List<GameObject> checklistItemObjects = new List<GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }

            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }

        private void Start()
        {
            if (ModelDataManager.Instance != null)
            {
                ModelDataManager.Instance.OnModelSelected += OnModelSelected;
                ModelDataManager.Instance.OnModelListChanged += RefreshModelList;
                ModelDataManager.Instance.OnModelDataUpdated += RefreshDetails;
            }

            if (ModelSelectionManager.Instance != null)
            {
                ModelSelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
            }

            SetupEventListeners();
            RefreshModelList();

            if (ModelDataManager.Instance != null && ModelDataManager.Instance.models.Count > 0)
            {
                ModelDataManager.Instance.SelectModel(0);
            }
        }

        private void SetupEventListeners()
        {
            if (orbitModeBtn != null) orbitModeBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Orbit));
            if (panModeBtn != null) panModeBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Pan));
            if (zoomModeBtn != null) zoomModeBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Zoom));
            if (focusSelectedBtn != null) focusSelectedBtn.onClick.AddListener(() => {
                if (ModelSelectionManager.Instance != null) ModelSelectionManager.Instance.FocusSelected();
            });
            if (fitModelBtn != null) fitModelBtn.onClick.AddListener(() => {
                var cur = ModelDataManager.Instance?.GetCurrentModel();
                if (cur != null && ModelViewerCamera.Instance != null) ModelViewerCamera.Instance.FitModel(cur.modelBounds);
            });
            if (resetViewBtn != null) resetViewBtn.onClick.AddListener(() => {
                if (ModelViewerCamera.Instance != null) ModelViewerCamera.Instance.ResetView();
            });

            if (bottomOrbitBtn != null) bottomOrbitBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Orbit));
            if (bottomPanBtn != null) bottomPanBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Pan));
            if (bottomZoomBtn != null) bottomZoomBtn.onClick.AddListener(() => SetMode(ViewerInteractionMode.Zoom));
            if (bottomResetBtn != null) bottomResetBtn.onClick.AddListener(() => {
                if (ModelViewerCamera.Instance != null) ModelViewerCamera.Instance.ResetView();
            });

            if (viewModeDropdown != null)
            {
                viewModeDropdown.onValueChanged.AddListener(OnViewModeDropdownChanged);
            }

            if (finalizeButton != null) finalizeButton.onClick.AddListener(OnFinalizeClicked);
            if (addCommentsButton != null) addCommentsButton.onClick.AddListener(OpenCommentsModal);

            if (postCommentBtn != null) postCommentBtn.onClick.AddListener(OnPostComment);
            if (closeCommentsBtn != null) closeCommentsBtn.onClick.AddListener(() => commentsModal.SetActive(false));

            if (confirmFinalizeBtn != null) confirmFinalizeBtn.onClick.AddListener(OnConfirmFinalize);
            if (cancelFinalizeBtn != null) cancelFinalizeBtn.onClick.AddListener(() => finalizeModal.SetActive(false));
        }

        public void SetMode(ViewerInteractionMode mode)
        {
            if (ModelViewerCamera.Instance != null)
            {
                ModelViewerCamera.Instance.SetInteractionMode(mode);
            }

            UpdateModeButtonsVisual(mode);
        }

        private void UpdateModeButtonsVisual(ViewerInteractionMode mode)
        {
            Color activeColor = UIProceduralTheme.BluePrimary;
            Color normalColor = UIProceduralTheme.CardBg;

            if (orbitModeBtn != null) orbitModeBtn.image.color = mode == ViewerInteractionMode.Orbit ? activeColor : normalColor;
            if (panModeBtn != null) panModeBtn.image.color = mode == ViewerInteractionMode.Pan ? activeColor : normalColor;
            if (zoomModeBtn != null) zoomModeBtn.image.color = mode == ViewerInteractionMode.Zoom ? activeColor : normalColor;

            if (bottomOrbitBtn != null) bottomOrbitBtn.image.color = mode == ViewerInteractionMode.Orbit ? activeColor : normalColor;
            if (bottomPanBtn != null) bottomPanBtn.image.color = mode == ViewerInteractionMode.Pan ? activeColor : normalColor;
            if (bottomZoomBtn != null) bottomZoomBtn.image.color = mode == ViewerInteractionMode.Zoom ? activeColor : normalColor;
        }

        private void OnViewModeDropdownChanged(int index)
        {
            switch (index)
            {
                case 0:
                    ModelMaterialController.Instance?.SetViewMode(ModelViewMode.Shaded);
                    break;
                case 1:
                    ModelMaterialController.Instance?.SetViewMode(ModelViewMode.Wireframe);
                    break;
                case 2:
                    ModelMaterialController.Instance?.SetViewMode(ModelViewMode.XRay);
                    break;
                case 3:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Isometric);
                    break;
                case 4:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Front);
                    break;
                case 5:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Back);
                    break;
                case 6:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Left);
                    break;
                case 7:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Right);
                    break;
                case 8:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Top);
                    break;
                case 9:
                    ModelViewerCamera.Instance?.SetViewPreset(CameraViewPreset.Bottom);
                    break;
                case 10:
                    ModelViewerCamera.Instance?.ToggleOrthographic();
                    break;
            }
        }

        public void RefreshModelList()
        {
            if (modelListContainer == null || ModelDataManager.Instance == null) return;

            for (int i = modelListContainer.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(modelListContainer.GetChild(i).gameObject);
            }
            modelCardObjects.Clear();

            var models = ModelDataManager.Instance.models;
            if (modelListCountText != null)
            {
                modelListCountText.text = $"({models.Count})";
            }

            int currentIndex = ModelDataManager.Instance.currentModelIndex;

            for (int i = 0; i < models.Count; i++)
            {
                int index = i;
                ModelItem item = models[i];
                GameObject cardObj = CreateModelCard(item, index, index == currentIndex);
                modelCardObjects.Add(cardObj);
            }
        }

        public GameObject CreateModelCard(ModelItem item, int index, bool isSelected)
        {
            GameObject card = new GameObject($"ModelCard_{index}");
            card.transform.SetParent(modelListContainer, false);

            RectTransform rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 72);

            LayoutElement cardLE = card.AddComponent<LayoutElement>();
            cardLE.minHeight = 72;
            cardLE.preferredHeight = 72;
            cardLE.flexibleHeight = 0;

            Image bg = card.AddComponent<Image>();
            bg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12,
                isSelected ? UIProceduralTheme.CardSelected : UIProceduralTheme.CardBg,
                isSelected ? UIProceduralTheme.CardBorderSelected : UIProceduralTheme.CardBorder,
                isSelected ? 2 : 1);
            bg.type = Image.Type.Sliced;

            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                ModelDataManager.Instance?.SelectModel(index);
            });

            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Thumbnail container
            GameObject thumbObj = new GameObject("Thumbnail");
            thumbObj.transform.SetParent(card.transform, false);
            RectTransform thumbRT = thumbObj.AddComponent<RectTransform>();
            thumbRT.sizeDelta = new Vector2(56, 56);

            Image thumbBg = thumbObj.AddComponent<Image>();
            if (item.thumbnailSprite != null)
            {
                thumbBg.sprite = item.thumbnailSprite;
                thumbBg.type = Image.Type.Simple;
            }
            else
            {
                thumbBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, new Color(0.12f, 0.16f, 0.24f));
                thumbBg.type = Image.Type.Sliced;
            }

            // Info column
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(card.transform, false);
            RectTransform infoRT = infoObj.AddComponent<RectTransform>();
            infoRT.sizeDelta = new Vector2(170, 52);
            VerticalLayoutGroup vlg = infoObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(infoObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.font = fontAsset;
            titleTMP.text = item.modelName;
            titleTMP.fontSize = 15;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = UIProceduralTheme.TextWhite;
            titleTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(infoObj.transform, false);
            TextMeshProUGUI subTMP = subObj.AddComponent<TextMeshProUGUI>();
            subTMP.font = fontAsset;
            subTMP.text = $"{item.fileFormat} • {item.uploadDate}";
            subTMP.fontSize = 11;
            subTMP.color = UIProceduralTheme.TextMuted;

            // Context Menu Button (Three dots icon)
            GameObject menuBtnObj = new GameObject("MenuButton");
            menuBtnObj.transform.SetParent(card.transform, false);
            RectTransform menuRT = menuBtnObj.AddComponent<RectTransform>();
            menuRT.sizeDelta = new Vector2(24, 28);
            Button menuBtn = menuBtnObj.AddComponent<Button>();

            Image dotImg = menuBtnObj.AddComponent<Image>();
            dotImg.sprite = UIProceduralTheme.GetIconSprite(IconType.MoreDots, UIProceduralTheme.TextMuted, 32);

            menuBtn.onClick.AddListener(() => {
                ShowCardContextMenu(index);
            });

            return card;
        }

        private void ShowCardContextMenu(int index)
        {
            if (ModelDataManager.Instance != null && index >= 0 && index < ModelDataManager.Instance.models.Count)
            {
                ModelItem item = ModelDataManager.Instance.models[index];
                if (item != null)
                {
                    ModelDataManager.Instance.DeleteModel(index);
                }
            }
        }

        public void OnModelSelected(ModelItem item)
        {
            RefreshModelList();

            if (item == null) return;

            if (ModelMaterialController.Instance != null && item.modelObject != null)
            {
                ModelMaterialController.Instance.RegisterModel(item.modelObject);
            }

            if (ModelSelectionManager.Instance != null)
            {
                ModelSelectionManager.Instance.Deselect();
                ModelSelectionManager.Instance.EnsureCollidersOnCurrentModel();
            }

            if (ModelViewerCamera.Instance != null)
            {
                ModelViewerCamera.Instance.FitModel(item.modelBounds);
            }

            RefreshDetails(item);
        }

        public void RefreshDetails(ModelItem item)
        {
            if (item == null) return;

            if (detailModelName != null) detailModelName.text = item.modelName;
            if (detailSubtitle != null) detailSubtitle.text = $"{item.fileFormat} • {item.uploadDate} • {item.fileSize}";

            if (detailVertices != null) detailVertices.text = item.vertexCount.ToString("N0");
            if (detailTriangles != null) detailTriangles.text = item.triangleCount.ToString("N0");
            if (detailDimensions != null) detailDimensions.text = $"{item.dimensions.x:F2} x {item.dimensions.z:F2} x {item.dimensions.y:F2}";
            if (detailMaterials != null) detailMaterials.text = item.materialCount.ToString();
            if (detailTextures != null) detailTextures.text = item.textureCount.ToString();

            if (detailThumbnail != null)
            {
                if (item.thumbnailSprite != null)
                    detailThumbnail.sprite = item.thumbnailSprite;
                else
                    detailThumbnail.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
            }

            if (miniThumbnail != null)
            {
                if (item.thumbnailSprite != null)
                    miniThumbnail.sprite = item.thumbnailSprite;
                else
                    miniThumbnail.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorderSelected, 2);
            }

            RefreshChecklist(item);
            RefreshStatusCard(item);
        }

        public void RefreshChecklist(ModelItem item)
        {
            if (checklistContainer == null || item == null) return;

            ContentSizeFitter csf = checklistContainer.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = checklistContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = checklistContainer.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(checklistContainer.GetChild(i).gameObject);
            }
            checklistItemObjects.Clear();

            for (int i = 0; i < item.checklist.Count; i++)
            {
                int index = i;
                ChecklistItem checkItem = item.checklist[i];

                GameObject row = new GameObject($"ChecklistItem_{index}");
                row.transform.SetParent(checklistContainer, false);
                RectTransform rt = row.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 48);

                LayoutElement rowLE = row.AddComponent<LayoutElement>();
                rowLE.minHeight = 48;
                rowLE.preferredHeight = 48;
                rowLE.flexibleHeight = 0;

                HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 12;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                // Status Icon / Button
                GameObject iconObj = new GameObject("StatusIcon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRT = iconObj.AddComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(28, 28);
                Image iconImg = iconObj.AddComponent<Image>();
                Button iconBtn = iconObj.AddComponent<Button>();

                Color circleColor = checkItem.status == ReviewItemStatus.Passed ? UIProceduralTheme.GreenReady :
                    (checkItem.status == ReviewItemStatus.CorrectionRequired ? UIProceduralTheme.OrangeWarning : UIProceduralTheme.TextSubtle);
                iconImg.sprite = UIProceduralTheme.GetCircleSprite(32, circleColor);

                GameObject markObj = new GameObject("Mark");
                markObj.transform.SetParent(iconObj.transform, false);
                RectTransform markRT = markObj.AddComponent<RectTransform>();
                markRT.anchorMin = Vector2.zero;
                markRT.anchorMax = Vector2.one;
                markRT.sizeDelta = new Vector2(-6, -6);
                Image markImg = markObj.AddComponent<Image>();

                if (checkItem.status == ReviewItemStatus.Passed)
                {
                    markImg.sprite = UIProceduralTheme.GetIconSprite(IconType.Checkmark, Color.white, 32);
                    markImg.color = Color.white;
                }
                else
                {
                    markImg.color = Color.clear;
                }

                iconBtn.onClick.AddListener(() => {
                    if (checkItem.status == ReviewItemStatus.Passed)
                        checkItem.status = ReviewItemStatus.CorrectionRequired;
                    else if (checkItem.status == ReviewItemStatus.CorrectionRequired)
                        checkItem.status = ReviewItemStatus.Pending;
                    else
                        checkItem.status = ReviewItemStatus.Passed;

                    item.UpdateOverallStatus();
                    ModelDataManager.Instance?.NotifyModelDataUpdated(item);
                });

                // Text column
                GameObject textCol = new GameObject("TextCol");
                textCol.transform.SetParent(row.transform, false);
                RectTransform textRT = textCol.AddComponent<RectTransform>();
                textRT.sizeDelta = new Vector2(250, 42);
                VerticalLayoutGroup vlg = textCol.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 2;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(textCol.transform, false);
                TextMeshProUGUI tTMP = titleObj.AddComponent<TextMeshProUGUI>();
                tTMP.font = fontAsset;
                tTMP.text = checkItem.title;
                tTMP.fontSize = 15;
                tTMP.fontStyle = FontStyles.Bold;
                tTMP.color = UIProceduralTheme.TextWhite;

                GameObject subObj = new GameObject("Subtitle");
                subObj.transform.SetParent(textCol.transform, false);
                TextMeshProUGUI sTMP = subObj.AddComponent<TextMeshProUGUI>();
                sTMP.font = fontAsset;
                sTMP.text = checkItem.subtitle;
                sTMP.fontSize = 11;
                sTMP.color = UIProceduralTheme.TextMuted;

                checklistItemObjects.Add(row);
            }
        }

        public void RefreshStatusCard(ModelItem item)
        {
            if (statusCardBg == null || item == null) return;

            switch (item.status)
            {
                case ModelStatus.Finalized:
                    statusCardBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.GreenReadyBg, UIProceduralTheme.GreenReady, 2);
                    if (statusCardTitle != null) { statusCardTitle.text = "Model Ready"; statusCardTitle.color = UIProceduralTheme.GreenReady; }
                    if (statusCardSubtitle != null) { statusCardSubtitle.text = "This model is finalized and approved."; statusCardSubtitle.color = UIProceduralTheme.TextMuted; }
                    if (finalizeButtonText != null) finalizeButtonText.text = "Mark as Finalized";
                    if (finalizeButton != null) finalizeButton.interactable = true;
                    break;

                case ModelStatus.CorrectionRequired:
                    statusCardBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, new Color(0.2f, 0.12f, 0.05f), UIProceduralTheme.OrangeWarning, 2);
                    if (statusCardTitle != null) { statusCardTitle.text = "Correction Required"; statusCardTitle.color = UIProceduralTheme.OrangeWarning; }
                    if (statusCardSubtitle != null) { statusCardSubtitle.text = "Issues flagged in review checklist."; statusCardSubtitle.color = UIProceduralTheme.TextMuted; }
                    if (finalizeButtonText != null) finalizeButtonText.text = "Mark as Finalized";
                    if (finalizeButton != null) finalizeButton.interactable = true;
                    break;

                case ModelStatus.UnderReview:
                    statusCardBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.CardBg, UIProceduralTheme.BluePrimary, 1);
                    if (statusCardTitle != null) { statusCardTitle.text = "Under Review"; statusCardTitle.color = UIProceduralTheme.BlueHover; }
                    if (statusCardSubtitle != null) { statusCardSubtitle.text = "Review checklist in progress."; statusCardSubtitle.color = UIProceduralTheme.TextMuted; }
                    if (finalizeButtonText != null) finalizeButtonText.text = "Mark as Finalized";
                    if (finalizeButton != null) finalizeButton.interactable = true;
                    break;

                case ModelStatus.NotReviewed:
                default:
                    statusCardBg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 12, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
                    if (statusCardTitle != null) { statusCardTitle.text = "Not Reviewed"; statusCardTitle.color = UIProceduralTheme.TextMuted; }
                    if (statusCardSubtitle != null) { statusCardSubtitle.text = "Pending technical review."; statusCardSubtitle.color = UIProceduralTheme.TextSubtle; }
                    if (finalizeButtonText != null) finalizeButtonText.text = "Mark as Finalized";
                    if (finalizeButton != null) finalizeButton.interactable = true;
                    break;
            }
        }

        private void OnSelectionChanged(GameObject selectedSubObject, string name)
        {
            if (selectionTagText == null) return;

            if (selectedSubObject != null)
            {
                selectionTagText.gameObject.SetActive(true);
                selectionTagText.text = $"Selected: {selectedSubObject.name}";
            }
            else
            {
                selectionTagText.gameObject.SetActive(false);
            }
        }

        private void OnFinalizeClicked()
        {
            var cur = ModelDataManager.Instance?.GetCurrentModel();
            if (cur == null) return;

            if (finalizeModal != null)
            {
                finalizeModal.SetActive(true);
                if (finalizeModalTitle != null) finalizeModalTitle.text = $"Finalize {cur.modelName}?";
                if (finalizeModalMessage != null) finalizeModalMessage.text = "All checklist criteria will be marked as approved. Please enter reviewer name.";
                if (reviewerNameInput != null) reviewerNameInput.text = "Chief 3D Inspector";
            }
            else
            {
                ModelDataManager.Instance?.FinalizeModel(cur.id, "Chief 3D Inspector");
            }
        }

        private void OnConfirmFinalize()
        {
            var cur = ModelDataManager.Instance?.GetCurrentModel();
            if (cur == null) return;

            string rev = reviewerNameInput != null && !string.IsNullOrEmpty(reviewerNameInput.text)
                ? reviewerNameInput.text : "CC Inspector";

            ModelDataManager.Instance?.FinalizeModel(cur.id, rev);

            if (finalizeModal != null)
            {
                finalizeModal.SetActive(false);
            }
        }

        private void OpenCommentsModal()
        {
            if (commentsModal == null) return;

            commentsModal.SetActive(true);
            RefreshCommentsList();
        }

        private void RefreshCommentsList()
        {
            if (commentsListContainer == null) return;

            for (int i = commentsListContainer.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(commentsListContainer.GetChild(i).gameObject);
            }

            var cur = ModelDataManager.Instance?.GetCurrentModel();
            if (cur == null) return;

            foreach (var c in cur.comments)
            {
                GameObject card = new GameObject("CommentCard");
                card.transform.SetParent(commentsListContainer, false);
                RectTransform rt = card.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 60);

                Image bg = card.AddComponent<Image>();
                bg.sprite = UIProceduralTheme.GetRoundedRectSprite(64, 64, 8, UIProceduralTheme.CardBg, UIProceduralTheme.CardBorder, 1);
                bg.type = Image.Type.Sliced;

                VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(10, 10, 8, 8);
                vlg.spacing = 3;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;

                GameObject hObj = new GameObject("Header");
                hObj.transform.SetParent(card.transform, false);
                TextMeshProUGUI hTMP = hObj.AddComponent<TextMeshProUGUI>();
                hTMP.font = fontAsset;
                hTMP.text = $"<b>{c.author}</b>  <size=10><color=#94a3b8>{c.timestamp}</color></size>";
                hTMP.fontSize = 12;
                hTMP.color = UIProceduralTheme.TextWhite;

                GameObject bodyObj = new GameObject("Body");
                bodyObj.transform.SetParent(card.transform, false);
                TextMeshProUGUI bTMP = bodyObj.AddComponent<TextMeshProUGUI>();
                bTMP.font = fontAsset;
                bTMP.text = c.commentText;
                bTMP.fontSize = 12;
                bTMP.color = UIProceduralTheme.TextMuted;
            }
        }

        private void OnPostComment()
        {
            if (commentInput == null || string.IsNullOrWhiteSpace(commentInput.text)) return;

            var cur = ModelDataManager.Instance?.GetCurrentModel();
            if (cur == null) return;

            ModelDataManager.Instance?.AddComment(cur.id, "Inspector", commentInput.text);
            commentInput.text = "";
            RefreshCommentsList();
        }

        public void TriggerUploadModel()
        {
#if UNITY_EDITOR
            string filePath = EditorUtility.OpenFilePanel("Select 3D Model", "", "fbx,obj,gltf,glb,dae");
            if (!string.IsNullOrEmpty(filePath))
            {
                StartCoroutine(SimulateUploadProgress(filePath));
            }
#else
            Debug.Log("[ModelViewerUI] Runtime file dialog triggered.");
#endif
        }

        private IEnumerator SimulateUploadProgress(string filePath)
        {
            if (uploadProgressModal != null)
            {
                uploadProgressModal.SetActive(true);
                if (uploadStatusText != null) uploadStatusText.text = $"Uploading {Path.GetFileName(filePath)}...";
                if (uploadProgressBar != null) uploadProgressBar.fillAmount = 0f;
            }

            for (float t = 0; t <= 1f; t += 0.05f)
            {
                if (uploadProgressBar != null) uploadProgressBar.fillAmount = t;
                yield return new WaitForSeconds(0.04f);
            }

            if (uploadProgressModal != null) uploadProgressModal.SetActive(false);

            if (ModelDataManager.Instance != null)
            {
                ModelDataManager.Instance.UploadModelFromFile(filePath);
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
