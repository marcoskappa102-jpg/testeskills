using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MMOClient.Skills
{
    /// <summary>
    /// Janela de lista de skills disponíveis para aprender (Skill Book)
    /// Tecla K para abrir
    /// </summary>
    public class SkillBookUI : MonoBehaviour
    {
        public static SkillBookUI Instance { get; private set; }

        [Header("Panels")]
        public GameObject skillBookPanel;

        [Header("Skill List")]
        public Transform skillListContainer;
        public GameObject skillEntryPrefab;

        [Header("Skill Details")]
        public GameObject detailsPanel;
        public TextMeshProUGUI skillNameText;
        public TextMeshProUGUI skillDescriptionText;
        public TextMeshProUGUI skillStatsText;
        public Button learnButton;
        public Button levelUpButton;
        public Button closeButton;

        [Header("Filters")]
        public TMP_Dropdown filterDropdown;

        private List<SkillTemplate> availableSkills = new List<SkillTemplate>();
        private SkillTemplate selectedSkill = null;
        private LearnedSkill selectedLearnedSkill = null;
        private bool isVisible = false;

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
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (learnButton != null)
                learnButton.onClick.AddListener(OnLearnButtonClick);

            if (levelUpButton != null)
                levelUpButton.onClick.AddListener(OnLevelUpButtonClick);

            if (filterDropdown != null)
                filterDropdown.onValueChanged.AddListener(OnFilterChanged);

            if (MessageHandler.Instance != null)
            {
                MessageHandler.Instance.OnMessageReceived += HandleSkillBookMessages;
            }

            Hide();
        }

        private void Update()
        {
            // Tecla K para abrir/fechar
            if (Input.GetKeyDown(KeyCode.K))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (skillBookPanel != null)
                skillBookPanel.SetActive(true);

            isVisible = true;

            // Solicita lista de skills do servidor
            RequestSkillList();
        }

        public void Hide()
        {
            if (skillBookPanel != null)
                skillBookPanel.SetActive(false);

            isVisible = false;
            selectedSkill = null;
            selectedLearnedSkill = null;

            if (detailsPanel != null)
                detailsPanel.SetActive(false);
        }

        /// <summary>
        /// Solicita lista de skills disponíveis
        /// </summary>
        private void RequestSkillList()
        {
            var message = new { type = "getSkillList" };
            string json = JsonConvert.SerializeObject(message);
            ClientManager.Instance.SendMessage(json);
        }

        /// <summary>
        /// Processa mensagens do servidor
        /// </summary>
        private void HandleSkillBookMessages(string message)
        {
            try
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(message);
                var type = json["type"]?.ToString();

                if (type == "skillListResponse")
                {
                    var skills = json["skills"]?.ToObject<List<SkillTemplate>>();
                    if (skills != null)
                    {
                        LoadSkillList(skills);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error handling skill book message: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega lista de skills
        /// </summary>
        private void LoadSkillList(List<SkillTemplate> skills)
        {
            availableSkills = skills;

            // Limpa lista
            foreach (Transform child in skillListContainer)
            {
                Destroy(child.gameObject);
            }

            // Cria entradas
            foreach (var skill in skills)
            {
                CreateSkillEntry(skill);
            }

            Debug.Log($"📚 Loaded {skills.Count} available skills");
        }

        /// <summary>
        /// Cria entrada de skill na lista
        /// </summary>
        private void CreateSkillEntry(SkillTemplate skill)
        {
            if (skillEntryPrefab == null || skillListContainer == null)
                return;

            GameObject entryObj = Instantiate(skillEntryPrefab, skillListContainer);
            var entry = entryObj.GetComponent<SkillBookEntry>();

            if (entry != null)
            {
                entry.Setup(skill, OnSkillSelected);
            }
        }

        /// <summary>
        /// Callback quando skill é selecionada
        /// </summary>
        private void OnSkillSelected(SkillTemplate skill)
        {
            selectedSkill = skill;

            // Verifica se já aprendeu
            var learnedSkills = SkillManager.Instance?.GetAllSkills();
            selectedLearnedSkill = learnedSkills?.Find(s => s.skillId == skill.id);

            ShowSkillDetails(skill);
        }

        /// <summary>
        /// Mostra detalhes da skill selecionada
        /// </summary>
        private void ShowSkillDetails(SkillTemplate skill)
        {
            if (detailsPanel != null)
                detailsPanel.SetActive(true);

            if (skillNameText != null)
            {
                string color = selectedLearnedSkill != null ? "lime" : "white";
                skillNameText.text = $"<color={color}>{skill.name}</color>";
            }

            if (skillDescriptionText != null)
            {
                skillDescriptionText.text = skill.description;
            }

            if (skillStatsText != null)
            {
                string stats = BuildSkillStatsText(skill);
                skillStatsText.text = stats;
            }

            // Atualiza botões
            UpdateButtons(skill);
        }

        /// <summary>
        /// Constrói texto de stats da skill
        /// </summary>
        private string BuildSkillStatsText(SkillTemplate skill)
        {
            string stats = "";

            stats += $"<b>Tipo:</b> {TranslateSkillType(skill.skillType)}\n";
            stats += $"<b>Alvo:</b> {TranslateTargetType(skill.targetType)}\n\n";

            if (skill.requiredLevel > 1)
                stats += $"<color=yellow>Nível Necessário:</color> {skill.requiredLevel}\n";

            if (!string.IsNullOrEmpty(skill.requiredClass))
                stats += $"<color=cyan>Classe:</color> {skill.requiredClass}\n";

            stats += "\n<b>Custos:</b>\n";
            if (skill.manaCost > 0)
                stats += $"  • Mana: {skill.manaCost}\n";
            if (skill.healthCost > 0)
                stats += $"  • HP: {skill.healthCost}\n";

            if (skill.cooldown > 0)
                stats += $"<b>Cooldown:</b> {skill.cooldown}s\n";

            if (skill.castTime > 0)
                stats += $"<b>Conjuração:</b> {skill.castTime}s\n";

            if (skill.range > 0)
                stats += $"<b>Alcance:</b> {skill.range}m\n";

            // Nível atual (se aprendida)
            if (selectedLearnedSkill != null)
            {
                stats += $"\n<color=lime>Nível Atual:</color> {selectedLearnedSkill.currentLevel}/{skill.maxLevel}\n";
                stats += $"<color=yellow>Slot:</color> {selectedLearnedSkill.slotNumber}\n";
            }

            return stats;
        }

        /// <summary>
        /// Atualiza estado dos botões
        /// </summary>
        private void UpdateButtons(SkillTemplate skill)
        {
            var charData = WorldManager.Instance?.GetLocalCharacterData();

            if (selectedLearnedSkill == null)
            {
                // Ainda não aprendeu
                if (learnButton != null)
                {
                    learnButton.gameObject.SetActive(true);

                    bool canLearn = charData != null && 
                                   charData.level >= skill.requiredLevel &&
                                   (string.IsNullOrEmpty(skill.requiredClass) || charData.classe == skill.requiredClass);

                    learnButton.interactable = canLearn;
                }

                if (levelUpButton != null)
                    levelUpButton.gameObject.SetActive(false);
            }
            else
            {
                // Já aprendeu
                if (learnButton != null)
                    learnButton.gameObject.SetActive(false);

                if (levelUpButton != null)
                {
                    levelUpButton.gameObject.SetActive(true);

                    bool canLevelUp = selectedLearnedSkill.currentLevel < skill.maxLevel &&
                                     charData != null && charData.statusPoints > 0;

                    levelUpButton.interactable = canLevelUp;
                }
            }
        }

        /// <summary>
        /// Aprender skill
        /// </summary>
        private void OnLearnButtonClick()
        {
            if (selectedSkill == null)
                return;

            // Mostra seletor de slot
            if (SkillSlotSelectorUI.Instance != null)
            {
                SkillSlotSelectorUI.Instance.Show(selectedSkill, OnSlotSelected);
            }
        }

        private void OnSlotSelected(int slotNumber)
        {
            if (selectedSkill == null)
                return;

            var message = new
            {
                type = "learnSkill",
                skillId = selectedSkill.id,
                slotNumber = slotNumber
            };

            string json = JsonConvert.SerializeObject(message);
            ClientManager.Instance.SendMessage(json);

            Debug.Log($"📚 Learning {selectedSkill.name} in slot {slotNumber}");
        }

        /// <summary>
        /// Upar nível da skill
        /// </summary>
        private void OnLevelUpButtonClick()
        {
            if (selectedSkill == null)
                return;

            var message = new
            {
                type = "levelUpSkill",
                skillId = selectedSkill.id
            };

            string json = JsonConvert.SerializeObject(message);
            ClientManager.Instance.SendMessage(json);

            Debug.Log($"⬆️ Leveling up {selectedSkill.name}");
        }

        private void OnFilterChanged(int index)
        {
            // TODO: Implementar filtros (todas/ativas/passivas/buffs)
            RequestSkillList();
        }

        private string TranslateSkillType(string type)
        {
            return type switch
            {
                "active" => "Ativa",
                "passive" => "Passiva",
                "buff" => "Buff",
                _ => type
            };
        }

        private string TranslateTargetType(string type)
        {
            return type switch
            {
                "enemy" => "Inimigo",
                "self" => "Próprio",
                "ally" => "Aliado",
                "area" => "Área",
                _ => type
            };
        }

        private void OnDestroy()
        {
            if (MessageHandler.Instance != null)
            {
                MessageHandler.Instance.OnMessageReceived -= HandleSkillBookMessages;
            }
        }
    }

    /// <summary>
    /// Entrada individual na lista de skills
    /// </summary>
    public class SkillBookEntry : MonoBehaviour
    {
        public TextMeshProUGUI skillNameText;
        public TextMeshProUGUI skillLevelText;
        public Image iconImage;
        public Button selectButton;

        private SkillTemplate skill;
        private System.Action<SkillTemplate> onSelectCallback;

        public void Setup(SkillTemplate skillTemplate, System.Action<SkillTemplate> callback)
        {
            skill = skillTemplate;
            onSelectCallback = callback;

            if (skillNameText != null)
            {
                skillNameText.text = skill.name;
            }

            if (skillLevelText != null)
            {
                skillLevelText.text = $"Lv.{skill.requiredLevel}";
            }

            if (iconImage != null)
            {
                var sprite = Resources.Load<Sprite>(skill.iconPath);
                if (sprite != null)
                    iconImage.sprite = sprite;
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClick);
            }
        }

        private void OnSelectClick()
        {
            onSelectCallback?.Invoke(skill);
        }
    }
}