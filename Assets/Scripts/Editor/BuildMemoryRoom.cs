using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot editor utility that populates the wife/child memory room in Level 3
/// and attaches ExaminableClue + MemoryRoomCard to each story object so the
/// player can read small descriptions about the memories in the room.
///
/// TARGET ROOM: Living Room (top-right room in the house floor plan)
///   Approximate world bounds: X -0.2 to -3.0, Z 58.5 to 63.5
///   Room center: X≈-1.5, Z≈61
///
/// Run via Tools > Level3 > Build Memory Room       — places prop objects in Living Room
/// Run via Tools > Level3 > Move Story Objects      — moves Bedroom2_* and FinalRoom_* here
/// Run via Tools > Level3 > Attach Memory Room Clues — attaches ExaminableClue + cards
/// </summary>
public static class BuildMemoryRoom
{
    // ── Story object descriptors ──────────────────────────────────────────────

    private struct ClueData
    {
        public string objectName;
        public string cardTitle;
        public string clueDescription;
        public string monologueLine;
    }

    private static readonly ClueData[] ExistingObjectClues = new[]
    {
        new ClueData
        {
            objectName     = "Bedroom2_FallenToy",
            cardTitle      = "Fallen Toy",
            clueDescription = "A stuffed bear lies face-down on the floor. Its button eye is cracked.",
            monologueLine  = "She used to sleep with this every night."
        },
        new ClueData
        {
            objectName     = "Bedroom2_CrackedFrame",
            cardTitle      = "Cracked Picture Frame",
            clueDescription = "A family photo. The glass is broken. The faces still smile.",
            monologueLine  = "I did that. I broke it."
        },
        new ClueData
        {
            objectName     = "Bedroom2_ChildDrawing",
            cardTitle      = "Child's Drawing",
            clueDescription = "A crayon drawing of three figures holding hands. Labeled: 'Daddy, Mommy, Me.'",
            monologueLine  = "She drew this before it all went wrong."
        },
        new ClueData
        {
            objectName     = "Bedroom2_Bed_Frame",
            cardTitle      = "Small Bed",
            clueDescription = "A child's bed. Too small. Too empty. Made up as though she might return.",
            monologueLine  = "She never slept here again after that night."
        },
    };

    private static readonly ClueData[] BuiltObjectClues = new[]
    {
        new ClueData
        {
            objectName     = "MemRoom_FamilyPhoto_Frame",
            cardTitle      = "Family Photo",
            clueDescription = "A large photograph. A husband, a wife, a daughter. Before everything.",
            monologueLine  = "We looked happy."
        },
        new ClueData
        {
            objectName     = "MemRoom_WeddingPhoto_Frame",
            cardTitle      = "Wedding Photo",
            clueDescription = "A wedding day portrait, slightly tilted. Her dress is white. His hands are steady.",
            monologueLine  = "I made a promise that day."
        },
        new ClueData
        {
            objectName     = "MemRoom_MissingPoster_Frame",
            cardTitle      = "Missing Poster",
            clueDescription = "A printed flyer. A child's face. A phone number no one answered.",
            monologueLine  = "She was never missing. I just could not face where she had gone."
        },
        new ClueData
        {
            objectName     = "MemRoom_SmallChair_Seat",
            cardTitle      = "Overturned Chair",
            clueDescription = "A small teal chair, tipped on its side. A child's chair, left where it fell.",
            monologueLine  = "I knocked it over that night. I never picked it up."
        },
        new ClueData
        {
            objectName     = "MemRoom_PlushToy2",
            cardTitle      = "Plush Toy",
            clueDescription = "A small round toy, dusty and forgotten near the chair.",
            monologueLine  = "She used to carry two of them everywhere."
        },
        new ClueData
        {
            objectName     = "MemRoom_Note_1",
            cardTitle      = "Floor Notes",
            clueDescription = "Pages covered in her handwriting. Letters she wrote but never sent.",
            monologueLine  = "I found them after. I could not read them then."
        },
        new ClueData
        {
            objectName     = "MemRoom_Letter_Frame",
            cardTitle      = "Framed Letter",
            clueDescription = "A letter mounted on the wall. The handwriting belongs to his wife. The last one.",
            monologueLine  = "She wrote: 'Come back to us. Not like this.'"
        },
        new ClueData
        {
            objectName     = "FinalRoom_Bottle",
            cardTitle      = "Final Bottle",
            clueDescription = "A full bottle. The last one. It sits here waiting, as it always has.",
            monologueLine  = "If I drink this, there is no coming back."
        },
    };
    private const string LitMatPath        = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat";
    private const string ChildDrawingPath  = "Assets/Materials/ChildDrawingMat.mat";
    private const string MissingPosterPath = "Assets/Materials/MissingPosterMat.mat";

