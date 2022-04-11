using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.EditorTool
{
    internal class ScriptableObjectCreateWindow : EditorWindow
    {
        [MenuItem("Tools/ScriptableObjectCreate")]
        private static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectCreateWindow>("ScriptableObjectCreate");
            window.Show();
        }

        private IEnumerable<Type> _types;
        private string[] _typeNames;
        private SearchField _searcherField;
        private int _index;

        private ScriptableObjectCreateTreeView _treeView;
        private TreeViewState _state;
        
        private void OnGUI()
        {
            _state ??= new TreeViewState();
            _treeView ??= new ScriptableObjectCreateTreeView(_state);
            
            if (_searcherField == null)
            {
                _searcherField = new SearchField();
                _searcherField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            }
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _treeView.searchString = _searcherField.OnToolbarGUI(_treeView.searchString);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Create", EditorStyles.toolbarButton))
                {
                    var selectType = _treeView.GetSelect();
                    if (selectType != null)
                    {
                        var path = EditorUtility.SaveFilePanelInProject("Select Asset", $"{selectType.Name}", "asset", "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var instance = ScriptableObject.CreateInstance(selectType);
                            AssetDatabase.CreateAsset(instance, path);
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView?.OnGUI(rect);
        }
    }

    internal class ScriptableObjectCreateTreeView : TreeView
    {
        public ScriptableObjectCreateTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            
            Reload();
        }

        internal Type GetSelect()
        {
            var selections = GetSelection();
            var findRows = FindRows(selections);
            if (findRows.Count > 0)
            {
                return (findRows[0] as ScriptableObjectCreateTreeViewItem).type;
            }

            return null;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1, "root");
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(ScriptableObject))
                            && !t.IsSubclassOf(typeof(EditorWindow))
                            && !t.IsSubclassOf(typeof(ScriptableSingleton<>))
                            && !t.IsSubclassOf(typeof(UnityEditor.EditorTools.EditorTool))
                            && !t.IsSubclassOf(typeof(Editor))
                            && t != typeof(EditorWindow)
                            && t != typeof(Editor)
                            && !(!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("UnityEditor") && !t.IsPublic)
                            && !t.IsAbstract
                            && !t.IsGenericType
                )
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                ;

            var id = 0;
            foreach (var type in types)
            {
                var child = new ScriptableObjectCreateTreeViewItem()
                {
                    id = id++,
                    depth = 0,
                    displayName = type.Name,
                    type = type,
                };
                
                root.AddChild(child);
            }
            
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }
        
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (!root.hasChildren)
                return new List<TreeViewItem>();

            return base.BuildRows(root);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            var item = args.item as ScriptableObjectCreateTreeViewItem; 
            var rect = args.rowRect;

            var size = EditorStyles.label.CalcSize(new GUIContent(item.NameSpace));
            rect.x = rect.x + rect.width - size.x - 6f;
            EditorGUI.LabelField(rect, item.NameSpace);
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;
    }

    internal class ScriptableObjectCreateTreeViewItem : TreeViewItem
    {
        internal Type type;
        internal string NameSpace => type.Namespace;
    }
}