
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates UXML")]
    [SerializeField] private VisualTreeAsset teamRowTemplate;
    [SerializeField] private VisualTreeAsset playerCardTemplate;
    [SerializeField] private VisualTreeAsset statRowTemplate;
    [SerializeField] private VisualTreeAsset editorPlayerRowTemplate;

    private LeagueData _data;
    private Team _selectedTeam;
    private Player _selectedPlayer;
    private string _currentStatFilter = "goals";

    // Tabs
    private Button _tabBtnLiga, _tabBtnConvocados, _tabBtnEditor;
    private VisualElement _tabLiga, _tabConvocados, _tabEditor;

    // Pestaña 1
    private ScrollView _teamsList, _statsList;
    private Slider _sliderTeamsCount, _sliderSquadSize;
    private Label _lblTeamsCount, _lblSquadSize;
    private Button _btnFilterGoals, _btnFilterAssists, _btnFilterBoth;

    // Pestaña 2
    private Label _squadTeamName, _squadCountLabel;
    private VisualElement _dropNoConvocados, _dropConvocados;
    private ScrollView _listNoConvocados, _listConvocados;

    // Pestaña 3
    private TextField _tfTeamName;
    private Button _btnSaveTeam, _btnSavePlayer, _btnSaveAll;
    private ScrollView _editorPlayerList;
    private VisualElement _playerEditorPanel, _editorEmptyState;
    private VisualElement _statBarsContainer, _attrEditors;
    private Label _editPlayerName, _editPlayerPos;
    private SliderInt _siSpeed, _siStamina, _siDefense, _siAttack, _siGoals, _siAssists;
    private Label _footerStatus;

    // ─── Estado ───────────────────────────────────────────────
    private VisualElement _selectedTeamRow;
    private VisualElement _selectedEditorPlayerRow;

    private void Awake()
    {
        _data = DataManager.Load();
    }

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        QueryAllElements(root);
        RegisterTabEvents();
        RegisterSliderEvents();
        RegisterFilterButtonEvents();
        RegisterEditorEvents();

        PopulateTeamsTable();
        PopulateStatsTable();
        ShowTab("liga");
    }

    // obtenemos todos los elementos apartir de la raiz 
    private void QueryAllElements(VisualElement root)
    {
        // Tabs
        _tabBtnLiga = root.Q<Button>("tabLigaButton");
        _tabBtnConvocados = root.Q<Button>("tabConvocadosButton");
        _tabBtnEditor = root.Q<Button>("tabEditorButton");
        _tabLiga = root.Q<VisualElement>("tabLiga");
        _tabConvocados = root.Q<VisualElement>("tabConvocados");
        _tabEditor = root.Q<VisualElement>("tabEditor");

        // Pestaña 1
        _teamsList = root.Q<ScrollView>("teamsList");
        _statsList = root.Q<ScrollView>("statsList");
        _sliderTeamsCount = root.Q<Slider>("sliderTeamsCount");
        _sliderSquadSize = root.Q<Slider>("sliderTeamSize");
        _lblTeamsCount = root.Q<Label>("lblTeamsCount");
        _lblSquadSize = root.Q<Label>("lblTeamSize");
        _btnFilterGoals = root.Q<Button>("btnGoals");
        _btnFilterAssists = root.Q<Button>("btnAssists");
        _btnFilterBoth = root.Q<Button>("btnBoth");

        // Pestaña 2
        _squadTeamName = root.Q<Label>("squadTeamName");
        _squadCountLabel = root.Q<Label>("squadTeamName");
        _dropNoConvocados = root.Q<VisualElement>("dropNoConvocados");
        _dropConvocados = root.Q<VisualElement>("dropConvocados");
        _listNoConvocados = root.Q<ScrollView>("listNoConvocados");
        _listConvocados = root.Q<ScrollView>("listConvocados");

        // Pestaña 3
        _tfTeamName = root.Q<TextField>("tf-team-name");
        _btnSaveTeam = root.Q<Button>("btnSaveTeam");
        _btnSavePlayer = root.Q<Button>("btnSavePlayer");
        _btnSaveAll = root.Q<Button>("btnSaveAll");
        _editorPlayerList = root.Q<ScrollView>("editorPlayerlist");
        _playerEditorPanel = root.Q<VisualElement>("playerEditorPanel");
        _editorEmptyState = root.Q<VisualElement>("editorEmptyState");
        _statBarsContainer = root.Q<VisualElement>("statBarsContainer");
        _editPlayerName = root.Q<Label>("editPlayerName");
        _editPlayerPos = root.Q<Label>("editPlayerPos");
        _siSpeed = root.Q<SliderInt>("speed");
        _siStamina = root.Q<SliderInt>("stamina");
        _siDefense = root.Q<SliderInt>("defense");
        _siAttack = root.Q<SliderInt>("attack");
        _siGoals = root.Q<SliderInt>("goals");
        _siAssists = root.Q<SliderInt>("assists");
        _footerStatus = root.Q<Label>("footer-status");

        // Sincronizar sliders con datos guardados
        _sliderTeamsCount.SetValueWithoutNotify(_data.teamsShown);
        _sliderSquadSize.SetValueWithoutNotify(_data.maxSquadSize);
        UpdateSliderLabels();

        // Estado inicial del editor
        ShowPlayerEditor(false);
    }


    private void RegisterTabEvents()
    {
        _tabBtnLiga.RegisterCallback<ClickEvent>(OnLigaClicked);
        _tabBtnConvocados.RegisterCallback<ClickEvent>(OnConvocadosClicked);
        _tabBtnEditor.RegisterCallback<ClickEvent>(OnEditorClicked);
    }

    private void OnLigaClicked(ClickEvent evt)
    {
        ShowTab("liga");
    }
    private void OnConvocadosClicked(ClickEvent evt)
    {
        ShowTab("convocados");
    }
    private void OnEditorClicked(ClickEvent evt) 
    {
        ShowTab("editor");
    }

    private void ShowTab(string tabName)
    {
        // Ocultar todos usando display
        _tabLiga.style.display       = DisplayStyle.None;
        _tabConvocados.style.display = DisplayStyle.None;
        _tabEditor.style.display     = DisplayStyle.None;

        // Quitar clase activa de botones
        _tabBtnLiga.RemoveFromClassList("buttonTab--active");
        _tabBtnConvocados.RemoveFromClassList("buttonTab--active");
        _tabBtnEditor.RemoveFromClassList("buttonTab--active");

        switch (tabName)
        {
            case "liga":
                _tabLiga.style.display = DisplayStyle.Flex;
                _tabBtnLiga.AddToClassList("buttonTab--active");
                break;

            case "convocados":
                _tabConvocados.style.display = DisplayStyle.Flex;
                _tabBtnConvocados.AddToClassList("buttonTab--active");
                PopulateSquadTab();
                break;

            case "editor":
                _tabEditor.style.display = DisplayStyle.Flex;
                _tabBtnEditor.AddToClassList("buttonTab--active");
                PopulateEditorTab();
                break;
        }
    }

    private void PopulateTeamsTable()
    {
        _teamsList.Clear();

        var sorted = _data.teams
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDiff)
            .Take(_data.teamsShown)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var team = sorted[i];
            var row  = CreateTeamRow(team, i + 1);
            _teamsList.Add(row);
        }
    }

    private VisualElement CreateTeamRow(Team team, int position)
    {
        // Instanciar template
        var rowInstance = teamRowTemplate.Instantiate();
        var row = rowInstance.Q<VisualElement>("team-row");

        // Si el template no tiene nombre raíz, usar el primer hijo
        if (row == null) row = rowInstance.Children().FirstOrDefault() as VisualElement
                               ?? rowInstance;

        // Datos
        row.Q<Label>("team-pos").text  = position.ToString();
        row.Q<Label>("team-name").text = team.name;
        row.Q<Label>("team-pj").text   = team.matchesPlayed.ToString();
        row.Q<Label>("team-pg").text   = team.wins.ToString();
        row.Q<Label>("team-pe").text   = team.draws.ToString();
        row.Q<Label>("team-pp").text   = team.losses.ToString();
        row.Q<Label>("team-pts").text  = team.Points.ToString();

        // Color lateral del equipo
        var colorBar = row.Q<VisualElement>("team-color-bar");
        if (colorBar != null && ColorUtility.TryParseHtmlString(team.color, out Color c))
        {
            colorBar.style.backgroundColor = new StyleColor(c);
        }

        // Clases de posición visual (top 3)
        if      (position == 1) row.AddToClassList("team-row--first");
        else if (position == 2) row.AddToClassList("team-row--second");
        else if (position == 3) row.AddToClassList("team-row--third");

        // Si ya estaba seleccionado, restaurar selección visual
        if (_selectedTeam != null && _selectedTeam.id == team.id)
        {
            row.AddToClassList("team-row--selected");
            _selectedTeamRow = row;
        }

        // Click para seleccionar equipo
        row.RegisterCallback<ClickEvent>(_ => SelectTeam(team, row));

        return rowInstance;
    }

    private void SelectTeam(Team team, VisualElement row)
    {
        // Quitar selección anterior
        _selectedTeamRow?.RemoveFromClassList("team-row--selected");

        _selectedTeam    = team;
        _selectedTeamRow = row;
        row.AddToClassList("team-row--selected");

        Debug.Log($"[MenuController] Equipo seleccionado: {team.name}");
    }

    private void PopulateStatsTable()
    {
        _statsList.Clear();

        // Recolectar todos los jugadores con estadísticas
        var allPlayers = _data.teams
            .SelectMany(t => t.players.Select(p => (player: p, teamName: t.name)))
            .ToList();

        IEnumerable<(Player player, string teamName)> filtered;

        switch (_currentStatFilter)
        {
            case "assists":
                filtered = allPlayers.OrderByDescending(x => x.player.assists).Take(10);
                break;
            case "both":
                filtered = allPlayers
                    .OrderByDescending(x => x.player.goals + x.player.assists)
                    .Take(10);
                break;
            default: // goals
                filtered = allPlayers.OrderByDescending(x => x.player.goals).Take(10);
                break;
        }

        int rank = 1;
        foreach (var (player, teamName) in filtered)
        {
            var rowInstance = statRowTemplate.Instantiate();
            var row = rowInstance.Q<VisualElement>("stat-row")
                      ?? rowInstance.Children().FirstOrDefault() as VisualElement
                      ?? rowInstance;

            row.Q<Label>("stat-rank").text    = rank.ToString();
            row.Q<Label>("stat-player").text  = player.name;
            row.Q<Label>("stat-team").text    = teamName;
            row.Q<Label>("stat-goals").text   = player.goals.ToString();
            row.Q<Label>("stat-assists").text = player.assists.ToString();

            _statsList.Add(rowInstance);
            rank++;
        }
    }

    private void RegisterFilterButtonEvents()
    {
        _btnFilterGoals.clicked += () =>
        {
            _currentStatFilter = "goals";
            UpdateFilterButtons(_btnFilterGoals);
            PopulateStatsTable();
        };
        _btnFilterAssists.clicked += () =>
        {
            _currentStatFilter = "assists";
            UpdateFilterButtons(_btnFilterAssists);
            PopulateStatsTable();
        };
        _btnFilterBoth.clicked += () =>
        {
            _currentStatFilter = "both";
            UpdateFilterButtons(_btnFilterBoth);
            PopulateStatsTable();
        };
    }

    private void UpdateFilterButtons(Button active)
    {
        foreach (var btn in new[] { _btnFilterGoals, _btnFilterAssists, _btnFilterBoth })
            btn.RemoveFromClassList("filterBtn--active");
        active.AddToClassList("filterBtn--active");
    }

    // ── Sliders ───────────────────────────────────────────────
    private void RegisterSliderEvents()
    {
        _sliderTeamsCount.RegisterValueChangedCallback(evt =>
        {
            _data.teamsShown = Mathf.RoundToInt(evt.newValue);
            _lblTeamsCount.text = $"Equipos mostrados: {_data.teamsShown}";
            PopulateTeamsTable();
        });

        _sliderSquadSize.RegisterValueChangedCallback(evt =>
        {
            _data.maxSquadSize = Mathf.RoundToInt(evt.newValue);
            _lblSquadSize.text = $"Máx. convocados: {_data.maxSquadSize}";
            UpdateSquadCountLabel();
        });

        // Botón guardar todo
        if (_btnSaveAll != null)
            _btnSaveAll.clicked += SaveAll;
    }

    private void UpdateSliderLabels()
    {
        _lblTeamsCount.text = $"Equipos mostrados: {_data.teamsShown}";
        _lblSquadSize.text  = $"Máx. convocados: {_data.maxSquadSize}";
    }

    private void PopulateSquadTab()
    {
        _listNoConvocados.Clear();
        _listConvocados.Clear();

        if (_selectedTeam == null)
        {
            _squadTeamName.text = "⚠  Selecciona un equipo en la pestaña Liga";
            UpdateSquadCountLabel();
            return;
        }

        _squadTeamName.text = _selectedTeam.name;

        // Separar jugadores: convocados vs plantilla
        var calledIds  = new HashSet<string>(_data.calledUpPlayerIds);

        foreach (var player in _selectedTeam.players)
        {
            var card = CreatePlayerCard(player);

            if (calledIds.Contains(player.id))
                _listConvocados.Add(card);
            else
                _listNoConvocados.Add(card);
        }

        UpdateSquadCountLabel();
    }

    private VisualElement CreatePlayerCard(Player player)
    {
        var cardInstance = playerCardTemplate.Instantiate();
        var card = cardInstance.Q<VisualElement>("player-card")
                   ?? cardInstance.Children().FirstOrDefault() as VisualElement
                   ?? cardInstance;

        card.Q<Label>("player-pos-badge").text = player.position;
        card.Q<Label>("player-rating").text    = player.OverallRating.ToString();
        card.Q<Label>("player-name").text      = player.name;
        card.Q<Label>("player-speed").text     = player.speed.ToString();
        card.Q<Label>("player-attack").text    = player.attack.ToString();
        card.Q<Label>("player-defense").text   = player.defense.ToString();

        // Guardar ID en userData para referencia
        card.userData = player.id;

        // Añadir manipulador de Drag & Drop
        var manipulator = new PlayerDragManipulator(
            uiDocument.rootVisualElement,
            _dropNoConvocados,
            _dropConvocados,
            CanAddToCallup
        );
        manipulator.OnDropped += HandleCardDropped;
        card.AddManipulator(manipulator);

        return cardInstance; // devolvemos la instancia completa para que ScrollView la reciba
    }

    private bool CanAddToCallup()
    {
        int currentCount = _listConvocados.childCount;
        return currentCount < _data.maxSquadSize;
    }

    private void HandleCardDropped(VisualElement card, VisualElement targetZone)
    {
        // Mover el elemento al nuevo contenedor
        card.RemoveFromHierarchy();
        var parent = targetZone.Q<ScrollView>() ?? targetZone;
        parent.Add(card);

        // Actualizar datos
        string playerId = card.userData as string ?? "";
        if (!string.IsNullOrEmpty(playerId))
        {
            if (targetZone == _dropConvocados)
                _data.calledUpPlayerIds.Add(playerId);
            else
                _data.calledUpPlayerIds.Remove(playerId);
        }

        UpdateSquadCountLabel();
    }

    private void UpdateSquadCountLabel()
    {
        int current = _listConvocados?.childCount ?? _data.calledUpPlayerIds.Count;
        int max     = _data.maxSquadSize;
        if (_squadCountLabel != null)
            _squadCountLabel.text = $"Convocados: {current} / {max}";
    }
    private void PopulateEditorTab()
    {
        _editorPlayerList.Clear();
        _selectedPlayer = null;
        ShowPlayerEditor(false);

        if (_selectedTeam == null)
        {
            _tfTeamName.value = "";
            return;
        }

        _tfTeamName.SetValueWithoutNotify(_selectedTeam.name);

        foreach (var player in _selectedTeam.players)
        {
            var rowInstance = editorPlayerRowTemplate.Instantiate();
            var row = rowInstance.Q<VisualElement>("editor-player-row")
                      ?? rowInstance.Children().FirstOrDefault() as VisualElement
                      ?? rowInstance;

            row.Q<Label>("epr-pos").text    = player.position;
            row.Q<Label>("epr-name").text   = player.name;
            row.Q<Label>("epr-rating").text = player.OverallRating.ToString();
            row.userData = player;

            row.RegisterCallback<ClickEvent>(_ =>
            {
                SelectEditorPlayer(player, row);
            });

            _editorPlayerList.Add(rowInstance);
        }
    }

    private void SelectEditorPlayer(Player player, VisualElement row)
    {
        // Quitar selección anterior
        _selectedEditorPlayerRow?.RemoveFromClassList("editor-player-row--selected");
        _selectedEditorPlayerRow = row;
        row.AddToClassList("editor-player-row--selected");

        _selectedPlayer = player;
        ShowPlayerEditor(true);
        BindPlayerToEditor(player);
    }

    private void ShowPlayerEditor(bool show)
    {
        if (_playerEditorPanel != null)
            _playerEditorPanel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (_editorEmptyState != null)
            _editorEmptyState.style.display  = show ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void BindPlayerToEditor(Player player)
    {
        _editPlayerName.text = player.name;
        _editPlayerPos.text  = player.position;

        // Sliders
        _siSpeed.SetValueWithoutNotify(player.speed);
        _siStamina.SetValueWithoutNotify(player.stamina);
        _siDefense.SetValueWithoutNotify(player.defense);
        _siAttack.SetValueWithoutNotify(player.attack);
        _siGoals.SetValueWithoutNotify(player.goals);
        _siAssists.SetValueWithoutNotify(player.assists);

        // Refrescar StatBars (Custom Controls)
        RefreshStatBars(player);
    }

    private void RefreshStatBars(Player player)
    {
        if (_statBarsContainer == null) return;
        _statBarsContainer.Clear();

        var stats = new (string label, int value)[]
        {
            ("Velocidad",   player.speed),
            ("Resistencia", player.stamina),
            ("Defensa",     player.defense),
            ("Ataque",      player.attack),
            ("General",     player.OverallRating),
        };

        foreach (var (label, value) in stats)
        {
            var bar = new StatBar
            {
                StatLabel = label,
                Value     = value
            };
            _statBarsContainer.Add(bar);
        }
    }

    private void RegisterEditorEvents()
    {

        // Guardar nombre de equipo
        _btnSaveTeam?.RegisterCallback<ClickEvent>(_ =>
        {
            if (_selectedTeam == null) return;
            _selectedTeam.name = _tfTeamName.value;
            PopulateTeamsTable(); // refrescar tabla
            ShowStatus("✔ Nombre de equipo guardado.");
        });

        // Guardar atributos de jugador
        _btnSavePlayer?.RegisterCallback<ClickEvent>(_ =>
        {
            if (_selectedPlayer == null) return;
            _selectedPlayer.speed   = _siSpeed.value;
            _selectedPlayer.stamina = _siStamina.value;
            _selectedPlayer.defense = _siDefense.value;
            _selectedPlayer.attack  = _siAttack.value;
            _selectedPlayer.goals   = _siGoals.value;
            _selectedPlayer.assists = _siAssists.value;

            RefreshStatBars(_selectedPlayer);
            PopulateStatsTable();
            ShowStatus($"✔ {_selectedPlayer.name} actualizado.");
        });

        // Live update de stat bars al mover sliders
        RegisterAttrSliderLiveUpdate(_siSpeed,   v => { if (_selectedPlayer != null) { _selectedPlayer.speed   = v; RefreshStatBars(_selectedPlayer); } });
        RegisterAttrSliderLiveUpdate(_siStamina, v => { if (_selectedPlayer != null) { _selectedPlayer.stamina = v; RefreshStatBars(_selectedPlayer); } });
        RegisterAttrSliderLiveUpdate(_siDefense, v => { if (_selectedPlayer != null) { _selectedPlayer.defense = v; RefreshStatBars(_selectedPlayer); } });
        RegisterAttrSliderLiveUpdate(_siAttack,  v => { if (_selectedPlayer != null) { _selectedPlayer.attack  = v; RefreshStatBars(_selectedPlayer); } });
    }

    private void RegisterAttrSliderLiveUpdate(SliderInt slider, System.Action<int> onChanged)
    {
        slider?.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
    }

    private void SaveAll()
    {
        DataManager.Save();
        ShowStatus("✔ Todos los datos guardados.");
    }

    private void ShowStatus(string msg)
    {
        if (_footerStatus == null) return;
        _footerStatus.text = msg;
        // Limpiar mensaje tras 3 segundos
        _footerStatus.schedule.Execute(() => _footerStatus.text = "")
                     .StartingIn(3000);
    }

    private void OnDisable()
    {
        // Unity limpia los callbacks automáticamente al destruir el panel,
        // pero guardamos en OnDisable por seguridad.
        DataManager.Save();
    }
}
