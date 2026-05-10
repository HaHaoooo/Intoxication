using UnityEngine;

/// <summary>
/// Provides simple procedural animation for the 6-legged monster model.
/// Bobs the body and oscillates the legs based on movement speed.
/// </summary>
public class MonsterProceduralAnimator : MonoBehaviour
{
    [Header("Settings")]
    public float bobAmount = 0.08f;
    public float bobSpeed = 10f;
    public float legOscillationAngle = 35f;
    public float legOscillationSpeed = 15f;

    [Header("Bones")]
    public Transform bodyRoot; // Usually C_hip
    public Transform[] leftLegs;
    public Transform[] rightLegs;

    private float _speed;
    private Vector3 _baseLocalPos;
    private float _time;
    private bool _initialized = false;

    private void Start()
    {
        InitializeBones();
    }

    private void InitializeBones()
    {
        if (_initialized) return;

        // Auto-find bones if not assigned
        if (bodyRoot == null) bodyRoot = FindBoneByPattern(transform, "C_hip");
        if (bodyRoot == null) bodyRoot = FindBoneByPattern(transform, "Hips");

        if (leftLegs == null || leftLegs.Length == 0)
            leftLegs = FindBonesByPattern(transform, "L_leg", "_1");
        
        if (rightLegs == null || rightLegs.Length == 0)
            rightLegs = FindBonesByPattern(transform, "R_leg", "_1");

        if (bodyRoot != null)
        {
            _baseLocalPos = bodyRoot.localPosition;
            _initialized = true;
            Debug.Log($"[MonsterAnim] Initialized on {name}. Body: {bodyRoot.name}, L-Legs: {leftLegs.Length}, R-Legs: {rightLegs.Length}");
        }
        else
        {
            Debug.LogWarning($"[MonsterAnim] Could not find body root on {name}. Procedural animation may fail.");
        }
    }

    public void UpdateSpeed(float speed)
    {
        _speed = speed;
    }

    private void Update()
    {
        if (!_initialized) InitializeBones();
        if (!_initialized) return;

        // Animate idle even when not moving
        float isMoving = (_speed > 0.1f) ? 1f : 0f;
        float animMultiplier = Mathf.Max(isMoving, 0.4f);
        float actualBobSpeed = isMoving > 0 ? bobSpeed : bobSpeed * 0.4f;

        float freqScale = isMoving > 0 ? (_speed / 5f) : 0.2f;
        _time += Time.deltaTime * legOscillationSpeed * freqScale;

        // Body Bob & Float
        if (bodyRoot != null)
        {
            float bob = Mathf.Sin(Time.time * actualBobSpeed) * bobAmount * animMultiplier;
            float hover = Mathf.Sin(Time.time * 2.5f) * 0.03f; // Ghostly hover
            bodyRoot.localPosition = _baseLocalPos + Vector3.up * (bob + hover);
        }

        // Leg Oscillation (Skittering)
        AnimateLegs(leftLegs, 0f, animMultiplier);
        AnimateLegs(rightLegs, Mathf.PI, animMultiplier); 
    }

    private void AnimateLegs(Transform[] legs, float offset, float multiplier)
    {
        if (legs == null) return;
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null) continue;
            // Alternate oscillation for each leg in the row
            float individualOffset = i * (Mathf.PI / 2.5f);
            float angle = Mathf.Sin(_time + offset + individualOffset) * legOscillationAngle * multiplier;
            legs[i].localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    private Transform[] FindBonesByPattern(Transform root, string prefix, string suffix)
    {
        var list = new System.Collections.Generic.List<Transform>();
        SearchRecursive(root, prefix, suffix, list);
        return list.ToArray();
    }

    private void SearchRecursive(Transform t, string prefix, string suffix, System.Collections.Generic.List<Transform> results)
    {
        if (t.name.Contains(prefix) && t.name.EndsWith(suffix))
            results.Add(t);
        
        foreach (Transform child in t)
            SearchRecursive(child, prefix, suffix, results);
    }

    private Transform FindBoneByPattern(Transform root, string name)
    {
        if (root.name.Contains(name)) return root;
        foreach (Transform child in root)
        {
            Transform found = FindBoneByPattern(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
