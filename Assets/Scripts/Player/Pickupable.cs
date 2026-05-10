using UnityEngine;

/// <summary>
/// Marks a GameObject as pickupable. Manages highlight, physics state, and collider toggling.
/// Requires a Rigidbody on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Pickupable : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] protected Color highlightEmissionColor = new Color(0.15f, 0.15f, 0.02f);

    [Header("Interaction")]
    [SerializeField] private bool isDrinkable;


    /// <summary>Whether this item can be consumed by the player (e.g. wine bottles).</summary>
    public bool IsDrinkable => isDrinkable;

    /// <summary>Whether this object is currently being held by the player.</summary>
    public bool IsHeld { get; private set; }

    /// <summary>Offset from transform.position to the visual center (in local space).</summary>
    public Vector3 VisualCenterOffset { get; private set; }

    private Renderer[] _renderers;
    private Rigidbody _rigidbody;
    private Collider[] _colliders;
    private int _originalLayer;
    private bool _isHighlighted;
    private bool _hasInstancedMaterials;
    private MaterialPropertyBlock _propertyBlock;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int CullId = Shader.PropertyToID("_Cull");
    private const int IGNORE_RAYCAST_LAYER = 2;

    protected virtual void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _rigidbody = GetComponent<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>();
        _originalLayer = gameObject.layer;
        _propertyBlock = new MaterialPropertyBlock();

        CalculateVisualCenterOffset();
    }


    /// <summary>Calculates the offset from transform origin to the combined visual center.</summary>
    private void CalculateVisualCenterOffset()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            VisualCenterOffset = Vector3.zero;
            return;
        }

        bool initialized = false;
        Bounds combined = new Bounds();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            
            Bounds b = _renderers[i].bounds;
            if (!float.IsFinite(b.center.x) || !float.IsFinite(b.size.x)) continue;

            if (!initialized)
            {
                combined = b;
                initialized = true;
            }
            else
            {
                combined.Encapsulate(b);
            }
        }

        if (!initialized)
        {
            VisualCenterOffset = Vector3.zero;
            return;
        }

        // Safety check for scale to avoid NaN in InverseTransformPoint
        Vector3 lossyScale = transform.lossyScale;
        if (Mathf.Abs(lossyScale.x) < 0.0001f || Mathf.Abs(lossyScale.y) < 0.0001f || Mathf.Abs(lossyScale.z) < 0.0001f)
        {
            VisualCenterOffset = Vector3.zero;
            return;
        }

        Vector3 offset = transform.InverseTransformPoint(combined.center);
        if (float.IsFinite(offset.x) && float.IsFinite(offset.y) && float.IsFinite(offset.z))
        {
            VisualCenterOffset = offset;
        }
        else
        {
            VisualCenterOffset = Vector3.zero;
        }
    }

    /// <summary>Returns the world-space visual center of this object.</summary>
    public Vector3 GetVisualCenterWorld()
    {
        return transform.TransformPoint(VisualCenterOffset);
    }


    /// <summary>Toggles emission highlight on all child renderers.
    /// Uses MaterialPropertyBlock for color so no material instances are created per-call.
    /// Material instances are created once on first highlight, then reused thereafter.</summary>
    public void SetHighlight(bool enabled)
    {
        if (_isHighlighted == enabled) return;
        _isHighlighted = enabled;

        Color emissionColor = enabled ? highlightEmissionColor : Color.black;

        // Create instanced materials only once to toggle the _EMISSION keyword.
        // r.materials returns NEW copies each call, so we trigger it once and let
        // Unity cache the instances on the renderer for subsequent accesses via sharedMaterials.
        if (!_hasInstancedMaterials && _renderers != null)
        {
            _hasInstancedMaterials = true;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                // Access r.materials once to force Unity to create per-renderer instances.
                // After this, r.sharedMaterials returns the same instances without re-allocating.
                Material[] mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat != null && mat.HasProperty(EmissionColorId))
                    {
                        // Initially enable if we're highlighting, or keep it off
                        if (enabled) mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null) continue;

            // Toggle keyword on material instances if they exist
            if (_hasInstancedMaterials)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m != null && m.HasProperty(EmissionColorId))
                    {
                        if (enabled) m.EnableKeyword("_EMISSION");
                        else m.DisableKeyword("_EMISSION");
                    }
                }
            }

            // Set emission color via property block — zero allocation.
            _propertyBlock.Clear();
            r.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, emissionColor);
            r.SetPropertyBlock(_propertyBlock);
        }
    }

    /// <summary>Transitions this object into the "held" state: freezes physics, disables colliders, changes layer.</summary>
    public void OnPickedUp()
    {
        IsHeld = true;
        _originalLayer = gameObject.layer;

        // Ensure highlight is off when picked up
        SetHighlight(false);

        // Freeze physics (order matters: clear velocity BEFORE setting kinematic)
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        // Disable colliders so the item doesn't block the player
        foreach (Collider c in _colliders)
        {
            if (c != null) c.enabled = false;
        }

        // Move to Ignore Raycast layer so detection skips this item
        ApplyLayerRecursively(gameObject, IGNORE_RAYCAST_LAYER);

        Debug.Log($"[Pickup] '{name}' picked up.");
    }

    /// <summary>Transitions this object back to the "released" state: restores physics, colliders, and layer.</summary>
    public void OnReleased()
    {
        IsHeld = false;

        // Restore layer
        ApplyLayerRecursively(gameObject, _originalLayer);

        // Re-enable colliders
        foreach (Collider c in _colliders)
        {
            if (c != null) c.enabled = true;
        }

        // Unfreeze physics
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
        }

        Debug.Log($"[Pickup] '{name}' released.");
    }

    /// <summary>Applies a force impulse (used for throwing).</summary>
    public void AddThrowForce(Vector3 force)
    {
        if (_rigidbody != null)
        {
            _rigidbody.AddForce(force, ForceMode.Impulse);
        }
    }

    private static void ApplyLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
    }
}