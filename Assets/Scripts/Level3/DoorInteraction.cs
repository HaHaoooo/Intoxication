using UnityEngine;

/// <summary>
/// Attaches to a door parent and makes it interactable via the F key.
/// Press F near a door to swing it open or closed.
///
/// When openDisablesWallCollider is true the sibling wall .Shape MeshCollider
/// is permanently disabled on Start and replaced with two BoxCollider slabs
/// that frame the doorway gap, leaving it physically open.
/// Only set this on doors where the FBX wall mesh is solid with no hole.
/// Leave it false on all other doors so their wall colliders stay intact.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Door Swing")]
    [SerializeField] private float openAngle   = 90f;
    [SerializeField] private float swingSpeed  = 180f;
    [SerializeField] private bool  invertHinge = false;

    [Header("Interaction")]
    [Tooltip("Player must be within this distance (world units) to see the prompt.")]
    [SerializeField] private float maxInteractionDistance = 2f;

    [Header("Wall Collision Override")]
    [Tooltip("Enable ONLY on doors whose wall FBX mesh is solid with no doorway hole.")]
    [SerializeField] private bool openDisablesWallCollider = false;

    [Header("Slab Settings (only used when openDisablesWallCollider = true)")]
    [SerializeField] private float leftWidth    = 1.2f;
    [SerializeField] private float leftCentreX  = -1.5f;
    [SerializeField] private float rightWidth   = 1.2f;
    [SerializeField] private float rightCentreX = 1.5f;
    [SerializeField] private float slabHeight   = 2.8f;
    [SerializeField] private float slabDepth    = 0.25f;

    private bool      _isOpen;
    private bool      _isMoving;
    private float     _currentAngle;
    private float     _targetAngle;
    private Transform _playerTransform;

    private void Awake()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size      = new Vector3(1.4f, 2.4f, 0.15f);
        bc.center    = Vector3.zero;
    }

    private void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) _playerTransform = playerGO.transform;

        if (!openDisablesWallCollider) return;

        Transform wallParent = transform.parent;
        if (wallParent == null) return;

        foreach (Transform sibling in wallParent)
        {
            if (sibling == transform) continue;
            if (!sibling.name.EndsWith(".Shape", System.StringComparison.OrdinalIgnoreCase)) continue;
            MeshCollider mc = sibling.GetComponent<MeshCollider>();
            if (mc != null) { mc.enabled = false; break; }
        }

        BuildSlab(wallParent, "_Slab_L", leftCentreX,  leftWidth);
        BuildSlab(wallParent, "_Slab_R", rightCentreX, rightWidth);
    }

    private void Update()
    {
        if (!_isMoving) return;
        _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, swingSpeed * Time.deltaTime);
        float dir = invertHinge ? -1f : 1f;
        transform.localRotation = Quaternion.Euler(0f, _currentAngle * dir, 0f);
        if (Mathf.Approximately(_currentAngle, _targetAngle)) _isMoving = false;
    }

    public string GetActionName()           => _isOpen ? "Close Door" : "Open Door";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    public bool CanInteract()
    {
        if (_playerTransform == null) return true;
        return Vector3.Distance(transform.position, _playerTransform.position) <= maxInteractionDistance;
    }

    public void OnInteract()
    {
        _isOpen      = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f;
        _isMoving    = true;
    }

    private void BuildSlab(Transform parent, string goName, float centreX, float width)
    {
        if (width <= 0f) return;

        // Avoid duplicating slabs on repeated Play-mode entries
        Transform existing = parent.Find(goName);
        if (existing != null) return;

        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(centreX, slabHeight * 0.5f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = false;
        bc.size      = new Vector3(width, slabHeight, slabDepth);
        bc.center    = Vector3.zero;
    }
}
