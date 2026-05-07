using System;
using UnityEngine;
using UnityEngine.UIElements;

public class StatBar : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StatBar, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription m_Label =
            new UxmlStringAttributeDescription { name = "label", defaultValue = "Stat" };
        UxmlIntAttributeDescription m_Value =
            new UxmlIntAttributeDescription { name = "value", defaultValue = 50 };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var sb = (StatBar)ve;
            sb.StatLabel = m_Label.GetValueFromBag(bag, cc);
            sb.Value     = m_Value.GetValueFromBag(bag, cc);
        }
    }

    private readonly Label      _labelEl;
    private readonly Label      _valueEl;
    private readonly VisualElement _track;
    private readonly VisualElement _fill;

    private string _statLabel = "Stat";
    public string StatLabel
    {
        get => _statLabel;
        set { _statLabel = value; _labelEl.text = value; }
    }

    private int _value = 50;
    public int Value
    {
        get => _value;
        set
        {
            _value = Mathf.Clamp(value, 0, 99);
            _valueEl.text = _value.ToString();
            UpdateFill();
        }
    }
    public StatBar()
    {
        AddToClassList("stat-bar");

        var header = new VisualElement();
        header.AddToClassList("stat-bar__header");

        _labelEl = new Label("Stat");
        _labelEl.AddToClassList("stat-bar__label");

        _valueEl = new Label("50");
        _valueEl.AddToClassList("stat-bar__value");

        header.Add(_labelEl);
        header.Add(_valueEl);
        Add(header);

        _track = new VisualElement();
        _track.AddToClassList("stat-bar__track");

        _fill = new VisualElement();
        _fill.AddToClassList("stat-bar__fill");
        _track.Add(_fill);
        Add(_track);

        // Actualizar fill tras layout
        RegisterCallback<GeometryChangedEvent>(_ => UpdateFill());
    }

    private void UpdateFill()
    {
        float pct = _value / 99f;
        // Colorear: rojo < 60, amarillo < 75, verde >= 75
        string colorClass = _value >= 75 ? "fill--high" :
                            _value >= 60 ? "fill--mid"  : "fill--low";

        _fill.ClearClassList();
        _fill.AddToClassList("stat-bar__fill");
        _fill.AddToClassList(colorClass);
        _fill.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
    }
}

public class PlayerDragManipulator : PointerManipulator
{
    public event Action<VisualElement, VisualElement> OnDropped;
    // (elemento arrastrado, nuevo contenedor padre)

    private Vector2        _startPos;
    private Vector2        _pointerStartPos;
    private VisualElement  _ghost;          // copia visual durante drag
    private VisualElement  _originalParent;
    private VisualElement  _rootContainer;  // panel raíz para posicionar ghost
    private bool           _isDragging;
    private VisualElement  _dropZoneA;      // "Plantilla"
    private VisualElement  _dropZoneB;      // "Convocados"
    private Func<bool>     _canDropToB;     // restricción de límite de convocados

    public PlayerDragManipulator(
        VisualElement rootContainer,
        VisualElement dropZoneA,
        VisualElement dropZoneB,
        Func<bool>    canDropToB)
    {
        _rootContainer = rootContainer;
        _dropZoneA     = dropZoneA;
        _dropZoneB     = dropZoneB;
        _canDropToB    = canDropToB;

        activators.Add(new ManipulatorActivationFilter
        {
            button = MouseButton.LeftMouse
        });
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!CanStartManipulation(evt)) return;

        _isDragging       = false;
        _startPos         = target.layout.position;
        _pointerStartPos  = evt.position;
        _originalParent   = target.parent;

        target.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!target.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - _pointerStartPos;

        // Umbral mínimo para iniciar arrastre
        if (!_isDragging && delta.magnitude > 5f)
        {
            StartDrag(evt);
        }

        if (_isDragging && _ghost != null)
        {
            // Posición del ghost relativa al rootContainer
            Vector2 ghostPos = (Vector2)evt.position - _rootContainer.worldBound.position
                               - new Vector2(_ghost.layout.width / 2, _ghost.layout.height / 2);
            _ghost.style.left = ghostPos.x;
            _ghost.style.top  = ghostPos.y;

            // Highlight drop zones
            HighlightDropZone((Vector2)evt.position);
        }

        evt.StopPropagation();
    }

    private void StartDrag(IPointerEvent evt)
    {
        _isDragging = true;
        target.AddToClassList("dragging");

        // Crear ghost visual
        _ghost = new VisualElement();
        _ghost.AddToClassList("drag-ghost");
        _ghost.style.position = Position.Absolute;
        _ghost.style.width    = target.layout.width;
        _ghost.style.height   = target.layout.height;

        // Copiar label del jugador al ghost
        var lbl = target.Q<Label>("player-name");
        if (lbl != null)
        {
            var ghostLabel = new Label(lbl.text);
            ghostLabel.AddToClassList("drag-ghost__label");
            _ghost.Add(ghostLabel);
        }

        // Posición inicial del ghost
        Vector2 initPos = (Vector2)evt.position - _rootContainer.worldBound.position
                          - new Vector2(target.layout.width / 2, target.layout.height / 2);
        _ghost.style.left = initPos.x;
        _ghost.style.top  = initPos.y;

        _rootContainer.Add(_ghost);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!target.HasPointerCapture(evt.pointerId)) return;
        target.ReleasePointer(evt.pointerId);

        if (_isDragging)
        {
            FinalizeDrop((Vector2)evt.position);
        }

        CleanupDrag();
        evt.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        if (!target.HasPointerCapture(evt.pointerId)) return;
        target.ReleasePointer(evt.pointerId);
        CleanupDrag();
    }

    private void FinalizeDrop(Vector2 screenPos)
    {
        VisualElement targetZone = GetDropZoneAt(screenPos);

        if (targetZone == null || targetZone == _originalParent)
        {
            // Sin zona válida o misma zona → no hacer nada
            return;
        }

        // Verificar restricción de convocados
        bool goingToB = (targetZone == _dropZoneB);
        if (goingToB && !_canDropToB.Invoke())
        {
            Debug.Log("[DragDrop] Límite de convocados alcanzado.");
            target.AddToClassList("shake"); // animación feedback
            target.schedule.Execute(() => target.RemoveFromClassList("shake")).StartingIn(400);
            return;
        }

        OnDropped?.Invoke(target, targetZone);
    }

    private VisualElement GetDropZoneAt(Vector2 screenPos)
    {
        if (_dropZoneA.worldBound.Contains(screenPos)) return _dropZoneA;
        if (_dropZoneB.worldBound.Contains(screenPos)) return _dropZoneB;
        return null;
    }

    private void HighlightDropZone(Vector2 screenPos)
    {
        bool overA = _dropZoneA.worldBound.Contains(screenPos);
        bool overB = _dropZoneB.worldBound.Contains(screenPos);

        _dropZoneA.EnableInClassList("drop-zone--highlight", overA);
        _dropZoneB.EnableInClassList("drop-zone--highlight", overB);
    }

    private void CleanupDrag()
    {
        _isDragging = false;
        target.RemoveFromClassList("dragging");

        if (_ghost != null)
        {
            _ghost.RemoveFromHierarchy();
            _ghost = null;
        }

        _dropZoneA?.RemoveFromClassList("drop-zone--highlight");
        _dropZoneB?.RemoveFromClassList("drop-zone--highlight");
    }
}