    // ── Color palette ───────────────────────────────────────────────────────────
    private static readonly Color ColorDarkWood    = new Color(0.22f, 0.14f, 0.07f, 1f);
    private static readonly Color ColorSepia       = new Color(0.72f, 0.62f, 0.48f, 1f);
    private static readonly Color ColorIvory       = new Color(0.88f, 0.84f, 0.77f, 1f);
    private static readonly Color ColorOldPaper    = new Color(0.90f, 0.85f, 0.68f, 1f);
    private static readonly Color ColorToyPink     = new Color(0.82f, 0.60f, 0.62f, 1f);
    private static readonly Color ColorTealChair   = new Color(0.44f, 0.60f, 0.58f, 1f);
    private static readonly Color ColorCandleWax   = new Color(0.93f, 0.88f, 0.75f, 1f);
    private static readonly Color ColorFlame       = new Color(1.00f, 0.65f, 0.10f, 1f);
    private static readonly Color ColorLightWood   = new Color(0.55f, 0.38f, 0.22f, 1f);
    private static readonly Color ColorRustyNail   = new Color(0.40f, 0.28f, 0.16f, 1f);
    private static readonly Color ColorFloorNote   = new Color(0.92f, 0.87f, 0.72f, 1f);
    private static readonly Color ColorSmudgedBlue = new Color(0.52f, 0.62f, 0.78f, 1f);

