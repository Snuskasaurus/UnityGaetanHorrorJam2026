// Place this file in any folder named "Editor" (e.g. Assets/Editor/MeshColliderRemover.cs)
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MeshColliderRemover : EditorWindow
{
    private bool includeInactive = true;
    private Vector2 scroll;
    private List<MeshCollider> found = new List<MeshCollider>();

    [MenuItem("Tools/Mesh Collider Remover")]
    public static void Open()
    {
        var window = GetWindow<MeshColliderRemover>("Mesh Collider Remover");
        window.Refresh();
    }

    // Right-click in the Hierarchy -> "Remove Mesh Colliders In Children"
    [MenuItem("GameObject/Remove Mesh Colliders In Children", false, 0)]
    private static void RemoveFromSelection(MenuCommand command)
    {
        // This menu item runs once per selected object; only act on the first call
        if (command.context != null && command.context != Selection.objects.FirstOrDefault())
            return;

        RemoveColliders(CollectFromSelection(true));
    }

    [MenuItem("GameObject/Remove Mesh Colliders In Children", true)]
    private static bool ValidateRemoveFromSelection() => Selection.gameObjects.Length > 0;

    private void OnSelectionChange()
    {
        Refresh();
        Repaint();
    }

    private void OnFocus() => Refresh();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Remove all MeshColliders in the selected actors", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        includeInactive = EditorGUILayout.Toggle("Include Inactive Children", includeInactive);
        if (EditorGUI.EndChangeCheck())
            Refresh();

        int selectedCount = Selection.gameObjects.Length;
        if (selectedCount == 0)
        {
            EditorGUILayout.HelpBox("Select one or more actors in the Hierarchy.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Selected actors: {selectedCount}    Mesh Colliders found: {found.Count}");

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        foreach (var mc in found)
        {
            if (mc == null) continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(mc.gameObject, typeof(GameObject), true);
            if (GUILayout.Button("Ping", GUILayout.Width(45)))
                EditorGUIUtility.PingObject(mc.gameObject);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUI.enabled = found.Count > 0;
        if (GUILayout.Button($"Remove {found.Count} Mesh Collider(s)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Remove Mesh Colliders",
                    $"Remove {found.Count} MeshCollider(s) from {selectedCount} selected actor(s)?\n(You can undo with Ctrl+Z)",
                    "Remove", "Cancel"))
            {
                RemoveColliders(found);
                Refresh();
            }
        }
        GUI.enabled = true;
    }

    private void Refresh()
    {
        found = CollectFromSelection(includeInactive);
    }

    private static List<MeshCollider> CollectFromSelection(bool includeInactive)
    {
        return Selection.gameObjects
            .Where(go => !EditorUtility.IsPersistent(go)) // scene objects only, skip prefab assets in Project view
            .SelectMany(go => go.GetComponentsInChildren<MeshCollider>(includeInactive))
            .Distinct() // avoids duplicates when a parent and its child are both selected
            .ToList();
    }

    private static void RemoveColliders(List<MeshCollider> colliders)
    {
        if (colliders == null || colliders.Count == 0)
        {
            Debug.Log("[MeshColliderRemover] No MeshColliders found in the selection.");
            return;
        }

        Undo.SetCurrentGroupName("Remove Mesh Colliders");
        int group = Undo.GetCurrentGroup();
        int removed = 0;

        foreach (var mc in colliders)
        {
            if (mc == null) continue;
            var scene = mc.gameObject.scene;
            Undo.DestroyObjectImmediate(mc);
            removed++;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"[MeshColliderRemover] Removed {removed} MeshCollider(s).");
    }
}
