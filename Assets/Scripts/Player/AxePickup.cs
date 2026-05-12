using System.Collections;
using UnityEngine;

/// <summary>
/// Placed on the world Axe GameObject.
/// Now inherits from Pickupable so it can be handled by PlayerPickupController.
/// </summary>
public class AxePickup : Pickupable, ILockable
{
    /// <summary>Requires the little girl sequence to be done first.</summary>
    public StorylineManager.Beat RequiredBeat => StorylineManager.Beat.LittleGirlDone;

    protected override bool IsStorylineLocked =>
        StorylineManager.Instance != null && !StorylineManager.Instance.IsUnlocked(RequiredBeat);
    [Header("Axe Settings")]
    [Tooltip("The bone transform where the axe will be parented (e.g. hand.R).")]
    public Transform handBone;

    [Header("Hold Transform")]
    [Tooltip("Local position offset applied to the axe once it is in the hand.")]
    public Vector3 holdLocalPosition = new Vector3(0f, 0f, 0f);

    [Tooltip("Local rotation applied to the axe once it is in the hand.")]
    public Vector3 holdLocalRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("Local scale applied to the axe once it is in the hand (use to resize if needed).")]
    public Vector3 holdLocalScale = new Vector3(0.2f, 0.2f, 0.2f);

    protected override void Awake()
    {
        base.Awake();
        // If the user hasn't changed it from 1,1,1, use the current scale
        if (holdLocalScale == Vector3.one)
        {
            holdLocalScale = transform.localScale;
        }
    }

    /// <summary>
    /// Special pickup logic for the axe, called by PlayerPickupController.
    /// Parents the axe to the player's hand and enables AxeWeapon.
    /// </summary>
    public void OnAxePickedUp(GameObject player)
    {
        // 1. Safety: Find components and configure physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable colliders
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        // 2. We no longer parent to handBone here; PlayerPickupController handles positioning via SmoothFollowHoldPoint.
        Debug.Log("[AxePickup] Axe picked up. Following holdPoint via PlayerPickupController.");

        // 3. Thoroughly remove "strong light" and any emission
        CleanupAllLighting();

        // 4. Enable the weapon system on the player
        AxeWeapon weapon = player.GetComponent<AxeWeapon>();
        if (weapon == null) weapon = player.AddComponent<AxeWeapon>();
        weapon.Enable();

        // 5. Update objective and advance storyline
        ObjectiveUI.Instance?.Set(ObjectiveState.UseAxe);
        StorylineManager.Instance?.Advance(StorylineManager.Beat.AxeDone);
    }

    private Transform FindBoneByName(Transform parent, string nameSnippet)
    {
        if (parent.name.ToLower().Contains(nameSnippet.ToLower())) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindBoneByName(child, nameSnippet);
            if (found != null) return found;
        }
        return null;
    }

    private void CleanupAllLighting()
    {
        Light[] lights = GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
            l.enabled = false;
            Destroy(l);
        }

        ReflectionProbe[] probes = GetComponentsInChildren<ReflectionProbe>(true);
        foreach (ReflectionProbe p in probes)
        {
            p.enabled = false;
            Destroy(p);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            MaterialPropertyBlock prop = new MaterialPropertyBlock();
            r.GetPropertyBlock(prop);
            prop.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(prop);

            foreach (Material m in r.sharedMaterials)
            {
                if (m != null)
                {
                    m.DisableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor"))
                        m.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
