using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace EvilOctane.Entities.Editor
{
    public class AssetLibraryEditorWindow : EditorWindow
    {
        [SerializeField]
        private AssetLibrary target;

        private SerializedObject serializedObject;
        private SerializedProperty assetsSerializedProperty;

        private Label titleLabel;
        private ListView assetListView;

        [MenuItem("Window/Evil Octane/Asset Library Editor")]
        public static void ShowEditorWindow()
        {
            AssetLibraryEditorWindow editorWindow = GetWindow<AssetLibraryEditorWindow>("Asset Library Editor");

            editorWindow.minSize = new Vector2(480, 180);
            editorWindow.maxSize = new Vector2(1920, 720);

            editorWindow.OnSelectionChange();
        }

        private static bool IsFolder(UnityObject asset, out string path)
        {
            path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                // Not a folder
                return false;
            }

            return true;
        }

        private static void AddAssetsRecursively(List<UnityObject> assets, UnityObject asset)
        {
            if (IsFolder(asset, out string path))
            {
                // Add all in folder

                if (!path.EndsWith("/"))
                {
                    path += "/";
                }

                string[] assetGUIDs = AssetDatabase.FindAssets("t:Object", new[] { path });

                foreach (string assetGUID in assetGUIDs)
                {
                    UnityObject assetInFolder = AssetDatabase.LoadAssetByGUID(new GUID(assetGUID), typeof(UnityObject));

                    // Append recursively
                    AddAssetsRecursively(assets, assetInFolder);
                }
            }
            else
            {
                // Single
                assets.Add(asset);
            }
        }

        private static void AddAndSortAssets(List<UnityObject> assets, List<UnityObject> assetsToAdd = null)
        {
            SortedSet<UnityObject> assetSet = new(new AssetComparer());
            AddAssets(assetSet, assets);

            if (assetsToAdd != null)
            {
                AddAssets(assetSet, assetsToAdd);
            }

            assets.Clear();
            assets.AddRange(assetSet);
        }

        private static void AddAssets(SortedSet<UnityObject> assetSet, List<UnityObject> assets)
        {
            foreach (UnityObject asset in assets)
            {
                if (asset is null)
                {
                    continue;
                }
                else if (asset is AssetLibrary)
                {
                    Debug.LogWarning("Nesting asset libraries is not supported.", asset);
                    continue;
                }

                _ = assetSet.Add(asset);
            }
        }

        private static VisualElement AssetListViewMakeItem()
        {
            ObjectField assetField = new();
            assetField.Q<VisualElement>(className: "unity-object-field__selector").RemoveFromHierarchy();

            assetField.RegisterCallback((DragEnterEvent @event) => @event.StopPropagation(), TrickleDown.TrickleDown);
            assetField.RegisterCallback((DragUpdatedEvent @event) => @event.StopPropagation(), TrickleDown.TrickleDown);
            assetField.RegisterCallback((DragPerformEvent @event) => @event.StopPropagation(), TrickleDown.TrickleDown);

            return assetField;
        }

        private static void StopDelete(KeyDownEvent @event)
        {
            if (@event.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                @event.StopImmediatePropagation();
            }
        }

        private void CreateGUI()
        {
            rootVisualElement.style.alignItems = Align.Stretch;

            rootVisualElement.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);

            titleLabel = new Label()
            {
                style =
                {
                    alignSelf = Align.Center,
                    fontSize = 18
                }
            };

            rootVisualElement.Add(titleLabel);

            assetListView = new ListView()
            {
                bindingSourceSelectionMode = BindingSourceSelectionMode.Manual,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,

                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                showBorder = true,

                allowAdd = false,
                allowRemove = false,
                showAddRemoveFooter = false,

                makeItem = AssetListViewMakeItem,
                bindItem = AssetListViewBindItem,

                style =
                {
                    flexGrow = 1
                }
            };

            assetListView.RegisterCallback<KeyDownEvent>(StopDelete, TrickleDown.TrickleDown);

            rootVisualElement.Add(assetListView);

            Button clearButton = new(OnClearAssets)
            {
                text = "Clear",
                style =
                {
                    fontSize = 18
                }
            };

            rootVisualElement.Add(clearButton);

            if (target)
            {
                // After reload
                OnTargetSet();
            }
        }

        private void OnSelectionChange()
        {
            UnityObject[] objects = Selection.objects;

            if (objects.Length == 0)
            {
                // Deselect
                ResetTarget();
                return;
            }
            else if (target && Array.IndexOf(objects, target) >= 0)
            {
                // Old target still selected
                return;
            }

            foreach (UnityObject obj in objects)
            {
                if (obj is AssetLibrary assetLibrary)
                {
                    // Set target
                    target = assetLibrary;
                    OnTargetSet();
                    break;
                }
            }
        }

        private void OnTargetSet()
        {
            serializedObject = new SerializedObject(target);
            assetsSerializedProperty = serializedObject.FindProperty(nameof(AssetLibrary.assets));

            titleLabel.BindProperty(serializedObject.FindProperty("m_Name"));
            assetListView.BindProperty(assetsSerializedProperty);
        }

        private void ResetTarget()
        {
            target = null;
            serializedObject = null;
            assetsSerializedProperty = null;

            titleLabel.Unbind();
            titleLabel.text = string.Empty;

            assetListView.Unbind();
        }

        private void OnDragUpdated(DragUpdatedEvent @event)
        {
            if (!target)
            {
                return;
            }

            if (DragAndDrop.entityIds.Length == 0)
            {
                // Nothing selected
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            @event.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent @event)
        {
            if (!target)
            {
                return;
            }

            UnityObject[] draggedObjects = DragAndDrop.objectReferences;

            if (draggedObjects.Length == 0)
            {
                // Nothing selected
                return;
            }

            DragAndDrop.AcceptDrag();
            @event.StopPropagation();

            List<UnityObject> assetsToAdd = new(draggedObjects.Length);

            foreach (UnityObject draggedObject in draggedObjects)
            {
                if (draggedObject)
                {
                    AddAssetsRecursively(assetsToAdd, draggedObject);
                }
            }

            // Add assets
            Undo.RecordObject(target, $"Add Assets to {target.name}");
            AddAndSortAssets(target.assets, assetsToAdd);
            EditorUtility.SetDirty(target);
        }

        private void OnClearAssets()
        {
            if (!target)
            {
                return;
            }

            // Clear assets
            Undo.RecordObject(target, $"Clear Assets in {target.name}");
            target.assets.Clear();
            EditorUtility.SetDirty(target);
        }

        private void AssetListViewBindItem(VisualElement visualElement, int index)
        {
            SerializedProperty item = assetListView.itemsSource[index] as SerializedProperty;
            ((ObjectField)visualElement).BindProperty(item);
        }
    }
}
