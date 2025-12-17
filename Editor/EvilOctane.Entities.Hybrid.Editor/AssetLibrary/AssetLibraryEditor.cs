using UnityEditor;
using UnityEngine.UIElements;

namespace EvilOctane.Entities.Editor
{
    [CustomEditor(typeof(AssetLibrary))]
    public class AssetLibraryEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new Button(AssetLibraryEditorWindow.ShowEditorWindow)
            {
                text = "Open Editor"
            };
        }
    }
}
