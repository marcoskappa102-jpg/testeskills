using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MMOClient.Skills
{
    /// <summary>
    /// UI para selecionar em qual slot (1-9) aprender uma skill
    /// </summary>
    public class SkillSlotSelectorUI : MonoBehaviour
    {
        public static SkillSlotSelectorUI Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject selectorPanel;
        public TextMeshProUGUI titleText;
        public Button[] slotButtons = new Button[9];
        public TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[9];
        public Button cancelButton;

        private SkillTemplate skillToLearn;
        private System.Action<int> onSlotSelectedCallback;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Configura botões de slot
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotNumber = i + 1;
                
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.AddListener(() => OnSlotButtonClick(slotNumber));
                }

                if (slotLabels[i] != null)
                {
                    slotLabels[i].text = slotNumber.ToString();
                }
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        /// <summary>
        /// Mostra seletor de slot
        /// </summary>
        public void Show(SkillTemplate skill, System.Action<int> callback)
        {
            skillToLearn = skill;
            onSlotSelectedCallback = callback;

            if (selectorPanel != null)
            {
                selectorPanel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = $"Escolha o slot para:\n<color=yellow>{skill.name}</color>";
            }

            UpdateSlotButtons();
        }

        /// <summary>
        /// Oculta seletor
        /// </summary>
        public void Hide()
        {
            if (selectorPanel != null)
            {
                selectorPanel.SetActive(false);
            }

            skillToLearn = null;
            onSlotSelectedCallback = null;
        }

        /// <summary>
        /// Atualiza estado dos botões de slot
        /// </summary>
        private void UpdateSlotButtons()
        {
            var skillSlots = SkillManager.Instance?.GetAllSkills();

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotNumber = i + 1;
                
                if (slotButtons[i] != null)
                {
                    // Verifica se slot já tem skill
                    var existingSkill = skillSlots?.Find(s => s.slotNumber == slotNumber);
                    
                    if (existingSkill != null && existingSkill.template != null)
                    {
                        // Slot ocupado
                        if (slotLabels[i] != null)
                        {
                            slotLabels[i].text = $"{slotNumber}\n<size=14><color=yellow>{existingSkill.template.name}</color></size>";
                        }
                        
                        // Permite substituir
                        slotButtons[i].interactable = true;
                    }
                    else
                    {
                        // Slot vazio
                        if (slotLabels[i] != null)
                        {
                            slotLabels[i].text = $"{slotNumber}\n<size=14><color=gray>Vazio</color></size>";
                        }
                        
                        slotButtons[i].interactable = true;
                    }
                }
            }
        }

        /// <summary>
        /// Callback de clique no botão de slot
        /// </summary>
        private void OnSlotButtonClick(int slotNumber)
        {
            // Verifica se slot já tem skill
            var existingSkill = SkillManager.Instance?.GetSkillInSlot(slotNumber);
            
            if (existingSkill != null && existingSkill.template != null)
            {
                // Confirma substituição
                if (ConfirmDialogUI.Instance != null)
                {
                    ConfirmDialogUI.Instance.Show(
                        "⚠️ Substituir Skill?",
                        $"O slot {slotNumber} já tem a skill:\n<color=yellow>{existingSkill.template.name}</color>\n\nSubstituir por:\n<color=lime>{skillToLearn.name}</color>?",
                        () => ConfirmSlotSelection(slotNumber),
                        null,
                        "Substituir",
                        "Cancelar"
                    );
                }
                else
                {
                    // Sem confirmação, substitui direto
                    ConfirmSlotSelection(slotNumber);
                }
            }
            else
            {
                // Slot vazio, seleciona direto
                ConfirmSlotSelection(slotNumber);
            }
        }

        /// <summary>
        /// Confirma seleção do slot
        /// </summary>
        private void ConfirmSlotSelection(int slotNumber)
        {
            onSlotSelectedCallback?.Invoke(slotNumber);
            Hide();
        }
    }
}