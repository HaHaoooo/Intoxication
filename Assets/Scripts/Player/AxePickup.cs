using UnityEngine;
using Player;

/// <summary>
/// Placed on the world Axe GameObject.
/// Automatically creates an interaction collider on the visible mesh child at startup —
/// no manual BoxCollider setup required.
/// When the player presses F, the axe disappears from the world,
/// attaches to the player's right hand bone, and enables the AxeWeapon component.
/// </summary>
public class AxePickup : MonoBehaviour, IInteractable
{
    private const string PromptText = "[ F ] Pick up axe";
    private const float AutoColliderRadius = 1.5f;

    [Header("References")]
    [Tooltip("The player GameObject that has an AxeWeapon component.")]
    public GameObject playerObject;

    [Tooltip("The bone transform where the axe will be parented (e.g. hand.R). Auto-detected if empty.")]
    public Transform handBone;

    [Header("Hold Transform")]
    [Tooltip("Local position offset applied to the axe once it is in the hand.")]
    public Vector3 holdLocalPosition = new Vector3(0f, 0f, 0f);

    [Tooltip("Local rotation applied to the axe once it is in the hand.")]
    public Vector3 holdLocalRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("Local scale applied to the axe once it is in the hand (use to resize if needed).")]
    public Vector3 holdLocalScale = new Vector3(1f, 1f, 1f);

    private bool _pickedUp = false;

    private void Awake()
    {
        EnsureInteractionCollider();
    }

    /// <summary>
    /// Adds a SphereCollider on the mesh child (or self as fallback) if no
    /// non-trigger collider already exists anywhere in the hierarchy.
    /// This means you never need to add a BoxCollider manually.
    /// </summary>
    private void EnsureInteractionCollider()
    {
        // If a solid (non-trigger) collider already exists anywhere, keep it
        foreach (Collider existing in GetComponentsInChildren<Collider>(includeInactive: true))
        {
            if (!existing.isTrigger)
                return;
        }

        // Find the mesh renderer child — that's where the visible geometry lives
        Renderer meshRend = GetComponentInChildren<Renderer>(includeInactive: true);
        GameObject target = meshRend != null ? meshRend.gameObject : gameObject;

        SphereCollider sphere = target.AddComponent<SphereCollider>();
        sphere.isTrigger = false;
        sphere.radius = AutoColliderRadius;
        Debug.Log($"[AxePickup] Auto-created SphereCollider (r={AutoColliderRadius}) on '{target.name}'.");
    }

    // --- IInteractable ---

    /// <summary>Returns the interaction prompt shown above the axe.</summary>
    public string GetInteractionPrompt() => PromptText;

    /// <summary>Only interactable once — before the player picks it up.</summary>
    public bool CanInteract() => !_pickedUp;

    /// <summary>Attaches the axe to the player's hand and enables the weapon component.</summary>
    public void OnInteract()
    {
        if (_pickedUp) return;
        _pickedUp = true;

        // Resolve the player if not manually assigned
        if (playerObject == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerObject = pm.gameObject;
        }

        if (playerObject == null)
        {
            Debug.LogWarning("[AxePickup] playerObject not found — cannot attach axe.");
            return;
        }

        // Resolve hand bone if not manually assigned
        Transform attachTo = handBone;
        if (attachTo == null)
        {
            attachTo = FindBoneByName(playerObject.transform, "hand.R");
            if (attachTo == null) attachTo = FindBoneByName(playerObject.transform, "hand_r");
            if (attachTo == null) attachTo = FindBoneByName(playerObject.transform, "RightHand");
            if (attachTo == null)
            {
                Debug.LogWarning("[AxePickup] Right hand bone not found — attaching to player root.");
                attachTo = playerObject.transform;
            }
        }

        // Disable all colliders so the prompt disappears and physics ignore it
        foreach (Collider col in GetComponentsInChildren<Collider>(includeInactive: true))
            col.enabled = false;

        // Parent axe to the hand bone
        transform.SetParent(attachTo, worldPositionStays: false);
        transform.localPosition = holdLocalPosition;
        transform.localEulerAngles = holdLocalRotation;
        transform.localScale = holdLocalScale;

        // Enable the weapon system on the player
        AxeWeapon weapon = playerObject.GetComponent<AxeWeapon>();
        if (weapon == null) weapon = playerObject.AddComponent<AxeWeapon>();
        weapon.Enable();

        Debug.Log("[AxePickup] Axe picked up and attached to hand.");
    }

    // --- Helpers ---

    /// <summary>Recursively searches for a bone by name (case-insensitive).</summary>
    private static Transform FindBoneByName(Transform root, string boneName)
    {
        if (root.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase))
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindBoneByName(child, boneName);
            if (result != null) return result;
        }
        return null;
    }
}