    [MenuItem("Tools/Level3/Build Memory Room")]
    public static void Build()
    {
        Material litMat       = AssetDatabase.LoadAssetAtPath<Material>(LitMatPath);
        Material childMat     = AssetDatabase.LoadAssetAtPath<Material>(ChildDrawingPath);
        Material posterMat    = AssetDatabase.LoadAssetAtPath<Material>(MissingPosterPath);

        if (litMat == null)
        {
            Debug.LogError("[BuildMemoryRoom] URP Lit material not found. Aborting.");
            return;
        }

        // ── MATERIALS ─────────────────────────────────────────────────────────────
        Material darkWood    = MakeMat("MemRoom_DarkWood",    ColorDarkWood,  0.30f);
        Material sepia       = MakeMat("MemRoom_Sepia",       ColorSepia,     0.05f);
        Material ivory       = MakeMat("MemRoom_Ivory",       ColorIvory,     0.05f);
        Material oldPaper    = MakeMat("MemRoom_OldPaper",    ColorOldPaper,  0.00f);
        Material toyPink     = MakeMat("MemRoom_ToyPink",     ColorToyPink,   0.10f);
        Material tealChair   = MakeMat("MemRoom_TealChair",   ColorTealChair, 0.15f);
        Material candleWax   = MakeMat("MemRoom_CandleWax",   ColorCandleWax, 0.20f);
        Material candleFlame = MakeMat("MemRoom_CandleFlame", ColorFlame,     0.50f,
                                       emissive: new Color(1.8f, 0.8f, 0.05f, 1f));
        Material lightWood   = MakeMat("MemRoom_LightWood",   ColorLightWood, 0.25f);
        Material rustyNail   = MakeMat("MemRoom_Rusty",       ColorRustyNail, 0.10f);
        Material floorNote   = MakeMat("MemRoom_FloorNote",   ColorFloorNote, 0.00f);
        Material smudgedBlue = MakeMat("MemRoom_SmudgedBlue", ColorSmudgedBlue, 0.05f);

        AssetDatabase.SaveAssets();

        // ── LIVING ROOM (top-right room) coordinate center: X≈-1.5, Z≈60.5 ───────
        // Room approximate bounds: X -0.2 to -3.0, Z 58.5 to 63.5
        // South wall of room faces the hallway at Z≈58.5
        // North wall at Z≈63.5, East wall at X≈-0.2, West wall at X≈-3.0

        // ── 1. FAMILY PHOTO — south wall (faces into room, north-facing) ─────────
        BuildBox("MemRoom_FamilyPhoto_Frame",
            pos:   new Vector3(-1.5f, 2.56f, 58.6f),
            rot:   new Vector3(0, 0, 0),
            scale: new Vector3(0.52f, 0.38f, 0.05f),
            mat:   darkWood, addCollider: false);
        BuildBox("MemRoom_FamilyPhoto_Surface",
            pos:   new Vector3(-1.5f, 2.56f, 58.62f),
            rot:   new Vector3(0, 0, 0),
            scale: new Vector3(0.44f, 0.30f, 0.02f),
            mat:   sepia, addCollider: false);

        // ── 2. WEDDING PHOTO — south wall, slightly left, tilted ─────────────────
        BuildBox("MemRoom_WeddingPhoto_Frame",
            pos:   new Vector3(-2.1f, 2.75f, 58.6f),
            rot:   new Vector3(0, 0, 3),
            scale: new Vector3(0.28f, 0.22f, 0.05f),
            mat:   darkWood, addCollider: false);
        BuildBox("MemRoom_WeddingPhoto_Surface",
            pos:   new Vector3(-2.1f, 2.75f, 58.62f),
            rot:   new Vector3(0, 0, 3),
            scale: new Vector3(0.22f, 0.17f, 0.02f),
            mat:   ivory, addCollider: false);

        // ── 3. CHILD DRAWING SHEET — south wall, pinned low ─────────────────────
        BuildBox("MemRoom_ChildSheet2",
            pos:   new Vector3(-0.9f, 2.28f, 58.6f),
            rot:   new Vector3(0, 0, -5),
            scale: new Vector3(0.18f, 0.24f, 0.01f),
            mat:   childMat ?? oldPaper, addCollider: false);

        // ── 4. MISSING-CHILD POSTER — west wall (faces east, X=-3 side) ──────────
        BuildBox("MemRoom_MissingPoster_Frame",
            pos:   new Vector3(-2.95f, 2.45f, 61.0f),
            rot:   new Vector3(0, 90, 0),
            scale: new Vector3(0.32f, 0.44f, 0.05f),
            mat:   darkWood, addCollider: false);
        BuildBox("MemRoom_MissingPoster_Surface",
            pos:   new Vector3(-2.93f, 2.45f, 61.0f),
            rot:   new Vector3(0, 90, 0),
            scale: new Vector3(0.26f, 0.38f, 0.02f),
            mat:   posterMat ?? ivory, addCollider: false);

        // ── 5. BEDSIDE NIGHTSTAND — north-east corner ────────────────────────────
        BuildBox("MemRoom_Nightstand_Top",
            pos:   new Vector3(-0.55f, 1.88f, 62.8f),
            rot:   Vector3.zero,
            scale: new Vector3(0.42f, 0.05f, 0.38f),
            mat:   lightWood);
        BuildBox("MemRoom_Nightstand_Leg_FL",
            pos:   new Vector3(-0.37f, 1.60f, 62.63f),
            rot:   Vector3.zero, scale: new Vector3(0.04f, 0.56f, 0.04f), mat: lightWood);
        BuildBox("MemRoom_Nightstand_Leg_FR",
            pos:   new Vector3(-0.37f, 1.60f, 62.98f),
            rot:   Vector3.zero, scale: new Vector3(0.04f, 0.56f, 0.04f), mat: lightWood);
        BuildBox("MemRoom_Nightstand_Leg_BL",
            pos:   new Vector3(-0.73f, 1.60f, 62.63f),
            rot:   Vector3.zero, scale: new Vector3(0.04f, 0.56f, 0.04f), mat: lightWood);
        BuildBox("MemRoom_Nightstand_Leg_BR",
            pos:   new Vector3(-0.73f, 1.60f, 62.98f),
            rot:   Vector3.zero, scale: new Vector3(0.04f, 0.56f, 0.04f), mat: lightWood);

        // ── 6. CANDLE on nightstand ──────────────────────────────────────────────
        BuildBox("MemRoom_Candle_Body",
            pos:   new Vector3(-0.55f, 1.97f, 62.72f),
            rot:   Vector3.zero, scale: new Vector3(0.045f, 0.14f, 0.045f), mat: candleWax);
        BuildBox("MemRoom_Candle_Flame",
            pos:   new Vector3(-0.55f, 2.09f, 62.72f),
            rot:   Vector3.zero, scale: new Vector3(0.018f, 0.028f, 0.018f),
            mat:   candleFlame, addCollider: false);
        BuildPointLight("MemRoom_CandleLight",
            pos:   new Vector3(-0.55f, 2.15f, 62.72f),
            color: new Color(1f, 0.72f, 0.28f, 1f),
            intensity: 0.55f, range: 3.8f);

        // ── 7. CHILD'S SMALL CHAIR (overturned) — middle of room ─────────────────
        BuildBox("MemRoom_SmallChair_Seat",
            pos:   new Vector3(-1.8f, 1.62f, 60.5f),
            rot:   new Vector3(0, 25, 78),
            scale: new Vector3(0.30f, 0.05f, 0.28f),
            mat:   tealChair);
        BuildBox("MemRoom_SmallChair_Back",
            pos:   new Vector3(-1.7f, 1.68f, 60.4f),
            rot:   new Vector3(0, 25, 78),
            scale: new Vector3(0.28f, 0.04f, 0.22f),
            mat:   tealChair, addCollider: false);

        // ── 8. PLUSH TOY near the chair ──────────────────────────────────────────
        BuildSphere("MemRoom_PlushToy2",
            pos:   new Vector3(-1.6f, 1.605f, 60.8f),
            rot:   new Vector3(0, -40, 0),
            scale: new Vector3(0.09f, 0.09f, 0.09f),
            mat:   toyPink);

        // ── 9. SCATTERED PAPER NOTES on the floor ────────────────────────────────
        BuildBox("MemRoom_Note_1",
            pos:   new Vector3(-1.4f, 1.625f, 60.2f),
            rot:   new Vector3(0, 18, 0),
            scale: new Vector3(0.18f, 0.004f, 0.14f),
            mat:   floorNote, addCollider: false);
        BuildBox("MemRoom_Note_2",
            pos:   new Vector3(-1.1f, 1.626f, 60.5f),
            rot:   new Vector3(0, -12, 0),
            scale: new Vector3(0.15f, 0.004f, 0.11f),
            mat:   floorNote, addCollider: false);
        BuildBox("MemRoom_Note_3",
            pos:   new Vector3(-2.0f, 1.626f, 60.7f),
            rot:   new Vector3(0, 5, 0),
            scale: new Vector3(0.14f, 0.004f, 0.10f),
            mat:   smudgedBlue, addCollider: false);

        // ── 10. FRAMED LETTER — east wall (faces west into room) ─────────────────
        BuildBox("MemRoom_Letter_Frame",
            pos:   new Vector3(-0.28f, 2.35f, 61.0f),
            rot:   new Vector3(0, -90, 0),
            scale: new Vector3(0.28f, 0.20f, 0.05f),
            mat:   rustyNail, addCollider: false);
        BuildBox("MemRoom_Letter_Surface",
            pos:   new Vector3(-0.30f, 2.35f, 61.0f),
            rot:   new Vector3(0, -90, 0),
            scale: new Vector3(0.22f, 0.16f, 0.02f),
            mat:   oldPaper, addCollider: false);

        // ── 11. CHILD'S SHOE on the floor ────────────────────────────────────────
        BuildBox("MemRoom_ChildShoe",
            pos:   new Vector3(-2.5f, 1.616f, 61.5f),
            rot:   new Vector3(0, -30, 0),
            scale: new Vector3(0.08f, 0.048f, 0.16f),
            mat:   tealChair, addCollider: false);

        EditorUtility.SetDirty(UnityEngine.Object.FindAnyObjectByType<UnityEngine.GameObject>());
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
        Debug.Log("[BuildMemoryRoom] Memory room built successfully. Save the scene.");
    }

