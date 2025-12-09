using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
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

        private Label titleLabel;
        private ListView assetListView;

        [MenuItem("Window/Evil Octane/Asset Library")]
        public static void ShowEditorWindow()
        {
            AssetLibraryEditorWindow editorWindow = GetWindow<AssetLibraryEditorWindow>();
            editorWindow.titleContent = new GUIContent("Asset Library Editor");

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
                if (asset)
                {
                    if (asset is AssetLibrary)
                    {
                        Debug.LogWarning("Nesting asset libraries is not supported.", asset);
                        continue;
                    }

                    _ = assetSet.Add(asset);
                }
            }
        }

        private void CreateGUI()
        {
            titleLabel = new Label()
            {
                style =
                {
                    alignSelf = Align.Center,
                    fontSize = 18
                }
            };

            rootVisualElement.Add(titleLabel);

            assetListView = new ListView
            {
                bindingSourceSelectionMode = BindingSourceSelectionMode.AutoAssign,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Multiple,
                allowAdd = false,
                //allowRemove = true,
                allowRemove = false,
                showBoundCollectionSize = true,
                //showAddRemoveFooter = true,
                showAddRemoveFooter = false,
                showBorder = true,
                style = { flexGrow = 1 },
                makeItem = () =>
                {
                    ObjectField objectField = new()
                    {
                        allowSceneObjects = false
                    };

                    _ = objectField.RegisterValueChangedCallback((ChangeEvent<UnityObject> changeEvent) =>
                    {
                        //if (!target)
                        //{
                        //    return;
                        //}
                        //
                        //if (changeEvent.newValue)
                        //{
                        //    // Replace
                        //    int index = target.assets.IndexOf(changeEvent.previousValue);

                        //    if (index >= 0)
                        //    {
                        //        target.assets[index] = changeEvent.newValue;
                        //    }
                        //}
                        //else
                        //{
                        //    // Remove
                        //    _ = target.assets.Remove(changeEvent.previousValue);
                        //}

                        //RemoveNullAndSortAssets(target.assets);
                        //EditorUtility.SetDirty(target);
                    });

                    return objectField;
                }
            };

            assetListView.bindItem = (VisualElement element, int index) =>
            {
                ((ObjectField)element).value = assetListView.itemsSource[index] as UnityObject;
            };

            //assetListView.onRemove = (BaseListView listView) =>
            //{
            //    if (!target)
            //    {
            //        return;
            //    }

            //    List<int> selectedIndices = new(listView.selectedIndices);

            //    if (selectedIndices.Count == 0)
            //    {
            //        // Nothing selected
            //        return;
            //    }

            //    int oldAssetCount = target.assets.Count;
            //    List<UnityObject> newAssets = new(oldAssetCount);

            //    for (int index = 0; index != oldAssetCount; ++index)
            //    {
            //        if (!selectedIndices.Contains(index))
            //        {
            //            // Keep
            //            newAssets.Add(target.assets[index]);
            //        }
            //    }

            //    // Remove
            //    Undo.RecordObject(target, "Remove assets");
            //    target.assets = newAssets;

            //    EditorUtility.SetDirty(target);
            //    listView.Rebuild();
            //};

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

                // Clear
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

            UnityObject[] objects = Selection.objects;

            if (objects.Length == 0)
            {
                // Deselect
                target = null;

                titleLabel.ClearBindings();
                titleLabel.text = string.Empty;

                assetListView.itemsSource = null;
                assetListView.Clear();

                return;
            }
            else if (target && objects.Contains(target))
            {
                // Old target still selected
                return;
            }

            foreach (UnityObject obj in objects)
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
            titleLabel.SetBinding("text", new DataBinding
            {
                dataSource = new NameBinding() { Target = target },
                dataSourcePath = new PropertyPath(nameof(NameBinding.Name)),
                bindingMode = BindingMode.ToTarget,
                updateTrigger = BindingUpdateTrigger.OnSourceChanged
            });

            assetListView.itemsSource = target.assets;
        }

        private void OnDragUpdate(DragUpdatedEvent @event)
        {
            if (!target)
            {
                return;
            }

            if (DragAndDrop.entityIds.Length != 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                @event.StopPropagation();
            }
        }

        private void OnDragPerform(DragPerformEvent @event)
        {
            if (!target)
            {
                return;
            }

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
                    AddAssetsRecursively(assetsToAdd, draggedObject);
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

            @event.StopPropagation();
        }

        private sealed class NameBinding
        {
            public UnityObject Target;

            [CreateProperty(ReadOnly = true)]
            public string Name => Target ? Target.name : string.Empty;
        }
    }
}
