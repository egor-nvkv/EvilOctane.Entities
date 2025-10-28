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

        private ListView assetListView;

        [MenuItem("Window/Evil Octane/Asset Library")]
        public static void ShowMyEditor()
        {
            EditorWindow editorWindow = GetWindow<AssetLibraryEditorWindow>();
            editorWindow.titleContent = new GUIContent("Asset Library Editor");

            editorWindow.minSize = new Vector2(480, 180);
            editorWindow.maxSize = new Vector2(1920, 720);
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

        private static void RemoveNullAndSortAssets(List<UnityObject> assets, List<UnityObject> assetsToAdd = null)
        {
            SortedSet<UnityObject> assetSet = new(new AssetComparer());
            AddNonNull(assetSet, assets);

            if (assetsToAdd != null)
            {
                AddNonNull(assetSet, assetsToAdd);
            }

            assets.Clear();
            assets.AddRange(assetSet);
        }

        private static void AddNonNull(SortedSet<UnityObject> assetSet, List<UnityObject> assets)
        {
            foreach (UnityObject asset in assets)
            {
                if (asset is not null)
                {
                    if (asset is AssetLibrary)
                    {
                        Debug.LogError("AssetLibrary | Nesting asset libraries is not supported.");
                        continue;
                    }

                    _ = assetSet.Add(asset);
                }
            }
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(new Label()
            {
                text = "Assets",
                style =
                {
                    alignSelf = Align.Center,
                    fontSize = 18
                }
            });

            assetListView = new ListView
            {
                bindingSourceSelectionMode = BindingSourceSelectionMode.AutoAssign,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Multiple,
                allowAdd = false,
                allowRemove = true,
                showBoundCollectionSize = true,
                showAddRemoveFooter = true,
                showBorder = true,
                style = { flexGrow = 1 },
                makeItem = () =>
                {
                    ObjectField objectField = new();

                    _ = objectField.RegisterValueChangedCallback((ChangeEvent<UnityObject> changeEvent) =>
                    {
                        // Remove item set to null

                        if (changeEvent.newValue is null)
                        {
                            if (target && target.assets.Remove(changeEvent.previousValue))
                            {
                                EditorUtility.SetDirty(target);
                                assetListView.Rebuild();
                            }
                        }
                    });

                    return objectField;
                }
            };

            assetListView.bindItem = (VisualElement element, int index) =>
            {
                ((ObjectField)element).value = assetListView.itemsSource[index] as UnityObject;
            };

            assetListView.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            assetListView.RegisterCallback<DragPerformEvent>(OnDragPerform);

            rootVisualElement.Add(assetListView);

            Button clearButton = new()
            {
                text = "Clear",
                style =
                {
                    alignSelf = Align.Center,
                    fontSize = 18
                }
            };

            clearButton.clicked += () =>
            {
                if (!target)
                {
                    return;
                }

                // Remove all

                Undo.RecordObject(target, "Clear assets");

                target.assets.Clear();

                EditorUtility.SetDirty(target);
                assetListView.Rebuild();
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
            // Set target

            foreach (UnityObject obj in Selection.objects)
            {
                if (obj is AssetLibrary assetLibrary)
                {
                    // New target
                    target = assetLibrary;
                    OnTargetSet();
                    break;
                }
            }
        }

        private void OnTargetSet()
        {
            if (assetListView != null)
            {
                assetListView.itemsSource = target.assets;
            }
        }

        private void OnDragUpdate(DragUpdatedEvent @event)
        {
            if (!target)
            {
                return;
            }

            @event.StopPropagation();

            if (DragAndDrop.entityIds.Length != 0)
            {
                // Accept drag and drop
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private void OnDragPerform(DragPerformEvent @event)
        {
            if (!target)
            {
                return;
            }

            @event.StopPropagation();

            UnityObject[] draggedObjects = DragAndDrop.objectReferences;
            DragAndDrop.AcceptDrag();

            if (draggedObjects.Length == 0)
            {
                // Nothing selected
                return;
            }

            List<UnityObject> assetsToAdd = new(draggedObjects.Length);

            foreach (UnityObject draggedObject in draggedObjects)
            {
                if (draggedObject)
                {
                    if (IsFolder(draggedObject, out string path))
                    {
                        // Add all in folder

                        if (!path.EndsWith("/"))
                        {
                            path += "/";
                        }

                        string[] assetGUIDs = AssetDatabase.FindAssets("t:Object", new[] { path });
                        List<UnityObject> assetsInFolder = new(assetGUIDs.Length);

                        foreach (string assetGUID in assetGUIDs)
                        {
                            assetsInFolder.Add(AssetDatabase.LoadAssetByGUID(new GUID(assetGUID), typeof(UnityObject)));
                        }

                        assetsToAdd.AddRange(assetsInFolder);
                    }
                    else
                    {
                        // Single
                        assetsToAdd.Add(draggedObject);
                    }
                }
            }

            if (assetsToAdd.Count == 0)
            {
                return;
            }

            // Update asset library

            Undo.RecordObject(target, "Register assets");

            RemoveNullAndSortAssets(target.assets, assetsToAdd);

            EditorUtility.SetDirty(target);
            assetListView.Rebuild();
        }
    }
}
