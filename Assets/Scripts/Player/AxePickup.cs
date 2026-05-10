using System.Collections;
using UnityEngine;

/// <summary>
/// Placed on the world Axe GameObject.
/// Now inherits from Pickupable so it can be handled by PlayerPickupController.
/// </summary>
public class AxePickup : Pickupable
{
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
        // 1. Safety: If handBone is not assigned, try to find a suitable one on the player
        if (handBone == null)
        {
            Debug.LogWarning($"[AxePickup] handBone was not assigned on {name}. Searching player hierarchy...");
            
            // Try to find HoldPoint first, as requested
            handBone = FindBoneByName(player.transform, "HoldPoint");
            
            if (handBone == null)
            {
                // Try to find a right hand bone specifically, or just any 'hand'
                handBone = FindBoneByName(player.transform, "hand.r") ?? FindBoneByName(player.transform, "hand");
            }
            
            if (handBone == null)
            {
                Debug.LogError("[AxePickup] Could not find a 'hand' bone or 'HoldPoint'. Defaulting to player root.");
                handBone = player.transform;
            }
        }

        // 1b. Extra safety: Ensure Rigidbody is kinematic and has no velocity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            if (rb.linearVelocity.sqrMagnitude > 0 || rb.angularVelocity.sqrMagnitude > 0)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 2. Parent axe to the hand bone
        transform.SetParent(handBone, worldPositionStays: false);
        
        transform.localPosition = holdLocalPosition;
        transform.localEulerAngles = holdLocalRotation;
        
        // 3. Force the scale
        transform.localScale = holdLocalScale;

        string boneName = handBone != null ? handBone.name : "null";
        Vector3 boneScale = handBone != null ? handBone.localScale : Vector3.one;
        Debug.Log($"[AxePickup] Applied hold scale: {holdLocalScale}. Attached to: {boneName} (Scale: {boneScale})");

        // 4. Thoroughly remove "strong light" and any emission
        CleanupAllLighting();

        // 5. Enable the weapon system on the player
        AxeWeapon weapon = player.GetComponent<AxeWeapon>();
        if (weapon == null) weapon = player.AddComponent<AxeWeapon>();
        weapon.Enable();

        // 6. Update objective directly
        ObjectiveUI.Instance?.Set(ObjectiveState.UseAxe);
        
        Debug.Log("[AxePickup] Axe picked up and attached to hand.");
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