    /// <summary>
    /// Moves all Bedroom2_* and FinalRoom_* story objects into the Living Room
    /// (top-right room: X≈-0.2 to -3.0, Z≈58.5 to 63.5).
    /// Safe to run multiple times — only moves objects that exist.
    /// </summary>
    [MenuItem("Tools/Level3/Move Story Objects to Living Room")]
    public static void MoveStoryObjects()
    {
        // Bed set — move to north wall of Living Room
        MoveObject("Bedroom2_Bed_Frame",    new Vector3(-1.5f,  1.65f, 62.8f), new Vector3(0, 90, 0));
        MoveObject("Bedroom2_Bed_Mattress", new Vector3(-1.5f,  1.72f, 62.8f), new Vector3(0, 90, 0));
        MoveObject("Bedroom2_Bed_Headboard",new Vector3(-0.65f, 2.00f, 62.8f), new Vector3(0, 90, 0));

        // Floor objects — scattered naturally in the room
        MoveObject("Bedroom2_FallenToy",    new Vector3(-2.3f,  1.60f, 60.0f), new Vector3(0, 35, 0));
        MoveObject("Bedroom2_CrackedFrame", new Vector3(-2.95f, 2.70f, 61.5f), new Vector3(0, 90, 0));
        MoveObject("Bedroom2_ChildDrawing", new Vector3(-2.95f, 2.30f, 61.5f), new Vector3(0, 90, 0));

        // Light
        MoveObject("BedroomLight",          new Vector3(-1.5f,  3.62f, 61.0f), Vector3.zero);
        MoveObject("Bedroom2_DimLight",     new Vector3(-1.5f,  3.62f, 59.5f), Vector3.zero);

        // NPC
        MoveObject("NPC_LittleGirl_Hallway",new Vector3(-1.5f,  1.52f, 62.0f), new Vector3(0, 180, 0));

        // FinalRoom table + bottle — stay in this room as the final choice
        MoveObject("FinalRoom_Table",           new Vector3(-1.5f, 1.92f, 60.0f), Vector3.zero);
        MoveObject("FinalRoom_Table_Leg_FL",    new Vector3(-1.05f,1.72f, 60.3f), Vector3.zero);
        MoveObject("FinalRoom_Table_Leg_FR",    new Vector3(-1.05f,1.72f, 59.7f), Vector3.zero);
        MoveObject("FinalRoom_Table_Leg_BL",    new Vector3(-1.95f,1.72f, 60.3f), Vector3.zero);
        MoveObject("FinalRoom_Table_Leg_BR",    new Vector3(-1.95f,1.72f, 59.7f), Vector3.zero);
        MoveObject("FinalRoom_Bottle",          new Vector3(-1.5f, 2.12f, 60.0f), Vector3.zero);
        MoveObject("FinalRoom_InterrogationLight", new Vector3(-1.5f, 4.12f, 60.0f), new Vector3(90, 0, 0));

        // Final room NPCs — keep them in the living room
        MoveObject("NPC_Bartender_FinalRoom",   new Vector3(-0.8f, 1.52f, 60.5f), new Vector3(0, 200, 0));
        MoveObject("NPC_HomelessMan_FinalRoom",  new Vector3(-2.2f, 1.52f, 60.5f), new Vector3(0, 160, 0));
        MoveObject("NPC_LittleGirl_FinalRoom",   new Vector3(-1.5f, 1.52f, 61.5f), new Vector3(0, 200, 0));

        // Move the triggers to the Living Room entrance and interior
        MoveObject("BedroomTrigger",    new Vector3(-1.5f, 2.2f, 59.5f), Vector3.zero);
        MoveObject("FinalRoomTrigger",  new Vector3(-1.5f, 2.2f, 60.5f), Vector3.zero);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
        Debug.Log("[BuildMemoryRoom] Story objects moved to Living Room. Save the scene.");
    }

