using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Cross-platform camera controller using Unity's Input System.
/// The camera may move inside the base store zone and inside every BUILT StoreSlot zone:
/// buying an expansion extends the area where the camera is allowed to go.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _dragSensitivity = 1f;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 20f;

    [Header("Store Boundary Settings")]
    [SerializeField] private bool _useBounds = true;
    [Tooltip("Camera's personal offset applied to the active boundary clamping.")]
    [SerializeField] private Vector2 _cameraOffset = Vector2.zero;
    [Tooltip("The base store area. The camera can always move inside it; every built StoreSlot adds its own area. Zones should touch or overlap, the camera cannot cross gaps between them.")]
    [SerializeField] private StoreSlot _defaultStoreSlot;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#else
        HandleMouseAndKeyboardInput();
#endif

        ClampPosition();
    }

    private void HandleMouseAndKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        // Typing into a text field (e.g. the currency exchange amount) must not move the camera.
        if (keyboard != null && !IsTypingInInputField())
        {
            Vector2 inputVector = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputVector.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputVector.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputVector.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputVector.x += 1f;

            if (inputVector != Vector2.zero)
            {
                Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y).normalized;
                transform.Translate(moveDirection * (_moveSpeed * Time.deltaTime), Space.World);
            }
        }

        if (mouse != null)
        {
            bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (mouse.leftButton.wasPressedThisFrame && !isPointerOverUI)
            {
                _dragOrigin = GetWorldPosition(mouse.position.ReadValue());
                _isDragging = true;
            }

            if (mouse.leftButton.isPressed && _isDragging)
            {
                Vector3 currentPosition = GetWorldPosition(mouse.position.ReadValue());
                Vector3 difference = _dragOrigin - currentPosition;
                transform.position += difference * _dragSensitivity;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (!isPointerOverUI)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    ApplyZoom(-Mathf.Sign(scroll) * _zoomSpeed);
                }
            }
        }
    }

    private void HandleTouchInput()
    {
        var activeTouches = Touch.activeTouches;

        if (activeTouches.Count == 1)
        {
            Touch touch = activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId))
                {
                    _isDragging = false;
                    return;
                }

                _dragOrigin = GetWorldPosition(touch.screenPosition);
                _isDragging = true;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && _isDragging)
            {
                Vector3 currentPosition = GetWorldPosition(touch.screenPosition);
                Vector3 difference = _dragOrigin - currentPosition;
                transform.position += difference * _dragSensitivity;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                _isDragging = false;
            }
        }
        else if (activeTouches.Count == 2)
        {
            _isDragging = false;

            Touch touch0 = activeTouches[0];
            Touch touch1 = activeTouches[1];

            Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
            Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

            float prevDistance = Vector2.Distance(touch0PrevPos, touch1PrevPos);
            float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

            float deltaDistance = prevDistance - currentDistance;

            ApplyZoom(deltaDistance * _zoomSpeed * 0.01f);
        }
    }

    private void ApplyZoom(float delta)
    {
        if (_camera.orthographic)
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + delta, _minZoom, _maxZoom);
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y + delta, _minZoom, _maxZoom);
            transform.position = pos;
        }
    }

    /// <summary>
    /// Keeps the camera inside the allowed area: the base zone plus every built StoreSlot zone.
    /// A position outside all zones is moved to the nearest point of the nearest zone,
    /// a position inside any zone is left alone.
    /// </summary>
    private void ClampPosition()
    {
        if (!_useBounds) return;

        Vector3 position = transform.position;
        Vector2 point = new Vector2(position.x, position.z);

        Vector2 best = point;
        float bestDistance = float.MaxValue;

        if (_defaultStoreSlot != null)
            ConsiderZone(_defaultStoreSlot, point, ref best, ref bestDistance);

        IReadOnlyList<StoreSlot> storeSlots = StoreRegistry.StoreSlots;
        for (int i = 0; i < storeSlots.Count; i++)
        {
            if (storeSlots[i].State == SlotState.Built)
                ConsiderZone(storeSlots[i], point, ref best, ref bestDistance);
        }

        // No zones at all: there is nothing to clamp to.
        if (bestDistance == float.MaxValue) return;

        position.x = best.x;
        position.z = best.y;
        transform.position = position;
    }

    /// <summary>Remembers the point of this zone that is closest to 'point' if it beats the best one so far.</summary>
    private void ConsiderZone(StoreSlot zone, Vector2 point, ref Vector2 best, ref float bestDistance)
    {
        Vector2 min = zone.MinBounds + _cameraOffset;
        Vector2 max = zone.MaxBounds + _cameraOffset;

        Vector2 closest = new Vector2(
            Mathf.Clamp(point.x, min.x, max.x),
            Mathf.Clamp(point.y, min.y, max.y));

        float distance = (closest - point).sqrMagnitude;
        if (distance < bestDistance)
        {
            bestDistance = distance;
            best = closest;
        }
    }

    /// <summary>True while a TextMeshPro input field has the keyboard focus.</summary>
    private static bool IsTypingInInputField()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        return selected != null
            && selected.TryGetComponent(out TMP_InputField inputField)
            && inputField.isFocused;
    }

    private Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
}