    private static void MoveObject(string name, Vector3 pos, Vector3 eulerRot)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"[BuildMemoryRoom] Not found, skipping move: {name}");
            return;
        }
        Undo.RecordObject(go.transform, $"Move {name} to Living Room");
        go.transform.position    = pos;
        go.transform.eulerAngles = eulerRot;
        EditorUtility.SetDirty(go);
        Debug.Log($"[BuildMemoryRoom] Moved {name} to {pos}");
    }

    /// <summary>
    /// Attaches ExaminableClue and MemoryRoomCard to existing story objects in the scene.
    /// Safe to run multiple times — skips objects that already have ExaminableClue.
    /// </summary>
    [MenuItem("Tools/Level3/Attach Memory Room Clues")]
    public static void AttachClues()
    {
        int attached = 0;

        foreach (ClueData clue in ExistingObjectClues)
            attached += AttachClueToObject(clue);

        foreach (ClueData clue in BuiltObjectClues)
            attached += AttachClueToObject(clue);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
        Debug.Log($"[BuildMemoryRoom] Attached clues to {attached} objects. Save the scene.");
    }

    private static int AttachClueToObject(ClueData clue)
    {
        GameObject go = GameObject.Find(clue.objectName);
        if (go == null)
        {
            Debug.LogWarning($"[BuildMemoryRoom] Object not found, skipping clue: {clue.objectName}");
            return 0;
        }

        // Skip if already has ExaminableClue
        if (go.GetComponent<ExaminableClue>() != null)
        {
            Debug.Log($"[BuildMemoryRoom] Already has ExaminableClue, skipping: {clue.objectName}");
            return 0;
        }

        ExaminableClue ec = go.AddComponent<ExaminableClue>();
        // Use SerializedObject so the private serialized fields are set correctly
        SerializedObject so = new SerializedObject(ec);
        so.FindProperty("clueDescription").stringValue = clue.clueDescription;
        so.FindProperty("monologueLine").stringValue    = clue.monologueLine;
        so.FindProperty("monologueSpeaker").stringValue = "You";
        so.FindProperty("oneTimeOnly").boolValue        = false;
        so.ApplyModifiedProperties();

        MemoryRoomCard card = go.AddComponent<MemoryRoomCard>();
        SerializedObject soCard = new SerializedObject(card);
        soCard.FindProperty("cardTitle").stringValue = clue.cardTitle;
        soCard.ApplyModifiedProperties();

        EditorUtility.SetDirty(go);
        Debug.Log($"[BuildMemoryRoom] Attached clue to: {clue.objectName}");
        return 1;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject BuildBox(string name, Vector3 pos, Vector3 rot, Vector3 scale,
                                       Material mat, bool addCollider = true)
    {
        // Reuse if already exists
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"[BuildMemoryRoom] Already exists, skipping: {name}");
            return existing;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position    = pos;
        go.transform.eulerAngles = rot;
        go.transform.localScale  = scale;

        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (!addCollider && bc != null) UnityEngine.Object.DestroyImmediate(bc);

        return go;
    }

    private static GameObject BuildSphere(string name, Vector3 pos, Vector3 rot, Vector3 scale,
                                          Material mat)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"[BuildMemoryRoom] Already exists, skipping: {name}");
            return existing;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position    = pos;
        go.transform.eulerAngles = rot;
        go.transform.localScale  = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject BuildPointLight(string name, Vector3 pos,
                                               Color color, float intensity, float range)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = color;
        l.intensity = intensity;
        l.range     = range;
        l.shadows   = LightShadows.None;
        return go;
    }

    private static Material MakeMat(string matName, Color color, float smoothness,
                                     Color emissive = default)
    {
        string path = $"Assets/Materials/{matName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat  = new Material(shader) { name = matName };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic",   0f);

        if (emissive != default && emissive != Color.clear)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissive);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
